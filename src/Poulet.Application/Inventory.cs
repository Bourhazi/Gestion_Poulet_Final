using Poulet.Domain;

namespace Poulet.Application;

public static class Inventory
{
    public static decimal Stock(IEnumerable<Purchase> purchases, IEnumerable<Sale> sales, int chamberId, string? type = null) =>
        purchases.Where(p => type == null || p.ChickenType == type).SelectMany(p => p.Allocations).Where(a => a.ChamberId == chamberId).Sum(a => a.Quantity)
        - sales.Where(s => type == null || s.ChickenType == type).SelectMany(s => s.Allocations).Where(a => a.ChamberId == chamberId).Sum(a => a.Quantity)
        + sales.SelectMany(s => s.Transfers).Where(t => t.ChamberId == chamberId && (type == null || t.ChickenType == type)).Sum(t => t.Quantity);
    public static decimal AverageCost(IEnumerable<Purchase> purchases, int chamberId, string type)
    {
        var rows = purchases.Where(p => p.ChickenType == type).SelectMany(p => p.Allocations.Where(a => a.ChamberId == chamberId).Select(a => new { a.Quantity, Price = p.UnitPrice * (p.ActualWeight ?? p.Quantity) / p.Quantity })).ToList();
        var quantity = rows.Sum(r => r.Quantity);
        return quantity == 0 ? 0 : rows.Sum(r => r.Quantity * r.Price) / quantity;
    }
    public static decimal Revenue(Sale s) => s.Type == "lundi" ? s.Lines.Sum(l => l.Quantity * l.UnitPrice) : s.Quantity * s.UnitPrice;
    public static decimal SoldCost(Sale s) => s.Type == "lundi" ? s.CostOfGoods * s.Lines.Sum(l => l.Quantity) / s.Quantity : s.CostOfGoods;
    public static decimal FeedCostForSales(IEnumerable<Purchase> purchases, IEnumerable<Feed> feed, IEnumerable<Sale> sales)
    {
        var received = purchases.SelectMany(p => p.Allocations).Where(a => a.ChamberId.HasValue).GroupBy(a => a.ChamberId!.Value).ToDictionary(g => g.Key, g => g.Sum(a => a.Quantity));
        var feedByChamber = feed.GroupBy(f => f.ChamberId).ToDictionary(g => g.Key, g => g.Sum(f => f.Cost));
        return sales.SelectMany(s => s.Allocations).Sum(a =>
            a.ChamberId != 0 && received.TryGetValue(a.ChamberId, out var quantity) && quantity > 0 && feedByChamber.TryGetValue(a.ChamberId, out var cost)
                ? a.Quantity * cost / quantity
                : 0);
    }
}
