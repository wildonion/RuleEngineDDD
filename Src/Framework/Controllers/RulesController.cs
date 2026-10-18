using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Controllers;

[ApiController]
[Route("api/devices/{deviceId}/rules")]
public sealed class RulesController(CreateRuleService createRule) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(string deviceId, [FromBody] CreateRuleRequest request, CancellationToken ct)
    {
        try { return Created("", await createRule.ExecuteAsync(deviceId, request, ct)); }
        catch (FluentValidation.ValidationException ex)
        { return BadRequest(new { errors = ex.Errors.Select(x => new { x.PropertyName, x.ErrorMessage }) }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
