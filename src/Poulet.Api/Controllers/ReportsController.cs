using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "admin")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => Ok(await sender.Send(new GetReport(from, to), ct));
}
