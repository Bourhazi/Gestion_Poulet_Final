using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/feed")]
[Authorize(Policy = "admin")]
public sealed class FeedController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Add([FromBody] AddFeed command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));
}
