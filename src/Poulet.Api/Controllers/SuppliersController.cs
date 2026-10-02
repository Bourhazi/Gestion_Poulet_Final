using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Policy = "admin")]
public sealed class SuppliersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Save([FromBody] SaveSupplier command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));
}
