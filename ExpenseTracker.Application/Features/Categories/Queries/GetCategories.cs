using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Categories.Queries
{
    public record GetCategoriesQuery(Guid? ExpenseListId = null) : IRequest<IReadOnlyList<CategoryDto>>;

    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetCategoriesQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<CategoryDto>> Handle(
            GetCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Categories.AsQueryable();

            if (request.ExpenseListId.HasValue)
            {
                query = query.Where(c => c.ExpenseListId == request.ExpenseListId.Value);
            }
            else
            {
                query = query.Where(c => c.UserId == _currentUser.UserId && c.ExpenseListId == null);
            }

            return await query
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryDto(
                    c.Id,
                    c.Name,
                    c.Icon,
                    c.Color,
                    c.IsDefault,
                    c.ExpenseListId,
                    c.Transactions.Count(t => !t.IsDeleted)))
                .ToListAsync(cancellationToken);
        }
    }
}
