using FluentAssertions;
using TVBridge.Webhook;
using Xunit;

namespace TVBridge.Webhook.Tests;

public sealed class WebhookSecretValidatorTests
{
    [Fact]
    public void Validate_MatchingSecret_ReturnsTrue()
    {
        var validator = new WebhookSecretValidator(() => "my-secret-123");
        validator.Validate("my-secret-123").Should().BeTrue();
    }

    [Fact]
    public void Validate_NonMatchingSecret_ReturnsFalse()
    {
        var validator = new WebhookSecretValidator(() => "my-secret-123");
        validator.Validate("wrong-secret").Should().BeFalse();
    }

    [Fact]
    public void Validate_NullStoredSecret_ReturnsFalse()
    {
        var validator = new WebhookSecretValidator(() => null);
        validator.Validate("any-secret").Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyStoredSecret_ReturnsFalse()
    {
        var validator = new WebhookSecretValidator(() => "");
        validator.Validate("any-secret").Should().BeFalse();
    }

    [Fact]
    public void Validate_CaseSensitive()
    {
        var validator = new WebhookSecretValidator(() => "MySecret");
        validator.Validate("mysecret").Should().BeFalse();
    }
}
