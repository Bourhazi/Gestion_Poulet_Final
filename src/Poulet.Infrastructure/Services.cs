using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Poulet.Application;
using Poulet.Domain;
namespace Poulet.Infrastructure;
public sealed class Repository(AppDbContext db) : IRepository
{
    private IQueryable<T> Query<T>() where T : Entity
    {
        if (typeof(T) == typeof(Purchase)) return (IQueryable<T>)db.Set<Purchase>().Include(p => p.Allocations);
        if (typeof(T) == typeof(Sale)) return (IQueryable<T>)db.Set<Sale>().Include(s => s.Allocations).Include(s => s.Lines).ThenInclude(l => l.Pieces).Include(s => s.Transfers).AsSplitQuery();
        return db.Set<T>();
    }
    public Task<List<T>> List<T>(CancellationToken ct) where T : Entity => Query<T>().ToListAsync(ct);
    public Task<T?> Find<T>(int id, CancellationToken ct) where T : Entity => Query<T>().SingleOrDefaultAsync(e => e.Id == id, ct);
    public void Add<T>(T e) where T : Entity => db.Add(e);
    public void Remove<T>(T e) where T : Entity => db.Remove(e);
    public async Task Save(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
public sealed class WriteGate { public SemaphoreSlim Semaphore { get; } = new(1, 1); }
public sealed class UnitOfWork(AppDbContext db, WriteGate gate) : IUnitOfWork
{
    public async Task<T> Execute<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await gate.Semaphore.WaitAsync(ct);
        try { await using var tx = await db.Database.BeginTransactionAsync(ct); var result = await action(); await tx.CommitAsync(ct); return result; }
        finally { gate.Semaphore.Release(); }
    }
}
public sealed class Passwords : IPasswords
{
    private readonly PasswordHasher<string> hasher = new();
    public string Hash(string password) => hasher.HashPassword("", password);
    public bool Verify(string hash, string password) => hasher.VerifyHashedPassword("", hash, password) != PasswordVerificationResult.Failed;
}
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(config.GetConnectionString("Database") ?? "Data Source=poulet-modern.db;Foreign Keys=True"));
        services.AddScoped<IRepository, Repository>(); services.AddScoped<IUnitOfWork, UnitOfWork>(); services.AddSingleton<WriteGate>(); services.AddSingleton<IPasswords, Passwords>(); return services;
    }
    public static async Task InitializeDatabase(IServiceProvider services, bool development, IConfiguration config)
    {
        using var scope = services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); await db.Database.EnsureCreatedAsync(); await EnsureFeedColumns(db);
        if (!await db.Set<User>().AnyAsync())
        {
            var password = config["Seed:AdminPassword"];
            if (string.IsNullOrEmpty(password)) { if (!development) throw new InvalidOperationException("Set Seed__AdminPassword before the first production startup."); password = "admin123"; }
            db.Add(new User { Username = "admin", Role = "admin", PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswords>().Hash(password) });
        }
        if (!await db.Set<Chamber>().AnyAsync(c => c.IsSouk)) db.Add(new Chamber { Name = "SOUK", IsSouk = true }); await db.SaveChangesAsync();
    }
    private static async Task EnsureFeedColumns(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var close = connection.State != System.Data.ConnectionState.Open;
        if (close) await connection.OpenAsync();
        try
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(Feed)";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
            }
            foreach (var (name, definition) in new[] { ("Unit", "TEXT NOT NULL DEFAULT 'kg'"), ("UnitPrice", "TEXT NOT NULL DEFAULT 0"), ("FeedType", "TEXT NULL") })
            {
                if (columns.Contains(name)) continue;
                await using var command = connection.CreateCommand();
                command.CommandText = $"ALTER TABLE Feed ADD COLUMN {name} {definition}";
                await command.ExecuteNonQueryAsync();
            }
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE IF NOT EXISTS MondayTransfer (Id INTEGER NOT NULL CONSTRAINT PK_MondayTransfer PRIMARY KEY AUTOINCREMENT, SaleId INTEGER NOT NULL, Date TEXT NOT NULL, ChamberId INTEGER NOT NULL, Quantity TEXT NOT NULL, ChickenType TEXT NOT NULL, UnitCost TEXT NOT NULL, UserId INTEGER NOT NULL, Notes TEXT NULL, CONSTRAINT FK_MondayTransfer_Sale_SaleId FOREIGN KEY (SaleId) REFERENCES Sale (Id) ON DELETE CASCADE, CONSTRAINT FK_MondayTransfer_Chamber_ChamberId FOREIGN KEY (ChamberId) REFERENCES Chamber (Id) ON DELETE RESTRICT, CONSTRAINT FK_MondayTransfer_User_UserId FOREIGN KEY (UserId) REFERENCES User (Id) ON DELETE RESTRICT)";
                await command.ExecuteNonQueryAsync();
            }
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE IF NOT EXISTS AuditLog (Id INTEGER NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY AUTOINCREMENT, Date TEXT NOT NULL, UserId INTEGER NULL, Action TEXT NOT NULL, Module TEXT NOT NULL, EntityType TEXT NOT NULL, EntityId INTEGER NULL, OldValues TEXT NULL, NewValues TEXT NULL, Description TEXT NOT NULL, IpAddress TEXT NULL)";
                await command.ExecuteNonQueryAsync();
            }
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "ALTER TABLE MondayLine ADD COLUMN PieceCount INTEGER NOT NULL DEFAULT 0";
                try { await command.ExecuteNonQueryAsync(); } catch (Microsoft.Data.Sqlite.SqliteException) { }
            }
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "ALTER TABLE Sale ADD COLUMN PaymentStatus TEXT NOT NULL DEFAULT 'UNPAID'";
                try { await command.ExecuteNonQueryAsync(); } catch (Microsoft.Data.Sqlite.SqliteException) { }
            }
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "ALTER TABLE Purchase ADD COLUMN PieceCount INTEGER NOT NULL DEFAULT 0";
                try { await command.ExecuteNonQueryAsync(); } catch (Microsoft.Data.Sqlite.SqliteException) { }
            }
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "ALTER TABLE User ADD COLUMN RefreshTokenHash TEXT NULL";
                try { await command.ExecuteNonQueryAsync(); } catch (Microsoft.Data.Sqlite.SqliteException) { }
            }
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "ALTER TABLE User ADD COLUMN RefreshTokenExpiresAt TEXT NULL";
                try { await command.ExecuteNonQueryAsync(); } catch (Microsoft.Data.Sqlite.SqliteException) { }
            }
        }
        finally { if (close) await connection.CloseAsync(); }
    }
}
