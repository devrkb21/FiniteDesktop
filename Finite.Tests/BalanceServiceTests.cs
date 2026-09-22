using Finite.Core;
using Finite.Services;
using Xunit;

namespace Finite.Tests;

public class BalanceServiceTests : IDisposable
{
    private readonly Data.FiniteDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly BalanceService _balances;

    public BalanceServiceTests()
    {
        (_db, _connection) = TestDb.Create();
        _balances = new BalanceService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Balance_WithNoMovements_EqualsInitialBalance()
    {
        var account = new Account { Name = "Cash", Type = AccountType.Cash, InitialBalance = 1000m };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var balance = await _balances.GetBalanceAsync(account.Id);

        Assert.Equal(1000m, balance);
    }

    [Fact]
    public async Task Balance_IncomeIncreases_ExpenseDecreases()
    {
        var account = new Account { Name = "Wallet", Type = AccountType.MobileWallet, InitialBalance = 500m };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        _db.Transactions.AddRange(
            new Transaction { AccountId = account.Id, Type = TransactionType.Income, Amount = 300m, Date = DateOnly.FromDateTime(DateTime.Today) },
            new Transaction { AccountId = account.Id, Type = TransactionType.Expense, Amount = 120.50m, Date = DateOnly.FromDateTime(DateTime.Today) });
        await _db.SaveChangesAsync();

        var balance = await _balances.GetBalanceAsync(account.Id);

        Assert.Equal(679.50m, balance);
    }

    [Fact]
    public async Task Balance_TransferMovesMoney_AndFeeIsChargedToSource()
    {
        var from = new Account { Name = "Bank", Type = AccountType.Bank, InitialBalance = 1000m };
        var to = new Account { Name = "Bkash", Type = AccountType.MobileWallet, InitialBalance = 0m };
        _db.Accounts.AddRange(from, to);
        await _db.SaveChangesAsync();

        _db.Transfers.Add(new Transfer
        {
            FromAccountId = from.Id, ToAccountId = to.Id,
            Amount = 400m, Fee = 10m, Date = DateOnly.FromDateTime(DateTime.Today),
        });
        await _db.SaveChangesAsync();

        Assert.Equal(590m, await _balances.GetBalanceAsync(from.Id));  // 1000 - 400 - 10
        Assert.Equal(400m, await _balances.GetBalanceAsync(to.Id));
    }

    [Fact]
    public async Task NetWorth_CreditCardBalanceCountsAsLiability()
    {
        var card = new Account { Name = "Visa", Type = AccountType.CreditCard, InitialBalance = 0m };
        _db.Accounts.Add(card);
        await _db.SaveChangesAsync();

        // Spending on the credit card makes its derived balance negative (debt).
        _db.Transactions.Add(new Transaction
        {
            AccountId = card.Id, Type = TransactionType.Expense, Amount = 250m,
            Date = DateOnly.FromDateTime(DateTime.Today),
        });
        await _db.SaveChangesAsync();

        var worth = await _balances.GetNetWorthAsync();

        Assert.Equal(0m, worth.Assets);
        Assert.Equal(250m, worth.Liabilities);
        Assert.Equal(-250m, worth.NetWorth);
    }

    [Fact]
    public async Task NetWorth_IncludesDebts()
    {
        var worth0 = await _balances.GetNetWorthAsync();

        _db.Debts.Add(new Debt
        {
            Type = DebtType.Receivable, PersonName = "Rahim",
            Amount = 500m, AmountPaid = 200m, Status = DebtStatus.Partial,
        });
        _db.Debts.Add(new Debt
        {
            Type = DebtType.Payable, PersonName = "Karim",
            Amount = 100m, AmountPaid = 0m, Status = DebtStatus.Pending,
        });
        await _db.SaveChangesAsync();

        var worth = await _balances.GetNetWorthAsync();

        Assert.Equal(300m, worth.Receivables);
        Assert.Equal(100m, worth.Payables);
        Assert.Equal(worth0.NetWorth + 200m, worth.NetWorth);
    }
}
