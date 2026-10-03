# OTP Bridge: context for Claude Code

Mac-like OTP autofill for Windows + Android. An Android app catches OTP SMS and sends them AES-256-GCM
encrypted over the LAN to a Windows tray app, which shows a popup, copies the OTP (auto-cleared after 2 min)
and types it on Ctrl+Shift+O. It replaces Microsoft Phone Link, which kept disconnecting.
The owner is Chetan (GitHub: chetanajage). He talks in Hinglish, so reply in Hinglish.

## Layout
- `windows/OtpBridge/`: .NET 8 WinForms tray app (`TrayApp.cs`, `OtpServer.cs` TCP+UDP, `Protocol.cs`, `PairForm.cs` QR, `Native.cs` hotkey/SendInput/startup)
- `windows/Harness/`: cross-platform console host for `OtpServer`, used for e2e tests
- `android/`: Kotlin app (`SmsReceiver`, `OtpExtractor`, `Protocol`, `Relay`, `MainActivity`), minSdk 26
- `SETUP.md`: human step-by-step install guide (Hinglish). Keep it in sync when behaviour changes.
- Protocol: TCP 47321 carries one base64 line, `nonce12 + GCM ciphertext + tag`, with the JSON `{type,otp,from,ts}`. The PC answers `OK`/`ERR`.
  UDP 47322 handles discovery: `OTPBRIDGE_DISCOVER <keyId>` → `OTPBRIDGE_HERE <keyId> <port>`. `Protocol.kt` and `Protocol.cs` must stay in sync.

## Status (2026-10-03)
- Step 1 (phone app + tray app) is done: built on a Mac, 11 extractor tests pass, and the Kotlin → .NET e2e test (incl. UDP discovery) passes.
- **Never run on real Windows or a real phone yet.** First job on Windows: build, run, pair, test.
- Release v0.1.0 on GitHub has `OtpBridge.exe` + `OtpBridge.apk`.
- Next: Step 2 is a Chrome/Edge extension that shows a "📱 From Phone: 123456" suggestion under OTP fields.
  The plan is for the tray app to expose the latest OTP on a localhost-only endpoint for the extension. Step 3 is a floating "Fill" button for non-browser apps.

## Setting up on Windows (what Claude can do vs. the human)
Claude can:
- Run `dotnet publish windows\OtpBridge -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o C:\Tools\OtpBridge`
  (install the SDK first with `winget install Microsoft.DotNet.SDK.8` if `dotnet` is missing)
- Check Wi-Fi profile: `Get-NetConnectionProfile`. Must be `Private`; fixing needs admin:
  `Set-NetConnectionProfile -InterfaceAlias "<Wi-Fi>" -NetworkCategory Private`
- Check firewall: `Get-NetFirewallApplicationFilter -Program C:\Tools\OtpBridge\OtpBridge.exe`; admin fix:
  `New-NetFirewallRule -DisplayName OtpBridge -Direction Inbound -Program C:\Tools\OtpBridge\OtpBridge.exe -Action Allow -Profile Private`
- Verify listening: `Get-NetTCPConnection -LocalPort 47321 -State Listen`
- Download APK: `gh release download v0.1.0 -p OtpBridge.apk -D $HOME\Downloads`

The human must do these (ask them, give exact clicks from SETUP.md):
- `gh auth login` (interactive browser login) and any UAC/admin prompt
- Launching the tray app the first time and clicking Allow on the firewall dialog
- Everything on the phone: install the APK, SMS permission ("Allow restricted settings" on Android 13+), scan the QR, Send test OTP, Autostart on Xiaomi/Oppo/Vivo/Realme

Admin-only commands: tell the user to run them in an elevated PowerShell (or approve UAC) instead of failing silently.
