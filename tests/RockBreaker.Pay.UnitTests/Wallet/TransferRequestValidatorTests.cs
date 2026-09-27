using RockBreaker.Pay.Modules.Wallet.Contracts;
using RockBreaker.Pay.Modules.Wallet.Validation;

namespace RockBreaker.Pay.UnitTests.Wallet;

/// <summary>
/// TR: Wallet transfer request validasyon kurallarını test eder.
/// EN: Tests wallet transfer request validation rules.
/// Architecture: Unit Test for Validation Boundary.
/// </summary>
public sealed class TransferRequestValidatorTests
{
    /// <summary>
    /// TR: Pozitif TRY transferinin validasyondan geçtiğini doğrular.
    /// EN: Verifies that a positive TRY transfer passes validation.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Validate_ValidTransfer_ShouldBeValid()
    {
        var validator = new TransferRequestValidator();
        var request = new TransferRequest
        {
            SourceWalletId = Guid.NewGuid(),
            DestinationWalletId = Guid.NewGuid(),
            Amount = 125.50m,
            Currency = "TRY"
        };

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// TR: Aynı wallet'a transferin reddedildiğini doğrular.
    /// EN: Verifies that transferring to the same wallet is rejected.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Validate_SameWallet_ShouldBeInvalid()
    {
        var validator = new TransferRequestValidator();
        var walletId = Guid.NewGuid();
        var request = new TransferRequest
        {
            SourceWalletId = walletId,
            DestinationWalletId = walletId,
            Amount = 10,
            Currency = "TRY"
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// TR: İki ondalık basamaktan fazla tutarın reddedildiğini doğrular.
    /// EN: Verifies that an amount with more than two decimal places is rejected.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Validate_TooManyDecimalPlaces_ShouldBeInvalid()
    {
        var validator = new TransferRequestValidator();
        var request = new TransferRequest
        {
            SourceWalletId = Guid.NewGuid(),
            DestinationWalletId = Guid.NewGuid(),
            Amount = 1.123m,
            Currency = "TRY"
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
