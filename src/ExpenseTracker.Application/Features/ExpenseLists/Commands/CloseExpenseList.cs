using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace ExpenseTracker.Application.Features.ExpenseLists.Commands
{
    /// <summary>
    /// Closes a list and projects it into each member's personal ledger: one expense equal to what
    /// they actually consumed, filed under a category named after the list. Members who have opted
    /// out via UserSettings, and mock members (who have no ledger), are skipped.
    /// </summary>
    public record CloseExpenseListCommand(Guid ExpenseListId) : IRequest;

    public class CloseExpenseListCommandValidator : AbstractValidator<CloseExpenseListCommand>
    {
        public CloseExpenseListCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
        }
    }

    public class CloseExpenseListCommandHandler : IRequestHandler<CloseExpenseListCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IBalanceCalculationService _balanceCalculation;

        public CloseExpenseListCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IBalanceCalculationService balanceCalculation)
        {
            _context = context;
            _currentUser = currentUser;
            _balanceCalculation = balanceCalculation;
        }

        public async Task Handle(
            CloseExpenseListCommand request,
            CancellationToken cancellationToken)
        {
            var membership = await _context.ExpenseListMembers
                .Include(m => m.ExpenseList)
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership?.ExpenseList == null)
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);

            if (membership.Role != ExpenseListRole.Owner)
                throw new ForbiddenException("Only the owner can close an expense list.");

            var expenseList = membership.ExpenseList;

            if (expenseList.IsClosed)
                throw new ValidationException([new ValidationFailure(
                    nameof(request.ExpenseListId), "This expense list is already closed.")]);

            var balances = await _balanceCalculation.CalculateAsync(request.ExpenseListId, cancellationToken);

            var memberUserIds = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == request.ExpenseListId && m.UserId != null)
                .ToDictionaryAsync(m => m.Id, m => m.UserId!, cancellationToken);

            var optedInUserIds = await GetOptedInUserIdsAsync(memberUserIds.Values, cancellationToken);

            var closedAt = DateTime.UtcNow;

            await using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);

            foreach (var member in balances.Members)
            {
                if (!memberUserIds.TryGetValue(member.MemberId, out var userId))
                    continue;

                if (!optedInUserIds.Contains(userId))
                    continue;

                if (member.TotalExpenseShare <= 0)
                    continue;

                var category = await GetOrCreateListCategoryAsync(
                    userId, expenseList, cancellationToken);

                _context.PersonalTransactions.Add(new PersonalTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Amount = member.TotalExpenseShare,
                    Description = $"Your share of \"{expenseList.Name}\"",
                    Date = closedAt,
                    Type = TransactionType.Expense,
                    CategoryId = category.Id,
                    SourceExpenseListId = expenseList.Id
                });
            }

            expenseList.ClosedAt = closedAt;
            expenseList.ClosedByUserId = _currentUser.UserId;

            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
        }

        /// <summary>Users with no settings row yet are opted in — the default is on.</summary>
        private async Task<HashSet<string>> GetOptedInUserIdsAsync(
            IEnumerable<string> userIds,
            CancellationToken cancellationToken)
        {
            var ids = userIds.Distinct().ToList();

            var optedOut = await _context.UserSettings
                .Where(s => ids.Contains(s.UserId) && !s.SyncClosedListsToPersonal)
                .Select(s => s.UserId)
                .ToListAsync(cancellationToken);

            return ids.Except(optedOut).ToHashSet();
        }

        private async Task<PersonalCategory> GetOrCreateListCategoryAsync(
            string userId,
            ExpenseList expenseList,
            CancellationToken cancellationToken)
        {
            var existing = await _context.PersonalCategories
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.SourceExpenseListId == expenseList.Id,
                    cancellationToken);

            if (existing != null)
                return existing;

            var category = new PersonalCategory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = expenseList.Name,
                Icon = "👥",
                Color = "#6366F1",
                IsDefault = false,
                SourceExpenseListId = expenseList.Id
            };

            _context.PersonalCategories.Add(category);

            return category;
        }
    }
}
