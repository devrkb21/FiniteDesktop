namespace Finite.Core;

/// <summary>
/// An automated transaction on a schedule. The engine (RecurrenceEngine) creates every
/// missed occurrence up to today — catch-up, not just the latest.
/// </summary>
public class RecurringTransaction
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public int AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public Frequency Frequency { get; set; } = Frequency.Monthly;

    /// <summary>Repeat every N intervals (e.g. every 2 months).</summary>
    public int Interval { get; set; } = 1;

    /// <summary>For monthly/quarterly/yearly: pin to this day (clamped to month length).</summary>
    public int? DayOfMonth { get; set; }

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly? EndDate { get; set; }

    /// <summary>The next date an occurrence should be created for.</summary>
    public DateOnly NextOccurrence { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>When false the engine skips it (user creates occurrences manually).</summary>
    public bool AutoCreate { get; set; } = true;

    public List<Transaction> Transactions { get; set; } = new();
}
