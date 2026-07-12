using ExpenseTracker.Application.Features.Dashboard;
using ExpenseTracker.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ISender _mediator;

        public DashboardController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Income, expenses and net for a period, against the preceding period of equal length.
        /// Defaults to the current month.
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var result = await _mediator.Send(new GetDashboardSummaryQuery(from, to));
            return Ok(result);
        }

        /// <summary>
        /// Spending broken down by category. Defaults to expenses in the current month.
        /// </summary>
        [HttpGet("by-category")]
        public async Task<ActionResult<CategoryBreakdownDto>> GetByCategory(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] TransactionType type = TransactionType.Expense)
        {
            var result = await _mediator.Send(new GetSpendingByCategoryQuery(from, to, type));
            return Ok(result);
        }
    }
}
