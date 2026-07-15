using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Settings
{
    public record UpdateUserSettingsCommand(
        bool SyncClosedListsToPersonal,
        string Currency = "USD") : IRequest<UserSettingsDto>;

    public class UpdateUserSettingsCommandValidator : AbstractValidator<UpdateUserSettingsCommand>
    {
        public UpdateUserSettingsCommandValidator()
        {
            RuleFor(x => x.Currency)
                .Must(SupportedCurrencies.IsSupported)
                .WithMessage("Unsupported currency.");
        }
    }

    public class UpdateUserSettingsCommandHandler
        : IRequestHandler<UpdateUserSettingsCommand, UserSettingsDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateUserSettingsCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<UserSettingsDto> Handle(
            UpdateUserSettingsCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId!;

            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

            if (settings == null)
            {
                settings = new UserSettings { Id = Guid.NewGuid(), UserId = userId };
                _context.UserSettings.Add(settings);
            }

            settings.SyncClosedListsToPersonal = request.SyncClosedListsToPersonal;
            settings.Currency = SupportedCurrencies.Normalize(request.Currency);

            await _context.SaveChangesAsync(cancellationToken);

            return new UserSettingsDto(settings.SyncClosedListsToPersonal, settings.Currency);
        }
    }
}
