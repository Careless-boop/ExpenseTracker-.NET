using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Services
{
    public class DefaultCategoryService : IDefaultCategoryService
    {
        private readonly IApplicationDbContext _context;

        private const string DefaultCategoryName = "Other";
        private const string DefaultCategoryIcon = "📦";
        private const string DefaultCategoryColor = "#9CA3AF";

        public DefaultCategoryService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> GetOrCreateDefaultPersonalCategoryAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var existing = await _context.PersonalCategories
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsDefault, cancellationToken);

            if (existing != null)
                return existing.Id;

            var category = new PersonalCategory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = DefaultCategoryName,
                Icon = DefaultCategoryIcon,
                Color = DefaultCategoryColor,
                IsDefault = true
            };

            _context.PersonalCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category.Id;
        }

        public async Task<Guid> GetOrCreateDefaultExpenseListCategoryAsync(
            Guid expenseListId,
            CancellationToken cancellationToken = default)
        {
            var existing = await _context.ExpenseListCategories
                .FirstOrDefaultAsync(c => c.ExpenseListId == expenseListId && c.IsDefault, cancellationToken);

            if (existing != null)
                return existing.Id;

            var category = new ExpenseListCategory
            {
                Id = Guid.NewGuid(),
                ExpenseListId = expenseListId,
                Name = DefaultCategoryName,
                Icon = DefaultCategoryIcon,
                Color = DefaultCategoryColor,
                IsDefault = true
            };

            _context.ExpenseListCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
    }
}
