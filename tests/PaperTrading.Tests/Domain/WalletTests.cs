using FluentAssertions;
using PaperTrading.Domain.Entities;
using PaperTrading.Domain.Enums;
using PaperTrading.Domain.Exceptions;

namespace PaperTrading.Tests.Domain;

public sealed class WalletTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_WithInitialDeposit_InitializesWalletAndCreatesInitialLedgerEntry()
    {
        // Arrange
        const decimal initialBalance = 1_000_000.00m;

        // Act
        var wallet = Wallet.Create(_userId, initialBalance);

        // Assert
        wallet.Id.Should().NotBeEmpty();
        wallet.UserId.Should().Be(_userId);
        wallet.Balance.Should().Be(initialBalance);
        wallet.LedgerEntries.Should().HaveCount(1);

        var entry = wallet.LedgerEntries.Single();
        entry.WalletId.Should().Be(wallet.Id);
        entry.Direction.Should().Be(LedgerEntryDirection.Credit);
        entry.Reason.Should().Be(LedgerEntryReason.InitialDeposit);
        entry.Amount.Should().Be(initialBalance);
        entry.BalanceAfter.Should().Be(initialBalance);
    }

    [Fact]
    public void Credit_WithValidAmount_IncreasesBalanceAndReturnsLedgerEntry()
    {
        // Arrange
        var wallet = Wallet.Create(_userId, 1000m);
        const decimal creditAmount = 500.50m;

        // Act
        var entry = wallet.Credit(creditAmount, LedgerEntryReason.InitialDeposit, "Additional top-up");

        // Assert
        wallet.Balance.Should().Be(1500.50m);
        wallet.LedgerEntries.Should().HaveCount(2);

        entry.Direction.Should().Be(LedgerEntryDirection.Credit);
        entry.Amount.Should().Be(creditAmount);
        entry.BalanceAfter.Should().Be(1500.50m);
        entry.Description.Should().Be("Additional top-up");
    }

    [Fact]
    public void Debit_WithSufficientBalance_DecreasesBalanceAndReturnsLedgerEntry()
    {
        // Arrange
        var wallet = Wallet.Create(_userId, 1000m);
        const decimal debitAmount = 400.25m;

        // Act
        var entry = wallet.Debit(debitAmount, LedgerEntryReason.InitialDeposit, "Stock purchase");

        // Assert
        wallet.Balance.Should().Be(599.75m);
        wallet.LedgerEntries.Should().HaveCount(2);

        entry.Direction.Should().Be(LedgerEntryDirection.Debit);
        entry.Amount.Should().Be(debitAmount);
        entry.BalanceAfter.Should().Be(599.75m);
        entry.Description.Should().Be("Stock purchase");
    }

    [Fact]
    public void Debit_WithInsufficientFunds_ThrowsInsufficientFundsExceptionAndLeavesBalanceUnchanged()
    {
        // Arrange
        var wallet = Wallet.Create(_userId, 100m);

        // Act
        var act = () => wallet.Debit(150m, LedgerEntryReason.InitialDeposit, "Overdraft attempt");

        // Assert
        act.Should().Throw<InsufficientFundsException>()
            .Which.Balance.Should().Be(100m);

        wallet.Balance.Should().Be(100m);
        wallet.LedgerEntries.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-500)]
    public void CreditAndDebit_WithZeroOrNegativeAmount_ThrowsInvalidMoneyAmountException(decimal invalidAmount)
    {
        // Arrange
        var wallet = Wallet.Create(_userId, 1000m);

        // Act & Assert
        var creditAct = () => wallet.Credit(invalidAmount, LedgerEntryReason.InitialDeposit, "Invalid credit");
        creditAct.Should().Throw<InvalidMoneyAmountException>();

        var debitAct = () => wallet.Debit(invalidAmount, LedgerEntryReason.InitialDeposit, "Invalid debit");
        debitAct.Should().Throw<InvalidMoneyAmountException>();

        wallet.Balance.Should().Be(1000m);
    }

    [Theory]
    [InlineData(10.001)]
    [InlineData(10.12345)]
    [InlineData(0.0001)]
    public void CreditAndDebit_WithMoreThanTwoDecimals_ThrowsInvalidMoneyAmountException(decimal invalidPrecision)
    {
        // Arrange
        var wallet = Wallet.Create(_userId, 1000m);

        // Act & Assert
        var creditAct = () => wallet.Credit(invalidPrecision, LedgerEntryReason.InitialDeposit, "Sub-paise credit");
        creditAct.Should().Throw<InvalidMoneyAmountException>()
            .WithMessage("*more than 2 decimal places*");

        var debitAct = () => wallet.Debit(invalidPrecision, LedgerEntryReason.InitialDeposit, "Sub-paise debit");
        debitAct.Should().Throw<InvalidMoneyAmountException>()
            .WithMessage("*more than 2 decimal places*");

        wallet.Balance.Should().Be(1000m);
    }

    [Fact]
    public void WalletBalance_AfterRandomSequenceOfOperations_AlwaysEqualsSumOfLedgerEntries()
    {
        // Arrange
        const decimal initialBalance = 1_000_000.00m;
        var wallet = Wallet.Create(_userId, initialBalance);
        var random = new Random(42);

        // Act - 100 random valid operations
        for (var i = 0; i < 100; i++)
        {
            var isCredit = random.Next(2) == 0;
            // Generate amount between 0.05 and 5000.00 with 2 decimal places
            var randomCents = random.Next(5, 500000);
            var amount = randomCents / 100m;

            if (isCredit)
            {
                wallet.Credit(amount, LedgerEntryReason.InitialDeposit, $"Random credit {i}");
            }
            else
            {
                if (wallet.Balance >= amount)
                {
                    wallet.Debit(amount, LedgerEntryReason.InitialDeposit, $"Random debit {i}");
                }
            }
        }

        // Assert: Invariant holds mathematically
        var totalCredits = wallet.LedgerEntries
            .Where(e => e.Direction == LedgerEntryDirection.Credit)
            .Sum(e => e.Amount);

        var totalDebits = wallet.LedgerEntries
            .Where(e => e.Direction == LedgerEntryDirection.Debit)
            .Sum(e => e.Amount);

        var expectedBalance = totalCredits - totalDebits;

        wallet.Balance.Should().Be(expectedBalance);
        wallet.LedgerEntries.Last().BalanceAfter.Should().Be(wallet.Balance);
    }
}
