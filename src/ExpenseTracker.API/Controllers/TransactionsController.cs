using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Application.Features.Personal;
using ExpenseTracker.Application.Features.Personal.Transactions;
using ExpenseTracker.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/personal/transactions")]
    [Authorize]
    public class PersonalTransactionsController : ControllerBase
    {
        private readonly ISender _mediator;

        public PersonalTransactionsController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedList<PersonalTransactionDto>>> GetTransactions(
            [FromQuery] Guid? categoryId = null,
            [FromQuery] TransactionType? type = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetPersonalTransactionsQuery(
                categoryId, type, fromDate, toDate, pageNumber, pageSize));
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PersonalTransactionDto>> GetTransaction(Guid id)
        {
            var result = await _mediator.Send(new GetPersonalTransactionByIdQuery(id));
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateTransaction(
            [FromBody] CreatePersonalTransactionCommand command)
        {
            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetTransaction), new { id }, new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTransaction(
            Guid id,
            [FromBody] UpdatePersonalTransactionCommand command)
        {
            if (id != command.Id)
                return BadRequest(new { error = "ID mismatch" });

            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            await _mediator.Send(new DeletePersonalTransactionCommand(id));
            return NoContent();
        }
    }
}
