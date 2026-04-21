using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace TVBridge.Core.Tests;

public sealed class SignalPipelineTests
{
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
        Secret = "secret"
    };

    private static Rule CreateWildcardRule(int destinationId = 1) => new()
    {
        Name = "Catch All",
        DestinationIds = destinationId.ToString()
    };

    private SignalPipeline CreatePipeline(params IOutputChannel[] channels) => new(
        NullLogger<SignalPipeline>.Instance,
        new SignalValidator(),
        new RuleEvaluator(NullLogger<RuleEvaluator>.Instance),
        channels);

    [Fact]
    public async Task ProcessAsync_ValidSignal_WithMatchingRule_Dispatches()
    {
        var channel = new FakeChannel(1);
        var pipeline = CreatePipeline(channel);

        var result = await pipeline.ProcessAsync(
            CreateValidSignal(), [CreateWildcardRule(1)], false);

        result.Success.Should().BeTrue();
        result.Status.Should().Be("Dispatched");
        result.MatchedRuleCount.Should().Be(1);
        channel.ReceivedSignals.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessAsync_InvalidSignal_ReturnsValidationFailed()
    {
        var pipeline = CreatePipeline();

        var result = await pipeline.ProcessAsync(
            CreateValidSignal() with { AlertId = "" }, [CreateWildcardRule()], false);

        result.Success.Should().BeFalse();
        result.Status.Should().Be("ValidationFailed");
    }

    [Fact]
    public async Task ProcessAsync_NoMatchingRules_ReturnsNoMatch()
    {
        var pipeline = CreatePipeline();
        var rule = new Rule { Name = "Specific", DestinationIds = "1", Symbol = "GBPUSD" };

        var result = await pipeline.ProcessAsync(CreateValidSignal(), [rule], false);

        result.Status.Should().Be("NoMatchingRules");
    }

    [Fact]
    public async Task ProcessAsync_DryRun_ChannelReceivesDryRunFlag()
    {
        var channel = new FakeChannel(1);
        var pipeline = CreatePipeline(channel);

        await pipeline.ProcessAsync(CreateValidSignal(), [CreateWildcardRule(1)], globalDryRun: true);

        channel.LastDryRun.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_LotMultiplier_AdjustsLotSize()
    {
        var channel = new FakeChannel(1);
        var pipeline = CreatePipeline(channel);
        var rule = CreateWildcardRule(1) with { LotMultiplier = 2.0m };

        await pipeline.ProcessAsync(CreateValidSignal(), [rule], false);

        channel.ReceivedSignals[0].LotSize.Should().Be(0.2m);
    }

    [Fact]
    public async Task ProcessAsync_ChannelNotFound_ReportsFailure()
    {
        var pipeline = CreatePipeline(); // no channels registered
        var result = await pipeline.ProcessAsync(CreateValidSignal(), [CreateWildcardRule(99)], false);

        result.DispatchResults.Should().HaveCount(1);
        result.DispatchResults[0].Success.Should().BeFalse();
        result.DispatchResults[0].Message.Should().Contain("not found");
    }

    private sealed class FakeChannel : IOutputChannel
    {
        public string Name => "Fake";
        public string ChannelType => "Test";
        public int ChannelId { get; }
        public List<Signal> ReceivedSignals { get; } = [];
        public bool LastDryRun { get; private set; }

        public FakeChannel(int id) => ChannelId = id;

        public Task<ChannelResult> SendAsync(Signal signal, bool dryRun, CancellationToken ct)
        {
            ReceivedSignals.Add(signal);
            LastDryRun = dryRun;
            return Task.FromResult(dryRun
                ? ChannelResult.DryRun("Would send")
                : ChannelResult.Ok("Sent"));
        }

        public Task<bool> ValidateConfigAsync(CancellationToken ct) => Task.FromResult(true);
    }
}
