using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Settings
{
    public record GetUserSettingsQuery : IRequest<UserSettingsDto>;

    public class GetUserSettingsQueryHandler : IRequestHandler<GetUserSettingsQuery, UserSettingsDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetUserSettingsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<UserSettingsDto> Handle(
            GetUserSettingsQuery request,
            CancellationToken cancellationToken)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId, cancellationToken);

            // Absent row means untouched defaults, which is the common case — don't write one on read.
            var defaults = new UserSettings();
            return new UserSettingsDto(
                settings?.SyncClosedListsToPersonal ?? defaults.SyncClosedListsToPersonal,
                settings?.Currency ?? defaults.Currency);
        }
    }
}
