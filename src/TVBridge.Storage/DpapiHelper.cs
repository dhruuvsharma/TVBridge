using System.Security.Cryptography;
using System.Text;

namespace TVBridge.Storage;

public static class DpapiHelper
{
    private static readonly byte[] Entropy = "TVBridge.v1"u8.ToArray();

    public static byte[] Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);
        var data = Encoding.UTF8.GetBytes(plainText);
        return ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
    }

    public static string Decrypt(byte[] encryptedData)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        var data = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(data);
    }
}
