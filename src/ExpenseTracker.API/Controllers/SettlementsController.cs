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
        public async Task<ActionResult<IReadOnlyList<SettlementDto>>> GetSettlements(
            Guid expenseListId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetSettlementsQuery(expenseListId, pageNumber, pageSize));
            return Ok(result);
        }

        /// <summary>
        /// Create a settlement (record that current user paid someone)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Guid>> CreateSettlement(
            Guid expenseListId,
            [FromBody] CreateSettlementRequest request)
        {
            var command = new CreateSettlementCommand(
                expenseListId,
                request.ToUserId,
                request.Amount,
                request.Note);

            var id = await _mediator.Send(command);
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

    public record CreateSettlementRequest(string ToUserId, decimal Amount, string? Note = null);
}
