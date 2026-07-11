using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Personal.Transactions
{
    public record GetPersonalTransactionByIdQuery(Guid Id) : IRequest<PersonalTransactionDto>;

    public class GetPersonalTransactionByIdQueryHandler
        : IRequestHandler<GetPersonalTransactionByIdQuery, PersonalTransactionDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetPersonalTransactionByIdQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PersonalTransactionDto> Handle(
            GetPersonalTransactionByIdQuery request,
            CancellationToken cancellationToken)
        {
            var dto = await _context.PersonalTransactions
                .Where(t => t.Id == request.Id && t.UserId == _currentUser.UserId)
                .Select(t => new PersonalTransactionDto(
                    t.Id,
                    t.Amount,
                    t.Description,
                    t.Date,
                    t.Type,
                    t.CategoryId,
                    t.Category != null ? t.Category.Name : null,
                    t.Category != null ? t.Category.Icon : null,
                    t.Category != null ? t.Category.Color : null,
                    t.CreatedAt))
                .FirstOrDefaultAsync(cancellationToken);

            if (dto == null)
                throw new NotFoundException(nameof(PersonalTransaction), request.Id);

            return dto;
        }
    }
}
