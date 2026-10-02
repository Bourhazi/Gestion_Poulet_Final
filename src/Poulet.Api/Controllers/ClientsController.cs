using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Policy = "admin")]
public sealed class ClientsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Save([FromBody] SaveClient command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));
}
