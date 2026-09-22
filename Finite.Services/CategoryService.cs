using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>Category CRUD — system categories are protected from edit/delete.</summary>
public class CategoryService(FiniteDbContext db)
{
    public Task<List<Category>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default) =>
        db.Categories.AsNoTracking()
            .Where(c => includeInactive || c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

    public Task<List<Category>> GetForTypeAsync(TransactionType type, CancellationToken ct = default) =>
        db.Categories.AsNoTracking()
            .Where(c => c.IsActive && (c.Type == CategoryType.Both
                || (type == TransactionType.Income ? c.Type == CategoryType.Income : c.Type == CategoryType.Expense)))
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<Category> CreateAsync(Category category, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
            throw new ValidationException("Category name is required.");

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        var existing = await db.Categories.FirstOrDefaultAsync(c => c.Id == category.Id, ct)
            ?? throw new KeyNotFoundException($"Category {category.Id} not found.");
        if (existing.IsSystem)
            throw new ValidationException("System categories cannot be edited.");

        existing.Name = category.Name;
        existing.Type = category.Type;
        existing.ParentId = category.ParentId;
        existing.Color = category.Color;
        existing.Icon = category.Icon;
        existing.IsActive = category.IsActive;
        existing.DisplayOrder = category.DisplayOrder;
        existing.Description = category.Description;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int categoryId, CancellationToken ct = default)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct)
            ?? throw new KeyNotFoundException($"Category {categoryId} not found.");
        if (category.IsSystem)
            throw new ValidationException("System categories cannot be deleted.");

        var inUse = await db.Transactions.AnyAsync(t => t.CategoryId == categoryId, ct)
            || await db.Budgets.AnyAsync(b => b.CategoryId == categoryId);
        if (inUse)
            throw new ValidationException("Category is in use — deactivate it instead of deleting.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
    }
}
