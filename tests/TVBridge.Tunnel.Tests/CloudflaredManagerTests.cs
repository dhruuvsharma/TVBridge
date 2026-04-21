using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TVBridge.Core;
using Xunit;

namespace TVBridge.Tunnel.Tests;

public sealed class CloudflaredManagerTests
{
    private static CloudflaredManager CreateManager() => new(
        new TunnelConfig(),
        new CloudflaredDownloader(NullLogger<CloudflaredDownloader>.Instance),
        NullLogger<CloudflaredManager>.Instance);

    [Theory]
    [InlineData(
        "2024-01-01T00:00:00Z INF Your quick Tunnel has been created! Visit it at https://abc-def-ghi.trycloudflare.com",
        "https://abc-def-ghi.trycloudflare.com")]
    [InlineData(
        "INF +--------------------------------------------------------------------------------------------+",
        null)]
    [InlineData(
        "https://my-tunnel-xyz.trycloudflare.com",
        "https://my-tunnel-xyz.trycloudflare.com")]
    [InlineData(
        "2024-01-01 INF Registered tunnel connection connIndex=0 connection=abc event=0 ip=1.2.3.4 location=AMS protocol=quic",
        null)]
    [InlineData(
        "some random log line without a URL",
        null)]
    public void ParseTunnelUrl_ExtractsCorrectUrl(string logLine, string? expectedUrl)
    {
        var manager = CreateManager();

        manager.ParseTunnelUrl(logLine);

        manager.TunnelUrl.Should().Be(expectedUrl);
    }

    [Fact]
    public void ParseTunnelUrl_SetsStatusToRunning()
    {
        var manager = CreateManager();

        manager.ParseTunnelUrl("Visit it at https://test-tunnel.trycloudflare.com");

        manager.Status.Should().Be(TunnelStatus.Running);
    }

    [Fact]
    public void ParseTunnelUrl_FiresStatusChangedEvent()
    {
        var manager = CreateManager();
        TunnelStatus? received = null;
        manager.StatusChanged += (_, s) => received = s;

        manager.ParseTunnelUrl("Visit it at https://test-tunnel.trycloudflare.com");

        received.Should().Be(TunnelStatus.Running);
    }

    [Fact]
    public void ParseTunnelUrl_NoUrl_DoesNotChangeStatus()
    {
        var manager = CreateManager();

        manager.ParseTunnelUrl("some random log line");

        manager.Status.Should().Be(TunnelStatus.Stopped);
        manager.TunnelUrl.Should().BeNull();
    }

    [Fact]
    public void InitialState_IsStopped()
    {
        var manager = CreateManager();

        manager.Status.Should().Be(TunnelStatus.Stopped);
        manager.TunnelUrl.Should().BeNull();
        manager.LastError.Should().BeNull();
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_SetsStopped()
    {
        var manager = CreateManager();

        await manager.StopAsync();

        manager.Status.Should().Be(TunnelStatus.Stopped);
    }

    [Fact]
    public void ParseTunnelUrl_MultipleCalls_KeepsLatestUrl()
    {
        var manager = CreateManager();

        manager.ParseTunnelUrl("Visit it at https://first-tunnel.trycloudflare.com");
        manager.ParseTunnelUrl("Visit it at https://second-tunnel.trycloudflare.com");

        manager.TunnelUrl.Should().Be("https://second-tunnel.trycloudflare.com");
    }

    [Theory]
    [InlineData("https://abc123.trycloudflare.com")]
    [InlineData("https://my-long-tunnel-name-with-dashes.trycloudflare.com")]
    [InlineData("https://A1B2C3.trycloudflare.com")]
    public void ParseTunnelUrl_VariousUrlFormats(string url)
    {
        var manager = CreateManager();

        manager.ParseTunnelUrl($"INF Your quick Tunnel URL is {url}");

        manager.TunnelUrl.Should().Be(url);
    }
}
