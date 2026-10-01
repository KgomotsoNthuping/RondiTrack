using Api.DTO;
using Api.Mappings;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/stokvels/{stokvelId:guid}/cycles/{cycleId:guid}/payout")]
public class PayoutsController : ControllerBase
{
    private readonly IPayoutService _payoutService;

    public PayoutsController(IPayoutService payoutService)
    {
        _payoutService = payoutService;
    }

    /// <summary>
    /// Processes the payout for a contribution cycle.
    /// </summary>
    /// <remarks>
    /// RondiTrack determines the next recipient from
    /// the stokvel's rotation order. The payout and
    /// contribution-cycle status update are committed
    /// in one database transaction.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(PayoutResponse),StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound,"application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status409Conflict,"application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status422UnprocessableEntity,"application/problem+json")]
    public async Task<ActionResult<PayoutResponse>> ProcessPayout(Guid stokvelId, Guid cycleId)
    {
        var payout = await _payoutService.ProcessPayoutAsync(stokvelId, cycleId);

        return StatusCode(
            StatusCodes.Status201Created,
            payout.ToResponse());
    }
}