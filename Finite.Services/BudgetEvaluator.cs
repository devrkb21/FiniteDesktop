using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>
/// Computes budget usage for the current period of each budget — one grouped query
/// for all budgets (fixes the Laravel N+1).
/// </summary>
public class BudgetEvaluator
{
    private readonly FiniteDbContext _db;

    public BudgetEvaluator(FiniteDbContext db) => _db = db;

    public async Task<List<BudgetStatus>> EvaluateActiveAsync(DateOnly? asOf = null, CancellationToken ct = default)
    {
        var today = asOf ?? DateOnly.FromDateTime(DateTime.Today);

        var budgets = await _db.Budgets.AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.IsActive
                        && b.StartDate <= today
                        && (b.EndDate == null || b.EndDate >= today))
            .ToListAsync(ct);

        if (budgets.Count == 0) return [];

        // One grouped query: total expense per category for ALL budgets' periods.
        var spends = new Dictionary<int, decimal>();
        foreach (var group in budgets.GroupBy(b => (b.CategoryId, b.Period)))
        {
            var periodStart = group.Min(b => PeriodStart(b.StartDate, b.Period, today));
            var periodEnd = group.Max(b => PeriodEnd(b, today));

            var sums = await _db.Transactions.AsNoTracking()
                .Where(t => t.Type == TransactionType.Expense
                            && t.CategoryId == group.Key.CategoryId
                            && t.Date >= periodStart
                            && t.Date <= periodEnd)
                .GroupBy(t => t.CategoryId)
                .Select(g => g.Sum(t => t.Amount))
                .FirstOrDefaultAsync(ct);

            if (sums > 0) spends[group.Key.CategoryId] = sums;
        }

        return budgets.Select(b =>
        {
            var spent = spends.GetValueOrDefault(b.CategoryId);
            return new BudgetStatus(
                Budget: b,
                Spent: spent,
                Remaining: Math.Max(0, b.Amount - spent),
                PercentUsed: b.Amount == 0 ? 0 : (double)(spent / b.Amount * 100));
        }).ToList();
    }

    public static DateOnly PeriodStart(DateOnly budgetStart, BudgetPeriod period, DateOnly today) => period switch
    {
        BudgetPeriod.Daily => today,
        BudgetPeriod.Weekly => StartOfWeek(today),
        BudgetPeriod.Monthly => new DateOnly(today.Year, today.Month, 1),
        BudgetPeriod.Yearly => new DateOnly(today.Year, 1, 1),
        _ => new DateOnly(today.Year, today.Month, 1),
    };

    public static DateOnly PeriodEnd(Budget b, DateOnly today)
    {
        var periodEnd = b.Period switch
        {
            BudgetPeriod.Daily => today,
            BudgetPeriod.Weekly => StartOfWeek(today).AddDays(6),
            BudgetPeriod.Monthly => new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)),
            BudgetPeriod.Yearly => new DateOnly(today.Year, 12, 31),
            _ => new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)),
        };
        return b.EndDate is DateOnly end && end < periodEnd ? end : periodEnd;
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        // Monday-start weeks (Bangladesh convention); adjust here if Sunday-start preferred.
        int diff = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-diff);
    }
}

public record BudgetStatus(Budget Budget, decimal Spent, decimal Remaining, double PercentUsed)
{
    public bool IsOver => PercentUsed >= 100;
    public bool ShouldAlert => PercentUsed >= Budget.AlertThreshold;
}
