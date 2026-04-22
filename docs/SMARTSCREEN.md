# Windows SmartScreen Warning

When you first run TVBridge or the installer, Windows SmartScreen may show a warning:
**"Windows protected your PC — Microsoft Defender SmartScreen prevented an unrecognized app from starting."**

This happens because TVBridge is not code-signed with an Extended Validation (EV) certificate.

## How to proceed

1. Click **"More info"**
2. Click **"Run anyway"**

This is a one-time action — Windows remembers your choice.

## Why this happens

- Code signing certificates cost $200-400/year (EV certs cost more)
- TVBridge is a free, open-source project
- SmartScreen warnings disappear once an app accumulates enough downloads/reputation
- The app is MIT-licensed and fully open-source — you can audit every line of code

## Verifying the download

To verify the installer hasn't been tampered with, check the SHA-256 hash published in the GitHub Release:

```powershell
Get-FileHash TVBridge_Setup_0.1.0.exe -Algorithm SHA256
```

Compare the output with the hash listed on the release page.

## Future plans

Once TVBridge has enough users/downloads, we plan to:
1. Add a code-signing certificate (community-funded or sponsored)
2. Submit to Windows Defender for reputation building
