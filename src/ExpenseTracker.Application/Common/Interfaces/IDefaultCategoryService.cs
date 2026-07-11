namespace ExpenseTracker.Application.Common.Interfaces
{
    public interface IDefaultCategoryService
    {
        Task<Guid> GetOrCreateDefaultPersonalCategoryAsync(string userId, CancellationToken cancellationToken = default);
        Task<Guid> GetOrCreateDefaultExpenseListCategoryAsync(Guid expenseListId, CancellationToken cancellationToken = default);
    }
}
