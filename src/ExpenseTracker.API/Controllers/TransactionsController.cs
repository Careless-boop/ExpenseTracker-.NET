using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Application.Features.Transactions;
using ExpenseTracker.Application.Features.Transactions.Commands;
using ExpenseTracker.Application.Features.Transactions.Queries;
using ExpenseTracker.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ISender _mediator;

        public TransactionsController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get transactions with filtering and pagination.
        /// If expenseListId is provided, returns list transactions. Otherwise returns personal transactions.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedList<TransactionDto>>> GetTransactions(
            [FromQuery] Guid? expenseListId = null,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] TransactionType? type = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetTransactionsQuery(
                expenseListId,
                categoryId,
                type,
                fromDate,
                toDate,
                pageNumber,
                pageSize));

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id)
        {
            var result = await _mediator.Send(new GetTransactionByIdQuery(id));
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateTransaction([FromBody] CreateTransactionRequest request)
        {
            var command = new CreateTransactionCommand(
                request.Amount,
                request.Description,
                request.Date,
                request.Type,
                request.CategoryId,
                request.ExpenseListId,
                request.PaidByUserId,
                request.ParticipantUserIds);

            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetTransaction), new { id }, new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateTransactionRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest(new { error = "ID mismatch" });
            }

            var command = new UpdateTransactionCommand(
                request.Id,
                request.Amount,
                request.Description,
                request.Date,
                request.Type,
                request.CategoryId,
                request.PaidByUserId,
                request.ParticipantUserIds);

            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            await _mediator.Send(new DeleteTransactionCommand(id));
            return NoContent();
        }
    }

    public record CreateTransactionRequest(
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid? CategoryId,
        Guid? ExpenseListId,
        string? PaidByUserId,
        List<string>? ParticipantUserIds
    );

    public record UpdateTransactionRequest(
        Guid Id,
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid CategoryId,
        string? PaidByUserId,
        List<string>? ParticipantUserIds
    );
}
