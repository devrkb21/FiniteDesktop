namespace Finite.Core;

/// <summary>A single money movement in or out of an account.</summary>
public class Transaction
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    public TransactionType Type { get; set; }

    /// <summary>Always positive; direction comes from Type. Stored decimal(15,2).</summary>
    public decimal Amount { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public TimeOnly? Time { get; set; }

    public string? Description { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(255)]
    public string? ReferenceNumber { get; set; }

    /// <summary>Path to a receipt file (image/pdf) inside the app data folder.</summary>
    public string? ReceiptPath { get; set; }

    public bool IsReconciled { get; set; }

    /// <summary>Set when the transaction was created by the recurrence engine.</summary>
    public int? RecurringTransactionId { get; set; }

    public RecurringTransaction? RecurringTransaction { get; set; }

    /// <summary>Set when the transaction belongs to a debt lifecycle (loan given/taken/payment).</summary>
    public int? DebtId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
