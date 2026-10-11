# XB-Launcher Release v1.0

## 📦 Download

**XB-Launcher-Release.zip** (75 MB) enthält:
- `XB-Launcher.appx` – Die installierbare App (82 MB)
- `XB-Launcher.pfx` – Signaturzertifikat

## 🎮 Installation auf Xbox (Dev Mode)

### Schritt 1: Entwicklermodus aktivieren
1. Öffne **Einstellungen** → **System** → **Konsoleninfo**
2. Aktiviere **Entwicklermodus**
3. Notiere die **IP-Adresse** der Xbox

### Schritt 2: Device Portal öffnen
1. Öffne auf deinem PC/Tablet: `https://<XBOX-IP>`
2. Klicke auf **"Xbox Device Portal"**
3. Gehe zu **"Apps"** → **"Apps installieren"**

### Schritt 3: App installieren
1. Lade `XB-Launcher.appx` hoch
2. Warte auf die Installation
3. Beim ersten Start: Zertifikat `XB-Launcher.pfx` (Passwort: `xblauncher`) zulassen

### Alternative: Per Kommandozeile
```powershell
WinAppDeployCmd.exe -install -ip <XBOX-IP> -file XB-Launcher.appx
```

## ✨ Features

- ✅ **Microsoft-Anmeldung** (Device-Code-Flow)
- ✅ **Ownership-Prüfung** (Minecraft: Java Edition erforderlich)
- ✅ **Alle Versionen** 1.21 – 26.4
- ✅ **Mod-Loader**: Vanilla / Fabric / NeoForge / Forge
- ✅ **Mods Hub** (Modrinth)
- ✅ **Shader Hub** (Modrinth)
- ✅ **Packs Hub** (Resource-/Textur-Packs)
- ✅ **Profile** (eigener Spielordner, RAM-Limit)
- ✅ **Automatische Java-Bereitstellung** (Temurin JRE 21/25)
- ✅ **Offizielle Mojang-Downloads** mit SHA1-Verifikation

## 🔧 Technische Details

- **Plattform**: Xbox Series X|S (Dev Mode)
- **Framework**: .NET 10 + WPF
- **Architektur**: x64
- **Pakettyp**: APPX (UWP/Desktop Bridge)
- **Mindest-OS**: Windows 10 17763

## 📁 Projektstruktur

```
XB-Launcher/
├── src/
│   ├── XBLauncher.Core/     # Kern-Logik (Auth, Downloads, Installer)
│   ├── XBLauncher.App/      # WPF-Oberfläche
│   └── XBLauncher.Uwp/      # UWP-Wrapper für Xbox
├── packaging/               # AppxManifest.xml
├── scripts/                 # Build-Skripte
├── native/                  # xb_shim.dll (Gamepad, Fenster)
└── output/                  # Fertiges Paket
```

## 🛠️ Building from Source

Siehe [CODESPACES.md](CODESPACES.md) für Details zu GitHub Codespaces.

### Schnellstart (Windows):
```powershell
git clone <repository-url>
cd XB-Launcher
powershell -ExecutionPolicy Bypass -File scripts\build-appx.ps1
```

## 📝 Lizenz

Für privaten Gebrauch. Minecraft, Java, Fabric, Forge, NeoForge und Modrinth unterliegen ihren eigenen Lizenzen.

## ⚠️ Wichtig

- **Pflicht**: Microsoft-Konto mit gekaufter **Minecraft: Java Edition**
- Kein Offline-, Crack- oder Umgehungs-Modus
- RAM-Limit auf Xbox: ca. 3-4 GB (Standard: 3072 MB)