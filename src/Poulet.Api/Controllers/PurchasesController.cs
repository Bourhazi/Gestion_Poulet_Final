using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/purchases")]
[Authorize(Policy = "admin")]
public sealed class PurchasesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Save([FromBody] SavePurchase command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));
}
