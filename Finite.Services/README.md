# Finite.Services

All business logic. UI-free (no WPF references), fully unit-testable over real SQLite. The UI talks **only** to these services — never to the DbContext directly.

## The three money rules

1. **Balances are derived, never stored.** Any code that adds a stored balance column or
   increments/decrements on write is wrong — recompute instead. This is the fix for the
   Laravel observer-drift bug and is not negotiable.
2. **Multi-write operations are atomic.** If a use case writes 2+ rows (debt payment +
   its transaction), wrap them in `db.Database.BeginTransactionAsync`.
3. **decimal only.** Services accept and return `decimal`. No float, no double, ever.

## Services

| Service | Key API | What it guarantees |
|---|---|---|
| `BalanceService` | `GetBalanceAsync(accountId)`, `GetBalancesAsync()`, `GetNetWorthAsync()` | Balance = `initial + Σincome − Σexpense − ΣtransferOut − Σfees + ΣtransferIn`, one grouped query. Net worth: assets = positive-balance accounts; liabilities = credit-card negatives + payables; receivables tracked separately |
| `AccountService` | `GetAllAsync(includeInactive)`, `CreateAsync`, `UpdateAsync`, `DeleteAsync(id)` | Blocks deleting an account that has transactions (soft-delete instead) |
| `TransactionService` | `CreateAsync(tx)`, `UpdateAsync`, `DeleteAsync`, `GetRecentAsync(n, accountId?)` | Validates: amount > 0, account exists, category type compatible → throws `ValidationException` |
| `TransferService` | `CreateAsync(transfer)`, `DeleteAsync`, `GetRecentAsync` | From ≠ To; fee ≥ 0; fee hits the source account |
| `DebtService` | `CreateAsync` (auto-creates opening transaction), `RecordPaymentAsync(debtId, amount, accountId)`, `SettleAsync(debtId, accountId)`, `GetAllAsync` | **Atomic**: payment/settlement writes the debt update + matching income/expense in ONE db transaction — settling can never desync balances (the Laravel "Mark Settled" bug) |
| `RecurrenceEngine` | `ProcessDueAsync(today)` | **Catch-up**: creates every missed occurrence (7 days downtime → 7 transactions), advances `NextOccurrence`, clamps day-of-month (Jan 31 → Feb 28), stops at `EndDate`. Runs on dashboard load |
| `RecurringTransactionService` | CRUD + `CreateNowAsync` (single occurrence, respects end date) | Day-of-month clamping shared with the engine |
| `BudgetService` | CRUD | Owns Budget rows |
| `BudgetEvaluator` | `EvaluateActiveAsync()` → `List<BudgetStatus>` | Spent amounts for the **current period only** (a monthly budget never counts last month), grouped query per category set — no N+1, no raw SQL |
| `CategoryService` | `GetAllAsync`, `GetForTypeAsync`, `CreateAsync`, `DeleteAsync` | System categories cannot be deleted; in-use categories rejected |
| `SavingsGoalService` | `GetViewsAsync()` → `SavingsGoalView`, `CreateAsync`, `ContributeAsync`, `DeleteAsync` | Progress = linked account's derived balance; auto-completes at ≥ target |
| `ReportService` | `GetCurrentMonthFlowAsync`, `GetMonthlyFlowAsync(months)`, `GetBalanceTrendAsync(days)`, `GetCategoryBreakdownAsync(start, topN)`, `GetTopMerchantsAsync`, `GetDailyExpensesAsync` | Analytics aggregates, all grouped queries; result records `MonthFlow`, `DailyPoint`, `CategoryTotal` |

Result records live next to their services: `NetWorthSummary`, `BudgetStatus`, `CategoryTotal`, `MonthFlow`, `DailyPoint`, `SavingsGoalView`.

## Validation pattern

```csharp
public class ValidationException(string message) : Exception(message);
// Services validate first, throw ValidationException; VMs catch it and show StatusText.
```

## Adding a service — checklist

1. Class in this folder, constructor takes `FiniteDbContext` (+ any other service)
2. Rules go here; queries use EF LINQ with grouping — no per-row queries in loops
3. Money logic? → unit test in `Finite.Tests` (see `BalanceServiceTests` for the pattern)
4. Register in `App.xaml.cs`: `services.AddScoped<ThingService>();`
5. Consume from a ViewModel via constructor injection
