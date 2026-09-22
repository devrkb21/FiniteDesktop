using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>
/// Single source of truth for balances. Balance is always recomputed from
/// InitialBalance + Σ(income) − Σ(expense) − Σ(transfer out + fees) + Σ(transfer in).
/// No stored balance that can drift.
/// </summary>
public class BalanceService
{
    private readonly FiniteDbContext _db;

    public BalanceService(FiniteDbContext db) => _db = db;

    /// <summary>Derived current balance for one account.</summary>
    public async Task<decimal> GetBalanceAsync(int accountId, CancellationToken ct = default)
    {
        var account = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new InvalidOperationException($"Account {accountId} not found.");

        var income = await _db.Transactions.AsNoTracking()
            .Where(t => t.AccountId == accountId && t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var expense = await _db.Transactions.AsNoTracking()
            .Where(t => t.AccountId == accountId && t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var transferOut = await _db.Transfers.AsNoTracking()
            .Where(t => t.FromAccountId == accountId)
            .SumAsync(t => (decimal?)(t.Amount + t.Fee), ct) ?? 0;

        var transferIn = await _db.Transfers.AsNoTracking()
            .Where(t => t.ToAccountId == accountId)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        return account.InitialBalance + income - expense - transferOut + transferIn;
    }

    /// <summary>Balances for all accounts in three grouped queries (no N+1).</summary>
    public async Task<Dictionary<int, decimal>> GetBalancesAsync(CancellationToken ct = default)
    {
        var initial = await _db.Accounts.AsNoTracking()
            .Select(a => new { a.Id, a.InitialBalance })
            .ToDictionaryAsync(x => x.Id, x => x.InitialBalance, ct);

        var income = await _db.Transactions.AsNoTracking()
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.AccountId)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Total, ct);

        var expense = await _db.Transactions.AsNoTracking()
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.AccountId)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Total, ct);

        var outflow = await _db.Transfers.AsNoTracking()
            .GroupBy(t => t.FromAccountId)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(t => t.Amount + t.Fee) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Total, ct);

        var inflow = await _db.Transfers.AsNoTracking()
            .GroupBy(t => t.ToAccountId)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Total, ct);

        var result = new Dictionary<int, decimal>(initial.Count);
        foreach (var (id, start) in initial)
        {
            result[id] = start
                + income.GetValueOrDefault(id)
                - expense.GetValueOrDefault(id)
                - outflow.GetValueOrDefault(id)
                + inflow.GetValueOrDefault(id);
        }
        return result;
    }

    /// <summary>
    /// Net worth: assets (cash/bank/wallet/debit/investment) − credit card debt
    /// + receivables − payables.
    /// </summary>
    public async Task<NetWorthSummary> GetNetWorthAsync(CancellationToken ct = default)
    {
        var balances = await GetBalancesAsync(ct);
        var accounts = await _db.Accounts.AsNoTracking()
            .Where(a => a.IsActive)
            .ToListAsync(ct);

        decimal assets = 0, liabilities = 0;
        foreach (var account in accounts)
        {
            var balance = balances.GetValueOrDefault(account.Id);
            if (account.IsCreditCard)
            {
                liabilities += Math.Abs(balance);
            }
            else
            {
                assets += balance;
            }
        }

        var activeDebts = _db.Debts.AsNoTracking()
            .Where(d => d.Status != DebtStatus.Settled && d.Status != DebtStatus.Cancelled);
        var receivables = await activeDebts.Where(d => d.Type == DebtType.Receivable)
            .SumAsync(d => (decimal?)(d.Amount - d.AmountPaid), ct) ?? 0;
        var payables = await activeDebts.Where(d => d.Type == DebtType.Payable)
            .SumAsync(d => (decimal?)(d.Amount - d.AmountPaid), ct) ?? 0;

        return new NetWorthSummary(
            Assets: assets + receivables,
            Liabilities: liabilities + payables,
            Receivables: receivables,
            Payables: payables);
    }
}

public record NetWorthSummary(decimal Assets, decimal Liabilities, decimal Receivables, decimal Payables)
{
    public decimal NetWorth => Assets - Liabilities;
}
