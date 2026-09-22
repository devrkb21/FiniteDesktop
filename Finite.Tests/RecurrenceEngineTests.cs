using Finite.Core;
using Finite.Services;
using Xunit;

namespace Finite.Tests;

public class RecurrenceEngineTests : IDisposable
{
    private readonly Data.FiniteDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly RecurrenceEngine _engine;

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    public RecurrenceEngineTests()
    {
        (_db, _connection) = TestDb.Create();
        _engine = new RecurrenceEngine(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void NextOccurrence_Monthly_ClampsToShortMonth()
    {
        var recurring = new RecurringTransaction
        {
            Name = "Rent", AccountId = 1, Type = TransactionType.Expense, Amount = 100m,
            Frequency = Frequency.Monthly, DayOfMonth = 31,
        };

        var next = RecurrenceEngine.NextOccurrence(recurring, new DateOnly(2026, 1, 31));

        Assert.Equal(new DateOnly(2026, 2, 28), next);
    }

    [Fact]
    public void NextOccurrence_BiWeekly_Adds14Days()
    {
        var recurring = new RecurringTransaction
        {
            Name = "Savings", AccountId = 1, Type = TransactionType.Income, Amount = 100m,
            Frequency = Frequency.BiWeekly,
        };

        var next = RecurrenceEngine.NextOccurrence(recurring, new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2026, 1, 15), next);
    }

    [Fact]
    public async Task ProcessDue_CreatesAllMissedOccurrences_CatchUp()
    {
        var account = new Account { Name = "Bank", Type = AccountType.Bank };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        _db.RecurringTransactions.Add(new RecurringTransaction
        {
            Name = "Gym", AccountId = account.Id, Type = TransactionType.Expense, Amount = 50m,
            Frequency = Frequency.Daily, StartDate = Today.AddDays(-3), NextOccurrence = Today.AddDays(-3),
        });
        await _db.SaveChangesAsync();

        var created = await _engine.ProcessDueAsync();

        Assert.Equal(4, created.Count); // -3, -2, -1, 0
        Assert.All(created, t => Assert.Equal(50m, t.Amount));
        Assert.All(created, t => Assert.Equal(TransactionType.Expense, t.Type));
    }

    [Fact]
    public async Task ProcessDue_RespectsEndDate()
    {
        var account = new Account { Name = "Bank", Type = AccountType.Bank };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        _db.RecurringTransactions.Add(new RecurringTransaction
        {
            Name = "Old subscription", AccountId = account.Id, Type = TransactionType.Expense, Amount = 10m,
            Frequency = Frequency.Daily, StartDate = Today.AddDays(-5),
            NextOccurrence = Today.AddDays(-5), EndDate = Today.AddDays(-3),
        });
        await _db.SaveChangesAsync();

        var created = await _engine.ProcessDueAsync();

        Assert.Equal(3, created.Count); // -5, -4, -3 only
    }

    [Fact]
    public async Task ProcessDue_DoesNotRecreate_AlreadyProcessed()
    {
        var account = new Account { Name = "Bank", Type = AccountType.Bank };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        _db.RecurringTransactions.Add(new RecurringTransaction
        {
            Name = "Bill", AccountId = account.Id, Type = TransactionType.Expense, Amount = 10m,
            Frequency = Frequency.Monthly, StartDate = Today, NextOccurrence = Today,
        });
        await _db.SaveChangesAsync();

        await _engine.ProcessDueAsync();
        var second = await _engine.ProcessDueAsync();

        Assert.Empty(second);
    }
}
