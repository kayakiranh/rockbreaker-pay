using System.Data;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Compliance;
using RockBreaker.Pay.Modules.Fraud;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Application;
using RockBreaker.Pay.Modules.Wallet.Contracts;
using RockBreaker.Pay.Modules.Wallet.Domain;
using RockBreaker.Pay.Modules.Wallet.Validation;
using WalletEntity = RockBreaker.Pay.Modules.Wallet.Domain.Wallet;

namespace RockBreaker.Pay.UnitTests.Wallet;

/// <summary>
/// TR: Wallet transfer ownership authorization guard davranışını test eder.
/// EN: Tests wallet-transfer ownership authorization guard behavior.
/// Architecture: Application Service Unit Test.
/// </summary>
public sealed class WalletTransferOwnershipTests
{
    /// <summary>
    /// TR: Authenticated kullanıcı kaynak wallet sahibi değilse finansal store'un çağrılmadığını doğrular.
    /// EN: Verifies that the financial store is not called when the authenticated user does not own the source wallet.
    /// Architecture: Authorization Guard Test.
    /// </summary>
    [Fact]
    public async Task TransferForUserAsync_DifferentOwner_ShouldRejectBeforeFinancialStore()
    {
        var sourceWallet = new WalletEntity
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = WalletStatus.Active,
            Currency = "TRY"
        };

        var repository = new OwnershipWalletRepository(sourceWallet);
        var store = new OwnershipTransferStore();
        var service = new WalletTransferService(
            new TransferRequestValidator(),
            new AllowFraudEvaluator(),
            repository,
            store,
            new VerifiedKycGuard());

        var result = await service.TransferForUserAsync(
            Guid.NewGuid(),
            new TransferRequest
            {
                SourceWalletId = sourceWallet.Id,
                DestinationWalletId = Guid.NewGuid(),
                Amount = 100,
                Currency = "TRY"
            },
            "key",
            "correlation");

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN_SOURCE_WALLET", result.ErrorCode);
        Assert.False(store.WasCalled);
    }

    /// <summary>
    /// TR: Test için kaynak wallet döndüren küçük repository double'ıdır.
    /// EN: Small repository test double returning the source wallet.
    /// Architecture: Hand-Written Test Double.
    /// </summary>
    private sealed class OwnershipWalletRepository : IWalletRepository
    {
        private readonly WalletEntity _wallet;

        /// <summary>
        /// TR: Test double'ına döndürülecek wallet'ı verir.
        /// EN: Supplies the wallet returned by the test double.
        /// Architecture: Constructor Injection.
        /// </summary>
        /// <param name="wallet">TR: Test wallet'ı. EN: Test wallet.</param>
        public OwnershipWalletRepository(WalletEntity wallet) => _wallet = wallet;

        /// <inheritdoc />
        public Task<WalletEntity?> GetByIdAsync(Guid walletId) =>
            Task.FromResult<WalletEntity?>(walletId == _wallet.Id ? _wallet : null);

        /// <inheritdoc />
        public Task<WalletEntity?> GetByUserIdAsync(Guid userId) =>
            Task.FromResult<WalletEntity?>(userId == _wallet.UserId ? _wallet : null);

        /// <inheritdoc />
        public Task InsertAsync(WalletEntity wallet) => Task.CompletedTask;

        /// <inheritdoc />
        public Task<WalletEntity?> GetForUpdateAsync(
            Guid walletId,
            IDbConnection connection,
            IDbTransaction transaction) => Task.FromResult<WalletEntity?>(_wallet);

        /// <inheritdoc />
        public Task UpdateAsync(
            WalletEntity wallet,
            IDbConnection connection,
            IDbTransaction transaction) => Task.CompletedTask;

        /// <inheritdoc />
        public Task UpdateStatusAsync(Guid walletId, WalletStatus status) => Task.CompletedTask;

        /// <inheritdoc />
        public Task UpdateLimitsAsync(WalletEntity wallet) => Task.CompletedTask;

        /// <inheritdoc />
        public Task<IReadOnlyCollection<WalletTransaction>> GetTransactionsAsync(Guid walletId) =>
            Task.FromResult<IReadOnlyCollection<WalletTransaction>>(Array.Empty<WalletTransaction>());
    }

    /// <summary>
    /// TR: Finansal store'un çağrılıp çağrılmadığını kaydeden test double'ıdır.
    /// EN: Test double recording whether the financial store was invoked.
    /// Architecture: Hand-Written Spy.
    /// </summary>
    private sealed class OwnershipTransferStore : IWalletTransferStore
    {
        /// <summary>TR: Store çağrıldıysa true. EN: True when the store was invoked. Architecture: Spy State.</summary>
        public bool WasCalled { get; private set; }

        /// <inheritdoc />
        public Task<OperationResult<TransferResponse>> ExecuteAsync(
            TransferRequest request,
            string idempotencyKey,
            string correlationId)
        {
            WasCalled = true;
            return Task.FromResult(OperationResult<TransferResponse>.Success(new TransferResponse()));
        }
    }

    /// <summary>
    /// TR: Fraud kontrolünü her zaman Allow döndüren test double'ıdır.
    /// EN: Test double always returning Allow for fraud evaluation.
    /// Architecture: Hand-Written Stub.
    /// </summary>
    private sealed class VerifiedKycGuard : IKycGuard
    {
        /// <inheritdoc />
        public Task<bool> IsUserVerifiedAsync(Guid userId) => Task.FromResult(true);

        /// <inheritdoc />
        public Task<bool> IsWalletOwnerVerifiedAsync(Guid walletId) => Task.FromResult(true);
    }

    private sealed class AllowFraudEvaluator : IFraudEvaluator
    {
        /// <inheritdoc />
        public Task<FraudDecision> EvaluateAsync(Guid walletId, decimal amount) =>
            Task.FromResult(new FraudDecision { Action = FraudAction.Allow });
    }
}
