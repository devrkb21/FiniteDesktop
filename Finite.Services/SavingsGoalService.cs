using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.Services;

/// <summary>
/// Savings goals backed by a dedicated account. Deposits/withdrawals are real
/// transactions on the linked account, so the goal progress = account balance (derived).
/// </summary>
public class SavingsGoalService(FiniteDbContext db, BalanceService balances)
{
    public async Task<List<SavingsGoalView>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var goals = await db.SavingsGoals.AsNoTracking()
            .Include(g => g.Account)
            .Where(g => !activeOnly || g.IsActive)
            .OrderBy(g => g.TargetDate ?? DateOnly.MaxValue)
            .ToListAsync(ct);

        var map = await balances.GetBalancesAsync(ct);
        return goals.Select(g =>
        {
            var current = map.GetValueOrDefault(g.AccountId);
            var completed = current >= g.TargetAmount;
            var statusChanged = completed != g.IsCompleted;
            return new SavingsGoalView(g, current, completed, statusChanged);
        }).ToList();
    }

    public async Task<SavingsGoal> CreateAsync(SavingsGoal goal, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(goal.Name))
            throw new ValidationException("Goal name is required.");
        if (goal.TargetAmount <= 0)
            throw new ValidationException("Target amount must be greater than zero.");

        db.SavingsGoals.Add(goal);
        await db.SaveChangesAsync(ct);
        return goal;
    }

    public async Task UpdateAsync(SavingsGoal goal, CancellationToken ct = default)
    {
        var existing = await db.SavingsGoals.FirstOrDefaultAsync(g => g.Id == goal.Id, ct)
            ?? throw new KeyNotFoundException($"Goal {goal.Id} not found.");

        existing.Name = goal.Name;
        existing.Description = goal.Description;
        existing.AccountId = goal.AccountId;
        existing.TargetAmount = goal.TargetAmount;
        existing.StartDate = goal.StartDate;
        existing.TargetDate = goal.TargetDate;
        existing.Color = goal.Color;
        existing.IsActive = goal.IsActive;

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Deposit into the goal's account (a real income transaction).</summary>
    public async Task DepositAsync(int goalId, decimal amount, DateOnly? date = null, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ValidationException("Deposit must be greater than zero.");

        var goal = await db.SavingsGoals.FirstOrDefaultAsync(g => g.Id == goalId, ct)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        db.Transactions.Add(new Transaction
        {
            AccountId = goal.AccountId,
            Type = TransactionType.Income,
            Amount = amount,
            Date = date ?? DateOnly.FromDateTime(DateTime.Today),
            Description = $"Deposit to goal: {goal.Name}",
        });
        await db.SaveChangesAsync(ct);
        await RefreshCompletionAsync(goal, ct);
    }

    /// <summary>Withdraw from the goal's account (a real expense transaction).</summary>
    public async Task WithdrawAsync(int goalId, decimal amount, DateOnly? date = null, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ValidationException("Withdrawal must be greater than zero.");

        var goal = await db.SavingsGoals.FirstOrDefaultAsync(g => g.Id == goalId, ct)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        var available = await balances.GetBalanceAsync(goal.AccountId, ct);
        if (amount > available)
            throw new ValidationException($"Withdrawal exceeds account balance ({available:N2}).");

        db.Transactions.Add(new Transaction
        {
            AccountId = goal.AccountId,
            Type = TransactionType.Expense,
            Amount = amount,
            Date = date ?? DateOnly.FromDateTime(DateTime.Today),
            Description = $"Withdrawal from goal: {goal.Name}",
        });
        await db.SaveChangesAsync(ct);
        await RefreshCompletionAsync(goal, ct);
    }

    public async Task DeleteAsync(int goalId, CancellationToken ct = default)
    {
        var count = await db.SavingsGoals.Where(g => g.Id == goalId).ExecuteDeleteAsync(ct);
        if (count == 0) throw new KeyNotFoundException($"Goal {goalId} not found.");
    }

    private async Task RefreshCompletionAsync(SavingsGoal goal, CancellationToken ct)
    {
        var fresh = await db.SavingsGoals.FirstAsync(g => g.Id == goal.Id, ct);
        var balance = await balances.GetBalanceAsync(fresh.AccountId, ct);
        fresh.IsCompleted = balance >= fresh.TargetAmount;
        fresh.CompletedAt = fresh.IsCompleted ? DateOnly.FromDateTime(DateTime.Today) : null;
        await db.SaveChangesAsync(ct);
    }
}

public record SavingsGoalView(SavingsGoal Goal, decimal Current, bool Completed, bool StatusChanged)
{
    public double Progress => Goal.TargetAmount == 0 ? 0 : Math.Min((double)(Current / Goal.TargetAmount) * 100, 100);
    public decimal Remaining => Math.Max(0, Goal.TargetAmount - Current);
}
