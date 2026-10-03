using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace OtpBridge;

/// <summary>Types an arriving OTP straight into the focused text box, but only if that box is empty.</summary>
static class AutoFill
{
    /// <summary>Blocking (UI Automation calls into other processes); call off the UI thread.</summary>
    public static bool TryFill(string otp)
    {
        if (!FocusIsEmptyTextBox()) return false;
        Typer.Type(otp);
        return true;
    }

    static bool FocusIsEmptyTextBox()
    {
        try
        {
            var element = AutomationElement.FocusedElement;
            if (element is null) return false;
            var info = element.Current;
            if (info.ProcessId == Environment.ProcessId) return false;
            // Edit = single-line inputs, incl. web page <input>s. Documents (Word, Notepad) are left alone,
            // and so are password boxes so an OTP never lands in a login password field.
            if (info.ControlType != ControlType.Edit || !info.IsEnabled || info.IsPassword) return false;
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
            {
                var value = ((ValuePattern)pattern).Current;
                if (value.IsReadOnly || value.Value.Length > 0) return false;
            }
            return true;
        }
        catch (Exception e) when (e is ElementNotAvailableException or COMException or InvalidOperationException)
        {
            return false;
        }
    }
}
