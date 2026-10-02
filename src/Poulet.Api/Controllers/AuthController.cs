using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Poulet.Api.Security;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender, IRepository repository, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] Login command, CancellationToken ct)
    {
        var user = await sender.Send(command, ct);
        var entity = (await repository.Find<Poulet.Domain.User>(user.Id, ct))!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("stamp", SessionStamp.Create(entity.PasswordHash))
        };
        if (user.ClientId.HasValue) claims.Add(new("client_id", user.ClientId.Value.ToString()));
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return Ok(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserDto> Me() => Ok(new UserDto(
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        User.Identity!.Name!,
        User.FindFirstValue(ClaimTypes.Role)!,
        int.TryParse(User.FindFirstValue("client_id"), out var id) ? id : null));
}
