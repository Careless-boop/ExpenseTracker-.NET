using ExpenseTracker.Application.Features.ExpenseLists;
using ExpenseTracker.Application.Features.ExpenseLists.Commands;
using ExpenseTracker.Application.Features.ExpenseLists.Queries;
using ExpenseTracker.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/expense-lists")]
    [Authorize]
    public class ExpenseListsController : ControllerBase
    {
        private readonly ISender _mediator;

        public ExpenseListsController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all expense lists the current user is a member of
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ExpenseListDto>>> GetExpenseLists()
        {
            var result = await _mediator.Send(new GetExpenseListsQuery());
            return Ok(result);
        }

        /// <summary>
        /// Get expense list details by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ExpenseListDetailDto>> GetExpenseList(Guid id)
        {
            var result = await _mediator.Send(new GetExpenseListByIdQuery(id));
            return Ok(result);
        }

        /// <summary>
        /// Get balances and simplified debts for an expense list
        /// </summary>
        [HttpGet("{id:guid}/balances")]
        public async Task<ActionResult<ExpenseListBalancesDto>> GetExpenseListBalances(Guid id)
        {
            var result = await _mediator.Send(new GetExpenseListBalancesQuery(id));
            return Ok(result);
        }

        /// <summary>
        /// Create a new expense list
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Guid>> CreateExpenseList(CreateExpenseListCommand command)
        {
            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetExpenseList), new { id }, new { id });
        }

        /// <summary>
        /// Update expense list details (Owner only)
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateExpenseList(Guid id, UpdateExpenseListCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest(new { error = "ID mismatch" });
            }

            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Delete an expense list (Owner only)
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteExpenseList(Guid id)
        {
            await _mediator.Send(new DeleteExpenseListCommand(id));
            return NoContent();
        }

        /// <summary>
        /// Add a registered user as a member (Owner only)
        /// </summary>
        [HttpPost("{id:guid}/members")]
        public async Task<ActionResult<Guid>> AddMember(
            Guid id,
            [FromBody] AddMemberRequest request)
        {
            var memberId = await _mediator.Send(new AddExpenseListMemberCommand(id, request.Email, request.Role));
            return CreatedAtAction(nameof(GetExpenseList), new { id }, new { memberId });
        }

        /// <summary>
        /// Add a mock (non-registered) member placeholder (Editor/Owner)
        /// </summary>
        [HttpPost("{id:guid}/mock-members")]
        public async Task<ActionResult<Guid>> AddMockMember(
            Guid id,
            [FromBody] AddMockMemberRequest request)
        {
            var memberId = await _mediator.Send(new AddMockExpenseListMemberCommand(id, request.DisplayName));
            return CreatedAtAction(nameof(GetExpenseList), new { id }, new { memberId });
        }

        /// <summary>
        /// Claim a mock member slot (current user takes over that placeholder)
        /// </summary>
        [HttpPost("{id:guid}/claim/{mockMemberId:guid}")]
        public async Task<IActionResult> ClaimMockMember(Guid id, Guid mockMemberId)
        {
            await _mediator.Send(new ClaimMockMemberCommand(id, mockMemberId));
            return NoContent();
        }

        /// <summary>
        /// Update a member's role (Owner only)
        /// </summary>
        [HttpPut("{id:guid}/members/{memberId:guid}")]
        public async Task<IActionResult> UpdateMemberRole(
            Guid id,
            Guid memberId,
            [FromBody] UpdateMemberRoleRequest request)
        {
            await _mediator.Send(new UpdateExpenseListMemberRoleCommand(id, memberId, request.Role));
            return NoContent();
        }

        /// <summary>
        /// Remove a member (Owner only, or self to leave)
        /// </summary>
        [HttpDelete("{id:guid}/members/{memberId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
        {
            await _mediator.Send(new RemoveExpenseListMemberCommand(id, memberId));
            return NoContent();
        }
    }

    public record AddMemberRequest(string Email, ExpenseListRole Role = ExpenseListRole.Editor);
    public record AddMockMemberRequest(string DisplayName);
    public record UpdateMemberRoleRequest(ExpenseListRole Role);
}
