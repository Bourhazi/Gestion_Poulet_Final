using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/chambers")]
[Authorize(Policy = "admin")]
public sealed class ChambersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Save([FromBody] SaveChamber command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));

    [HttpGet("{id:int}/profit")]
    public async Task<IActionResult> Profit([FromRoute] int id, CancellationToken ct)
        => Ok(await sender.Send(new GetChamberProfit(id), ct));
}
