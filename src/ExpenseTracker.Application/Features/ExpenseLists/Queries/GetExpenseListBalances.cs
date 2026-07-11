using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Queries
{
    public record GetExpenseListBalancesQuery(Guid ExpenseListId) : IRequest<ExpenseListBalancesDto>;

    public record ExpenseListBalancesDto(
        Guid ExpenseListId,
        string ExpenseListName,
        IReadOnlyList<MemberBalanceDto> MemberBalances,
        IReadOnlyList<DebtDto> SimplifiedDebts,
        decimal TotalExpenses,
        decimal TotalIncome
    );

    public record MemberBalanceDto(
        Guid MemberId,
        string DisplayName,
        bool IsMock,
        decimal Balance,
        decimal TotalPaid,
        decimal TotalShare
    );

    public class GetExpenseListBalancesQueryHandler
        : IRequestHandler<GetExpenseListBalancesQuery, ExpenseListBalancesDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IBalanceCalculationService _balanceCalculation;

        public GetExpenseListBalancesQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IBalanceCalculationService balanceCalculation)
        {
            _context = context;
            _currentUser = currentUser;
            _balanceCalculation = balanceCalculation;
        }

        public async Task<ExpenseListBalancesDto> Handle(
            GetExpenseListBalancesQuery request,
            CancellationToken cancellationToken)
        {
            var membership = await _context.ExpenseListMembers
                .Include(m => m.ExpenseList)
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership?.ExpenseList == null)
            {
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);
            }

            var balances = await _balanceCalculation.CalculateAsync(
                request.ExpenseListId, cancellationToken);

            var memberBalances = balances.Members
                .Select(m => new MemberBalanceDto(
                    m.MemberId,
                    m.DisplayName,
                    m.IsMock,
                    m.Balance,
                    m.TotalPaid,
                    m.TotalShare))
                .ToList();

            return new ExpenseListBalancesDto(
                request.ExpenseListId,
                membership.ExpenseList.Name,
                memberBalances,
                balances.SimplifiedDebts,
                balances.TotalExpenses,
                balances.TotalIncome
            );
        }
    }
}
