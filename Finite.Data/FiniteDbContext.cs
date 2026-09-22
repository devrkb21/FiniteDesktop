using Finite.Core;
using Microsoft.EntityFrameworkCore;

namespace Finite.Data;

public class FiniteDbContext : DbContext
{
    public FiniteDbContext(DbContextOptions<FiniteDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite has no decimal column type — map all money to TEXT and convert via
        // decimal.ToString / decimal.Parse so no float rounding ever touches amounts.
        ConfigureDecimal(modelBuilder.Entity<Account>(), a => a.InitialBalance);
        ConfigureDecimal(modelBuilder.Entity<Account>(), a => a.CreditLimit);
        ConfigureDecimal(modelBuilder.Entity<Transaction>(), t => t.Amount);
        ConfigureDecimal(modelBuilder.Entity<Transfer>(), t => t.Amount);
        ConfigureDecimal(modelBuilder.Entity<Transfer>(), t => t.Fee);
        ConfigureDecimal(modelBuilder.Entity<Budget>(), b => b.Amount);
        ConfigureDecimal(modelBuilder.Entity<RecurringTransaction>(), r => r.Amount);
        ConfigureDecimal(modelBuilder.Entity<Debt>(), d => d.Amount);
        ConfigureDecimal(modelBuilder.Entity<Debt>(), d => d.AmountPaid);
        ConfigureDecimal(modelBuilder.Entity<SavingsGoal>(), g => g.TargetAmount);
        ConfigureDecimal(modelBuilder.Entity<SavingsGoal>(), g => g.CurrentAmount);

        modelBuilder.Entity<Account>(e =>
        {
            e.Property(a => a.Name).IsRequired();
            e.Property(a => a.WalletProvider).HasMaxLength(50);
            e.Property(a => a.Color).HasMaxLength(7).IsRequired();
            e.HasIndex(a => new { a.Type, a.IsActive });
            e.HasIndex(a => a.DisplayOrder);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasIndex(c => new { c.Type, c.IsActive });
            e.HasOne(c => c.Parent)
                .WithMany()
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasIndex(t => new { t.AccountId, t.Date });
            e.HasIndex(t => new { t.CategoryId, t.Type });
            e.HasIndex(t => t.Date);
            e.HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.RecurringTransaction)
                .WithMany(r => r.Transactions)
                .HasForeignKey(t => t.RecurringTransactionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Transfer>(e =>
        {
            e.HasIndex(t => new { t.FromAccountId, t.Date });
            e.HasIndex(t => new { t.ToAccountId, t.Date });
            e.HasOne(t => t.FromAccount)
                .WithMany()
                .HasForeignKey(t => t.FromAccountId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.ToAccount)
                .WithMany()
                .HasForeignKey(t => t.ToAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Budget>(e =>
        {
            e.HasIndex(b => new { b.CategoryId, b.IsActive });
            e.HasOne(b => b.Category)
                .WithMany()
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecurringTransaction>(e =>
        {
            e.HasIndex(r => new { r.IsActive, r.AutoCreate, r.NextOccurrence });
            e.HasOne(r => r.Account)
                .WithMany()
                .HasForeignKey(r => r.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Debt>(e =>
        {
            e.HasIndex(d => new { d.Type, d.Status });
            e.HasIndex(d => d.DueDate);
            e.HasMany(d => d.Transactions)
                .WithOne()
                .HasForeignKey(t => t.DebtId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SavingsGoal>(e =>
        {
            e.HasOne(g => g.Account)
                .WithMany()
                .HasForeignKey(g => g.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDecimal<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> entity, System.Linq.Expressions.Expression<System.Func<T, decimal?>> property)
        where T : class
    {
        entity.Property(property)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
                v => v != null ? decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture) : null)
            .HasColumnType("TEXT");
    }

    private static void ConfigureDecimal<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> entity, System.Linq.Expressions.Expression<System.Func<T, decimal>> property)
        where T : class
    {
        entity.Property(property)
            .HasConversion(
                v => v.ToString(System.Globalization.CultureInfo.InvariantCulture),
                v => decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture))
            .HasColumnType("TEXT");
    }
}
