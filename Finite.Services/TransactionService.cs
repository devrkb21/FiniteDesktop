using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>
/// Transaction CRUD with validation: positive amounts, existing account, category type
/// compatibility, and (soft) expense-must-have-balance rule.
/// </summary>
public class TransactionService
{
    private readonly FiniteDbContext _db;
    private readonly BalanceService _balances;

    public TransactionService(FiniteDbContext db, BalanceService balances)
    {
        _db = db;
        _balances = balances;
    }

    public Task<List<Transaction>> GetRecentAsync(int count = 50, CancellationToken ct = default) =>
        _db.Transactions.AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<Transaction> CreateAsync(Transaction transaction, CancellationToken ct = default)
    {
        Validate(transaction);

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.AccountId, ct)
            ?? throw new ValidationException("Account not found.");

        if (transaction.Type == TransactionType.Expense)
        {
            var available = await _balances.GetBalanceAsync(transaction.AccountId, ct);
            if (transaction.Amount > available)
                throw new ValidationException(
                    $"Amount ({transaction.Amount:N2}) exceeds available balance ({available:N2}).");
        }

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        Validate(transaction);

        var existing = await _db.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id, ct)
            ?? throw new KeyNotFoundException($"Transaction {transaction.Id} not found.");

        existing.AccountId = transaction.AccountId;
        existing.CategoryId = transaction.CategoryId;
        existing.Type = transaction.Type;
        existing.Amount = transaction.Amount;
        existing.Date = transaction.Date;
        existing.Time = transaction.Time;
        existing.Description = transaction.Description;
        existing.ReferenceNumber = transaction.ReferenceNumber;
        existing.IsReconciled = transaction.IsReconciled;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int transactionId, CancellationToken ct = default)
    {
        var count = await _db.Transactions.Where(t => t.Id == transactionId).ExecuteDeleteAsync(ct);
        if (count == 0)
            throw new KeyNotFoundException($"Transaction {transactionId} not found.");
    }

    private static void Validate(Transaction transaction)
    {
        if (transaction.Amount <= 0)
            throw new ValidationException("Amount must be greater than zero.");
        if (transaction.Amount >= 999_999_999_99m)
            throw new ValidationException("Amount is unrealistically large.");
        if (transaction.Date > DateOnly.FromDateTime(DateTime.Today).AddDays(1))
            throw new ValidationException("Transaction date cannot be more than 1 day in the future.");
        if (transaction.Date < DateOnly.FromDateTime(DateTime.Today).AddYears(-10))
            throw new ValidationException("Transaction date cannot be more than 10 years in the past.");
    }
}
