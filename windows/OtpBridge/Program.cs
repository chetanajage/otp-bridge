namespace OtpBridge;

static class Program
{
    /// <summary>Passed by the "Start with Windows" entry so a boot doesn't pop the QR window.</summary>
    public const string BackgroundArg = "--background";

    [STAThread]
    static void Main(string[] args)
    {
        // Created before the mutex check so a second launch can always find it.
        using var showPairing = new EventWaitHandle(false, EventResetMode.AutoReset, "OtpBridge.ShowPairing");
        using var mutex = new Mutex(true, "OtpBridge.SingleInstance", out bool first);
        if (!first)
        {
            // Already running (e.g. desktop shortcut clicked): ask that instance to show the QR window.
            showPairing.Set();
            return;
        }
        ApplicationConfiguration.Initialize();
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        Application.Run(new TrayApp(showPairing, showPairingOnStart: !args.Contains(BackgroundArg)));
    }
}
