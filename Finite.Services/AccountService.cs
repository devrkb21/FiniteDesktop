using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>Account CRUD. Initial balance is fixed after creation (matches the Laravel rule).</summary>
public class AccountService
{
    private readonly FiniteDbContext _db;

    public AccountService(FiniteDbContext db) => _db = db;

    public Task<List<Account>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default) =>
        _db.Accounts.AsNoTracking()
            .Where(a => includeInactive || a.IsActive)
            .OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<Account> CreateAsync(Account account, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(account.Name))
            throw new ValidationException("Account name is required.");
        if (account.InitialBalance < 0)
            throw new ValidationException("Initial balance cannot be negative.");

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(ct);
        return account;
    }

    public async Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        var existing = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id, ct)
            ?? throw new KeyNotFoundException($"Account {account.Id} not found.");

        existing.Name = account.Name;
        existing.Type = account.Type;
        existing.WalletProvider = account.WalletProvider;
        existing.CreditLimit = account.CreditLimit;
        existing.AccountNumber = account.AccountNumber;
        existing.BankName = account.BankName;
        existing.Color = account.Color;
        existing.Icon = account.Icon;
        existing.IsActive = account.IsActive;
        existing.DisplayOrder = account.DisplayOrder;
        existing.Notes = account.Notes;
        // InitialBalance intentionally NOT updated — immutable after creation.

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int accountId, CancellationToken ct = default)
    {
        var account = await _db.Accounts.Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        if (account.Transactions.Any())
            throw new ValidationException("Cannot delete an account that has transactions. Deactivate it instead.");

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>User-facing validation failure.</summary>
public class ValidationException(string message) : Exception(message);
