using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OtpBridge;

/// <summary>Receives encrypted OTPs from the phone over TCP and answers discovery probes over UDP. UI-free.</summary>
public sealed class OtpServer : IDisposable
{
    const int MaxLine = 8192;
    const long MaxClockSkewSeconds = 600;

    readonly byte[] _key;
    readonly int _port;
    readonly string _keyId;
    readonly CancellationTokenSource _cts = new();
    readonly Dictionary<string, DateTime> _seen = new();
    TcpListener? _tcp;
    UdpClient? _udp;

    public event Action<OtpMessage>? OtpReceived;
    public event Action<string>? Error;

    public OtpServer(byte[] key, int port)
    {
        _key = key;
        _port = port;
        _keyId = Protocol.KeyId(key);
    }

    public void Start()
    {
        _tcp = new TcpListener(IPAddress.Any, _port);
        _tcp.Start();
        _ = AcceptLoop();
        _udp = new UdpClient(new IPEndPoint(IPAddress.Any, Protocol.DiscoveryPort));
        _ = DiscoveryLoop();
    }

    async Task AcceptLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _tcp!.AcceptTcpClientAsync(_cts.Token); }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException) { return; }
            catch (SocketException) { continue; }
            _ = Handle(client);
        }
    }

    async Task Handle(TcpClient client)
    {
        using (client)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                timeout.CancelAfter(5000);
                var stream = client.GetStream();
                var line = await ReadLine(stream, timeout.Token);
                var msg = line is null ? null : Accept(line);
                await stream.WriteAsync(Encoding.ASCII.GetBytes(msg is null ? "ERR\n" : "OK\n"), timeout.Token);
                if (msg is not null) OtpReceived?.Invoke(msg);
            }
            catch (Exception e) when (e is IOException or OperationCanceledException or SocketException) { }
        }
    }

    static async Task<string?> ReadLine(NetworkStream stream, CancellationToken ct)
    {
        var buf = new byte[MaxLine];
        int n = 0;
        while (n < buf.Length)
        {
            int read = await stream.ReadAsync(buf.AsMemory(n), ct);
            if (read == 0) return null;
            int newline = Array.IndexOf(buf, (byte)'\n', n, read);
            n += read;
            if (newline >= 0) return Encoding.ASCII.GetString(buf, 0, newline);
        }
        return null;
    }

    OtpMessage? Accept(string line)
    {
        var msg = Protocol.Decrypt(_key, line);
        if (msg is null) return null;
        if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - msg.Ts) > MaxClockSkewSeconds)
        {
            Error?.Invoke("Rejected an OTP: phone and PC clocks differ by more than 10 minutes.");
            return null;
        }
        lock (_seen)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-2 * MaxClockSkewSeconds);
            foreach (var old in _seen.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList()) _seen.Remove(old);
            if (!_seen.TryAdd(line, DateTime.UtcNow)) return null; // replayed message
        }
        return msg;
    }

    async Task DiscoveryLoop()
    {
        var probe = $"OTPBRIDGE_DISCOVER {_keyId}";
        var reply = Encoding.ASCII.GetBytes($"OTPBRIDGE_HERE {_keyId} {_port}");
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var result = await _udp!.ReceiveAsync(_cts.Token);
                if (Encoding.ASCII.GetString(result.Buffer) == probe)
                    await _udp.SendAsync(reply, result.RemoteEndPoint, _cts.Token);
            }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException) { return; }
            catch (SocketException) { } // e.g. ICMP port unreachable from a previous reply
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _tcp?.Stop();
        _udp?.Dispose();
    }
}
