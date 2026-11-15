using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipTrack.Api.Dtos.PricingRules;
using ShipTrack.Api.Services;

namespace ShipTrack.Api.Controllers;

[ApiController]
// SPEC §6 lists the URL as "/pricing-rules" (kebab-case); "[controller]" would
// yield "pricingrules", so the route is spelled out explicitly.
[Route("api/pricing-rules")]
[Authorize(Roles = "Dispatcher")]
public class PricingRulesController : ControllerBase
{
    private readonly IPricingRuleService _pricingRuleService;

    public PricingRulesController(IPricingRuleService pricingRuleService)
    {
        _pricingRuleService = pricingRuleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PricingRuleResponse>>> GetAll(CancellationToken ct)
    {
        var rules = await _pricingRuleService.GetAllAsync(ct);
        return Ok(rules);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PricingRuleResponse>> GetById(int id, CancellationToken ct)
    {
        var rule = await _pricingRuleService.GetByIdAsync(id, ct);
        return Ok(rule);
    }

    [HttpPost]
    public async Task<ActionResult<PricingRuleResponse>> Create(PricingRuleRequest request, CancellationToken ct)
    {
        var rule = await _pricingRuleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, rule);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PricingRuleResponse>> Update(int id, PricingRuleRequest request, CancellationToken ct)
    {
        var rule = await _pricingRuleService.UpdateAsync(id, request, ct);
        return Ok(rule);
    }
}