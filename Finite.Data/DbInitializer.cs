using Microsoft.EntityFrameworkCore;

namespace Finite.Data;

/// <summary>
/// Ensures the database exists and is up to date, seeding system categories once.
/// Uses EnsureCreated for the simple local-first story; migrations can replace it later.
/// </summary>
public static class DbInitializer
{
    public static void Initialize(FiniteDbContext context)
    {
        context.Database.EnsureCreated();

        if (!context.Categories.Any(c => c.IsSystem))
        {
            context.Categories.AddRange(SystemCategorySeeder.Build());
            context.SaveChanges();
        }
    }
}
