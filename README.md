# OTP Bridge

Android phone pe aaya OTP SMS turant Windows PC pe: popup + clipboard + `Ctrl+Shift+O` se type.
Phone Link ki zaroorat nahi. Phone aur PC same Wi-Fi pe hone chahiye.

```
SMS → Android app (OTP nikalta hai) ──AES-256-GCM, Wi-Fi──▶ Windows tray app → popup + clipboard
```

## Setup

Windows pe: repo clone karo → `setup.bat` pe double-click → sab apne aap install ho jayega.
Poore step-by-step instructions (Windows + Android): **[SETUP.md](SETUP.md)**

## Build (developers)
- Windows: `cd windows && dotnet publish OtpBridge -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o ../dist/windows`
- Android: `cd android && ./gradlew testDebugUnitTest assembleDebug`
- End-to-end test (kisi bhi OS pe): `dotnet run --project windows/Harness` chalao, phir
  `OTPB_E2E_CODE="<PAIRCODE>" ./gradlew testDebugUnitTest --tests '*endToEnd*'`

Ports: TCP 47321 (OTP), UDP 47322 (PC ka IP badle to auto-discovery).

## Roadmap
- [x] Step 1: Android app + Windows tray app
- [ ] Step 2: Chrome/Edge extension (OTP field ke neeche "📱 From Phone: 482913" suggestion)
- [ ] Step 3: Browser ke bahar ke apps ke liye floating "Fill" button
