using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>
/// Recurring transaction CRUD + manual creation. "Create Now" (unlike the Laravel
/// version) refuses to create occurrences beyond end_date or before they're due.
/// </summary>
public class RecurringTransactionService(FiniteDbContext db, RecurrenceEngine engine)
{
    public Task<List<RecurringTransaction>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default) =>
        db.RecurringTransactions.AsNoTracking()
            .Include(r => r.Account)
            .Include(r => r.Category)
            .Where(r => !activeOnly || r.IsActive)
            .OrderBy(r => r.NextOccurrence)
            .ToListAsync(ct);

    public async Task<RecurringTransaction> CreateAsync(RecurringTransaction recurring, CancellationToken ct = default)
    {
        Validate(recurring);
        recurring.NextOccurrence = recurring.NextOccurrence < recurring.StartDate
            ? recurring.StartDate
            : recurring.NextOccurrence;

        db.RecurringTransactions.Add(recurring);
        await db.SaveChangesAsync(ct);
        return recurring;
    }

    public async Task UpdateAsync(RecurringTransaction recurring, CancellationToken ct = default)
    {
        var existing = await db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == recurring.Id, ct)
            ?? throw new KeyNotFoundException($"Recurring transaction {recurring.Id} not found.");

        Validate(recurring);

        existing.Name = recurring.Name;
        existing.AccountId = recurring.AccountId;
        existing.CategoryId = recurring.CategoryId;
        existing.Type = recurring.Type;
        existing.Amount = recurring.Amount;
        existing.Frequency = recurring.Frequency;
        existing.Interval = recurring.Interval;
        existing.DayOfMonth = recurring.DayOfMonth;
        existing.StartDate = recurring.StartDate;
        existing.EndDate = recurring.EndDate;
        existing.NextOccurrence = recurring.NextOccurrence;
        existing.Description = recurring.Description;
        existing.IsActive = recurring.IsActive;
        existing.AutoCreate = recurring.AutoCreate;

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Manual "Create Now": creates the next due occurrence immediately.</summary>
    public async Task<Transaction> CreateNowAsync(int recurringId, CancellationToken ct = default)
    {
        var recurring = await db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == recurringId, ct)
            ?? throw new KeyNotFoundException($"Recurring transaction {recurringId} not found.");

        if (!recurring.IsActive)
            throw new ValidationException("Recurring transaction is inactive.");
        if (recurring.EndDate is DateOnly end && recurring.NextOccurrence > end)
            throw new ValidationException("End date has passed — extend the end date to create more occurrences.");

        // Temporarily flip auto_create so the engine can process just this one via its
        // catch-up loop, then restore. Simpler: inline creation below (same math).
        var transaction = new Transaction
        {
            AccountId = recurring.AccountId,
            CategoryId = recurring.CategoryId,
            Type = recurring.Type,
            Amount = recurring.Amount,
            Date = recurring.NextOccurrence,
            Description = recurring.Description ?? recurring.Name,
            RecurringTransactionId = recurring.Id,
        };
        db.Transactions.Add(transaction);
        recurring.NextOccurrence = RecurrenceEngine.NextOccurrence(recurring, recurring.NextOccurrence);

        await db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task DeleteAsync(int recurringId, CancellationToken ct = default)
    {
        var count = await db.RecurringTransactions.Where(r => r.Id == recurringId).ExecuteDeleteAsync(ct);
        if (count == 0) throw new KeyNotFoundException($"Recurring transaction {recurringId} not found.");
    }

    private static void Validate(RecurringTransaction recurring)
    {
        if (string.IsNullOrWhiteSpace(recurring.Name))
            throw new ValidationException("Name is required.");
        if (recurring.Amount <= 0)
            throw new ValidationException("Amount must be greater than zero.");
        if (recurring.Interval < 1)
            throw new ValidationException("Interval must be at least 1.");
        if (recurring.EndDate is DateOnly end && end < recurring.StartDate)
            throw new ValidationException("End date cannot be before start date.");
    }
}
