using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poulet.Application;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/records")]
[Authorize(Policy = "admin")]
public sealed class RecordsController(ISender sender) : ControllerBase
{
    [HttpDelete("{kind}/{id:int}")]
    public async Task<ActionResult<bool>> Delete([FromRoute] string kind, [FromRoute] int id, [FromQuery] int? parentId, CancellationToken ct)
        => Ok(await sender.Send(new DeleteEntity(kind, id, parentId), ct));
}
