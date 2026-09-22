namespace Finite.Core;

/// <summary>
/// A savings target backed by a dedicated account. Progress = that account's derived balance.
/// </summary>
public class SavingsGoal
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>The savings account whose balance tracks this goal.</summary>
    public int AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public decimal TargetAmount { get; set; }

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly? TargetDate { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(7)]
    public string Color { get; set; } = "#10B981";

    public bool IsActive { get; set; } = true;

    public bool IsCompleted { get; set; }

    public DateOnly? CompletedAt { get; set; }

    public decimal CurrentAmount { get; set; }

    public decimal Remaining => Math.Max(0, TargetAmount - CurrentAmount);

    public double ProgressPercentage =>
        TargetAmount == 0 ? 0 : Math.Min((double)(CurrentAmount / TargetAmount) * 100, 100);

    public bool IsAchieved => CurrentAmount >= TargetAmount;
}
