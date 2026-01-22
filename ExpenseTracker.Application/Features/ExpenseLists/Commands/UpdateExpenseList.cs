using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Commands
{
    public record UpdateExpenseListCommand(
        Guid Id,
        string Name,
        string? Description,
        string? CoverImage
    ) : IRequest;

    public class UpdateExpenseListCommandValidator : AbstractValidator<UpdateExpenseListCommand>
    {
        public UpdateExpenseListCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).When(x => x.Description != null);

            RuleFor(x => x.CoverImage)
                .MaximumLength(500).When(x => x.CoverImage != null);
        }
    }

    public class UpdateExpenseListCommandHandler : IRequestHandler<UpdateExpenseListCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateExpenseListCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            UpdateExpenseListCommand request,
            CancellationToken cancellationToken)
        {
            var membership = await _context.ExpenseListMembers
                .Include(m => m.ExpenseList)
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.Id &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership == null)
            {
                throw new NotFoundException(nameof(ExpenseList), request.Id);
            }

            if (membership.Role != ExpenseListRole.Owner)
            {
                throw new ForbiddenException("Only the owner can update expense list details.");
            }

            var expenseList = membership.ExpenseList;
            expenseList.Name = request.Name;
            expenseList.Description = request.Description;
            expenseList.CoverImage = request.CoverImage;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
