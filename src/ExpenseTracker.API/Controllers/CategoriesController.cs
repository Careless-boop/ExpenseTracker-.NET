using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Application.Features.Categories.Commands;
using ExpenseTracker.Application.Features.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ISender _mediator;

        public CategoriesController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get categories. If expenseListId is provided, returns list categories. Otherwise returns personal categories.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(
            [FromQuery] Guid? expenseListId = null)
        {
            var result = await _mediator.Send(new GetCategoriesQuery(expenseListId));
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(Guid id)
        {
            var result = await _mediator.Send(new GetCategoryByIdQuery(id));
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateCategory(CreateCategoryCommand command)
        {
            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetCategory), new { id }, new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCategory(Guid id, UpdateCategoryCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest(new { error = "ID mismatch" });
            }

            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCategory(
            Guid id,
            [FromQuery] Guid? reassignToCategoryId = null)
        {
            await _mediator.Send(new DeleteCategoryCommand(id, reassignToCategoryId));
            return NoContent();
        }
    }
}
