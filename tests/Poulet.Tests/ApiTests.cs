using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Poulet.Application;

namespace Poulet.Tests;
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string path=Path.Combine(Path.GetTempPath(), $"poulet-tests-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development").ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:Database"]=$"Data Source={path};Foreign Keys=True", ["Logging:LogLevel:Default"]="Warning", ["Jwt:Secret"]="test-secret-that-is-long-enough-for-hs256", ["Jwt:Issuer"]="Poulet.Api", ["Jwt:Audience"]="Poulet.Web" }));
}
public sealed class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;
    public ApiTests(ApiFactory factory) { this.factory=factory; }
    private static async Task Csrf(HttpClient client)
    {
        var token=await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf"); client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",token.GetProperty("token").GetString());
    }
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect=false });
    [Fact] public async Task Anonymous_access_and_missing_csrf_are_rejected()
    {
        using var client=Client(); Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/snapshot")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/auth/login",new Login("admin","admin123"))).StatusCode);
    }
    [Fact] public async Task Login_logout_and_csrf_flow_work()
    {
        using var client=Client(); await Csrf(client);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/auth/login",new Login("admin","admin123"))).StatusCode);
        await Csrf(client);
        var snapshot=await client.GetFromJsonAsync<JsonElement>("/api/snapshot");
        Assert.Equal(new[] { "chambers", "clients", "feed", "purchases", "sales", "suppliers", "users" }, snapshot.EnumerateObject().Select(p=>p.Name).OrderBy(n=>n).ToArray());
        Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsync("/api/auth/logout",null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/snapshot")).StatusCode);
    }
    [Fact] public async Task Invalid_password_is_rejected_without_disclosing_details()
    {
        using var client=Client(); await Csrf(client);
        var response=await client.PostAsJsonAsync("/api/auth/login",new Login("admin","wrong-password"));
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        Assert.Contains("Incorrect username or password",await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task Expired_jwt_is_rejected()
    {
        var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-that-is-long-enough-for-hs256"));
        var expired=new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("Poulet.Api","Poulet.Web",[new Claim(ClaimTypes.NameIdentifier,"1")],expires:DateTime.UtcNow.AddMinutes(-1),signingCredentials:new SigningCredentials(key,SecurityAlgorithms.HmacSha256)));
        using var client=Client(); client.DefaultRequestHeaders.Add("Cookie",$"poulet.access={expired}");
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact] public async Task Grossiste_cannot_access_admin_data_or_other_client_receipts()
    {
        int ownSale,otherSale;
        using(var scope=factory.Services.CreateScope())
        {
            var s=scope.ServiceProvider.GetRequiredService<ISender>();
            var a=await s.Send(new SaveClient(0,"Wholesale A",null,"grossiste")); var b=await s.Send(new SaveClient(0,"Wholesale B",null,"grossiste"));
            await s.Send(new AddUser("grossiste-test","password123","grossiste",a)); var c=await s.Send(new SaveChamber(0,"Test chamber",100));
            await s.Send(new SavePurchase(0,null,new(2026,10,5),100,10,"normal",null,null,null,[new(c,null,100)]));
            ownSale=await s.Send(new AddSale(new(2026,10,5),"grossiste",a,null,c,"normal","vivant",10,20,0,0,null));
            otherSale=await s.Send(new AddSale(new(2026,10,5),"grossiste",b,null,c,"normal","vivant",10,20,0,0,null));
        }
        using var client=Client(); await Csrf(client); (await client.PostAsJsonAsync("/api/auth/login",new Login("grossiste-test","password123"))).EnsureSuccessStatusCode(); await Csrf(client);
        var snapshot=await client.GetFromJsonAsync<Snapshot>("/api/snapshot"); Assert.NotNull(snapshot); Assert.Single(snapshot.Sales); Assert.Equal(0,snapshot.Sales[0].CostOfGoods); Assert.Empty(snapshot.Users); Assert.Empty(snapshot.Purchases); Assert.Empty(snapshot.Clients);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/suppliers",new SaveSupplier(0,"Forbidden",null))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync($"/api/sales/{otherSale}/receipt")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync($"/api/sales/{ownSale}/receipt")).StatusCode);
    }
    [Fact] public async Task Controller_routes_bind_commands_queries_and_delete_requests()
    {
        using var client=Client(); await Csrf(client);
        (await client.PostAsJsonAsync("/api/auth/login",new Login("admin","admin123"))).EnsureSuccessStatusCode(); await Csrf(client);
        var response=await client.PostAsJsonAsync("/api/suppliers",new SaveSupplier(0,"Controller supplier","0612345678"));
        response.EnsureSuccessStatusCode(); var id=await response.Content.ReadFromJsonAsync<int>();
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/reports?from=2026-10-01&to=2026-10-31")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/chambers/1/profit")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/deduction-plan?quantity=0&chickenType=normal")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/reports?from=invalid&to=2026-10-31")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.DeleteAsync($"/api/records/supplier/{id}")).StatusCode);
    }
}
