using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;
[ApiController, Route("api/audit"), Authorize(Policy = "admin")]
public sealed class AuditController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] DateOnly? date, [FromQuery] int? userId, [FromQuery] string? action, [FromQuery] string? module, [FromQuery] string? entityType, CancellationToken ct)
        => Ok(await sender.Send(new GetAuditLogs(date, userId, action, module, entityType), ct));
}
