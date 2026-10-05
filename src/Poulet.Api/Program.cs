using System.Security.Claims;
using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Poulet.Api.Security;
using Poulet.Application;
using Poulet.Domain;
using Poulet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddHttpClient("openai", client => client.Timeout = TimeSpan.FromSeconds(45));
builder.Services.AddHttpContextAccessor(); builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddMediatR(c => { c.RegisterServicesFromAssemblyContaining<GetSnapshot>(); c.AddOpenBehavior(typeof(TransactionBehavior<,>)); c.AddOpenBehavior(typeof(AuditBehavior<,>)); });
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "poulet.session"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
    o.Events.OnValidatePrincipal = async c =>
    {
        var db = c.HttpContext.RequestServices.GetRequiredService<IRepository>();
        var id = int.TryParse(c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : 0;
        var u = await db.Find<User>(id, c.HttpContext.RequestAborted);
        if (u == null || c.Principal?.FindFirstValue("stamp") != SessionStamp.Create(u.PasswordHash)) { c.RejectPrincipal(); await c.HttpContext.SignOutAsync(); }
    };
});
builder.Services.AddAuthorization(o => o.AddPolicy("admin", p => p.RequireRole("admin")));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("assistant", c => RateLimitPartition.GetFixedWindowLimiter(c.User.Identity?.Name ?? c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
await DependencyInjection.InitializeDatabase(app.Services, app.Environment.IsDevelopment(), app.Configuration);
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (BusinessException e) { ctx.Response.StatusCode = 400; await ctx.Response.WriteAsJsonAsync(new { message = e.Message }); }
    catch (BadHttpRequestException) { ctx.Response.StatusCode = 400; await ctx.Response.WriteAsJsonAsync(new { message = "Invalid request data." }); }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException) { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new { message = "This change conflicts with existing records." }); }
});
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseDefaultFiles(); app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Request.Method is "POST" or "PUT" or "DELETE")
    {
        try { await ctx.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(ctx); }
        catch (AntiforgeryValidationException) { ctx.Response.StatusCode = 400; await ctx.Response.WriteAsJsonAsync(new { message = "Refresh the page and retry (invalid CSRF token)." }); return; }
    }
    await next();
});
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program;
