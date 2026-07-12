using ExpenseTracker.Domain.Interfaces;
using ExpenseTracker.Infrastructure.Identity;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExpenseTracker.UnitTests
{
    /// <summary>
    /// SQLite in-memory rather than the EF InMemory provider: the handlers under test open real
    /// transactions and rely on query filters, neither of which InMemory supports.
    /// </summary>
    public sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;

        public ApplicationDbContext Context { get; }
        public ICurrentUserService CurrentUser { get; }

        public TestDatabase(string userId = "user-alice")
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            CurrentUser = Substitute.For<ICurrentUserService>();
            CurrentUser.UserId.Returns(userId);
            CurrentUser.IsAuthenticated.Returns(true);

            var interceptor = new AuditableEntityInterceptor(CurrentUser, TimeProvider.System);

            Context = new ApplicationDbContext(options, interceptor);
            Context.Database.EnsureCreated();
        }

        public void SignInAs(string userId) => CurrentUser.UserId.Returns(userId);

        /// <summary>SQLite enforces the FKs into AspNetUsers, so any referenced user must exist.</summary>
        public ApplicationUser AddUser(string userId)
        {
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = userId,
                NormalizedUserName = userId.ToUpperInvariant(),
                Email = $"{userId}@test.io",
                NormalizedEmail = $"{userId}@test.io".ToUpperInvariant(),
                CreatedAt = DateTime.UtcNow
            };

            Context.Users.Add(user);
            return user;
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
