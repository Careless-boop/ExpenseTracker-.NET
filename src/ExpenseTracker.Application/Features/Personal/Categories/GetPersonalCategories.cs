using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Personal.Categories
{
    public record GetPersonalCategoriesQuery : IRequest<IReadOnlyList<PersonalCategoryDto>>;

    public class GetPersonalCategoriesQueryHandler
        : IRequestHandler<GetPersonalCategoriesQuery, IReadOnlyList<PersonalCategoryDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetPersonalCategoriesQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<PersonalCategoryDto>> Handle(
            GetPersonalCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            return await _context.PersonalCategories
                .Where(c => c.UserId == _currentUser.UserId)
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Name)
                .Select(c => new PersonalCategoryDto(
                    c.Id,
                    c.Name,
                    c.Icon,
                    c.Color,
                    c.IsDefault,
                    c.Transactions.Count))
                .ToListAsync(cancellationToken);
        }
    }
}
