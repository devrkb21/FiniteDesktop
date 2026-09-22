# Finite.Data

EF Core persistence: the DbContext, initialization, and seeding. The **only** folder that knows about SQLite.

## Files

| File | Purpose |
|---|---|
| `FiniteDbContext.cs` | `FiniteDbContext : DbContext`. DbSets for all 8 entities. OnModelCreating: table mappings, **money as TEXT** (decimal ⇄ invariant-culture string converter), indices (transaction date/type/account, category, recurring next-occurrence…), unique constraints (e.g. transfer accounts) |
| `DbInitializer.cs` | `Initialize(context)` — applies migrations / ensures created. Called once from `App.OnStartup` inside a scope |
| `SystemCategorySeeder.cs` | Seeds the 15 system categories — **Income:** Salary, Freelance, Investment, Other Income · **Expense:** Food & Dining, Transportation, Shopping, Entertainment, Bills & Utilities, Healthcare, Education, Rent, Subscriptions, Personal Care, Other Expense (mirrors Laravel `CategorySeeder`) — only if the Categories table is empty |

## Rules for this folder

- **Money storage:** SQLite has no decimal type. Every amount property must go through
  the TEXT converter in `OnModelCreating`. If you add a new decimal property and see
  `SqliteException` about storage or weird rounding — you forgot the converter.
- **No business logic here.** Services own rules; this folder owns schema.
- Migrations vs EnsureCreated: dev DB is recreated freely; if you change the model,
  add an EF migration (`dotnet migrations add`) or delete `%APPDATA%\FiniteDesktop\finite.db` in dev.
- Connection string is built in `App.xaml.cs` from `DbPaths.GetDatabasePath()` — not here.

## Recipes

**Add a table:** entity in `Finite.Core` → `DbSet` + config here → migration → (usually) a service in `Finite.Services`.

**Add a money field:** `decimal` property on the entity → add the TEXT conversion in this folder's config → done. Never store as REAL.

**Reset local dev data:** close the app, delete `%APPDATA%\FiniteDesktop\finite.db`, relaunch — schema + 15 categories come back.
