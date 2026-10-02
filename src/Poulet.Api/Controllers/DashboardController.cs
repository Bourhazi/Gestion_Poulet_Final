using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/snapshot")]
[Authorize]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Snapshot>> Get(CancellationToken ct)
        => Ok(await sender.Send(new GetSnapshot(), ct));
}
