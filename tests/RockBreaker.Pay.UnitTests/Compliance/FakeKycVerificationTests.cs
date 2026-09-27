using Microsoft.AspNetCore.Mvc;
using RockBreaker.FakeKyc.Controllers;

namespace RockBreaker.Pay.UnitTests.Compliance;

/// <summary>
/// TR: FakeKyc provider'ın deterministik doğrulama senaryolarını test eder.
/// EN: Tests deterministic verification scenarios of the FakeKyc provider.
/// Architecture: Unit Tests for Fake External Service Rules.
/// </summary>
public sealed class FakeKycVerificationTests
{
    /// <summary>
    /// TR: Normal kullanıcı bilgisinin Verified sonucu ürettiğini doğrular.
    /// EN: Verifies that normal user data produces a Verified result.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Verify_NormalUser_ShouldReturnVerified()
    {
        var controller = new KycVerificationsController();

        var action = controller.Verify(new KycVerificationRequest
        {
            UserId = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Wallet",
            Email = "alice@rockbreaker.local"
        });

        var result = GetResponse(action);

        Assert.Equal("Verified", result.Status);
        Assert.Equal(10, result.RiskScore);
        Assert.NotNull(result.VerifiedAtUtc);
    }

    /// <summary>
    /// TR: manual.local domaininin Pending sonucu ürettiğini doğrular.
    /// EN: Verifies that the manual.local domain produces a Pending result.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Verify_ManualDomain_ShouldReturnPending()
    {
        var controller = new KycVerificationsController();

        var action = controller.Verify(new KycVerificationRequest
        {
            UserId = Guid.NewGuid(),
            FirstName = "Manual",
            LastName = "Review",
            Email = "person@manual.local"
        });

        var result = GetResponse(action);

        Assert.Equal("Pending", result.Status);
        Assert.Equal(55, result.RiskScore);
        Assert.Null(result.VerifiedAtUtc);
    }

    /// <summary>
    /// TR: reject.local domaininin Rejected sonucu ürettiğini doğrular.
    /// EN: Verifies that the reject.local domain produces a Rejected result.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Verify_RejectDomain_ShouldReturnRejected()
    {
        var controller = new KycVerificationsController();

        var action = controller.Verify(new KycVerificationRequest
        {
            UserId = Guid.NewGuid(),
            FirstName = "Rejected",
            LastName = "User",
            Email = "person@reject.local"
        });

        var result = GetResponse(action);

        Assert.Equal("Rejected", result.Status);
        Assert.Equal(90, result.RiskScore);
        Assert.Null(result.VerifiedAtUtc);
    }

    /// <summary>
    /// TR: Zorunlu kimlik alanı eksikse Rejected sonucu üretildiğini doğrular.
    /// EN: Verifies that missing required identity data produces a Rejected result.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Verify_MissingIdentityData_ShouldReturnRejected()
    {
        var controller = new KycVerificationsController();

        var action = controller.Verify(new KycVerificationRequest
        {
            UserId = Guid.NewGuid(),
            FirstName = string.Empty,
            LastName = "User",
            Email = "person@rockbreaker.local"
        });

        var result = GetResponse(action);

        Assert.Equal("Rejected", result.Status);
        Assert.Equal(100, result.RiskScore);
    }

    /// <summary>
    /// TR: Controller ActionResult içinden typed response nesnesini çıkarır.
    /// EN: Extracts the typed response object from the controller ActionResult.
    /// Architecture: Test Helper.
    /// </summary>
    private static KycVerificationResponse GetResponse(
        ActionResult<KycVerificationResponse> action)
    {
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsType<KycVerificationResponse>(ok.Value);
    }
}
