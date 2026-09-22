using System.ComponentModel.DataAnnotations;

namespace Finite.Core;

/// <summary>
/// A place money lives: cash, bank, wallet, card, or investment.
/// Balance is always derived from InitialBalance + movements (never stored).
/// </summary>
public class Account
{
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    /// <summary>Wallet provider (Bkash, Nagad, Rocket, Upay, Other) for MobileWallet accounts.</summary>
    [MaxLength(50)]
    public string? WalletProvider { get; set; }

    /// <summary>Starting balance when the account was created (immutable after creation).</summary>
    public decimal InitialBalance { get; set; }

    /// <summary>Credit limit for CreditCard accounts.</summary>
    public decimal? CreditLimit { get; set; }

    [MaxLength(255)]
    public string? AccountNumber { get; set; }

    [MaxLength(255)]
    public string? BankName { get; set; }

    /// <summary>Hex color for UI, e.g. "#3B82F6".</summary>
    [MaxLength(7)]
    public string Color { get; set; } = "#3B82F6";

    [MaxLength(255)]
    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Transaction> Transactions { get; set; } = new();

    public bool IsCreditCard => Type == AccountType.CreditCard;

    public bool IsCard => Type is AccountType.DebitCard or AccountType.CreditCard;
}
