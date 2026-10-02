using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = "admin")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpPost("product-purchases")]
    public async Task<ActionResult<int>> AddPurchase([FromBody] AddProductPurchase command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));

    [HttpPost("product-sales")]
    public async Task<ActionResult<int>> SaveSale([FromBody] SaveProductSale command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));
}
