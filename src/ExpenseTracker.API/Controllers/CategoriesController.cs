using ExpenseTracker.Application.Features.Personal;
using ExpenseTracker.Application.Features.Personal.Categories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/personal/categories")]
    [Authorize]
    public class PersonalCategoriesController : ControllerBase
    {
        private readonly ISender _mediator;

        public PersonalCategoriesController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<PersonalCategoryDto>>> GetCategories()
        {
            var result = await _mediator.Send(new GetPersonalCategoriesQuery());
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PersonalCategoryDto>> GetCategory(Guid id)
        {
            var result = await _mediator.Send(new GetPersonalCategoryByIdQuery(id));
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateCategory(
            [FromBody] CreatePersonalCategoryCommand command)
        {
            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetCategory), new { id }, new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCategory(
            Guid id,
            [FromBody] UpdatePersonalCategoryCommand command)
        {
            if (id != command.Id)
                return BadRequest(new { error = "ID mismatch" });

            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCategory(
            Guid id,
            [FromQuery] Guid? reassignToCategoryId = null)
        {
            await _mediator.Send(new DeletePersonalCategoryCommand(id, reassignToCategoryId));
            return NoContent();
        }
    }
}
