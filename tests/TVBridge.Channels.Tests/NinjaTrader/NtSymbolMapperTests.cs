using FluentAssertions;
using TVBridge.Channels.NinjaTrader;
using Xunit;

namespace TVBridge.Channels.Tests.NinjaTrader;

public sealed class NtSymbolMapperTests
{
    [Fact]
    public void Map_KnownSymbol_ReturnsMapped()
    {
        var mapper = new NtSymbolMapper(new Dictionary<string, string> { ["EURUSD"] = "EUR/USD" });

        mapper.Map("EURUSD").Should().Be("EUR/USD");
    }

    [Fact]
    public void Map_UnknownSymbol_ReturnOriginal()
    {
        var mapper = new NtSymbolMapper(new Dictionary<string, string> { ["EURUSD"] = "EUR/USD" });

        mapper.Map("XAUUSD").Should().Be("XAUUSD");
    }

    [Fact]
    public void Map_CaseInsensitive()
    {
        var mapper = new NtSymbolMapper(new Dictionary<string, string> { ["EURUSD"] = "EUR/USD" });

        mapper.Map("eurusd").Should().Be("EUR/USD");
    }
}
