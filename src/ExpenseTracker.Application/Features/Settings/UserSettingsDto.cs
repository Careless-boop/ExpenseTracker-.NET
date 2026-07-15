namespace ExpenseTracker.Application.Features.Settings
{
    public record UserSettingsDto(bool SyncClosedListsToPersonal, string Currency);
}
