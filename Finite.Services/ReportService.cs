using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>Dashboard/report aggregates — grouped queries, no per-row hits.</summary>
public class ReportService
{
    private readonly FiniteDbContext _db;
    private readonly BalanceService _balances;

    public ReportService(FiniteDbContext db, BalanceService balances)
    {
        _db = db;
        _balances = balances;
    }

    /// <summary>Income and expense totals for the current calendar month.</summary>
    public async Task<(decimal Income, decimal Expense)> GetCurrentMonthFlowAsync(CancellationToken ct = default)
    {
        var start = MonthStart();

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= start)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        var income = rows.FirstOrDefault(r => r.Type == TransactionType.Income)?.Total ?? 0;
        var expense = rows.FirstOrDefault(r => r.Type == TransactionType.Expense)?.Total ?? 0;
        return (income, expense);
    }

    /// <summary>Income/expense per month for the last N months (for bar charts).</summary>
    public async Task<List<MonthFlow>> GetMonthlyFlowAsync(int months = 6, CancellationToken ct = default)
    {
        var start = MonthStart().AddMonths(-(months - 1));

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= start)
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Type })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Type, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        var result = new List<MonthFlow>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        for (var i = months - 1; i >= 0; i--)
        {
            var month = today.AddMonths(-i);
            var income = rows.FirstOrDefault(r => r.Year == month.Year && r.Month == month.Month && r.Type == TransactionType.Income)?.Total ?? 0;
            var expense = rows.FirstOrDefault(r => r.Year == month.Year && r.Month == month.Month && r.Type == TransactionType.Expense)?.Total ?? 0;
            result.Add(new MonthFlow($"{month:MMM yy}", income, expense));
        }
        return result;
    }

    /// <summary>Top N expense categories for the current month.</summary>
    public async Task<List<CategoryTotal>> GetCategoryBreakdownAsync(int top = 8, CancellationToken ct = default)
    {
        var start = MonthStart();

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.Type == TransactionType.Expense && t.Date >= start && t.CategoryId != null)
            .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category!.Color })
            .Select(g => new { g.Key.CategoryId, g.Key.Name, g.Key.Color, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Total)
            .Take(top)
            .ToListAsync(ct);

        return rows.Select(r => new CategoryTotal(r.Name, r.Color, r.Total)).ToList();
    }

    /// <summary>Daily expense totals for the last N days (for trend charts).</summary>
    public async Task<Dictionary<DateOnly, decimal>> GetDailyExpensesAsync(int days = 30, CancellationToken ct = default)
    {
        var start = DateOnly.FromDateTime(DateTime.Today).AddDays(-(days - 1));

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.Type == TransactionType.Expense && t.Date >= start)
            .GroupBy(t => t.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.Date, r => r.Total);
    }

    /// <summary>Daily expense totals AND day-by-day available balance for the last N days.</summary>
    public async Task<List<DailyPoint>> GetBalanceTrendAsync(int days = 30, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = today.AddDays(-(days - 1));

        var dailyExpenses = await GetDailyExpensesAsync(days, ct);
        var netWorthToday = await _balances.GetNetWorthAsync(ct);
        var currentTotal = netWorthToday.Assets - netWorthToday.Liabilities;

        var expensesByDate = await _db.Transactions.AsNoTracking()
            .Where(t => t.Type == TransactionType.Expense && t.Date >= start)
            .GroupBy(t => t.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(r => r.Date, r => r.Total, ct);
        var incomeByDate = await _db.Transactions.AsNoTracking()
            .Where(t => t.Type == TransactionType.Income && t.Date >= start)
            .GroupBy(t => t.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(r => r.Date, r => r.Total, ct);
        var feesByDate = await _db.Transfers.AsNoTracking()
            .Where(t => t.Date >= start && t.Fee > 0)
            .GroupBy(t => t.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(t => t.Fee) })
            .ToDictionaryAsync(r => r.Date, r => r.Total, ct);

        // Walk backwards from today: balance(day) = today − everything after that day.
        var points = new List<DailyPoint>(days);
        decimal cumIncome = 0, cumExpense = 0, cumFees = 0;
        for (var i = 0; i < days; i++)
        {
            var date = today.AddDays(-i);
            if (i > 0)
            {
                cumIncome += incomeByDate.GetValueOrDefault(date);
                cumExpense += expensesByDate.GetValueOrDefault(date);
                cumFees += feesByDate.GetValueOrDefault(date);
            }
            var balance = currentTotal - cumIncome + cumExpense - cumFees;
            points.Insert(0, new DailyPoint(date, balance, dailyExpenses.GetValueOrDefault(date)));
        }
        return points;
    }

    /// <summary>Top transaction descriptions ("merchants") by spend this month.</summary>
    public async Task<List<CategoryTotal>> GetTopMerchantsAsync(int top = 8, CancellationToken ct = default)
    {
        var start = MonthStart();

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.Type == TransactionType.Expense && t.Date >= start && t.Description != null)
            .GroupBy(t => t.Description!)
            .Select(g => new { Name = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Total)
            .Take(top)
            .ToListAsync(ct);

        return rows.Select(r => new CategoryTotal(r.Name, "#6B7280", r.Total)).ToList();
    }

    private static DateOnly MonthStart()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new DateOnly(today.Year, today.Month, 1);
    }
}

public record MonthFlow(string Label, decimal Income, decimal Expense);
public record CategoryTotal(string Name, string Color, decimal Total);
public record DailyPoint(DateOnly Date, decimal Balance, decimal Expense);
