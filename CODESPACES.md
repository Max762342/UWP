# XB-Launcher – GitHub Codespaces Anleitung

## Wichtiger Hinweis

**UWP/Windows-Desktop-Apps können NICHT vollständig in GitHub Codespaces gebaut werden**, weil:
- Codespaces auf **Linux** laufen
- WPF/UWP **Windows-spezifische DLLs** benötigt
- `makeappx.exe` / UWP-Build **nur auf Windows** funktioniert

## Was in Codespaces geht

✅ **Code ansehen und bearbeiten**  
✅ **C#-Syntax prüfen** (IntelliSense, Fehlermeldungen)  
✅ **Logik-Code kompilieren** (XBLauncher.Core – keine UI)  
✅ **Tests schreiben und ausführen**  

## Was NICHT in Codespaces geht

❌ **WPF-UI kompilieren** (XBLauncher.App)  
❌ **UWP-Paket erstellen** (XBLauncher.Uwp)  
❌ **Xbox APPX bauen und signieren**  

## So baust du die App (außerhalb von Codespaces)

### Option 1: Windows-PC (empfohlen)
```powershell
# Repository klonen
git clone https://github.com/DEIN_USERNAME/XB-Launcher.git
cd XB-Launcher

# Build ausführen
powershell -ExecutionPolicy Bypass -File scripts\build-appx.ps1
```

### Option 2: Windows-Cloud-VM
- **Azure Windows VM** (kostenloses Guthaben)
- **AWS EC2 Windows Instance**
- **Windows 365 Cloud PC**

Dort Visual Studio installieren und bauen.

### Option 3: GitHub Actions (Windows Runner)
Erstelle `.github/workflows/build.yml`:

```yaml
name: Build XB-Launcher
on: [push]
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - name: Build
        run: |
          dotnet publish src/XBLauncher.Uwp/XBLauncher.Uwp.csproj -c Release -r win-x64
      - name: Upload Artifact
        uses: actions/upload-artifact@v3
        with:
          name: XB-Launcher
          path: output/
```

## Projektstruktur

```
XB-Launcher/
├── src/
│   ├── XBLauncher.Core/     # Logik (kann in Codespaces gebaut werden)
│   ├── XBLauncher.App/      # WPF-UI (braucht Windows)
│   └── XBLauncher.Uwp/      # UWP-Wrapper (braucht Windows SDK)
├── packaging/               # AppxManifest.xml
├── scripts/                 # Build-Skripte
└── .devcontainer/           # Codespaces-Konfiguration
```

## Fertiges Paket herunterladen

Das bereits gebaute Paket befindet sich in:
- `output/XB-Launcher.appx` (82 MB)
- `output/XB-Launcher.pfx` (Signaturzertifikat)

Diese Dateien können direkt auf die Xbox installiert werden.