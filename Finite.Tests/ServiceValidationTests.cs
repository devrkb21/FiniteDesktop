using Finite.Core;
using Finite.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finite.Tests;

public class ServiceValidationTests : IDisposable
{
    private readonly Data.FiniteDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    public ServiceValidationTests()
    {
        (_db, _connection) = TestDb.Create();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Transfer_ToSameAccount_Throws()
    {
        var account = new Account { Name = "Cash", Type = AccountType.Cash, InitialBalance = 100m };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var service = new TransferService(_db, new BalanceService(_db));

        await Assert.ThrowsAsync<Services.ValidationException>(() =>
            service.CreateAsync(new Transfer { FromAccountId = account.Id, ToAccount = account, ToAccountId = account.Id, Amount = 10m }));
    }

    [Fact]
    public async Task Transfer_ExceedingBalance_Throws()
    {
        var from = new Account { Name = "Cash", Type = AccountType.Cash, InitialBalance = 100m };
        var to = new Account { Name = "Bank", Type = AccountType.Bank };
        _db.Accounts.AddRange(from, to);
        await _db.SaveChangesAsync();

        var service = new TransferService(_db, new BalanceService(_db));

        await Assert.ThrowsAsync<Services.ValidationException>(() =>
            service.CreateAsync(new Transfer { FromAccountId = from.Id, ToAccountId = to.Id, Amount = 150m }));
    }

    [Fact]
    public async Task Transfer_WithFee_MustCoverBoth()
    {
        var from = new Account { Name = "Cash", Type = AccountType.Cash, InitialBalance = 100m };
        var to = new Account { Name = "Bank", Type = AccountType.Bank };
        _db.Accounts.AddRange(from, to);
        await _db.SaveChangesAsync();

        var service = new TransferService(_db, new BalanceService(_db));

        // 90 + 20 fee > 100 balance → must throw
        await Assert.ThrowsAsync<Services.ValidationException>(() =>
            service.CreateAsync(new Transfer { FromAccountId = from.Id, ToAccountId = to.Id, Amount = 90m, Fee = 20m }));

        // 90 + 10 fee = 100 exactly → OK
        var transfer = await service.CreateAsync(new Transfer { FromAccountId = from.Id, ToAccountId = to.Id, Amount = 90m, Fee = 10m });
        Assert.Equal(10m, transfer.Fee);
    }

    [Fact]
    public async Task Transaction_ExpenseExceedingBalance_Throws()
    {
        var account = new Account { Name = "Cash", Type = AccountType.Cash, InitialBalance = 50m };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var service = new TransactionService(_db, new BalanceService(_db));

        await Assert.ThrowsAsync<Services.ValidationException>(() =>
            service.CreateAsync(new Transaction { AccountId = account.Id, Type = TransactionType.Expense, Amount = 60m, Date = Today }));
    }

    [Fact]
    public async Task Transaction_ZeroOrNegativeAmount_Throws()
    {
        var account = new Account { Name = "Cash", Type = AccountType.Cash, InitialBalance = 50m };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var service = new TransactionService(_db, new BalanceService(_db));

        await Assert.ThrowsAsync<Services.ValidationException>(() =>
            service.CreateAsync(new Transaction { AccountId = account.Id, Type = TransactionType.Expense, Amount = 0m, Date = Today }));
        await Assert.ThrowsAsync<Services.ValidationException>(() =>
            service.CreateAsync(new Transaction { AccountId = account.Id, Type = TransactionType.Income, Amount = -5m, Date = Today }));
    }

    [Fact]
    public async Task DebtService_RecordPayment_CreatesTransaction_AndSettles()
    {
        var account = new Account { Name = "Cash", Type = AccountType.Cash, InitialBalance = 0m };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var debts = new DebtService(_db);
        var debt = await debts.CreateAsync(new Debt
        {
            Type = DebtType.Payable, PersonName = "Karim", Amount = 300m,
            AccountId = account.Id, CreatedDate = Today,
        });

        // Opening: borrowing = income 300 → balance 300
        Assert.Equal(300m, await new BalanceService(_db).GetBalanceAsync(account.Id));

        await debts.RecordPaymentAsync(debt.Id, 100m);

        // Payment to him = expense 100 → balance 200; debt partial
        Assert.Equal(200m, await new BalanceService(_db).GetBalanceAsync(account.Id));
        var updated = await _db.Debts.FirstAsync(d => d.Id == debt.Id);
        Assert.Equal(DebtStatus.Partial, updated.Status);
        Assert.Equal(100m, updated.AmountPaid);

        // Settle the rest: creates the final 200 payment and marks settled
        await debts.SettleAsync(debt.Id);
        var settled = await _db.Debts.FirstAsync(d => d.Id == debt.Id);
        Assert.Equal(DebtStatus.Settled, settled.Status);
        Assert.Equal(300m, settled.AmountPaid);
        Assert.Equal(0m, await new BalanceService(_db).GetBalanceAsync(account.Id));
    }

    [Fact]
    public async Task BudgetEvaluator_UsesCurrentPeriodOnly()
    {
        var category = new Category { Name = "Food", Type = CategoryType.Expense };
        var account = new Account { Name = "Cash", Type = AccountType.Cash, InitialBalance = 10000m };
        _db.Categories.Add(category);
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        _db.Budgets.Add(new Budget
        {
            Name = "Food budget", CategoryId = category.Id, Amount = 500m,
            Period = BudgetPeriod.Monthly, StartDate = Today.AddMonths(-1),
        });

        // This month's spending
        _db.Transactions.Add(new Transaction { AccountId = account.Id, CategoryId = category.Id, Type = TransactionType.Expense, Amount = 300m, Date = Today });
        // Last month's spending (must NOT count)
        _db.Transactions.Add(new Transaction { AccountId = account.Id, CategoryId = category.Id, Type = TransactionType.Expense, Amount = 900m, Date = Today.AddMonths(-1).AddDays(-1) });
        await _db.SaveChangesAsync();

        var evaluator = new BudgetEvaluator(_db);
        var status = (await evaluator.EvaluateActiveAsync()).Single();

        Assert.Equal(300m, status.Spent);
        Assert.Equal(200m, status.Remaining);
        Assert.Equal(60.0, status.PercentUsed, precision: 1);
        Assert.False(status.IsOver);
    }
}
