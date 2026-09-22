using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>
/// Inter-account transfers. Validates different accounts and sufficient funds
/// (amount + fee), and saves atomically.
/// </summary>
public class TransferService
{
    private readonly FiniteDbContext _db;
    private readonly BalanceService _balances;

    public TransferService(FiniteDbContext db, BalanceService balances)
    {
        _db = db;
        _balances = balances;
    }

    public Task<List<Transfer>> GetRecentAsync(int count = 50, CancellationToken ct = default) =>
        _db.Transfers.AsNoTracking()
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<Transfer> CreateAsync(Transfer transfer, CancellationToken ct = default)
    {
        if (transfer.Amount <= 0)
            throw new ValidationException("Transfer amount must be greater than zero.");
        if (transfer.Fee < 0)
            throw new ValidationException("Fee cannot be negative.");
        if (transfer.FromAccountId == transfer.ToAccountId)
            throw new ValidationException("Cannot transfer to the same account.");

        var available = await _balances.GetBalanceAsync(transfer.FromAccountId, ct);
        if (transfer.Amount + transfer.Fee > available)
            throw new ValidationException(
                $"Amount + fee ({transfer.Amount + transfer.Fee:N2}) exceeds available balance ({available:N2}).");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Transfers.Add(transfer);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return transfer;
    }

    public async Task DeleteAsync(int transferId, CancellationToken ct = default)
    {
        var count = await _db.Transfers.Where(t => t.Id == transferId).ExecuteDeleteAsync(ct);
        if (count == 0)
            throw new KeyNotFoundException($"Transfer {transferId} not found.");
    }
}
