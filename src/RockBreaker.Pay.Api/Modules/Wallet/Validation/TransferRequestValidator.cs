using FluentValidation;
using RockBreaker.Pay.Modules.Wallet.Contracts;

namespace RockBreaker.Pay.Modules.Wallet.Validation;

/// <summary>
/// TR: Wallet transfer isteğinin API/application sınırındaki temel doğrulamalarını yapar.
/// EN: Performs basic validation for wallet transfer requests at the API/application boundary.
/// Architecture: Validation Pipeline using FluentValidation.
/// </summary>
public sealed class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    /// <summary>
    /// TR: Transfer doğrulama kurallarını tanımlar.
    /// EN: Defines transfer validation rules.
    /// Architecture: FluentValidation Rule Set.
    /// </summary>
    public TransferRequestValidator()
    {
        RuleFor(x => x.SourceWalletId).NotEmpty();
        RuleFor(x => x.DestinationWalletId).NotEmpty();
        RuleFor(x => x.DestinationWalletId).NotEqual(x => x.SourceWalletId);
        RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, false);
        RuleFor(x => x.Currency).Equal("TRY");
    }
}
