# Finite.Core

Domain entities and enums. **No dependencies, no logic, no EF attributes** — safe to reference from anywhere.

## Rules for this folder

- Properties + XML docs only. No methods that touch a database, no services.
- Add new entities here, then register the `DbSet` + config in `Finite.Data/FiniteDbContext.cs`.
- Amounts are always `decimal`. Storage conversion (TEXT) happens in `Finite.Data`.
- Enums live in `Enums.cs` — one place, documented.

## Files

| File | Entity | Key fields | Notes |
|---|---|---|---|
| `Account.cs` | `Account` | Name, Type, InitialBalance, WalletProvider, BankName, AccountNumber, CreditLimit, Color, Icon, IsActive, DisplayOrder | **Do not add a `Balance` column.** Balance is derived by `BalanceService` — that's the core design decision that prevents the Laravel drift bug |
| `Category.cs` | `Category` | Name, Type (Income/Expense/Both), Color, Icon, ParentId, IsSystem, IsActive, DisplayOrder | `IsSystem` = seeded category → UI hides Delete |
| `Transaction.cs` | `Transaction` | AccountId, CategoryId?, Type (Income/Expense), Amount, Date, Time?, Description, ReferenceNumber?, ReceiptPath?, DebtId?, RecurringTransactionId?, IsReconciled | `Date` is `DateOnly`; stored as TEXT |
| `Transfer.cs` | `Transfer` | FromAccountId, ToAccountId, Amount, Fee, Date | From ≠ To (enforced in `TransferService`); fee is charged to the source account |
| `Budget.cs` | `Budget` | CategoryId, Amount, Period, AlertThreshold (%), StartDate, EndDate?, IsActive | Evaluation of "spent" happens per current period only — see `BudgetEvaluator` |
| `RecurringTransaction.cs` | `RecurringTransaction` | Name, Type, Amount, CategoryId?, AccountId, Frequency, Interval, DayOfMonth, StartDate, EndDate?, NextOccurrence, AutoCreate, IsActive | `NextOccurrence` is the engine's cursor — always kept in the future by `RecurrenceEngine` |
| `Debt.cs` | `Debt` | Type (Receivable=owed to me / Payable=I owe), PersonName, PersonContact?, Amount, AmountPaid, DueDate?, Status, Currency, Notes | Settlement must always go through `DebtService` (atomic payment + transaction) |
| `SavingsGoal.cs` | `SavingsGoal` | Name, TargetAmount, TargetDate?, LinkedAccountId?, IsCompleted, CompletedAt | Progress = derived balance of the linked account |
| `Enums.cs` | 7 enums | `AccountType` (Cash, Bank, DebitCard, MobileWallet, CreditCard, Investment), `TransactionType`, `CategoryType`, `Frequency` (Daily…Yearly), `BudgetPeriod`, `DebtStatus` (Pending, Partial, Settled, Overdue, Cancelled), `DebtType` | Mirrors the Laravel schema (+ DebitCard) |

## Adding a new entity — checklist

1. Create `Thing.cs` here with `decimal` for all money fields
2. `Finite.Data/FiniteDbContext.cs`: add `DbSet<Thing>`, configure table + money-as-TEXT conversion + indices
3. If it needs CRUD rules: add a `ThingService` in `Finite.Services` (never let the UI write via DbContext directly)
4. Unit tests in `Finite.Tests` for any money touching that entity
