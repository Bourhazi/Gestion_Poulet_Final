using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Poulet.Application;
using Poulet.Domain;
using Poulet.Infrastructure;

namespace Poulet.Tests;
public sealed class TestUser : ICurrentUser { public int Id => 1; public string Role => "admin"; public int? ClientId => null; }
public sealed class BusinessTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ServiceProvider provider;
    private static readonly DateOnly Monday = new(2026, 10, 5);
    public BusinessTests()
    {
        connection.Open(); var services = new ServiceCollection();
        services.AddLogging(); services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddScoped<IRepository, Repository>(); services.AddScoped<IUnitOfWork, UnitOfWork>(); services.AddSingleton<WriteGate>(); services.AddSingleton<IPasswords, Passwords>(); services.AddSingleton<ICurrentUser, TestUser>();
        services.AddMediatR(c => { c.RegisterServicesFromAssemblyContaining<GetSnapshot>(); c.AddOpenBehavior(typeof(TransactionBehavior<,>)); });
        provider = services.BuildServiceProvider(); using var scope = provider.CreateScope(); scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }
    private async Task<T> Send<T>(IRequest<T> command) { using var scope = provider.CreateScope(); return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command); }
    private async Task<int> Stock(decimal quantity=100, string type="normal", int? chamberId=null)
    {
        var chamber = chamberId ?? await Send(new SaveChamber(0, "SOUK", 200));
        await Send(new SavePurchase(0, null, Monday, quantity, 10, type, null, null, null, [new(chamber, null, quantity)])); return chamber;
    }
    private static AddSale Sale(int chamber, decimal kg, string type="normal") => new(Monday,"grossiste",null,"Test client",chamber,type,"vivant",kg,20,0,0,null);
    [Fact] public async Task Purchase_requires_full_allocation_and_aggregates_capacity()
    {
        var c=await Send(new SaveChamber(0,"A",100));
        await Assert.ThrowsAsync<BusinessException>(()=>Send(new SavePurchase(0,null,Monday,100,10,"normal",null,null,null,[new(c,null,99)])));
        await Assert.ThrowsAsync<BusinessException>(()=>Send(new SavePurchase(0,null,Monday,120,10,"normal",null,null,null,[new(c,null,60),new(c,null,60)])));
        Assert.Empty((await Send(new GetSnapshot())).Purchases);
    }
    [Fact] public async Task Sales_cannot_use_other_chicken_type_or_negative_quantities()
    {
        var c=await Stock();
        await Assert.ThrowsAsync<BusinessException>(()=>Send(Sale(c,1,"bibi")));
        await Assert.ThrowsAsync<BusinessException>(()=>Send(Sale(c,-1)));
        await Send(Sale(c,80));
        await Assert.ThrowsAsync<BusinessException>(()=>Send(Sale(c,21)));
        Assert.Equal(20,(await Send(new GetSnapshot())).Chambers.Single().Normal);
    }
    [Fact] public async Task Monday_prioritizes_souk_and_tracks_payments_kg_and_piece_numbers()
    {
        var a=await Send(new SaveChamber(0,"A",200)); var c=await Stock(30);
        using(var scope=provider.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<AppDbContext>(); (await db.Set<Chamber>().SingleAsync(x=>x.Id==c)).IsSouk=true; await db.SaveChangesAsync(); }
        await Stock(100,chamberId:a);
        var id=await Send(new AddSale(Monday,"lundi",null,null,null,"normal","vivant",50,0,10,5,null));
        var lot=(await Send(new GetSnapshot())).Sales.Single(); Assert.Equal(30,lot.Allocations.Single(x=>x.ChamberId==c).Quantity); Assert.Equal(20,lot.Allocations.Single(x=>x.ChamberId==a).Quantity);
        var line=await Send(new AddMondayLine(id,"Client",10,20,false,"vivant",["001","002"],null));
        await Assert.ThrowsAsync<BusinessException>(()=>Send(new AddMondayLine(id,"Client",41,20,false,"vivant",["003"],null)));
        await Assert.ThrowsAsync<BusinessException>(()=>Send(new AddMondayLine(id,"Client",1,20,false,"vivant",["001"],null)));
        Assert.True(await Send(new TogglePayment(id,line)));
        await Assert.ThrowsAsync<BusinessException>(()=>Send(new AddSale(Monday.AddDays(1),"lundi",null,null,null,"normal","vivant",1,0,1,0,null)));
    }
    [Fact] public async Task Purchase_edit_delete_and_capacity_reduction_preserve_consumed_stock()
    {
        var c=await Stock(); await Send(Sale(c,70)); var p=(await Send(new GetSnapshot())).Purchases.Single();
        await Assert.ThrowsAsync<BusinessException>(()=>Send(new SavePurchase(p.Id,null,Monday,60,10,"normal",null,null,null,[new(c,null,60)])));
        await Assert.ThrowsAsync<BusinessException>(()=>Send(new DeleteEntity("purchase",p.Id)));
        await Assert.ThrowsAsync<BusinessException>(()=>Send(new SaveChamber(c,"SOUK",29)));
        Assert.Equal(30,(await Send(new GetSnapshot())).Chambers.Single().Total);
    }
    [Fact] public async Task Report_includes_direct_and_monday_chicken_sales_and_cost_of_sold_quantity()
    {
        var chamber=await Stock(); var id=await Send(new AddSale(Monday,"lundi",null,null,null,"normal","vivant",20,0,10,5,null));
        await Send(new AddMondayLine(id,"Client",10,20,true,"vivant",["01"],null));
        await Send(Sale(chamber,10));
        await Send(new AddFeed(chamber,Monday,1,3,null));
        await Send(new AddSale(Monday.AddDays(1),"detail",null,null,chamber,"normal","vivant",5,30,0,0,null));
        var json=System.Text.Json.JsonSerializer.SerializeToElement(await Send(new GetReport(Monday,Monday)));
        Assert.Equal(400,json.GetProperty("Revenue").GetDecimal());
        Assert.Equal(200,json.GetProperty("CostOfGoods").GetDecimal());
        Assert.Equal(200,json.GetProperty("GrossProfit").GetDecimal());
        Assert.Equal(192,json.GetProperty("NetProfit").GetDecimal());
        Assert.Equal(20,json.GetProperty("ChickenKg").GetDecimal());
        Assert.Equal(200,json.GetProperty("PaidMonday").GetDecimal());
        Assert.Equal(0,json.GetProperty("UnpaidMonday").GetDecimal());
        var day=Assert.Single(json.GetProperty("ByDay").EnumerateArray());
        Assert.Equal(400,day.GetProperty("Revenue").GetDecimal());
    }
    [Fact] public async Task Real_weight_cost_is_prorated_across_chambers()
    {
        var a=await Send(new SaveChamber(0,"A",100)); var b=await Send(new SaveChamber(0,"B",100));
        await Send(new SavePurchase(0,null,Monday,100,10,"normal",100,90,null,[new(a,null,50),new(b,null,50)]));
        var id=await Send(Sale(a,10)); Assert.Equal(90,(await Send(new GetSnapshot())).Sales.Single(s=>s.Id==id).CostOfGoods);
    }
    [Fact] public async Task Passwords_are_hashed_and_login_validates_credentials()
    {
        await Send(new AddUser("admin","admin123","admin",null)); var user=await Send(new Login("ADMIN","admin123")); Assert.Equal("admin",user.Role);
        await Assert.ThrowsAsync<BusinessException>(()=>Send(new Login("admin","wrong")));
        await Send(new ResetPassword(user.Id,"newPassword123")); await Assert.ThrowsAsync<BusinessException>(()=>Send(new Login("admin","admin123")));
    }
    [Fact] public async Task Concurrent_sales_do_not_oversell()
    {
        var c=await Stock();
        async Task<bool> Sell() { try { await Send(Sale(c,80)); return true; } catch(BusinessException) { return false; } }
        var outcomes=await Task.WhenAll(Sell(),Sell()); Assert.Single(outcomes,x=>x);
        Assert.Equal(20,(await Send(new GetSnapshot())).Chambers.Single().Total);
    }
    [Fact] public async Task Chamber_profit_and_deduction_preview_are_consistent()
    {
        var c=await Stock(); await Send(Sale(c,10)); await Send(new AddFeed(c,Monday,1,5,null));
        var report=System.Text.Json.JsonSerializer.SerializeToElement(await Send(new GetChamberProfit(c)));
        Assert.Equal(95,report.GetProperty("NetProfit").GetDecimal());
        var preview=System.Text.Json.JsonSerializer.SerializeToElement(await Send(new GetDeductionPlan(95,"normal")));
        Assert.Equal(5,preview.GetProperty("Uncovered").GetDecimal());
    }
    [Fact] public async Task Valid_purchase_edit_replaces_allocations_without_double_counting()
    {
        var c=await Stock(); var p=(await Send(new GetSnapshot())).Purchases.Single();
        await Send(new SavePurchase(p.Id,null,Monday,80,12,"normal",null,null,"Corrected",[new(c,null,40),new(c,null,40)]));
        var snapshot=await Send(new GetSnapshot()); Assert.Single(snapshot.Purchases); Assert.Equal(80,snapshot.Chambers.Single().Total); Assert.Equal(2,snapshot.Purchases.Single().Allocations.Count);
    }
    public void Dispose() { provider.Dispose(); connection.Dispose(); }
}
