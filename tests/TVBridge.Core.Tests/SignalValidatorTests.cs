using FluentAssertions;
using Xunit;

namespace TVBridge.Core.Tests;

public sealed class SignalValidatorTests
{
    private readonly SignalValidator _validator = new();

    private static Signal CreateValidSignal() => new()
    {
        AlertId = "test-1",
        StrategyId = "MA_CROSS",
        AccountTag = "default",
        Symbol = "EURUSD",
        Action = SignalAction.Buy,
        OrderType = OrderType.Market,
        LotSize = 0.1m,
        Timeframe = "1H",
        Timestamp = DateTimeOffset.UtcNow,
        Secret = "my-secret"
    };

    [Fact]
    public void Validate_ValidMarketOrder_ReturnsValid()
    {
        var result = _validator.Validate(CreateValidSignal());
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ValidLimitOrderWithPrice_ReturnsValid()
    {
        var signal = CreateValidSignal() with { OrderType = OrderType.Limit, EntryPrice = 1.1000m };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_LimitOrderWithoutPrice_ReturnsError()
    {
        var signal = CreateValidSignal() with { OrderType = OrderType.Limit, EntryPrice = null };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("entry_price"));
    }

    [Fact]
    public void Validate_StopOrderWithoutPrice_ReturnsError()
    {
        var signal = CreateValidSignal() with { OrderType = OrderType.Stop, EntryPrice = null };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("entry_price"));
    }

    [Fact]
    public void Validate_BothLotSizeAndRiskPercent_ReturnsError()
    {
        var signal = CreateValidSignal() with { LotSize = 0.1m, RiskPercent = 2.0m };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("mutually exclusive"));
    }

    [Fact]
    public void Validate_MissingAlertId_ReturnsError()
    {
        var signal = CreateValidSignal() with { AlertId = "" };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("alert_id"));
    }

    [Fact]
    public void Validate_MissingSymbol_ReturnsError()
    {
        var signal = CreateValidSignal() with { Symbol = "" };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("symbol"));
    }

    [Fact]
    public void Validate_MissingSecret_ReturnsError()
    {
        var signal = CreateValidSignal() with { Secret = "" };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("secret"));
    }

    [Fact]
    public void Validate_NegativeLotSize_ReturnsError()
    {
        var signal = CreateValidSignal() with { LotSize = -0.1m };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("lot_size"));
    }

    [Fact]
    public void Validate_RiskPercentOver100_ReturnsError()
    {
        var signal = CreateValidSignal() with { LotSize = null, RiskPercent = 150m };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("risk_percent"));
    }

    [Fact]
    public void Validate_FutureTimestamp_ReturnsWarning()
    {
        var signal = CreateValidSignal() with { Timestamp = DateTimeOffset.UtcNow.AddMinutes(10) };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("future"));
    }

    [Fact]
    public void Validate_NullOptionalFields_ReturnsValid()
    {
        var signal = CreateValidSignal() with
        {
            EntryPrice = null,
            StopLoss = null,
            TakeProfit = null,
            LotSize = null,
            RiskPercent = null,
            Comment = null
        };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAll()
    {
        var signal = CreateValidSignal() with
        {
            AlertId = "",
            Symbol = "",
            Secret = ""
        };
        var result = _validator.Validate(signal);
        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().BeGreaterThanOrEqualTo(3);
    }
}
