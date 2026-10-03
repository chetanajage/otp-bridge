using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;

namespace OtpBridge;

public sealed record OtpMessage(string Type, string Otp, string From, long Ts);

/// <summary>
/// Must stay in sync with android/.../Protocol.kt.
/// TCP line: base64(nonce[12] + AES-256-GCM ciphertext + tag[16]) of a JSON <see cref="OtpMessage"/>.
/// UDP discovery: "OTPBRIDGE_DISCOVER {keyId}" -> "OTPBRIDGE_HERE {keyId} {port}".
/// </summary>
public static class Protocol
{
    public const int DefaultPort = 47321;
    public const int DiscoveryPort = 47322;

    static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static string KeyId(byte[] key) => Convert.ToHexString(SHA256.HashData(key))[..8].ToLowerInvariant();

    public static string PairCode(string name, IEnumerable<string> hosts, int port, byte[] key) =>
        string.Join("|", "OTPB1", name.Replace("|", ""), string.Join(",", hosts), port, Convert.ToBase64String(key));

    public static OtpMessage? Decrypt(byte[] key, string line)
    {
        try
        {
            var data = Convert.FromBase64String(line.Trim());
            if (data.Length < 12 + 16) return null;
            var plaintext = new byte[data.Length - 28];
            using var gcm = new AesGcm(key, 16);
            gcm.Decrypt(data.AsSpan(0, 12), data.AsSpan(12, plaintext.Length), data.AsSpan(data.Length - 16), plaintext);
            var msg = JsonSerializer.Deserialize<OtpMessage>(plaintext, JsonOptions);
            return msg is { Otp.Length: > 0 } ? msg : null;
        }
        catch (Exception e) when (e is FormatException or CryptographicException or JsonException)
        {
            return null;
        }
    }

    /// <summary>LAN IPv4 addresses, interfaces with a default gateway (real Wi-Fi/Ethernet) first.</summary>
    public static List<string> LocalIPv4s() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                        && n.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
            .Select(n => n.GetIPProperties())
            .OrderByDescending(p => p.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
            .SelectMany(p => p.UnicastAddresses.Select(u => u.Address))
            .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !a.ToString().StartsWith("169.254."))
            .Select(a => a.ToString())
            .Distinct()
            .Take(3)
            .ToList();
}
