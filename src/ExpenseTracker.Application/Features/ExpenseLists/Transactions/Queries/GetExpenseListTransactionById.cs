using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Transactions.Queries
{
    public record GetExpenseListTransactionByIdQuery(Guid Id) : IRequest<ExpenseListTransactionDto>;

    public class GetExpenseListTransactionByIdQueryHandler
        : IRequestHandler<GetExpenseListTransactionByIdQuery, ExpenseListTransactionDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetExpenseListTransactionByIdQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ExpenseListTransactionDto> Handle(
            GetExpenseListTransactionByIdQuery request,
            CancellationToken cancellationToken)
        {
            var transaction = await _context.ExpenseListTransactions
                .Include(t => t.PaidByMember)
                .Include(t => t.Category)
                .Include(t => t.Participants)
                    .ThenInclude(p => p.Member)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (transaction == null)
                throw new NotFoundException(nameof(ExpenseListTransaction), request.Id);

            var isMember = await _context.ExpenseListMembers
                .AnyAsync(m =>
                    m.ExpenseListId == transaction.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (!isMember)
                throw new NotFoundException(nameof(ExpenseListTransaction), request.Id);

            var shares = transaction.CalculateShares();

            var participants = transaction.Participants
                .Select(p => new ExpenseListParticipantDto(
                    p.MemberId,
                    p.Member.DisplayName,
                    p.CustomShareAmount,
                    shares.GetValueOrDefault(p.MemberId, 0)
                ))
                .ToList();

            return new ExpenseListTransactionDto(
                transaction.Id,
                transaction.ExpenseListId,
                transaction.Amount,
                transaction.Description,
                transaction.Date,
                transaction.Type,
                transaction.PaidByMemberId,
                transaction.PaidByMember.DisplayName,
                transaction.CategoryId,
                transaction.Category?.Name,
                transaction.Category?.Icon,
                transaction.Category?.Color,
                transaction.CreatedAt,
                participants,
                shares
            );
        }
    }
}
