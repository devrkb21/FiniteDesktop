# Finite Desktop

A local-first **personal finance tracker for Windows** — WPF (.NET 8), offline SQLite database, no accounts, no cloud, no telemetry.

Desktop sibling of the Laravel "Finite" app (`../../laravel`), redesigned so every bug found in that audit **cannot happen by construction**.

---

## ✨ Features at a glance

- **12 pages**: Dashboard, Transactions, Accounts, Transfers, Budgets, Debts & Loans, Savings Goals, Recurring, Categories, Analytics, Bill Calendar, Settings
- Derived balances (never stored), atomic debt payments, recurring catch-up engine, current-period budgets
- Dark / Light / Match-Windows themes, custom color picker, hand-rolled dependency-free charts
- Self-contained single-file exe installer — runs on any Windows 10/11 x64 with nothing installed

---

## 🧰 Tech stack

| Layer | Technology |
|---|---|
| Runtime | **.NET 8** (LTS), C# 12 |
| UI | **WPF** + **WPF-UI 3.0.5** (Fluent: NavigationView, cards, dark/light themes) |
| Pattern | **MVVM** — CommunityToolkit.Mvvm 8.3 (source-generated commands/observables) |
| Data | **SQLite** + **EF Core 8** |
| Host | Microsoft.Extensions.Hosting (generic host + DI) |
| Tests | xUnit over real SQLite (in-memory connection) |
| Packaging | `dotnet publish` single-file exe; optional Inno Setup 6 Setup.exe |
| Charts | Hand-rolled `DrawingContext` controls — zero chart dependencies |

**Money rule:** SQLite has no decimal type → amounts are `TEXT` columns, converted
with invariant culture, `decimal` in C# end-to-end. **Floats are never used.**

---

## 🚀 Quick start

Requires the **.NET 8 SDK**.

```bash
cd net/FiniteDesktop
dotnet run --project Finite.App     # run the app
dotnet test                          # 17/17 tests green
```

Data lives in `%APPDATA%\FiniteDesktop\` (`finite.db` + `settings.json`), created and seeded on first launch.

Build an installable:

```powershell
powershell -ExecutionPolicy Bypass -File build-installer.ps1
# → installer/Finite-Desktop-1.0.0-win-x64.zip  (+ Setup.exe if Inno Setup 6 installed)
```

---

## 📂 Complete file map

Every file, what it does, and where to look when changing something.

### Root files

| File | Purpose |
|---|---|
| `build-installer.ps1` | One-command build: publish single-file exe → zip → optional Setup.exe (Inno Setup) |
| `.gitignore` | Keeps `bin/`, `obj/`, `publish/`, installer artifacts out of git |

### `Finite.Core/` — domain entities (no dependencies, no logic)

| File | Contents |
|---|---|
| `Account.cs` | Account: Name, Type, InitialBalance, WalletProvider, BankName, AccountNumber, CreditLimit, Color, Icon, IsActive, DisplayOrder. Balance is **derived elsewhere** |
| `Category.cs` | Category: Name, Type (Income/Expense/Both), Color, Icon, ParentId, IsSystem (seeded, protected) |
| `Transaction.cs` | Income/Expense entry: AccountId, CategoryId?, Type, Amount, Date, Time, Description, ReferenceNumber, ReceiptPath, DebtId?, RecurringTransactionId?, IsReconciled |
| `Transfer.cs` | FromAccountId, ToAccountId (never equal), Amount, Fee, Date |
| `Budget.cs` | CategoryId, Amount, Period (Daily/Weekly/Monthly/Yearly), AlertThreshold, StartDate, EndDate |
| `RecurringTransaction.cs` | Name, Type, Amount, CategoryId, AccountId, Frequency (Daily→Yearly), Interval, DayOfMonth, StartDate, EndDate?, NextOccurrence, AutoCreate, IsActive |
| `Debt.cs` | Type (Receivable/Payable), PersonName, PersonContact, Amount, AmountPaid, DueDate?, Status (Pending/Partial/Settled/Overdue/Cancelled) |
| `SavingsGoal.cs` | Name, TargetAmount, TargetDate?, LinkedAccountId (its balance = progress), IsCompleted |
| `Enums.cs` | All enums: `AccountType`, `TransactionType`, `CategoryType`, `Frequency`, `BudgetPeriod`, `DebtStatus`, `DebtType` |

### `Finite.Data/` — persistence

| File | Contents |
|---|---|
| `FiniteDbContext.cs` | EF Core context: SQLite config, TEXT money conversion, unique/relationship indices, soft-delete query filters |
| `DbInitializer.cs` | `Database.Migrate()`/EnsureCreated on first run |
| `SystemCategorySeeder.cs` | Seeds the 15 system categories (matches Laravel `CategorySeeder`) once, if none exist |

### `Finite.Services/` — all business logic (UI-free, fully unit-testable)

| File | Key methods |
|---|---|
| `BalanceService.cs` | `GetBalanceAsync` / `GetBalancesAsync` — derived `initial + Σincome − Σexpense − ΣtransferOut − Σfees + ΣtransferIn`; `GetNetWorthAsync` — assets (cash-positive accounts) vs liabilities (credit-card negatives, payables) + receivables |
| `AccountService.cs` | Create/update/delete with safety checks (can't delete account with transactions) |
| `TransactionService.cs` | `CreateAsync` (validation → `ValidationException`), `UpdateAsync`, `DeleteAsync`, `GetRecentAsync` |
| `TransferService.cs` | `CreateAsync` — blocks same-account transfers, fee charged to source |
| `DebtService.cs` | `RecordPaymentAsync` / `SettleAsync` — **atomic** (debt update + transaction in one DB transaction), auto-creates the opening transaction when a debt is created |
| `RecurrenceEngine.cs` | `ProcessDueAsync` — **catch-up loop**: generates *every* missed occurrence, clamps day-of-month (Jan 31 → Feb 28), respects `EndDate` |
| `RecurringTransactionService.cs` | CRUD for recurring definitions |
| `BudgetService.cs` | Budget CRUD |
| `BudgetEvaluator.cs` | `EvaluateActiveAsync` — current-period spent/remaining/percent with grouped queries (no N+1, no raw SQL) |
| `CategoryService.cs` | CRUD + system-category delete protection + in-use checks |
| `SavingsGoalService.cs` | Goals + progress (`SavingsGoalView`), auto-complete on payment |
| `ReportService.cs` | Analytics: `GetMonthlyFlowAsync` (6-month bars), `GetBalanceTrendAsync` (30-day), `GetCategoryBreakdownAsync`, `GetTopMerchantsAsync`, `GetDailyExpensesAsync`, `GetCurrentMonthFlowAsync` |

Result records: `NetWorthSummary`, `BudgetStatus`, `CategoryTotal`, `MonthFlow`, `DailyPoint`, `SavingsGoalView`.

### `Finite.App/` — the WPF application

Root files:

| File | Purpose |
|---|---|
| `App.xaml` | App resources: WPF-UI ControlsDictionary + **ThemesDictionary (required for theme switching)**, shared converters |
| `App.xaml.cs` | Generic host + DI registrations (DbContext, 12 services, 12 pages/VMs), DB init, **theme application from settings**, global exception dialog |
| `AppSettings.cs` | `AppThemeChoice` (Dark/Light/System) persisted to `settings.json` |
| `DbPaths.cs` | Data folder: `%APPDATA%\FiniteDesktop\` |
| `NavigationPageProvider.cs` | WPF-UI `IPageService` resolving pages from DI |
| `AssemblyInfo.cs` | `ThemeInfo` → points custom control styles to `Themes/generic.xaml` |

`Views/`:

| File | Purpose |
|---|---|
| `MainWindow.xaml(.cs)` | FluentWindow shell: NavigationView sidebar (grouped sections), footer Settings item, page resolution via `NavigationPageProvider` |

`Pages/` (each `XPage.xaml` + `XPage.xaml.cs`; code-behind is 10 lines — constructor injection of the VM):

| Page | What it shows |
|---|---|
| `DashboardPage` | Net worth cards, monthly in/out, cash flow, daily average, 30-day trend chart, budget alerts, goals, recent transactions; triggers recurring catch-up |
| `TransactionsPage` | List + filters (account/type), add form, per-type colored amounts |
| `AccountsPage` | Account list with color swatches + balances; add form with **conditional fields** (wallet provider / bank fields / credit limit) and **ColorPickerButton** |
| `TransfersPage` | Recent transfers + add form (from/to/amount/fee) |
| `BudgetsPage` | Budget cards with `ColoredProgressBar` green→yellow→red, spent/remaining |
| `DebtsPage` | Owed-to-me / I-owe badges, overdue red, Record Payment + Settle buttons |
| `SavingsGoalsPage` | Goal cards with progress bars, ACHIEVED marker, add-contribution |
| `RecurringPage` | Recurring list with overdue highlight, Create Now (respects end date), pause/resume |
| `CategoriesPage` | Category list with swatches, system delete-protected, add with ColorPickerButton |
| `AnalyticsPage` | 6-month income/expense bars, 30-day trend, top categories, top merchants, health score |
| `BillCalendarPage` | Monday-start month grid: recurring + debt due chips (green/red/amber), today highlight, month navigation |
| `SettingsPage` | Theme picker (live), database path + open folder, backup/restore, about |

`ViewModels/` (one per page, CommunityToolkit MVVM):

| VM | Notes |
|---|---|
| `DashboardViewModel` | Loads everything in parallel; runs `RecurrenceEngine.ProcessDueAsync` on load |
| `AccountsViewModel`, `TransactionsViewModel`, `TransfersViewModel` | Form state + validation + list load |
| `BudgetsViewModel`, `DebtsViewModel`, `SavingsGoalsViewModel`, `RecurringViewModel` | Domain lists + actions |
| `CategoriesViewModel` | Uses `Color` string bound two-way to `ColorPickerButton.HexText` |
| `AnalyticsViewModel`, `BillCalendarViewModel` | Pre-computes brushes (category colors, chips) so XAML binding never fails |

`Controls/` (custom, dependency-free):

| File | Purpose |
|---|---|
| `ChartBase.cs` | Shared base: re-renders on `ObservableCollection` **contents** change + on theme change |
| `SimpleBarChart.cs` | Grouped 1–2 series bars, axis labels, hover tooltips, legend |
| `SimpleLineChart.cs` | Line + gradient area (built in one `StreamGeometry` pass — see `BeginFigure` note), end-dot, hover tooltips |
| `ColoredProgressBar.cs` | Progress bar honoring `Foreground` (WPF-UI's themed one ignores it); template in `Themes/generic.xaml` |
| `ColorPickerButton.cs` | Full picker: SV box + hue slider (mouse-captured drags) + presets + hex entry; two-way `HexText` string property |

`Converters/`: `InverseBoolConverter`, `BoolToRedConverter`, `BoolToVisibilityConverter`, `HexToBrushConverter` (throw-safe), `CalendarConverters` (today brush, out-of-month opacity).

`Themes/generic.xaml`: default templates for `ColoredProgressBar` + `ColorPickerButton` (loaded via `ThemeInfo`).

### `Finite.Tests/` — 17 xUnit tests over real SQLite

| File | Covers |
|---|---|
| `TestDb.cs` | Shared fixture: SQLite in-memory connection + schema |
| `BalanceServiceTests.cs` | Initial balance, income/expense, transfer + fee, credit-card-as-liability, debts in net worth |
| `RecurrenceEngineTests.cs` | Catch-up (missed days all generated), day-of-month clamping, end date |
| `ServiceValidationTests.cs` | Negative amounts, same-account transfers, missing account/category rules |

### `installer/`

| File | Purpose |
|---|---|
| `Finite.iss` | Inno Setup script → `Finite-Desktop-<ver>-setup.exe` (shortcuts, clean uninstall, **user data preserved**) |
| `*.zip` | Build output — portable app (git-ignored) |

---

## 🏛️ How it fits together

```
App.xaml.cs (DI host)
   │
   ├─ FiniteDbContext ──► SQLite (%APPDATA%\FiniteDesktop\finite.db)
   ├─ 12 services ──► all business rules, validation, money math
   │
   └─ NavigationView ──► NavigationPageProvider ──► Page(VM(service))
            VM loads data async ──► pre-computes brushes/labels ──► XAML binds
```

**Adding a new page (the repeatable recipe):**
1. Service method if new logic is needed (in `Finite.Services`, unit-test it)
2. ViewModel in `Finite.App/ViewModels` (CommunityToolkit `[ObservableProperty]`/`[RelayCommand]`)
3. Page XAML + code-behind taking the VM via constructor
4. Register VM + Page in `App.xaml.cs` DI
5. Add `NavigationViewItem` in `MainWindow.xaml` with `TargetPageType`

**Money rule reminder:** never `float`/`double` for amounts; services accept/return `decimal`; storage is TEXT with invariant culture.

---

## 🧠 Design decisions (the Laravel bugs, designed out)

| Laravel bug | Here |
|---|---|
| Observer-based balance updates drifted | **Balance is derived, never stored** — recomputed in grouped queries; self-healing by construction |
| Recurring made 1 transaction after downtime | **Catch-up loop** generates every missed occurrence; respects `end_date`; clamps day-of-month |
| "Mark Settled" didn't touch balances; no DB transaction | `DebtService` writes payment + settlement **atomically** |
| Budget filter: raw SQL quoting bugs + counted soft-deleted rows | `BudgetEvaluator`: typed EF queries, current period only, grouped |
| Savings rate computed from top-8 categories only | Full sums in `ReportService` |
| Ownership guards skipped in CLI context | Single-user desktop — no multi-tenant scoping at all |
| Service worker cached authenticated HTML | No web layer; data never leaves the machine |

---

## 🗺️ Roadmap

- [ ] CSV import/export (Laravel-importer-style column guessing + skip report)
- [ ] Global hotkey Quick-Add tray (Ctrl+Alt+F)
- [ ] App icon + MSIX packaging option
- [ ] SQLite WAL mode + auto-backup rotation

> 📁 **Folder-level developer guides live next to the code**: each project folder has its own `README.md` (`Finite.Core/README.md`, `Finite.Data/README.md`, `Finite.Services/README.md`, `Finite.App/README.md`, `Finite.Tests/README.md`) with API tables, recipes and pitfalls.
