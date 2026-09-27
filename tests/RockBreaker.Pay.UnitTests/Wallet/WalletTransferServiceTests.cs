using System.Data;
using FluentValidation;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Fraud;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Application;
using RockBreaker.Pay.Modules.Wallet.Contracts;
using RockBreaker.Pay.Modules.Wallet.Domain;
using RockBreaker.Pay.Modules.Wallet.Validation;
using WalletEntity = RockBreaker.Pay.Modules.Wallet.Domain.Wallet;

namespace RockBreaker.Pay.UnitTests.Wallet;

/// <summary>
/// TR: WalletTransferService orkestrasyon davranışlarını bağımlılıklardan izole şekilde test eder.
/// EN: Tests WalletTransferService orchestration behavior in isolation from infrastructure dependencies.
/// Architecture: Application Service Unit Test with hand-written test doubles.
/// </summary>
public sealed class WalletTransferServiceTests
{
    /// <summary>
    /// TR: Fraud BlockWallet kararı geldiğinde wallet'ın bloke edildiğini ve store'un çağrılmadığını doğrular.
    /// EN: Verifies that a BlockWallet fraud decision blocks the wallet and prevents store execution.
    /// Architecture: Arrange-Act-Assert + Test Doubles.
    /// </summary>
    [Fact]
    public async Task TransferAsync_HighRiskFraud_ShouldBlockWallet()
    {
        var sourceWalletId = Guid.NewGuid();
        var repository = new FakeWalletRepository();
        var store = new FakeTransferStore();
        var fraud = new FakeFraudEvaluator(new FraudDecision
        {
            Action = FraudAction.BlockWallet,
            RiskScore = 90,
            TriggeredRules = ["FRAUD_TEST"]
        });

        var service = new WalletTransferService(
            new TransferRequestValidator(),
            fraud,
            repository,
            store);

        var result = await service.TransferAsync(
            new TransferRequest
            {
                SourceWalletId = sourceWalletId,
                DestinationWalletId = Guid.NewGuid(),
                Amount = 100,
                Currency = "TRY"
            },
            "test-key",
            "correlation-id");

        Assert.False(result.IsSuccess);
        Assert.Equal("WALLET_BLOCKED_BY_FRAUD", result.ErrorCode);
        Assert.Equal(WalletStatus.Blocked, repository.LastStatus);
        Assert.Equal(sourceWalletId, repository.LastWalletId);
        Assert.False(store.WasCalled);
    }

    /// <summary>
    /// TR: Idempotency key eksik olduğunda finansal store'a gidilmediğini doğrular.
    /// EN: Verifies that the financial store is not called when the idempotency key is missing.
    /// Architecture: Arrange-Act-Assert + Guard Clause Test.
    /// </summary>
    [Fact]
    public async Task TransferAsync_MissingIdempotencyKey_ShouldFailBeforeStore()
    {
        var store = new FakeTransferStore();
        var service = new WalletTransferService(
            new TransferRequestValidator(),
            new FakeFraudEvaluator(new FraudDecision { Action = FraudAction.Allow }),
            new FakeWalletRepository(),
            store);

        var result = await service.TransferAsync(
            new TransferRequest
            {
                SourceWalletId = Guid.NewGuid(),
                DestinationWalletId = Guid.NewGuid(),
                Amount = 100,
                Currency = "TRY"
            },
            string.Empty,
            "correlation-id");

        Assert.False(result.IsSuccess);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", result.ErrorCode);
        Assert.False(store.WasCalled);
    }

    private sealed class FakeFraudEvaluator : IFraudEvaluator
    {
        private readonly FraudDecision _decision;

        public FakeFraudEvaluator(FraudDecision decision) => _decision = decision;

        public Task<FraudDecision> EvaluateAsync(Guid walletId, decimal amount) => Task.FromResult(_decision);
    }

    private sealed class FakeWalletRepository : IWalletRepository
    {
        public Guid? LastWalletId { get; private set; }
        public WalletStatus? LastStatus { get; private set; }

        public Task<WalletEntity?> GetByIdAsync(Guid walletId) => Task.FromResult<WalletEntity?>(null);

        public Task<WalletEntity?> GetByUserIdAsync(Guid userId) => Task.FromResult<WalletEntity?>(null);

        public Task InsertAsync(WalletEntity wallet) => Task.CompletedTask;

        public Task<WalletEntity?> GetForUpdateAsync(
            Guid walletId,
            IDbConnection connection,
            IDbTransaction transaction) => Task.FromResult<WalletEntity?>(null);

        public Task UpdateAsync(
            WalletEntity wallet,
            IDbConnection connection,
            IDbTransaction transaction) => Task.CompletedTask;

        public Task UpdateStatusAsync(Guid walletId, WalletStatus status)
        {
            LastWalletId = walletId;
            LastStatus = status;
            return Task.CompletedTask;
        }

        public Task UpdateLimitsAsync(WalletEntity wallet) => Task.CompletedTask;

        public Task<IReadOnlyCollection<WalletTransaction>> GetTransactionsAsync(Guid walletId) =>
            Task.FromResult<IReadOnlyCollection<WalletTransaction>>(Array.Empty<WalletTransaction>());
    }

    private sealed class FakeTransferStore : IWalletTransferStore
    {
        public bool WasCalled { get; private set; }

        public Task<OperationResult<TransferResponse>> ExecuteAsync(
            TransferRequest request,
            string idempotencyKey,
            string correlationId)
        {
            WasCalled = true;
            return Task.FromResult(OperationResult<TransferResponse>.Success(new TransferResponse()));
        }
    }
}
