using FluentValidation;
using ExpenseTracker.Application.Common;
﻿using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Settlements.Commands
{
    public record DeleteSettlementCommand(Guid Id) : IRequest;

    public class DeleteSettlementCommandValidator : AbstractValidator<DeleteSettlementCommand>
    {
        public DeleteSettlementCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class DeleteSettlementCommandHandler : IRequestHandler<DeleteSettlementCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteSettlementCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            DeleteSettlementCommand request,
            CancellationToken cancellationToken)
        {
            var settlement = await _context.Settlements
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (settlement == null)
            {
                throw new NotFoundException(nameof(Settlement), request.Id);
            }

            var membership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == settlement.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership == null)
            {
                throw new ForbiddenException();
            }

            var canDelete = settlement.FromMemberId == membership.Id ||
                            membership.Role == Domain.Enums.ExpenseListRole.Owner;

            if (!canDelete)
            {
                throw new ForbiddenException("Only the settlement creator or list owner can delete this settlement.");
            }

            await _context.EnsureNotClosedAsync(settlement.ExpenseListId, cancellationToken);

            _context.Settlements.Remove(settlement);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
