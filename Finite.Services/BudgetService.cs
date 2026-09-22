using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>Budget CRUD.</summary>
public class BudgetService(FiniteDbContext db)
{
    public Task<List<Budget>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default) =>
        db.Budgets.AsNoTracking()
            .Include(b => b.Category)
            .Where(b => !activeOnly || b.IsActive)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync(ct);

    public async Task<Budget> CreateAsync(Budget budget, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(budget.Name))
            throw new ValidationException("Budget name is required.");
        if (budget.Amount <= 0)
            throw new ValidationException("Budget amount must be greater than zero.");
        if (budget.AlertThreshold is < 1 or > 100)
            throw new ValidationException("Alert threshold must be between 1 and 100.");

        db.Budgets.Add(budget);
        await db.SaveChangesAsync(ct);
        return budget;
    }

    public async Task UpdateAsync(Budget budget, CancellationToken ct = default)
    {
        var existing = await db.Budgets.FirstOrDefaultAsync(b => b.Id == budget.Id, ct)
            ?? throw new KeyNotFoundException($"Budget {budget.Id} not found.");

        existing.Name = budget.Name;
        existing.CategoryId = budget.CategoryId;
        existing.Amount = budget.Amount;
        existing.Period = budget.Period;
        existing.StartDate = budget.StartDate;
        existing.EndDate = budget.EndDate;
        existing.AlertThreshold = budget.AlertThreshold;
        existing.IsActive = budget.IsActive;
        existing.Notes = budget.Notes;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int budgetId, CancellationToken ct = default)
    {
        var count = await db.Budgets.Where(b => b.Id == budgetId).ExecuteDeleteAsync(ct);
        if (count == 0) throw new KeyNotFoundException($"Budget {budgetId} not found.");
    }
}
