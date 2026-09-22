namespace Finite.Core;

/// <summary>
/// A loan between the user and a person/company. Receivable = owed to me, Payable = I owe.
/// Payments always create Transactions (via DebtService) in one DB transaction.
/// </summary>
public class Debt
{
    public int Id { get; set; }

    public DebtType Type { get; set; }

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(255)]
    public string PersonName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(255)]
    public string? PersonContact { get; set; }

    public decimal Amount { get; set; }

    public decimal AmountPaid { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(3)]
    public string Currency { get; set; } = "BDT";

    public string? Description { get; set; }

    public string? Notes { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? ReminderDate { get; set; }

    public DebtStatus Status { get; set; } = DebtStatus.Pending;

    /// <summary>Account the loan money moved through (for the opening transaction).</summary>
    public int? AccountId { get; set; }

    public Account? Account { get; set; }

    public DateOnly CreatedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public List<Transaction> Transactions { get; set; } = new();

    public decimal Remaining => Math.Max(0, Amount - AmountPaid);

    public bool IsSettledOrCancelled => Status is DebtStatus.Settled or DebtStatus.Cancelled;

    public bool IsOverdue =>
        DueDate is not null
        && DueDate.Value < DateOnly.FromDateTime(DateTime.Today)
        && !IsSettledOrCancelled;
}
