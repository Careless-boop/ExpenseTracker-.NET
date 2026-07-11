using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Application.Features.ExpenseLists.Transactions;
using ExpenseTracker.Application.Features.ExpenseLists.Transactions.Commands;
using ExpenseTracker.Application.Features.ExpenseLists.Transactions.Queries;
using ExpenseTracker.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/expense-lists/{expenseListId:guid}/transactions")]
    [Authorize]
    public class ExpenseListTransactionsController : ControllerBase
    {
        private readonly ISender _mediator;

        public ExpenseListTransactionsController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedList<ExpenseListTransactionDto>>> GetTransactions(
            Guid expenseListId,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] TransactionType? type = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetExpenseListTransactionsQuery(
                expenseListId, categoryId, type, fromDate, toDate, pageNumber, pageSize));
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ExpenseListTransactionDto>> GetTransaction(
            Guid expenseListId, Guid id)
        {
            var result = await _mediator.Send(new GetExpenseListTransactionByIdQuery(id));
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateTransaction(
            Guid expenseListId,
            [FromBody] CreateExpenseListTransactionRequest request)
        {
            var id = await _mediator.Send(new CreateExpenseListTransactionCommand(
                expenseListId,
                request.Amount,
                request.Description,
                request.Date,
                request.Type,
                request.PaidByMemberId,
                request.CategoryId,
                request.Participants));

            return CreatedAtAction(nameof(GetTransaction), new { expenseListId, id }, new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTransaction(
            Guid expenseListId,
            Guid id,
            [FromBody] UpdateExpenseListTransactionRequest request)
        {
            if (id != request.Id)
                return BadRequest(new { error = "ID mismatch" });

            await _mediator.Send(new UpdateExpenseListTransactionCommand(
                request.Id,
                request.Amount,
                request.Description,
                request.Date,
                request.Type,
                request.PaidByMemberId,
                request.CategoryId,
                request.Participants));

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTransaction(Guid expenseListId, Guid id)
        {
            await _mediator.Send(new DeleteExpenseListTransactionCommand(id));
            return NoContent();
        }
    }

    public record CreateExpenseListTransactionRequest(
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid PaidByMemberId,
        Guid? CategoryId,
        IReadOnlyList<ParticipantInput>? Participants
    );

    public record UpdateExpenseListTransactionRequest(
        Guid Id,
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid PaidByMemberId,
        Guid? CategoryId,
        IReadOnlyList<ParticipantInput>? Participants
    );
}
