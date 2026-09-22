# Finite.Tests

xUnit tests over **real SQLite** (in-memory connection kept open for the test's lifetime) — the same SQL dialect the app runs in production, unlike InMemory provider fakes.

## Run

```bash
dotnet test          # from net/FiniteDesktop
```

17 tests, all must stay green. They exist to protect the money math — if one fails, do not "fix the test" until you're sure the business rule didn't change.

## Files

| File | Covers |
|---|---|
| `TestDb.cs` | Fixture: creates `SqliteConnection` (in-memory) + `FiniteDbContext` on top of it, ensures schema. `IDisposable` cleanup |
| `BalanceServiceTests.cs` | Initial balance with no movements · income/expense arithmetic · transfer moves money and fee hits the source · credit-card spending counts as liability in net worth · receivable/payable debts adjust net worth |
| `RecurrenceEngineTests.cs` | Catch-up: all missed occurrences generated after downtime · day-of-month clamping (Jan 31 → Feb 28) · end date respected (no generation past it) |
| `ServiceValidationTests.cs` | Negative/zero amounts rejected · same-account transfer rejected · missing account/category rejected (`ValidationException`) |

## Writing a new test

```csharp
public class MyThingTests : IDisposable
{
    private readonly Data.FiniteDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

    public MyThingTests() => (_db, _connection) = TestDb.Create();
    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task The_rule_it_must_hold()
    {
        // arrange: seed accounts/rows via _db, SaveChanges first to get Ids
        // act:    call the service
        // assert: Assert.Equal(expected, actual);
    }
}
```

Gotchas learned the hard way:
- **Save changes before using entity `Id`s** (auto-increment assigns on save).
- Use `DateOnly.FromDateTime(DateTime.Today)` for dates.
- Assert on derived values via the service, not by poking DbContext state — that's what makes the tests catch real regressions.
