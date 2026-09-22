using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>
/// Creates transactions for due recurring items. Unlike the Laravel version this runs a
/// catch-up loop: every missed occurrence is created (cron down for 7 days → 7 transactions),
/// respecting end_date, with day-of-month clamping so dates never drift.
/// </summary>
public class RecurrenceEngine
{
    private readonly FiniteDbContext _db;

    public RecurrenceEngine(FiniteDbContext db) => _db = db;

    /// <summary>Process all active, auto-create recurring items that are due. Returns created transactions.</summary>
    public async Task<List<Transaction>> ProcessDueAsync(DateOnly? asOf = null, CancellationToken ct = default)
    {
        var today = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var created = new List<Transaction>();

        var due = await _db.RecurringTransactions
            .Where(r => r.IsActive && r.AutoCreate && r.NextOccurrence <= today)
            .ToListAsync(ct);

        foreach (var recurring in due)
        {
            var guard = 0;
            while (recurring.NextOccurrence <= today
                   && (recurring.EndDate is null || recurring.NextOccurrence <= recurring.EndDate)
                   && guard++ < 3660) // safety: max ~10 years of catch-up per item
            {
                var transaction = new Transaction
                {
                    AccountId = recurring.AccountId,
                    CategoryId = recurring.CategoryId,
                    Type = recurring.Type,
                    Amount = recurring.Amount,
                    Date = recurring.NextOccurrence,
                    Description = recurring.Description ?? recurring.Name,
                    RecurringTransactionId = recurring.Id,
                    CreatedAt = DateTime.UtcNow,
                };

                _db.Transactions.Add(transaction);
                created.Add(transaction);

                recurring.NextOccurrence = NextOccurrence(recurring, recurring.NextOccurrence);
            }

            // End date passed: occurrences beyond it are skipped; next_occurrence moves past
            // today so the item goes dormant (user can deactivate or extend end date).
            if (recurring.EndDate is not null && recurring.NextOccurrence > recurring.EndDate)
            {
                while (recurring.NextOccurrence <= today)
                {
                    recurring.NextOccurrence = NextOccurrence(recurring, recurring.NextOccurrence);
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }

    /// <summary>Pure next-occurrence calculation (unit-testable).</summary>
    public static DateOnly NextOccurrence(RecurringTransaction recurring, DateOnly current)
    {
        var interval = Math.Max(1, recurring.Interval);

        DateOnly next = recurring.Frequency switch
        {
            Frequency.Daily => current.AddDays(interval),
            Frequency.Weekly => current.AddDays(7 * interval),
            Frequency.BiWeekly => current.AddDays(14 * interval),
            Frequency.Monthly => AddMonths(current, interval),
            Frequency.Quarterly => AddMonths(current, 3 * interval),
            Frequency.Yearly => AddMonths(current, 12 * interval),
            _ => AddMonths(current, interval),
        };

        // Pin monthly-style recurrences to the configured day, clamped to month length,
        // so Jan-31 → Feb-28 → Mar-31 (no drift).
        if (recurring.DayOfMonth is int day && day >= 1 && day <= 31
            && recurring.Frequency is Frequency.Monthly or Frequency.Quarterly or Frequency.Yearly)
        {
            next = new DateOnly(next.Year, next.Month, Math.Min(day, DateTime.DaysInMonth(next.Year, next.Month)));
        }

        return next;
    }

    private static DateOnly AddMonths(DateOnly date, int months) => date.AddMonths(months);
}
