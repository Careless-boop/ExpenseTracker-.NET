using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.ExpenseLists.Commands;
using ExpenseTracker.Application.Features.Settings;
using ExpenseTracker.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExpenseTracker.UnitTests
{
    public class CurrencyTests : IDisposable
    {
        private readonly TestDatabase _db = new("user-alice");

        public CurrencyTests()
        {
            _db.AddUser("user-alice");
            _db.Context.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private static ExpenseTracker.Application.Common.Interfaces.IIdentityService Identity()
        {
            var identity = Substitute.For<ExpenseTracker.Application.Common.Interfaces.IIdentityService>();
            identity.GetUserAsync(Arg.Any<string>()).Returns(
                new ExpenseTracker.Application.Common.Models.UserDto(
                    "user-alice", "alice", "alice@test.io", "Alice", null));
            return identity;
        }

        private static ExpenseTracker.Application.Common.Interfaces.IDefaultCategoryService DefaultCategory()
        {
            var svc = Substitute.For<ExpenseTracker.Application.Common.Interfaces.IDefaultCategoryService>();
            svc.GetOrCreateDefaultExpenseListCategoryAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Guid.NewGuid());
            return svc;
        }

        private CreateExpenseListCommandHandler CreateHandler() =>
            new(_db.Context, _db.CurrentUser, Identity(), DefaultCategory());

        [Fact]
        public async Task New_list_inherits_the_creators_currency()
        {
            await new UpdateUserSettingsCommandHandler(_db.Context, _db.CurrentUser)
                .Handle(new UpdateUserSettingsCommand(true, "EUR"), default);

            var id = await CreateHandler().Handle(new CreateExpenseListCommand("Trip"), default);

            var list = await _db.Context.ExpenseLists.FindAsync(id);
            list!.Currency.Should().Be("EUR");
        }

        [Fact]
        public async Task New_list_uses_an_explicit_currency_over_the_setting()
        {
            await new UpdateUserSettingsCommandHandler(_db.Context, _db.CurrentUser)
                .Handle(new UpdateUserSettingsCommand(true, "EUR"), default);

            var id = await CreateHandler().Handle(
                new CreateExpenseListCommand("Trip", Currency: "GBP"), default);

            var list = await _db.Context.ExpenseLists.FindAsync(id);
            list!.Currency.Should().Be("GBP");
        }

        [Fact]
        public async Task New_list_falls_back_to_the_default_when_the_user_has_no_setting()
        {
            var id = await CreateHandler().Handle(new CreateExpenseListCommand("Trip"), default);

            var list = await _db.Context.ExpenseLists.FindAsync(id);
            list!.Currency.Should().Be(SupportedCurrencies.Default);
        }

        [Fact]
        public async Task Settings_persist_and_normalise_the_currency()
        {
            var result = await new UpdateUserSettingsCommandHandler(_db.Context, _db.CurrentUser)
                .Handle(new UpdateUserSettingsCommand(false, "uah"), default);

            result.Currency.Should().Be("UAH");

            var read = await new GetUserSettingsQueryHandler(_db.Context, _db.CurrentUser)
                .Handle(new GetUserSettingsQuery(), default);
            read.Currency.Should().Be("UAH");
        }

        [Theory]
        [InlineData("USD", true)]
        [InlineData("UAH", true)]
        [InlineData("XYZ", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Only_supported_currencies_validate(string? code, bool expected)
        {
            SupportedCurrencies.IsSupported(code).Should().Be(expected);
        }
    }
}
