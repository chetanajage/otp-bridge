using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OtpBridge;

/// <summary>Stored at %APPDATA%\OtpBridge\config.json.</summary>
sealed class Config
{
    public string Key { get; set; } = "";
    public int Port { get; set; } = Protocol.DefaultPort;
    public bool AutoFill { get; set; } = true;

    [JsonIgnore] public bool IsNew { get; private set; }
    [JsonIgnore] public byte[] KeyBytes => Convert.FromBase64String(Key);

    static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OtpBridge");
    static string FilePath => Path.Combine(Dir, "config.json");

    public static Config Load()
    {
        try
        {
            var existing = JsonSerializer.Deserialize<Config>(File.ReadAllText(FilePath));
            if (existing is not null && existing.KeyBytes.Length == 32) return existing;
        }
        catch (Exception e) when (e is IOException or JsonException or FormatException or UnauthorizedAccessException) { }

        var config = new Config { IsNew = true };
        config.NewKey();
        config.Save();
        return config;
    }

    public void NewKey() => Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
    }
}
