using System.Security.Claims;
using Poulet.Application;

namespace Poulet.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new();
    public int Id => int.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public string Role => Principal.FindFirstValue(ClaimTypes.Role) ?? "";
    public int? ClientId => int.TryParse(Principal.FindFirstValue("client_id"), out var id) ? id : null;
}
