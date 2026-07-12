using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Queries
{
    public record GetExpenseListByIdQuery(Guid Id) : IRequest<ExpenseListDetailDto>;

    public class GetExpenseListByIdQueryHandler : IRequestHandler<GetExpenseListByIdQuery, ExpenseListDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetExpenseListByIdQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ExpenseListDetailDto> Handle(
            GetExpenseListByIdQuery request,
            CancellationToken cancellationToken)
        {
            var membership = await _context.ExpenseListMembers
                .Include(m => m.ExpenseList)
                    .ThenInclude(e => e.Members)
                .Include(m => m.ExpenseList)
                    .ThenInclude(e => e.Transactions)
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.Id &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            // ExpenseList can be null even when the membership row survives: the query filter hides a
            // soft-deleted list, and dereferencing it here used to NRE into a 500.
            if (membership?.ExpenseList == null)
            {
                throw new NotFoundException(nameof(ExpenseList), request.Id);
            }

            var expenseList = membership.ExpenseList;

            var memberDtos = expenseList.Members
                .OrderByDescending(m => m.Role)
                .ThenBy(m => m.JoinedAt)
                .Select(m => new ExpenseListMemberDto(
                    m.Id,
                    m.DisplayName,
                    m.UserId,
                    m.Email,
                    m.Role,
                    m.JoinedAt,
                    m.IsMock
                ))
                .ToList();

            var totalExpenses = expenseList.Transactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            var totalIncome = expenseList.Transactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            return new ExpenseListDetailDto(
                expenseList.Id,
                expenseList.Name,
                expenseList.Description,
                expenseList.CoverImage,
                memberDtos,
                expenseList.Transactions.Count,
                totalExpenses,
                totalIncome,
                membership.Role,
                expenseList.CreatedAt,
                expenseList.ClosedAt
            );
        }
    }
}
