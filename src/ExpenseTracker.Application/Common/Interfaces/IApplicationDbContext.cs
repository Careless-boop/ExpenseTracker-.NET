using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Category> Categories { get; }
        DbSet<Transaction> Transactions { get; }
        DbSet<TransactionParticipant> TransactionParticipants { get; }
        DbSet<ExpenseList> ExpenseLists { get; }
        DbSet<ExpenseListMember> ExpenseListMembers { get; }
        DbSet<Settlement> Settlements { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
