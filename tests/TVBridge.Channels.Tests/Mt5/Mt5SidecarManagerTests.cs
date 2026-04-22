using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TVBridge.Channels.Mt5;
using TVBridge.Core;
using Xunit;

namespace TVBridge.Channels.Tests.Mt5;

public sealed class Mt5SidecarManagerTests
{
    [Fact]
    public void InitialStatus_IsStopped()
    {
        var config = new Mt5Config();
        var manager = new Mt5SidecarManager(config, NullLogger<Mt5SidecarManager>.Instance);

        manager.Status.Should().Be(Mt5SidecarStatus.Stopped);
    }

    [Fact]
    public void ParseReadyLine_ValidLine_SetsRunning()
    {
        var config = new Mt5Config();
        var manager = new Mt5SidecarManager(config, NullLogger<Mt5SidecarManager>.Instance);

        manager.ParseReadyLine("READY rep=5556 pub=5557");

        manager.Status.Should().Be(Mt5SidecarStatus.Running);
    }

    [Fact]
    public void ParseReadyLine_InvalidLine_DoesNotChangeStatus()
    {
        var config = new Mt5Config();
        var manager = new Mt5SidecarManager(config, NullLogger<Mt5SidecarManager>.Instance);

        manager.ParseReadyLine("some random output");

        manager.Status.Should().Be(Mt5SidecarStatus.Stopped);
    }

    [Fact]
    public void ParseReadyLine_FiresStatusChanged()
    {
        var config = new Mt5Config();
        var manager = new Mt5SidecarManager(config, NullLogger<Mt5SidecarManager>.Instance);
        Mt5SidecarStatus? receivedStatus = null;
        manager.StatusChanged += (_, status) => receivedStatus = status;

        manager.ParseReadyLine("READY rep=5556 pub=5557");

        receivedStatus.Should().Be(Mt5SidecarStatus.Running);
    }

    [Fact]
    public async Task DisposeAsync_WhenStopped_DoesNotThrow()
    {
        var config = new Mt5Config();
        var manager = new Mt5SidecarManager(config, NullLogger<Mt5SidecarManager>.Instance);

        var act = async () => await manager.DisposeAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void LastError_IsNull_Initially()
    {
        var config = new Mt5Config();
        var manager = new Mt5SidecarManager(config, NullLogger<Mt5SidecarManager>.Instance);

        manager.LastError.Should().BeNull();
    }
}
