namespace Finite.Core;

/// <summary>
/// Money moved between two accounts. Not income/expense — net worth unaffected (except fee).
/// </summary>
public class Transfer
{
    public int Id { get; set; }

    public int FromAccountId { get; set; }

    public Account FromAccount { get; set; } = null!;

    public int ToAccountId { get; set; }

    public Account ToAccount { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal Fee { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public TimeOnly? Time { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
