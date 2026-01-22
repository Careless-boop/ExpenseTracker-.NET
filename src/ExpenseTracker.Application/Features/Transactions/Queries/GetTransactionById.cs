using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Transactions.Queries
{
    public record GetTransactionByIdQuery(Guid Id) : IRequest<TransactionDto>;

    public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IIdentityService _identityService;

        public GetTransactionByIdQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IIdentityService identityService)
        {
            _context = context;
            _currentUser = currentUser;
            _identityService = identityService;
        }

        public async Task<TransactionDto> Handle(
            GetTransactionByIdQuery request,
            CancellationToken cancellationToken)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.ExpenseList)
                .Include(t => t.Participants)
                .Where(t => t.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (transaction == null)
            {
                throw new NotFoundException(nameof(Transaction), request.Id);
            }

            if (transaction.IsPersonal)
            {
                if (transaction.CreatedByUserId != _currentUser.UserId)
                {
                    throw new ForbiddenException();
                }
            }
            else
            {
                var isMember = await _context.ExpenseListMembers
                    .AnyAsync(m =>
                        m.ExpenseListId == transaction.ExpenseListId &&
                        m.UserId == _currentUser.UserId,
                        cancellationToken);

                if (!isMember)
                {
                    throw new ForbiddenException();
                }
            }

            var userIds = new List<string> { transaction.PaidByUserId };
            userIds.AddRange(transaction.Participants.Select(p => p.UserId));

            var users = await _identityService.GetUsersByIdsAsync(userIds.Distinct());
            var userMap = users.ToDictionary(u => u.Id, u => u.DisplayName ?? u.UserName);

            var shares = transaction.CalculateShares();

            var participantDtos = transaction.Participants.Select(p => new ParticipantDto(
                p.UserId,
                userMap.GetValueOrDefault(p.UserId),
                p.CustomShareAmount,
                shares.GetValueOrDefault(p.UserId, 0)
            )).ToList();

            return new TransactionDto(
                transaction.Id,
                transaction.Amount,
                transaction.Description,
                transaction.Date,
                transaction.Type,
                transaction.PaidByUserId,
                userMap.GetValueOrDefault(transaction.PaidByUserId),
                transaction.CategoryId,
                transaction.Category.Name,
                transaction.Category.Icon,
                transaction.Category.Color,
                transaction.ExpenseListId,
                transaction.ExpenseList?.Name,
                transaction.CreatedAt,
                participantDtos.Count > 0 ? participantDtos : null,
                shares.Count > 0 ? shares : null
            );
        }
    }
}
