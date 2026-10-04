using MediatR;
using Poulet.Domain;

namespace Poulet.Application;

public sealed record GetSnapshot : IRequest<Snapshot>;
public sealed class GetSnapshotHandler(IRepository db, ICurrentUser user) : IRequestHandler<GetSnapshot, Snapshot>
{
    public async Task<Snapshot> Handle(GetSnapshot request, CancellationToken ct)
    {
        var sales = await db.List<Sale>(ct);
        if (user.Role == "grossiste")
            return new([], [], [], [], [], sales.Where(s => s.ClientId == user.ClientId).Select(SaleVisibility.CustomerView).ToList(), []);
        var purchases = await db.List<Purchase>(ct);
        var chambers = (await db.List<Chamber>(ct)).Select(c => new ChamberStock(c.Id, c.Name, c.Capacity, c.IsSouk, Inventory.Stock(purchases, sales, c.Id, "normal"), Inventory.Stock(purchases, sales, c.Id, "bibi"))).ToList();
        return new(await db.List<Supplier>(ct), await db.List<Client>(ct), chambers, purchases, await db.List<Feed>(ct), sales, (await db.List<User>(ct)).Select(u => new UserDto(u.Id, u.Username, u.Role, u.ClientId)).ToList());
    }
}
public sealed record GetReport(DateOnly From, DateOnly To) : IRequest<object>;
public sealed class GetReportHandler(IRepository db) : IRequestHandler<GetReport, object>
{
    public async Task<object> Handle(GetReport request, CancellationToken ct)
    {
        Rules.Require(request.From <= request.To, "Invalid date range.");
        var sales = (await db.List<Sale>(ct)).Where(s => s.Date >= request.From && s.Date <= request.To).ToList();
        var purchases = (await db.List<Purchase>(ct)).Where(p => p.Date >= request.From && p.Date <= request.To).ToList();
        var suppliers = await db.List<Supplier>(ct);
        var clients = await db.List<Client>(ct);
        var feed = (await db.List<Feed>(ct)).Where(f => f.Date >= request.From && f.Date <= request.To).Sum(f => f.Cost);
        var revenue = sales.Sum(Inventory.Revenue);
        var cost = sales.Sum(Inventory.SoldCost);
        var crates = sales.Sum(s => s.CrateCost);
        return new {
            request.From, request.To, ChickenKg = sales.Sum(s => s.Type == "lundi" ? s.Lines.Sum(l => l.Quantity) : s.Quantity),
            Revenue = revenue, CostOfGoods = cost, FeedCost = feed, CrateCost = crates, GrossProfit = revenue - cost, NetProfit = revenue - cost - feed - crates,
            PaidMonday = sales.SelectMany(s => s.Lines).Where(l => l.Paid).Sum(l => l.Quantity * l.UnitPrice),
            UnpaidMonday = sales.SelectMany(s => s.Lines).Where(l => !l.Paid).Sum(l => l.Quantity * l.UnitPrice),
            ByDay = sales.Select(s => new { s.Date, Revenue = Inventory.Revenue(s) }).GroupBy(s => s.Date).OrderBy(g => g.Key).Select(g => new { Date = g.Key, Revenue = g.Sum(s => s.Revenue) }),
            TopClients = sales.Where(s => s.Type == "grossiste").GroupBy(s => s.ClientId.HasValue ? clients.FirstOrDefault(c => c.Id == s.ClientId)?.Name ?? "Deleted client" : s.ClientName ?? "Anonymous").Select(g => new { Name = g.Key, Quantity = g.Sum(s => s.Quantity), Revenue = g.Sum(Inventory.Revenue) }).OrderByDescending(c => c.Revenue).Take(10),
            Suppliers = purchases.GroupBy(p => p.SupplierId).Select(g => new { Name = suppliers.FirstOrDefault(s => s.Id == g.Key)?.Name ?? "Unspecified", Count = g.Count(), Quantity = g.Sum(p => p.Quantity), Cost = g.Sum(p => (p.ActualWeight ?? p.Quantity) * p.UnitPrice) })
        };
    }
}
public sealed record GetReceipt(int SaleId, int? LineId) : IRequest<object>;
public sealed class GetReceiptHandler(IRepository db, ICurrentUser user) : IRequestHandler<GetReceipt, object>
{
    public async Task<object> Handle(GetReceipt request, CancellationToken ct)
    {
        var sale = await db.Find<Sale>(request.SaleId, ct) ?? throw new BusinessException("Sale not found.");
        Rules.Require(user.Role == "admin" || sale.ClientId == user.ClientId, "Access denied.");
        MondayLine? line = null;
        if (request.LineId.HasValue) line = sale.Lines.SingleOrDefault(l => l.Id == request.LineId) ?? throw new BusinessException("Line not found.");
        return new { Sale = user.Role == "admin" ? sale : SaleVisibility.CustomerView(sale), Line = line, Client = sale.ClientId.HasValue ? await db.Find<Client>(sale.ClientId.Value, ct) : null };
    }
}
internal static class SaleVisibility
{
    // Customer responses contain their purchases, never internal procurement costs.
    public static Sale CustomerView(Sale sale) => new()
    {
        Id=sale.Id, Date=sale.Date, Type=sale.Type, ClientId=sale.ClientId, ClientName=sale.ClientName,
        ChickenType=sale.ChickenType, Mode=sale.Mode, Quantity=sale.Quantity, UnitPrice=sale.UnitPrice,
        Pieces=sale.Pieces, Notes=sale.Notes, Allocations=sale.Allocations
    };
}
