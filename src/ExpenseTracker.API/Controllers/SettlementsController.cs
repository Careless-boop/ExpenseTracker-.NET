using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Application.Features.Settlements;
using ExpenseTracker.Application.Features.Settlements.Commands;
using ExpenseTracker.Application.Features.Settlements.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/expense-lists/{expenseListId:guid}/settlements")]
    [Authorize]
    public class SettlementsController : ControllerBase
    {
        private readonly ISender _mediator;

        public SettlementsController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all settlements for an expense list
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedList<SettlementDto>>> GetSettlements(
            Guid expenseListId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetSettlementsQuery(expenseListId, pageNumber, pageSize));
            return Ok(result);
        }

        /// <summary>
        /// Create a settlement. Defaults to "I paid ToMemberId"; Editors/Owners may pass
        /// FromMemberId to record a payment made by a mock member.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Guid>> CreateSettlement(
            Guid expenseListId,
            [FromBody] CreateSettlementRequest request)
        {
            var id = await _mediator.Send(new CreateSettlementCommand(
                expenseListId,
                request.ToMemberId,
                request.Amount,
                request.Note,
                request.FromMemberId));

            return Created($"/api/v1/expense-lists/{expenseListId}/settlements/{id}", new { id });
        }

        /// <summary>
        /// Delete a settlement (Creator or Owner only)
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteSettlement(Guid expenseListId, Guid id)
        {
            await _mediator.Send(new DeleteSettlementCommand(id));
            return NoContent();
        }
    }

    public record CreateSettlementRequest(
        Guid ToMemberId,
        decimal Amount,
        string? Note = null,
        Guid? FromMemberId = null);
}
