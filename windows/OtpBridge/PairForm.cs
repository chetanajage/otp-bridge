using QRCoder;

namespace OtpBridge;

sealed class PairForm : Form
{
    public event Action? ResetRequested;

    public PairForm(string code)
    {
        Text = "OTP Bridge: pair phone";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        var steps = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(340, 0),
            Text = "1. Phone pe OTP Bridge app kholo\n" +
                   "2. \"Scan QR from PC\" dabao aur ye QR scan karo\n" +
                   "3. \"Send test OTP\" dabao, yahan notification aana chahiye\n\n" +
                   "Phone aur PC same Wi-Fi pe hone chahiye. Windows Firewall puche to \"Allow\" karna.",
        };
        var qr = new PictureBox { Image = MakeQr(code), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(320, 320) };
        var codeBox = new TextBox { Text = code, ReadOnly = true, Width = 320 };
        var copy = new Button { Text = "Copy code", AutoSize = true };
        copy.Click += (_, _) => Clipboard.SetText(code);
        var reset = new Button { Text = "Unpair all phones (new key)", AutoSize = true };
        reset.Click += (_, _) =>
        {
            if (MessageBox.Show("Already paired phones will stop working until re-paired. Continue?", "OTP Bridge",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            ResetRequested?.Invoke();
            Close();
        };

        var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0) };
        buttons.Controls.AddRange([copy, reset]);
        var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        layout.Controls.AddRange([steps, qr, codeBox, buttons]);
        Controls.Add(layout);
    }

    static Image MakeQr(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(10);
        return Image.FromStream(new MemoryStream(png)); // stream must stay open for the Image's lifetime
    }
}
