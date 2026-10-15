using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TimeBlocker.Web.Data;
using TimeBlocker.Web.Models;

namespace TimeBlocker.Tests;

public sealed class TestDbFactory : IDisposable
{
    public const string UserAId = "user-a";
    public const string UserBId = "user-b";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDbFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateContext();
        db.Database.EnsureCreated();

        db.Users.AddRange(
            CreateUser(UserAId, "a@example.com"),
            CreateUser(UserBId, "b@example.com"));
        db.SaveChanges();
    }

    public AppDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();

    private static ApplicationUser CreateUser(string id, string email) => new()
    {
        Id = id,
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        SecurityStamp = Guid.NewGuid().ToString()
    };
}
