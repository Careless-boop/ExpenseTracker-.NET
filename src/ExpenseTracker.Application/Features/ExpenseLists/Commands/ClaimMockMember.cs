using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace ExpenseTracker.Application.Features.ExpenseLists.Commands
{
    /// <summary>
    /// Merges the caller's membership into a mock member slot, so the history already recorded
    /// against the placeholder becomes theirs.
    ///
    /// Membership is a precondition, not a disqualifier: an owner adds you first, then you say which
    /// placeholder you are. Without that check this is an unauthenticated join — anyone holding a
    /// list id and a mock member id could attach themselves to a list they were never invited to.
    /// </summary>
    public record ClaimMockMemberCommand(
        Guid ExpenseListId,
        Guid MockMemberId
    ) : IRequest;

    public class ClaimMockMemberCommandValidator : AbstractValidator<ClaimMockMemberCommand>
    {
        public ClaimMockMemberCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.MockMemberId).NotEmpty();
        }
    }

    public class ClaimMockMemberCommandHandler : IRequestHandler<ClaimMockMemberCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ClaimMockMemberCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            ClaimMockMemberCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUser.UserId!;

            var callerMembership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == currentUserId,
                    cancellationToken);

            if (callerMembership == null)
                throw new ForbiddenException(
                    "You must be a member of this expense list before you can claim a placeholder member.");

            var mockMember = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.Id == request.MockMemberId &&
                    m.ExpenseListId == request.ExpenseListId,
                    cancellationToken);

            if (mockMember == null)
                throw new NotFoundException(nameof(ExpenseListMember), request.MockMemberId);

            if (!mockMember.IsMock)
                throw new ValidationException([new ValidationFailure(
                    nameof(request.MockMemberId),
                    "This slot is already claimed by a registered user")]);

            // Both members taking a share of one expense would merge into a duplicate participant.
            var sharesATransaction = await _context.ExpenseListTransactionParticipants
                .Where(p => p.MemberId == callerMembership.Id)
                .Select(p => p.TransactionId)
                .Intersect(_context.ExpenseListTransactionParticipants
                    .Where(p => p.MemberId == mockMember.Id)
                    .Select(p => p.TransactionId))
                .AnyAsync(cancellationToken);

            if (sharesATransaction)
                throw new ValidationException([new ValidationFailure(
                    nameof(request.MockMemberId),
                    "Cannot claim this member: you and the placeholder both take a share of the same transaction.")]);

            await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

            // The mock row is the one carrying the history, so it survives and the caller's
            // (usually empty) row is folded into it.
            await _context.ExpenseListTransactions
                .Where(t => t.PaidByMemberId == callerMembership.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.PaidByMemberId, mockMember.Id), cancellationToken);

            await _context.ExpenseListTransactionParticipants
                .Where(p => p.MemberId == callerMembership.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.MemberId, mockMember.Id), cancellationToken);

            await _context.Settlements
                .Where(s => s.FromMemberId == callerMembership.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.FromMemberId, mockMember.Id), cancellationToken);

            await _context.Settlements
                .Where(s => s.ToMemberId == callerMembership.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ToMemberId, mockMember.Id), cancellationToken);

            // DisplayName stays as-is: it is the name the list already knows this person by.
            mockMember.UserId = currentUserId;
            mockMember.Email = callerMembership.Email;
            mockMember.Role = callerMembership.Role;
            mockMember.JoinedAt = callerMembership.JoinedAt;

            _context.ExpenseListMembers.Remove(callerMembership);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
