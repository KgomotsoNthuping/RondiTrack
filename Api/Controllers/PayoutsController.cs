using Api.DTO;
using Api.Mappings;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Api.Auth;
using Api.Common;

namespace Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 +"api/stokvels/{stokvelId:guid}/cycles/{cycleId:guid}/payout")]
public class PayoutsController : ControllerBase
{
    private readonly IPayoutService _payoutService;

    private readonly StokvelAccessAuthorizer _access;

    public PayoutsController(IPayoutService payoutService, StokvelAccessAuthorizer access)
    {
        _payoutService = payoutService;
        _access = access;
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
        if (!await _access.AuthorizeAsync(
        User,
        stokvelId,
        Policies.StokvelAdmin))
        {
            return Forbid();
        }
        var payout = await _payoutService.ProcessPayoutAsync(stokvelId, cycleId);

        return StatusCode(
            StatusCodes.Status201Created,
            payout.ToResponse());
    }
}