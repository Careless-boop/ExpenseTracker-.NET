using ExpenseTracker.Application.Features.Settings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/settings")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ISender _mediator;

        public SettingsController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get the current user's account settings
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<UserSettingsDto>> GetSettings()
        {
            var result = await _mediator.Send(new GetUserSettingsQuery());
            return Ok(result);
        }

        /// <summary>
        /// Update the current user's account settings
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<UserSettingsDto>> UpdateSettings(
            [FromBody] UpdateUserSettingsCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
