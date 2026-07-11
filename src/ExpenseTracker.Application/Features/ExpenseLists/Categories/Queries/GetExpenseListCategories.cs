using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Categories.Queries
{
    public record GetExpenseListCategoriesQuery(Guid ExpenseListId)
        : IRequest<IReadOnlyList<ExpenseListCategoryDto>>;

    public class GetExpenseListCategoriesQueryHandler
        : IRequestHandler<GetExpenseListCategoriesQuery, IReadOnlyList<ExpenseListCategoryDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetExpenseListCategoriesQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<ExpenseListCategoryDto>> Handle(
            GetExpenseListCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            var isMember = await _context.ExpenseListMembers
                .AnyAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (!isMember)
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);

            return await _context.ExpenseListCategories
                .Where(c => c.ExpenseListId == request.ExpenseListId)
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Name)
                .Select(c => new ExpenseListCategoryDto(
                    c.Id,
                    c.ExpenseListId,
                    c.Name,
                    c.Icon,
                    c.Color,
                    c.IsDefault,
                    c.Transactions.Count()
                ))
                .ToListAsync(cancellationToken);
        }
    }
}
