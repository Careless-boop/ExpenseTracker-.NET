using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ExpenseTracker.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<PersonalTransaction> PersonalTransactions { get; }
        DbSet<PersonalCategory> PersonalCategories { get; }
        DbSet<ExpenseList> ExpenseLists { get; }
        DbSet<ExpenseListMember> ExpenseListMembers { get; }
        DbSet<ExpenseListTransaction> ExpenseListTransactions { get; }
        DbSet<ExpenseListTransactionParticipant> ExpenseListTransactionParticipants { get; }
        DbSet<ExpenseListCategory> ExpenseListCategories { get; }
        DbSet<Settlement> Settlements { get; }
        DbSet<UserSettings> UserSettings { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
