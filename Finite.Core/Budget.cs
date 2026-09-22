namespace Finite.Core;

/// <summary>
/// A spending limit for one category. Spent amount is evaluated per current period
/// (daily/weekly/monthly/yearly) by BudgetEvaluator — never cached on the entity.
/// </summary>
public class Budget
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public decimal Amount { get; set; }

    public BudgetPeriod Period { get; set; } = BudgetPeriod.Monthly;

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Null = recurring forever.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Alert when percent used reaches this (1-100).</summary>
    public int AlertThreshold { get; set; } = 80;

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}
