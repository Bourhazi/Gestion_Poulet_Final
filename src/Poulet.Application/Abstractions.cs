using MediatR;
using Poulet.Domain;

namespace Poulet.Application;

public interface IRepository
{
    Task<List<T>> List<T>(CancellationToken ct) where T : Entity;
    Task<T?> Find<T>(int id, CancellationToken ct) where T : Entity;
    void Add<T>(T entity) where T : Entity;
    void Remove<T>(T entity) where T : Entity;
    Task Save(CancellationToken ct);
}
public interface IUnitOfWork { Task<T> Execute<T>(Func<Task<T>> action, CancellationToken ct); }
public interface IPasswords { string Hash(string password); bool Verify(string hash, string password); }
public interface ICurrentUser { int Id { get; } string Role { get; } int? ClientId { get; } string? IpAddress => null; }
public interface ICommand { }
public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unit) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) => request is ICommand ? unit.Execute(() => next(), ct) : next();
}
public sealed class AuditBehavior<TRequest, TResponse>(IRepository db, ICurrentUser user) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var result = await next();
        if (request is not ICommand || request is SetSalePaymentStatus) return result;
        var name = typeof(TRequest).Name;
        var action = name.StartsWith("Delete") ? "Deleted" : name.StartsWith("Save") ? "Saved" : name.StartsWith("Add") ? "Created" : name.StartsWith("Transfer") ? "Transferred" : name.StartsWith("Toggle") ? "Updated" : "Changed";
        var module = name.Replace("Save", "").Replace("Add", "").Replace("Delete", "").Replace("Toggle", "").Replace("Transfer", "");
        db.Add(new AuditLog { Date = DateTimeOffset.UtcNow, UserId = user.Id == 0 ? null : user.Id, Action = action, Module = module, EntityType = module, NewValues = System.Text.Json.JsonSerializer.Serialize(request), Description = $"{action} {module}.", IpAddress = user.IpAddress });
        await db.Save(ct);
        return result;
    }
}
public sealed record UserDto(int Id, string Username, string Role, int? ClientId);
public sealed record ChamberStock(int Id, string Name, decimal Capacity, bool IsSouk, decimal Normal, decimal Bibi) { public decimal Total => Normal + Bibi; }
public sealed record Snapshot(List<Supplier> Suppliers, List<Client> Clients, List<ChamberStock> Chambers, List<Purchase> Purchases, List<Feed> Feed, List<Sale> Sales, List<UserDto> Users);
