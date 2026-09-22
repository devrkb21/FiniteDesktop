using Finite.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Finite.Tests;

/// <summary>Each test gets an isolated SQLite in-memory database with a live connection.</summary>
public static class TestDb
{
    public static (FiniteDbContext Context, SqliteConnection Connection) Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<FiniteDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new FiniteDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }
}
