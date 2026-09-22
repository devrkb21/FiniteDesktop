namespace Finite.Core;

/// <summary>Types of accounts supported. Mirrors the Laravel schema (plus debit_card).</summary>
public enum AccountType
{
    Cash,
    Bank,
    DebitCard,
    MobileWallet,
    CreditCard,
    Investment,
}

/// <summary>Direction of a transaction.</summary>
public enum TransactionType
{
    Income,
    Expense,
}

/// <summary>Whether a category applies to income, expense, or both.</summary>
public enum CategoryType
{
    Income,
    Expense,
    Both,
}

/// <summary>Recurrence frequency for automated transactions.</summary>
public enum Frequency
{
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Quarterly,
    Yearly,
}

/// <summary>Budget tracking period.</summary>
public enum BudgetPeriod
{
    Daily,
    Weekly,
    Monthly,
    Yearly,
}

/// <summary>Lifecycle status of a debt.</summary>
public enum DebtStatus
{
    Pending,
    Partial,
    Settled,
    Overdue,
    Cancelled,
}

/// <summary>Whether the debt is money owed to the user or owed by the user.</summary>
public enum DebtType
{
    Receivable, // owed to me
    Payable,    // I owe
}
