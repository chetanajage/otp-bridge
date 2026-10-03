using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace OtpBridge;

sealed class TrayApp : ApplicationContext
{
    static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);

    readonly SynchronizationContext _ui = SynchronizationContext.Current!;
    readonly Config _config = Config.Load();
    readonly NotifyIcon _tray;
    readonly ToolStripMenuItem _lastItem;
    readonly HotkeyWindow _hotkey = new(Keys.O);
    readonly System.Windows.Forms.Timer _clipboardClear = new() { Interval = 120_000 };
    OtpServer? _server;
    PairForm? _pairForm;
    string? _lastOtp;
    DateTime _lastAt;

    public TrayApp()
    {
        _lastItem = new ToolStripMenuItem("No OTP yet") { Enabled = false };
        _lastItem.Click += (_, _) => { if (CurrentOtp() is { } otp) CopyToClipboard(otp); };
        var startupItem = new ToolStripMenuItem("Start with Windows") { CheckOnClick = true, Checked = Startup.Enabled };
        startupItem.CheckedChanged += (_, _) => Startup.Enabled = startupItem.Checked;

        var menu = new ContextMenuStrip();
        menu.Items.Add(_lastItem);
        menu.Items.Add("Pair phone…", null, (_, _) => ShowPairing());
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        _tray = new NotifyIcon { Icon = SystemIcons.Shield, Text = "OTP Bridge", ContextMenuStrip = menu, Visible = true };
        _tray.DoubleClick += (_, _) => ShowPairing();
        _hotkey.Pressed += TypeLastOtp;
        _clipboardClear.Tick += (_, _) => ClearClipboardIfOtp();

        StartServer();
        if (_config.IsNew)
        {
            startupItem.Checked = true;
            ShowPairing();
        }
        if (!_hotkey.Registered)
            Notify("Ctrl+Shift+O is taken", "Another app uses this hotkey. OTPs will still be copied to the clipboard.");
    }

    void StartServer()
    {
        _server?.Dispose();
        _server = new OtpServer(_config.KeyBytes, _config.Port);
        _server.OtpReceived += m => _ui.Post(_ => OnOtp(m), null);
        _server.Error += e => _ui.Post(_ => Notify("OTP Bridge", e), null);
        try { _server.Start(); }
        catch (SocketException e) { Notify("OTP Bridge can't start", $"Port {_config.Port} or {Protocol.DiscoveryPort} is busy: {e.Message}"); }
    }

    void OnOtp(OtpMessage m)
    {
        _lastOtp = m.Otp;
        _lastAt = DateTime.Now;
        CopyToClipboard(m.Otp);
        _lastItem.Text = $"Last OTP: {m.Otp} (click to copy)";
        _lastItem.Enabled = true;
        var from = string.IsNullOrEmpty(m.From) ? "" : $"{m.From} • ";
        if (m.Type == "test")
        {
            _pairForm?.Close();
            Notify($"Test OTP {m.Otp} received ✓", "Phone is connected. Real OTPs will show up like this.");
        }
        else
        {
            Notify($"OTP {m.Otp}", $"{from}Copied. Paste with Ctrl+V or type it with Ctrl+Shift+O.");
        }
    }

    string? CurrentOtp() => _lastOtp is not null && DateTime.Now - _lastAt < OtpLifetime ? _lastOtp : null;

    void TypeLastOtp()
    {
        if (CurrentOtp() is { } otp) Typer.TypeWhenKeysReleased(otp);
        else Notify("No recent OTP", "No OTP received in the last 5 minutes.");
    }

    void CopyToClipboard(string otp)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try { Clipboard.SetText(otp); break; }
            catch (ExternalException) { Thread.Sleep(50); } // clipboard briefly locked by another app
        }
        _clipboardClear.Stop();
        _clipboardClear.Start();
    }

    /// <summary>Don't leave OTPs sitting in the clipboard.</summary>
    void ClearClipboardIfOtp()
    {
        _clipboardClear.Stop();
        try { if (_lastOtp is not null && Clipboard.GetText() == _lastOtp) Clipboard.Clear(); }
        catch (ExternalException) { }
    }

    void Notify(string title, string text) => _tray.ShowBalloonTip(5000, title, text, ToolTipIcon.Info);

    void ShowPairing()
    {
        if (_pairForm is { IsDisposed: false })
        {
            _pairForm.Activate();
            return;
        }
        var code = Protocol.PairCode(Environment.MachineName, Protocol.LocalIPv4s(), _config.Port, _config.KeyBytes);
        _pairForm = new PairForm(code);
        _pairForm.ResetRequested += () =>
        {
            _config.NewKey();
            _config.Save();
            StartServer();
            _ui.Post(_ => ShowPairing(), null);
        };
        _pairForm.Show();
        _pairForm.Activate();
    }

    protected override void ExitThreadCore()
    {
        _server?.Dispose();
        _hotkey.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        base.ExitThreadCore();
    }
}
