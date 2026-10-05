using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public sealed class SalesController(ISender sender) : ControllerBase
{
    [HttpPost("/api/monday-transfers")]
    [Authorize(Policy = "admin")]
    public async Task<ActionResult<int>> TransferMondayStock([FromBody] TransferMondayStock command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));
    [HttpPost]
    [Authorize(Policy = "admin")]
    public async Task<ActionResult<int>> Add([FromBody] AddSale command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));

    [HttpGet("{id:int}/receipt")]
    public async Task<IActionResult> Receipt([FromRoute] int id, [FromQuery] int? lineId, CancellationToken ct)
        => Ok(await sender.Send(new GetReceipt(id, lineId), ct));

    [HttpGet("/api/deduction-plan")]
    [Authorize(Policy = "admin")]
    public async Task<IActionResult> DeductionPlan([FromQuery] decimal quantity, [FromQuery] string chickenType, CancellationToken ct)
        => Ok(await sender.Send(new GetDeductionPlan(quantity, chickenType), ct));

    [HttpPost("/api/monday-lines")]
    [Authorize(Policy = "admin")]
    public async Task<ActionResult<int>> AddMondayLine([FromBody] AddMondayLine command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));

    [HttpPost("/api/payments")]
    [Authorize(Policy = "admin")]
    public async Task<ActionResult<bool>> TogglePayment([FromBody] TogglePayment command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));
    [HttpPost("/api/sales/payment-status")]
    [Authorize(Policy = "admin")]
    public async Task<ActionResult> SetPaymentStatus([FromBody] SetSalePaymentStatus command, CancellationToken ct)
        => Ok(new { paymentStatus = await sender.Send(command, ct) });
}
