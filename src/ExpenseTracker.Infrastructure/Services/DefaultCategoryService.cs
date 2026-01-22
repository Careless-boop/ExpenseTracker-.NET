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

        public async Task<Guid> CreateDefaultCategoryForUserAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = DefaultCategoryName,
                Icon = DefaultCategoryIcon,
                Color = DefaultCategoryColor,
                IsDefault = true,
                UserId = userId,
                ExpenseListId = null
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category.Id;
        }

        public async Task<Guid> CreateDefaultCategoryForListAsync(
            Guid expenseListId,
            CancellationToken cancellationToken = default)
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = DefaultCategoryName,
                Icon = DefaultCategoryIcon,
                Color = DefaultCategoryColor,
                IsDefault = true,
                UserId = null,
                ExpenseListId = expenseListId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category.Id;
        }

        public async Task<Guid> GetDefaultCategoryIdAsync(
            string? userId,
            Guid? expenseListId,
            CancellationToken cancellationToken = default)
        {
            Category? defaultCategory;

            if (expenseListId.HasValue)
            {
                defaultCategory = await _context.Categories
                    .FirstOrDefaultAsync(c =>
                        c.ExpenseListId == expenseListId.Value &&
                        c.IsDefault,
                        cancellationToken);

                if (defaultCategory == null)
                {
                    return await CreateDefaultCategoryForListAsync(expenseListId.Value, cancellationToken);
                }
            }
            else if (userId != null)
            {
                defaultCategory = await _context.Categories
                    .FirstOrDefaultAsync(c =>
                        c.UserId == userId &&
                        c.ExpenseListId == null &&
                        c.IsDefault,
                        cancellationToken);

                if (defaultCategory == null)
                {
                    return await CreateDefaultCategoryForUserAsync(userId, cancellationToken);
                }
            }
            else
            {
                throw new ArgumentException("Either userId or expenseListId must be provided");
            }

            return defaultCategory.Id;
        }
    }
}
