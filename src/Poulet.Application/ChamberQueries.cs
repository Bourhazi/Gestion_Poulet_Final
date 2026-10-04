using MediatR;
using Poulet.Domain;
namespace Poulet.Application;
public sealed record GetChamberProfit(int ChamberId) : IRequest<object>;
public sealed class GetChamberProfitHandler(IRepository db) : IRequestHandler<GetChamberProfit, object>
{
    public async Task<object> Handle(GetChamberProfit r, CancellationToken ct)
    {
        var chamber=await db.Find<Chamber>(r.ChamberId,ct) ?? throw new BusinessException("Chamber not found.");
        var purchases=await db.List<Purchase>(ct); var sales=await db.List<Sale>(ct); var feedRows=(await db.List<Feed>(ct)).Where(f=>f.ChamberId==r.ChamberId).ToList();
        var assigned=purchases.SelectMany(p=>p.Allocations.Where(a=>a.ChamberId==r.ChamberId).Select(a=>new { Cost=a.Quantity/p.Quantity*(p.ActualWeight??p.Quantity)*p.UnitPrice, WeightDifference=p.DepartureWeight.HasValue&&p.ActualWeight.HasValue?(p.ActualWeight.Value-p.DepartureWeight.Value)*a.Quantity/p.Quantity:0 })).ToList();
        var sold=sales.SelectMany(s=>s.Allocations.Where(a=>a.ChamberId==r.ChamberId).Select(a=>new { Revenue=Inventory.Revenue(s)*a.Quantity/s.Quantity, Cost=Inventory.SoldCost(s)*a.Quantity/s.Quantity, Crate=s.CrateCost*a.Quantity/s.Quantity })).ToList();
        var feed=feedRows.Sum(f=>f.Cost);
        var feedSold=Inventory.FeedCostForSales(purchases,feedRows,sales.Where(s=>s.Allocations.Any(a=>a.ChamberId==r.ChamberId)));
        return new { chamber.Id, chamber.Name, Stock=Inventory.Stock(purchases,sales,r.ChamberId), PurchaseCost=assigned.Sum(p=>p.Cost), WeightDifference=assigned.Sum(p=>p.WeightDifference), Revenue=sold.Sum(s=>s.Revenue), CostOfGoods=sold.Sum(s=>s.Cost), FeedQuantityKg=feedRows.Where(f=>f.Unit=="kg").Sum(f=>f.Quantity), FeedQuantityBags=feedRows.Where(f=>f.Unit=="sac").Sum(f=>f.Quantity), FeedCost=feed, FeedSoldCost=feedSold, FeedStockCost=feed-feedSold, CrateCost=sold.Sum(s=>s.Crate), GrossProfit=sold.Sum(s=>s.Revenue-s.Cost), NetProfit=sold.Sum(s=>s.Revenue-s.Cost-s.Crate)-feedSold };
    }
}
public sealed record GetDeductionPlan(decimal Quantity, string ChickenType) : IRequest<object>;
public sealed class GetDeductionPlanHandler(IRepository db) : IRequestHandler<GetDeductionPlan, object>
{
    public async Task<object> Handle(GetDeductionPlan r,CancellationToken ct)
    {
        Rules.NonNegative(r.Quantity,"Quantity"); Rules.Chicken(r.ChickenType);
        var purchases=await db.List<Purchase>(ct); var sales=await db.List<Sale>(ct); var left=r.Quantity;
        var plan=new List<object>();
        foreach(var c in (await db.List<Chamber>(ct)).OrderByDescending(c=>c.IsSouk).ThenBy(c=>c.Name))
        {
            var quantity=Math.Min(left,Inventory.Stock(purchases,sales,c.Id,r.ChickenType));
            if(quantity>0) { plan.Add(new { ChamberId=c.Id, c.Name, Quantity=quantity }); left-=quantity; }
            if(left==0)break;
        }
        return new { Plan=plan, Uncovered=left };
    }
}
