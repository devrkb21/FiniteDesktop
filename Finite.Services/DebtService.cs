using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>
/// Debt lifecycle. Every payment writes the Debt update AND the Transaction in one DB
/// transaction — the "Mark Settled without balance sync" bug from the Laravel app can't happen.
/// </summary>
public class DebtService
{
    private readonly FiniteDbContext _db;

    public DebtService(FiniteDbContext db) => _db = db;

    public Task<List<Debt>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default) =>
        _db.Debts.AsNoTracking()
            .Include(d => d.Account)
            .Where(d => !activeOnly || (d.Status != DebtStatus.Settled && d.Status != DebtStatus.Cancelled))
            .OrderBy(d => d.DueDate ?? DateOnly.MaxValue)
            .ToListAsync(ct);

    public async Task<Debt> CreateAsync(Debt debt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(debt.PersonName))
            throw new ValidationException("Person/company name is required.");
        if (debt.Amount <= 0)
            throw new ValidationException("Amount must be greater than zero.");
        if (debt.AmountPaid < 0 || debt.AmountPaid > debt.Amount)
            throw new ValidationException("Amount paid must be between 0 and the total amount.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        debt.Status = debt.AmountPaid >= debt.Amount ? DebtStatus.Settled : DebtStatus.Pending;
        _db.Debts.Add(debt);
        await _db.SaveChangesAsync(ct);

        // Opening transaction: lending money = expense (money left my account);
        // borrowing money = income (money entered my account).
        if (debt.AccountId is int accountId)
        {
            _db.Transactions.Add(new Transaction
            {
                AccountId = accountId,
                Type = debt.Type == DebtType.Receivable ? TransactionType.Expense : TransactionType.Income,
                Amount = debt.Amount,
                Date = debt.CreatedDate,
                Description = debt.Type == DebtType.Receivable
                    ? $"Lent to {debt.PersonName}"
                    : $"Borrowed from {debt.PersonName}",
                DebtId = debt.Id,
            });
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return debt;
    }

    /// <summary>Records a payment: updates debt, creates the matching transaction — atomically.</summary>
    public async Task RecordPaymentAsync(int debtId, decimal amount, int? accountId = null,
        DateOnly? date = null, CancellationToken ct = default)
    {
        if (amount <= 0)
            throw new ValidationException("Payment must be greater than zero.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var debt = await _db.Debts.FirstOrDefaultAsync(d => d.Id == debtId, ct)
            ?? throw new KeyNotFoundException($"Debt {debtId} not found.");

        if (debt.IsSettledOrCancelled)
            throw new ValidationException("Debt is already settled or cancelled.");

        var pay = Math.Min(amount, debt.Remaining);

        // Find the payment account: explicit, or fall back to the debt's linked account.
        var payAccountId = accountId ?? debt.AccountId
            ?? throw new ValidationException("No payment account available for this debt.");

        // Receivable: they paid me → income. Payable: I paid them → expense.
        _db.Transactions.Add(new Transaction
        {
            AccountId = payAccountId,
            Type = debt.Type == DebtType.Receivable ? TransactionType.Income : TransactionType.Expense,
            Amount = pay,
            Date = date ?? DateOnly.FromDateTime(DateTime.Today),
            Description = debt.Type == DebtType.Receivable
                ? $"Payment received from {debt.PersonName}"
                : $"Payment to {debt.PersonName}",
            DebtId = debt.Id,
        });

        debt.AmountPaid += pay;
        debt.Status = debt.AmountPaid >= debt.Amount ? DebtStatus.Settled : DebtStatus.Partial;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>Settles the remaining balance in one step (proper equivalent of "Mark Settled").</summary>
    public Task SettleAsync(int debtId, int? accountId = null, CancellationToken ct = default) =>
        RecordPaymentAsync(debtId, decimal.MaxValue, accountId, ct: ct);

    public async Task DeleteAsync(int debtId, CancellationToken ct = default)
    {
        var count = await _db.Debts.Where(d => d.Id == debtId).ExecuteDeleteAsync(ct);
        if (count == 0)
            throw new KeyNotFoundException($"Debt {debtId} not found.");
    }
}
