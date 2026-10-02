using MediatR;
using Poulet.Domain;

namespace Poulet.Application;

public sealed class DeleteHandler(IRepository db, ICurrentUser user) : IRequestHandler<DeleteEntity, bool>
{
    private async Task Remove<T>(int id, CancellationToken ct) where T : Entity => db.Remove(await db.Find<T>(id, ct) ?? throw new BusinessException("Record not found."));
    public async Task<bool> Handle(DeleteEntity r, CancellationToken ct)
    {
        switch (r.Kind)
        {
            case "supplier":
                Rules.Require(!(await db.List<Purchase>(ct)).Any(p => p.SupplierId == r.Id) && !(await db.List<ProductPurchase>(ct)).Any(p => p.SupplierId == r.Id), "Supplier is already used."); await Remove<Supplier>(r.Id, ct); break;
            case "client":
                Rules.Require(!(await db.List<Sale>(ct)).Any(s => s.ClientId == r.Id) && !(await db.List<Purchase>(ct)).SelectMany(p => p.Allocations).Any(a => a.ClientId == r.Id) && !(await db.List<User>(ct)).Any(u => u.ClientId == r.Id), "Client is already used."); await Remove<Client>(r.Id, ct); break;
            case "chamber":
                Rules.Require((await db.Find<Chamber>(r.Id, ct))?.IsSouk != true, "The SOUK chamber is required.");
                Rules.Require(!(await db.List<Purchase>(ct)).SelectMany(p => p.Allocations).Any(a => a.ChamberId == r.Id) && !(await db.List<Sale>(ct)).SelectMany(s => s.Allocations).Any(a => a.ChamberId == r.Id) && !(await db.List<Feed>(ct)).Any(f => f.ChamberId == r.Id), "Chamber is already used."); await Remove<Chamber>(r.Id, ct); break;
            case "purchase":
                var remaining = (await db.List<Purchase>(ct)).Where(p => p.Id != r.Id).ToList(); var sales = await db.List<Sale>(ct);
                foreach (var c in await db.List<Chamber>(ct)) foreach (var type in new[] { "normal", "bibi" }) Rules.Require(Inventory.Stock(remaining, sales, c.Id, type) >= 0, "This purchase contains stock already sold.");
                await Remove<Purchase>(r.Id, ct); break;
            case "sale": await Remove<Sale>(r.Id, ct); break;
            case "line":
                var sale = await db.Find<Sale>(r.ParentId ?? 0, ct) ?? throw new BusinessException("Sale not found.");
                var line = sale.Lines.SingleOrDefault(l => l.Id == r.Id) ?? throw new BusinessException("Line not found."); db.Remove(line); break;
            case "feed": await Remove<Feed>(r.Id, ct); break;
            case "product-purchase":
                var pa = await db.Find<ProductPurchase>(r.Id, ct) ?? throw new BusinessException("Purchase not found.");
                Rules.Require(Inventory.ProductStock((await db.List<ProductPurchase>(ct)).Where(p => p.Id != r.Id), await db.List<ProductSale>(ct), pa.Product, pa.Variety) >= 0, "This purchase contains stock already sold."); db.Remove(pa); break;
            case "product-sale": await Remove<ProductSale>(r.Id, ct); break;
            case "user": Rules.Require(r.Id != user.Id, "Cannot delete your own account."); await Remove<User>(r.Id, ct); break;
            default: throw new BusinessException("Unknown record type.");
        }
        await db.Save(ct); return true;
    }
}
