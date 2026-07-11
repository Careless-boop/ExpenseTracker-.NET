using ExpenseTracker.Application.Features.ExpenseLists.Categories;
using ExpenseTracker.Application.Features.ExpenseLists.Categories.Commands;
using ExpenseTracker.Application.Features.ExpenseLists.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/expense-lists/{expenseListId:guid}/categories")]
    [Authorize]
    public class ExpenseListCategoriesController : ControllerBase
    {
        private readonly ISender _mediator;

        public ExpenseListCategoriesController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ExpenseListCategoryDto>>> GetCategories(
            Guid expenseListId)
        {
            var result = await _mediator.Send(new GetExpenseListCategoriesQuery(expenseListId));
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateCategory(
            Guid expenseListId,
            [FromBody] CreateExpenseListCategoryRequest request)
        {
            var id = await _mediator.Send(new CreateExpenseListCategoryCommand(
                expenseListId, request.Name, request.Icon, request.Color));
            return Created(
                $"/api/v1/expense-lists/{expenseListId}/categories/{id}",
                new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCategory(
            Guid expenseListId,
            Guid id,
            [FromBody] UpdateExpenseListCategoryRequest request)
        {
            await _mediator.Send(new UpdateExpenseListCategoryCommand(id, request.Name, request.Icon, request.Color));
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCategory(
            Guid expenseListId,
            Guid id,
            [FromQuery] Guid? reassignToCategoryId = null)
        {
            await _mediator.Send(new DeleteExpenseListCategoryCommand(id, reassignToCategoryId));
            return NoContent();
        }
    }

    public record CreateExpenseListCategoryRequest(string Name, string? Icon, string? Color);
    public record UpdateExpenseListCategoryRequest(string Name, string? Icon, string? Color);
}
