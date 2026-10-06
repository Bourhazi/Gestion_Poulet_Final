using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Poulet.Api.Security;
using Poulet.Application;
using Poulet.Domain;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender, IRepository repository, IAntiforgery antiforgery, JwtTokenService tokens, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    // Accounts are provisioned by an administrator: public self-registration would allow unauthorized client linking.
    [HttpPost("register")]
    [Authorize(Policy = "admin")]
    public async Task<ActionResult<int>> Register([FromBody] AddUser command, CancellationToken ct) => Ok(await sender.Send(command, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] Login command, CancellationToken ct)
    {
        var user = await sender.Send(command, ct);
        var entity = (await repository.Find<User>(user.Id, ct))!;
        await IssueTokens(entity, ct);
        return Ok(user);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<UserDto>> Refresh(CancellationToken ct)
    {
        var rawToken = Request.Cookies["poulet.refresh"];
        if (string.IsNullOrWhiteSpace(rawToken)) return Unauthorized(new { message = "Authentication required." });
        var digest = tokens.HashRefreshToken(rawToken);
        var user = (await repository.List<User>(ct)).SingleOrDefault(u => u.RefreshTokenHash != null &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(u.RefreshTokenHash), Encoding.UTF8.GetBytes(digest)) &&
            u.RefreshTokenExpiresAt > DateTimeOffset.UtcNow);
        if (user == null) { ClearTokens(); return Unauthorized(new { message = "Authentication required." }); }
        await IssueTokens(user, ct);
        return Ok(new UserDto(user.Id, user.Username, user.Role, user.ClientId));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await repository.Find<User>(id, ct);
        if (user != null) { user.RefreshTokenHash = null; user.RefreshTokenExpiresAt = null; await repository.Save(ct); }
        ClearTokens();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserDto> Me() => Ok(new UserDto(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.Identity!.Name!, User.FindFirstValue(ClaimTypes.Role)!, int.TryParse(User.FindFirstValue("client_id"), out var id) ? id : null));

    private async Task IssueTokens(User user, CancellationToken ct)
    {
        var refresh = tokens.CreateRefreshToken();
        user.RefreshTokenHash = tokens.HashRefreshToken(refresh);
        user.RefreshTokenExpiresAt = tokens.RefreshExpiry;
        await repository.Save(ct);
        Response.Cookies.Append("poulet.access", tokens.CreateAccessToken(user), Cookie("/", tokens.AccessLifetime));
        Response.Cookies.Append("poulet.refresh", refresh, Cookie("/api/auth/refresh", tokens.RefreshLifetime));
    }

    private CookieOptions Cookie(string path, TimeSpan lifetime) => new()
    {
        HttpOnly = true, Secure = !environment.IsDevelopment(), SameSite = SameSiteMode.Strict, Path = path,
        MaxAge = lifetime, IsEssential = true
    };

    private void ClearTokens()
    {
        Response.Cookies.Delete("poulet.access", Cookie("/", TimeSpan.Zero));
        Response.Cookies.Delete("poulet.refresh", Cookie("/api/auth/refresh", TimeSpan.Zero));
    }
}
