using FluentAssertions;
using Xunit;

namespace TVBridge.Storage.Tests;

public sealed class DpapiHelperTests
{
    [Fact]
    public void Encrypt_Decrypt_RoundTrip_ReturnsOriginal()
    {
        const string original = "my-secret-webhook-token-123";

        var encrypted = DpapiHelper.Encrypt(original);
        var decrypted = DpapiHelper.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void Encrypt_ProducesDifferentBytes_ThanInput()
    {
        const string input = "plain-text-secret";

        var encrypted = DpapiHelper.Encrypt(input);

        encrypted.Should().NotBeEquivalentTo(System.Text.Encoding.UTF8.GetBytes(input));
    }

    [Fact]
    public void Encrypt_EmptyString_Throws()
    {
        var act = () => DpapiHelper.Encrypt("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Encrypt_Null_Throws()
    {
        var act = () => DpapiHelper.Encrypt(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrypt_Null_Throws()
    {
        var act = () => DpapiHelper.Decrypt(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Encrypt_DifferentInputs_ProduceDifferentOutputs()
    {
        var enc1 = DpapiHelper.Encrypt("secret-1");
        var enc2 = DpapiHelper.Encrypt("secret-2");

        enc1.Should().NotBeEquivalentTo(enc2);
    }
}
