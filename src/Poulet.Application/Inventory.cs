using Poulet.Domain;

namespace Poulet.Application;

public static class Inventory
{
    public static decimal Stock(IEnumerable<Purchase> purchases, IEnumerable<Sale> sales, int chamberId, string? type = null) =>
        purchases.Where(p => type == null || p.ChickenType == type).SelectMany(p => p.Allocations).Where(a => a.ChamberId == chamberId).Sum(a => a.Quantity)
        - sales.Where(s => type == null || s.ChickenType == type).SelectMany(s => s.Allocations).Where(a => a.ChamberId == chamberId).Sum(a => a.Quantity);
    public static decimal AverageCost(IEnumerable<Purchase> purchases, int chamberId, string type)
    {
        var rows = purchases.Where(p => p.ChickenType == type).SelectMany(p => p.Allocations.Where(a => a.ChamberId == chamberId).Select(a => new { a.Quantity, Price = p.UnitPrice * (p.ActualWeight ?? p.Quantity) / p.Quantity })).ToList();
        var quantity = rows.Sum(r => r.Quantity);
        return quantity == 0 ? 0 : rows.Sum(r => r.Quantity * r.Price) / quantity;
    }
    public static decimal ProductStock(IEnumerable<ProductPurchase> purchases, IEnumerable<ProductSale> sales, string product, string variety) => purchases.Where(p => p.Product == product && p.Variety == variety).Sum(p => p.Quantity) - sales.Where(s => s.Product == product && s.Variety == variety).Sum(s => s.Quantity);
    public static decimal ProductCost(IEnumerable<ProductPurchase> purchases, string product, string variety)
    {
        var rows = purchases.Where(p => p.Product == product && p.Variety == variety).ToList();
        var qty = rows.Sum(p => p.Quantity);
        return qty == 0 ? 0 : rows.Sum(p => p.Quantity * p.UnitPrice) / qty;
    }
    public static decimal Revenue(Sale s) => s.Type == "lundi" ? s.Lines.Sum(l => l.Quantity * l.UnitPrice) : s.Quantity * s.UnitPrice;
    public static decimal SoldCost(Sale s) => s.Type == "lundi" ? s.CostOfGoods * s.Lines.Sum(l => l.Quantity) / s.Quantity : s.CostOfGoods;
}
