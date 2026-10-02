using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "admin")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Add([FromBody] AddUser command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));

    [HttpPost("/api/passwords")]
    public async Task<ActionResult<bool>> ResetPassword([FromBody] ResetPassword command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));
}
