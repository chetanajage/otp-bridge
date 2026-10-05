# OTP Bridge: Setup Guide

Pehle PC (Windows) setup karo, phir phone (Android). Total 10 minute lagenge.

**Zaroori:** phone aur PC same Wi-Fi pe hone chahiye.

## Asaan setup: ek double-click (recommended)

**Step 1: Repo PC pe lao.** Dono mein se koi ek tareeka:

- **Git se clone** (PowerShell mein):
  ```powershell
  winget install --id Git.Git -e
  ```
  PowerShell band karke dobara kholo, phir:
  ```powershell
  cd $HOME\Documents
  git clone https://github.com/chetanajage/otp-bridge.git
  ```
- **Bina Git ke:** https://github.com/chetanajage/otp-bridge kholo → hara **Code** button → **Download ZIP** →
  ZIP pe right-click → **Extract All**.

> **ZIP download kiya hai?** Extract karne ke baad folder mein PowerShell kholo (folder mein khali jagah pe
> Shift + right-click → "Open PowerShell window here") aur ye chalao, warna Windows files block karega:
> ```powershell
> Get-ChildItem -Recurse | Unblock-File
> ```
> Git se clone kiya hai to iski zaroorat nahi.

**Step 2: `setup.bat` pe double-click karo.**
Repo folder (`Documents\otp-bridge`) kholo aur `setup.bat` pe double-click karo.
- "Windows protected your PC" aaye to **More info → Run anyway**.
- Admin popup (UAC) aaye to **Yes**.

Ek PowerShell window khulegi aur khud ye 6 kaam karegi:
1. .NET 8 SDK install (agar pehle se nahi hai)
2. `OtpBridge.exe` build karke `C:\Tools\OtpBridge` mein rakhna
3. Firewall mein allow karna
4. Wi-Fi Public ho to poochega "Private kar dein? (Y/n)". Ghar ya office ka Wi-Fi hai to **Y** dabao.
5. Desktop pe "OTP Bridge" shortcut banana (baad mein QR dobara dekhna ho to isi pe double-click)
6. App start karna. Pairing window (QR code) khul jayegi.

Pehli baar 5-10 minute lag sakte hain. Window band mat karna. Kahin laal **XX** error aaye to us window ka
screenshot bhejo.

**Step 3: Phone.** Window ke end mein phone ke steps bhi likhe aayenge. Neeche **Android setup** section dekho.
APK ka seedha link (phone ke browser mein kholo):
https://github.com/chetanajage/otp-bridge/releases/latest/download/OtpBridge.apk

**Baad mein update karna ho to:** repo folder mein `git pull` (ZIP wale naya ZIP download karein), phir `setup.bat`
dobara double-click karo. Pairing waise hi rahegi.

## Manual Windows setup (setup.bat na chale tab)

Release page se exe download karo: https://github.com/chetanajage/otp-bridge/releases/latest → `OtpBridge.exe`.


**Step 1: Exe sahi jagah rakho**
1. File Explorer kholo → `C:\` drive → naya folder banao `Tools`, uske andar `OtpBridge`.
2. `OtpBridge.exe` ko `C:\Tools\OtpBridge\` mein move karo.
   (Downloads mein mat chhodo. "Start with Windows" isi path ko yaad rakhta hai, file hilaoge to auto-start toot jayega.)

**Step 2: Pehli baar chalao**
1. `OtpBridge.exe` pe double-click karo.
2. Blue screen "Windows protected your PC" aaye to **More info** → **Run anyway**.
   (App signed nahi hai isliye ye warning aati hai. Sirf pehli baar aayegi.)

**Step 3: Firewall allow karo (zaroori)**
1. "Windows Security Alert" window aayegi.
2. **Private networks** pe tick ✓ hona chahiye → **Allow access**.
3. Galti se Cancel ho gaya to: Start → "Allow an app through Windows Firewall" → **Change settings** →
   list mein **OtpBridge** → **Private** ✓ → OK.

**Step 4: Wi-Fi ko "Private" network banao**
Firewall sirf Private network pe allow karta hai. Wi-Fi Public pe set ho to phone connect nahi hoga.
1. Settings → **Network & internet** → **Wi-Fi** → apne Wi-Fi ka naam (Properties).
2. **Network profile type** → **Private network** select karo.

**Step 5: Pairing window**
1. Pehli baar chalane pe "OTP Bridge: pair phone" window khulti hai, usme QR code dikhega.
2. Ab phone wale steps karo (isi file mein neeche "Android setup"), phone se ye QR scan karna hai.
3. Phone se "Send test OTP" dabao. PC pe neeche-right **"Test OTP … received ✓"** popup aayega aur window apne aap band ho jayegi.

**Step 6: Tray icon dhoondho**
- App window ke bina background mein chalta hai. Taskbar pe right side **^** (hidden icons) dabao, wahan 🛡 shield icon milega.
- Hamesha dikhe, iske liye icon ko drag karke taskbar pe chhod do.
- **Double-click** (tray icon ya desktop ka "OTP Bridge" shortcut): pairing window (QR) dobara khulti hai.
- **Right-click** menu:
  - *Last OTP*: click karke dobara copy karo
  - *Pair phone…*: QR dikhao / naya phone jodo
  - *Start with Windows*: PC on hote hi app chalu (default ✓)
  - *Exit*: app band

**Update karna ho to:** tray → Exit → naya `OtpBridge.exe` purane ki jagah `C:\Tools\OtpBridge\` mein replace karo → double-click. Pairing waise hi rahegi.

**Hatana ho to:** tray → *Start with Windows* ka tick hatao → Exit → `C:\Tools\OtpBridge` folder delete karo →
`Win+R` → `%APPDATA%\OtpBridge` → wo folder bhi delete karo.

## Android setup

`OtpBridge.apk`: Windows Step 5 pe pairing window khuli ho tab ye karo.
1. APK phone pe bhejo aur install karo ("Install unknown apps" allow karna padega).
   Play Protect "unsafe app blocked" bole aur install na ho (India mein SMS permission wale apps ke saath hota hai):
   Play Store → profile photo → **Play Protect** → ⚙️ → **Scan apps with Play Protect** off → APK install karo →
   Play Protect wapas **on** karo.
2. App kholo → SMS permission **Allow**.
   WhatsApp / browser / file manager se install kiya ho to Android 13+ SMS permission block kar deta hai ("Restricted setting"):
   Settings → Apps → OTP Bridge → ⋮ (upar right) → **Allow restricted settings** → PIN daalo → Permissions → SMS → **Allow**.
   ⋮ mein option na dikhe to pehle ek baar SMS Allow karne ki koshish karo, "Restricted setting" aane ke baad ⋮ mein aa jata hai.
   Ye nahi kiya to test OTP PC pe aayega lekin real OTP nahi.
3. **Scan QR from PC** dabao → PC ka QR scan karo.
4. **Send test OTP** dabao → PC pe "Test OTP received ✓" aana chahiye.
5. **Allow running in background** dabao. Xiaomi/Oppo/Vivo/Realme pe Settings mein OTP Bridge ka **Autostart** bhi on karo.

## Use
**Auto-fill (Mac jaisa):** pehle OTP wale box pe click karo (cursor wahan blink kare), phir OTP mangwao.
SMS aate hi OTP seedha us box mein type ho jayega, kuch dabana nahi padega.

Auto-fill tabhi hota hai jab cursor kisi **khaali** text box mein ho. Cursor chat, Word ya kisi bhare hue box mein
ho to galti se wahan number na chhape, isliye tab sirf popup aata hai aur OTP copy ho jata hai:
- OTP box pe click karke `Ctrl+V` dabao, ya
- `Ctrl+Shift+O` dabao, OTP cursor wale box mein type ho jayega (5 min tak).

Auto-fill band karna ho to: tray icon pe right-click → **Auto-fill OTP into empty text box** ka tick hatao.
2 minute baad OTP clipboard se apne aap hat jata hai.

## Problem aaye to
| Problem | Fix |
|---|---|
| OTP box mein apne aap nahi aaya | OTP mangwane se *pehle* box pe click karo. Box khaali hona chahiye. Kuch apps mein auto-fill nahi chalta, wahan `Ctrl+V` karo |
| "Smart App Control blocked a file" | Pehle files unblock karo (Step 1 ka note) ya Git se clone karo. Phir bhi block ho to: Windows Security → App & browser control → Smart App Control settings → **Off** |
| Test OTP pe "Couldn't reach" | Same Wi-Fi? PC app tray mein chal raha hai? Firewall: Windows Security → Firewall → Allow an app → OtpBridge (Private ✓) |
| Install pe "unsafe app blocked" (Play Protect) | Android setup Step 1: Play Protect thodi der off, install, phir on |
| Real OTP nahi aata, test aata hai | 1) App info → Permissions → SMS **Allowed** hai? Grey / "Restricted" ho to ⋮ → **Allow restricted settings**, phir SMS Allow (Android setup Step 2). 2) Autostart / battery "No restrictions" (OnePlus: Battery → **Allow background activity** on). 3) Play Protect ne baad mein permission hata di ho to dobara check karo |
| "clocks differ" message | Phone aur PC dono pe automatic time on karo |
| Naya phone ya galat pairing | Tray → Pair phone… → "Unpair all phones", phir dobara scan |
