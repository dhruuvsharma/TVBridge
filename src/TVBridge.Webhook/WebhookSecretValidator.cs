using System.Security.Cryptography;
using System.Text;

namespace TVBridge.Webhook;

public sealed class WebhookSecretValidator
{
    private readonly Func<string?> _getStoredSecret;

    public WebhookSecretValidator(Func<string?> getStoredSecret)
    {
        _getStoredSecret = getStoredSecret;
    }

    public bool Validate(string incomingSecret)
    {
        var storedSecret = _getStoredSecret();
        if (string.IsNullOrEmpty(storedSecret))
            return false;

        var incoming = Encoding.UTF8.GetBytes(incomingSecret);
        var stored = Encoding.UTF8.GetBytes(storedSecret);

        return CryptographicOperations.FixedTimeEquals(incoming, stored);
    }
}
