namespace ExpenseTracker.Application.Common.Interfaces
{
    public interface IDefaultCategoryService
    {
        Task<Guid> CreateDefaultCategoryForUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<Guid> CreateDefaultCategoryForListAsync(Guid expenseListId, CancellationToken cancellationToken = default);
        Task<Guid> GetDefaultCategoryIdAsync(string? userId, Guid? expenseListId, CancellationToken cancellationToken = default);
    }
}
