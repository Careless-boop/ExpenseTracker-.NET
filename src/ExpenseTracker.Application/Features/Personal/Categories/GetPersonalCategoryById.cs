using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Personal.Categories
{
    public record GetPersonalCategoryByIdQuery(Guid Id) : IRequest<PersonalCategoryDto>;

    public class GetPersonalCategoryByIdQueryHandler
        : IRequestHandler<GetPersonalCategoryByIdQuery, PersonalCategoryDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetPersonalCategoryByIdQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PersonalCategoryDto> Handle(
            GetPersonalCategoryByIdQuery request,
            CancellationToken cancellationToken)
        {
            var category = await _context.PersonalCategories
                .Where(c => c.Id == request.Id && c.UserId == _currentUser.UserId)
                .Select(c => new PersonalCategoryDto(
                    c.Id, c.Name, c.Icon, c.Color, c.IsDefault,
                    c.Transactions.Count))
                .FirstOrDefaultAsync(cancellationToken);

            if (category == null)
                throw new NotFoundException(nameof(PersonalCategory), request.Id);

            return category;
        }
    }
}
