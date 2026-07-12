using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Identity;
using ExpenseTracker.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;

namespace ExpenseTracker.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
    {
        private readonly AuditableEntityInterceptor _auditableInterceptor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            AuditableEntityInterceptor auditableInterceptor)
            : base(options)
        {
            _auditableInterceptor = auditableInterceptor;
        }

        public DbSet<PersonalTransaction> PersonalTransactions => Set<PersonalTransaction>();
        public DbSet<PersonalCategory> PersonalCategories => Set<PersonalCategory>();
        public DbSet<ExpenseList> ExpenseLists => Set<ExpenseList>();
        public DbSet<ExpenseListMember> ExpenseListMembers => Set<ExpenseListMember>();
        public DbSet<ExpenseListTransaction> ExpenseListTransactions => Set<ExpenseListTransaction>();
        public DbSet<ExpenseListTransactionParticipant> ExpenseListTransactionParticipants => Set<ExpenseListTransactionParticipant>();
        public DbSet<ExpenseListCategory> ExpenseListCategories => Set<ExpenseListCategory>();
        public DbSet<Settlement> Settlements => Set<Settlement>();
        public DbSet<UserSettings> UserSettings => Set<UserSettings>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.AddInterceptors(_auditableInterceptor);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
