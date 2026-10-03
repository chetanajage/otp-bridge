namespace OtpBridge;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "OtpBridge.SingleInstance", out bool first);
        if (!first)
        {
            MessageBox.Show("OTP Bridge is already running. Look for its icon in the system tray.", "OTP Bridge");
            return;
        }
        ApplicationConfiguration.Initialize();
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        Application.Run(new TrayApp());
    }
}
