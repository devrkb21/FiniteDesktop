namespace Finite.Data;

/// <summary>
/// The 15 system categories seeded on first run — same set as the Laravel CategorySeeder.
/// </summary>
public static class SystemCategorySeeder
{
    public static List<Core.Category> Build() =>
    [
        new() { Name = "Salary", Type = Core.CategoryType.Income, Color = "#10B981", Icon = "Wallet24", IsSystem = true, DisplayOrder = 1 },
        new() { Name = "Freelance", Type = Core.CategoryType.Income, Color = "#34D399", Icon = "Briefcase24", IsSystem = true, DisplayOrder = 2 },
        new() { Name = "Investment", Type = Core.CategoryType.Income, Color = "#6EE7B7", Icon = "ChartMultiple24", IsSystem = true, DisplayOrder = 3 },
        new() { Name = "Other Income", Type = Core.CategoryType.Income, Color = "#A7F3D0", Icon = "AddCircle24", IsSystem = true, DisplayOrder = 4 },

        new() { Name = "Food & Dining", Type = Core.CategoryType.Expense, Color = "#EF4444", Icon = "Food24", IsSystem = true, DisplayOrder = 10 },
        new() { Name = "Transportation", Type = Core.CategoryType.Expense, Color = "#F59E0B", Icon = "VehicleTruckProfile24", IsSystem = true, DisplayOrder = 11 },
        new() { Name = "Shopping", Type = Core.CategoryType.Expense, Color = "#8B5CF6", Icon = "ShoppingBag24", IsSystem = true, DisplayOrder = 12 },
        new() { Name = "Entertainment", Type = Core.CategoryType.Expense, Color = "#EC4899", Icon = "MoviesAndTv24", IsSystem = true, DisplayOrder = 13 },
        new() { Name = "Bills & Utilities", Type = Core.CategoryType.Expense, Color = "#3B82F6", Icon = "Flash24", IsSystem = true, DisplayOrder = 14 },
        new() { Name = "Healthcare", Type = Core.CategoryType.Expense, Color = "#06B6D4", Icon = "Heart24", IsSystem = true, DisplayOrder = 15 },
        new() { Name = "Education", Type = Core.CategoryType.Expense, Color = "#14B8A6", Icon = "HatGraduation24", IsSystem = true, DisplayOrder = 16 },
        new() { Name = "Rent", Type = Core.CategoryType.Expense, Color = "#F97316", Icon = "Home24", IsSystem = true, DisplayOrder = 17 },
        new() { Name = "Subscriptions", Type = Core.CategoryType.Expense, Color = "#A855F7", Icon = "ArrowRepeatAll24", IsSystem = true, DisplayOrder = 18 },
        new() { Name = "Personal Care", Type = Core.CategoryType.Expense, Color = "#F472B6", Icon = "Person24", IsSystem = true, DisplayOrder = 19 },
        new() { Name = "Other Expense", Type = Core.CategoryType.Expense, Color = "#6B7280", Icon = "MoreCircle24", IsSystem = true, DisplayOrder = 20 },
    ];
}
