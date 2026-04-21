using FluentAssertions;
using Xunit;

namespace TVBridge.Core.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void Signal_Record_CanBeCreated()
    {
        var signal = new Signal();
        signal.Should().NotBeNull();
    }

    [Fact]
    public void Rule_Record_CanBeCreated()
    {
        var rule = new Rule();
        rule.Should().NotBeNull();
    }
}
