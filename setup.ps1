# OTP Bridge one-click setup for Windows. Start it by double-clicking setup.bat.
# Installs .NET 8 SDK if needed, builds OtpBridge.exe into C:\Tools\OtpBridge, adds the firewall rule,
# makes the Wi-Fi network Private, creates a desktop shortcut and starts the app.
# Safe to run again later to update (git pull first).

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

$InstallDir = 'C:\Tools\OtpBridge'
$Exe = Join-Path $InstallDir 'OtpBridge.exe'
$ApkUrl = 'https://github.com/chetanajage/otp-bridge/releases/download/v0.1.0/OtpBridge.apk'
$Project = Join-Path $PSScriptRoot 'windows\OtpBridge\OtpBridge.csproj'

function Step($n, $text) { Write-Host "`n[$n/6] $text" -ForegroundColor Cyan }
function Ok($text) { Write-Host "  OK  $text" -ForegroundColor Green }
function Warn($text) { Write-Host "  !!  $text" -ForegroundColor Yellow }
function Fail($text) {
    Write-Host "`n  XX  $text" -ForegroundColor Red
    Read-Host "`nEnter dabao window band karne ke liye"
    exit 1
}

# --- Admin (firewall + network profile need it) ---------------------------------------------
$identity = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $identity.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Admin permission chahiye (firewall aur Wi-Fi setting ke liye). Popup pe 'Yes' dabao..."
    try {
        Start-Process powershell -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    } catch {
        Fail "Admin permission nahi mili. setup.bat dobara chalao aur popup pe 'Yes' dabao."
    }
    exit 0
}

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "        OTP Bridge setup (Windows)" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# Smart App Control (Windows 11) blocks unsigned apps, including the OtpBridge.exe we build here.
$sacState = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -Name VerifiedAndReputablePolicyState -ErrorAction SilentlyContinue).VerifiedAndReputablePolicyState
if ($sacState -eq 1) {
    Warn "Smart App Control ON hai. Ye OtpBridge.exe ko block kar sakta hai (exe signed nahi hai)."
    Warn "Block ho to: Windows Security > App & browser control > Smart App Control settings > Off, phir setup.bat dobara."
}

if (-not (Test-Path $Project)) {
    Fail "windows\OtpBridge folder nahi mila. setup.bat ko repo folder (otp-bridge) ke andar se hi chalao."
}

# --- 1. .NET 8 SDK ------------------------------------------------------------------------------
function Find-Dotnet {
    $candidates = @("$env:ProgramFiles\dotnet\dotnet.exe", 'C:\Tools\dotnet-sdk\dotnet.exe')
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { $candidates = @($cmd.Source) + $candidates }
    foreach ($d in $candidates) {
        if ((Test-Path $d) -and (@(& $d --list-sdks 2>$null) -match '^8\.')) { return $d }
    }
    return $null
}

Step 1 ".NET 8 SDK check kar rahe hain (exe build karne ke liye)"
$dotnet = Find-Dotnet
if (-not $dotnet) {
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        Write-Host "  .NET 8 SDK install ho raha hai (2-5 min lagenge)..."
        winget install --id Microsoft.DotNet.SDK.8 -e --silent --accept-source-agreements --accept-package-agreements
        $dotnet = Find-Dotnet
    }
    if (-not $dotnet) {
        Warn "winget se nahi hua, direct download kar rahe hain..."
        $script = Join-Path $env:TEMP 'dotnet-install.ps1'
        Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $script -UseBasicParsing
        & $script -Channel 8.0 -InstallDir 'C:\Tools\dotnet-sdk'
        $dotnet = Find-Dotnet
    }
    if (-not $dotnet) { Fail ".NET 8 SDK install nahi hua. Internet check karke setup.bat dobara chalao." }
}
Ok ".NET SDK mil gaya: $dotnet"

# --- 2. Build -----------------------------------------------------------------------------------
Step 2 "OtpBridge.exe build ho raha hai -> $InstallDir (pehli baar 2-3 min)"
Get-Process OtpBridge -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
& $dotnet publish $Project -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $InstallDir -v quiet
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $Exe)) { Fail "Build fail hua. Upar ka error ka screenshot bhejo." }
Remove-Item (Join-Path $InstallDir '*.pdb') -ErrorAction SilentlyContinue
Ok "Ban gaya: $Exe"

# --- 3. Firewall --------------------------------------------------------------------------------
Step 3 "Firewall mein OTP Bridge ko allow kar rahe hain"
Get-NetFirewallRule -DisplayName 'OTP Bridge' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
New-NetFirewallRule -DisplayName 'OTP Bridge' -Direction Inbound -Program $Exe -Action Allow -Profile Private, Domain | Out-Null
Ok "Firewall rule add ho gaya (Private network)"

# --- 4. Network profile -------------------------------------------------------------------------
Step 4 "Wi-Fi network check kar rahe hain (Private hona chahiye)"
$profiles = @(Get-NetConnectionProfile | Where-Object { $_.IPv4Connectivity -ne 'Disconnected' })
if ($profiles.Count -eq 0) { Warn "Koi network connected nahi hai. Wi-Fi connect karke setup dobara chalao." }
foreach ($p in $profiles) {
    if ($p.NetworkCategory -eq 'Public') {
        $answer = Read-Host "  '$($p.Name)' network abhi Public hai. Ghar/office ka Wi-Fi hai to Private kar dein? (Y/n)"
        if ($answer -notmatch '^[nN]') {
            Set-NetConnectionProfile -InterfaceIndex $p.InterfaceIndex -NetworkCategory Private
            Ok "'$($p.Name)' ab Private hai"
        } else {
            Warn "'$($p.Name)' Public hi rakha. Is network pe phone PC tak nahi pahunch payega."
        }
    } else {
        Ok "'$($p.Name)': $($p.NetworkCategory)"
    }
}

# --- 5. Desktop shortcut ------------------------------------------------------------------------
Step 5 "Desktop pe shortcut bana rahe hain"
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'OTP Bridge.lnk'))
$shortcut.TargetPath = $Exe
$shortcut.WorkingDirectory = $InstallDir
$shortcut.Save()
Ok "Desktop pe 'OTP Bridge' shortcut ban gaya"

# --- 6. Start (as the normal user, not admin) ----------------------------------------------------
Step 6 "OTP Bridge start kar rahe hain"
Start-Process explorer.exe -ArgumentList "`"$Exe`""
Ok "App chalu. Pairing window (QR code) khulegi. Tray mein shield icon aayega."

Write-Host "`n==============================================" -ForegroundColor Green
Write-Host " PC ka setup ho gaya! Ab phone pe:" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
Write-Host " 1. Phone ke browser mein ye link kholo aur APK install karo:"
Write-Host "    $ApkUrl" -ForegroundColor Yellow
Write-Host " 2. App kholo -> SMS permission 'Allow'"
Write-Host "    (block ho to: Settings > Apps > OTP Bridge > 3 dots > Allow restricted settings)"
Write-Host " 3. 'Scan QR from PC' -> PC pe khuli window ka QR scan karo"
Write-Host " 4. 'Send test OTP' -> PC pe 'Test OTP received' popup aana chahiye"
Write-Host " 5. 'Allow running in background' dabao (Xiaomi/Oppo/Vivo pe Autostart bhi on karo)"
Write-Host "`n Pairing window band ho gayi ho to: desktop pe 'OTP Bridge' ya tray icon pe double-click."
Read-Host "`nEnter dabao window band karne ke liye"
