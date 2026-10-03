using System.Security.Cryptography;
using OtpBridge;

// Usage: dotnet run [base64Key] [port]
var key = args.Length > 0 ? Convert.FromBase64String(args[0]) : RandomNumberGenerator.GetBytes(32);
var port = args.Length > 1 ? int.Parse(args[1]) : Protocol.DefaultPort;

if (Protocol.KeyId(new byte[32]) != "66687aad") throw new Exception("KeyId mismatch with Android");

using var server = new OtpServer(key, port);
server.OtpReceived += m => Console.WriteLine($"RECEIVED type={m.Type} otp={m.Otp} from={m.From}");
server.Error += e => Console.WriteLine($"ERROR {e}");
server.Start();
Console.WriteLine($"PAIRCODE {Protocol.PairCode("harness", ["127.0.0.1"], port, key)}");
await Task.Delay(Timeout.Infinite);
