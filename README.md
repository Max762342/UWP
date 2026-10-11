# XB-Launcher

**XB-Launcher** ist ein Minecraft: Java Edition Launcher für **Xbox Series X|S im Entwicklermodus**.
Er ist in **C# (.NET 10 + WPF)** geschrieben und wird als **APPX/MSIX-Paket** ausgeliefert.

## Was kann XB-Launcher?

| Funktion | Status |
| --- | --- |
| **Microsoft-Anmeldung** (Device-Code-Flow) | ✅ |
| **Ownership-Prüfung** „Minecraft: Java Edition“ | ✅ Pflicht – kein Umgehen möglich |
| **Alle Versionen 1.21 – 26.4** | ✅ `1.21, 1.21.1, 1.21.3, 1.21.4, 1.21.5, 1.21.6, 1.21.9, 1.21.11, 26.1, 26.2, 26.3, 26.4` |
| **Mod-Loader**: Vanilla / Fabric / NeoForge / Forge | ✅ |
| **Mods Hub** (Modrinth) | ✅ Suche + Installation |
| **Shader Hub** (Modrinth) | ✅ Suche + Installation |
| **Packs Hub** (Resource-/Textur-Packs, Modrinth) | ✅ Suche + Installation |
| **Profile** (eigener Spielordner, RAM-Limit) | ✅ |
| **Automatische Java-Bereitstellung** (Temurin JRE 21/25) | ✅ kein manuelles Java nötig |
| **Offizielle Mojang-Downloads** (Client, Assets, Libraries) | ✅ mit SHA1-Verifikation |

## Versionen & Loader

Jede Minecraft-Version steht mit folgenden Loadern zur Verfügung:

- **Vanilla** (ohne Mods)
- **Fabric** (Loader `0.19.2`)
- **NeoForge** (passende Loader-Version je MC-Version)
- **Forge** (für 1.21 – 1.21.5)

Die vollständige Ziel-Matrix ist in
[`src/XBLauncher.Core/Data/TargetCatalog.cs`](src/XBLauncher.Core/Data/TargetCatalog.cs) hinterlegt.

## Projektstruktur

```
XB-Launcher/
├─ src/
│  ├─ XBLauncher.Core/        # Kern-Bibliothek (C#)
│  │  ├─ Data/                # Modelle, Profile, Version-Catalog
│  │  ├─ Mojang/              # Offizielle Mojang-APIs
│  │  ├─ Modrinth/            # Mods/Shaders/Packs Hub
│  │  ├─ Auth/                # Microsoft + Minecraft Auth, Ownership-Check
│  │  ├─ Install/             # Java, Runtime, Fabric, NeoForge, Content
│  │  └─ Launch/              # Start-Plan, Argumente, Prozess
│  └─ XBLauncher.App/         # WPF-Oberfläche
│     ├─ Views/               # PlayPage, HubPage, SettingsPage
│     └─ Themes/              # Dark Theme
├─ native/xb_shim/            # C-DLL (Gamepad, Fenster) – MinGW
├─ packaging/                 # AppxManifest.xml
├─ scripts/build-appx.ps1     # Baut & signiert die APPX
└─ output/
   ├─ publish/                # .NET self-contained Build
   ├─ XB-Launcher.appx        # Fertiges installierbares Paket
   └─ XB-Launcher.pfx         # Dev-Signaturzertifikat
```

## Build

### Voraussetzungen

- Windows 10/11
- .NET SDK 10 (portabel reicht)
- MinGW/GCC (für die native `xb_shim.dll`) – optional, Build überspringt sie sonst
- Die Build-Tools `makeappx.exe` / `signtool.exe` werden aus dem NuGet-Paket
  `Microsoft.Windows.SDK.BuildTools` verwendet – **kein Visual Studio nötig**.

### Ein-Kommando-Build

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-appx.ps1
```

Das Skript:
1. publiziert die .NET-App self-contained (win-x64),
2. generiert die Store-Kachel-Assets,
3. stellt das Payload zusammen,
4. packt mit `makeappx.exe` nach `output\XB-Launcher.appx`,
5. erzeugt ein selbstsigniertes Dev-Zertifikat,
6. signiert die APPX mit `signtool.exe`.

## Installation auf der Xbox (Dev Mode)

1. **Entwicklermodus** auf der Xbox aktivieren (Xbox Dev Home).
2. **Device Portal** öffnen: `https://<ip-der-xbox>` im Browser.
3. Unter **Apps → Apps installieren** die Datei `output\XB-Launcher.appx` hochladen.
4. Beim ersten Start das mitgelieferte Zertifikat `output\XB-Launcher.pfx`
   (Passwort: `xblauncher`) vertrauenswürdig setzen bzw. im Dev Portal zulassen.
5. **XB-Launcher** starten, mit dem Microsoft-Konto anmelden (das Minecraft:
   Java Edition besitzt), Version wählen und **Spielen** drücken.

Alternativ per Kommandozeile:

```powershell
WinAppDeployCmd.exe -install -ip <xbox-ip> -file output\XB-Launcher.appx
```

## Erster Start

1. **Microsoft-Anmeldung** → Device-Code wird angezeigt.
2. Auf Handy/PC `https://microsoft.com/link` öffnen, Code eingeben.
3. Der Launcher prüft automatisch den **Besitz von Minecraft: Java Edition**.
4. Version + Loader wählen (z. B. `1.21.11 + Fabric 0.19.2`).
5. Beim ersten Start werden die offiziellen Spieldateien heruntergeladen
   (kann je nach Internet dauern).
6. **▶ SPIELEN** – Minecraft startet.

## Mods / Shader / Packs installieren

Über die Hubs in der Seitenleiste:

- **🧩 Mods Hub** – z. B. `sodium`, `iris`, `lithium`, `create`, …
- **🎨 Shader Hub** – z. B. `complementary`, `bsl`, `sildurs`, …
- **📦 Packs Hub** – Resource-/Textur-Packs, z. B. `faithful`, …

Alles wird direkt aus **Modrinth** geladen, per SHA1 geprüft und in den
Profil-Ordner installiert (`%LOCALAPPDATA%\XB-Launcher\instances\<profil>\`).

## Hinweise

- **Performance** hängt von der Java-/Grafik-Laufzeit auf der Xbox ab. XB-Launcher
  installiert die Spieldateien korrekt und startet sie; für maximale
  Grafikleistung kann eine kompatible GL-Laufzeit nachgerüstet werden.
- **Xbox Series X|S** sind die Ziel-Hardware. Xbox One wird nicht unterstützt.
- **Pflicht**: Ein Microsoft-Konto mit gekaufter **Minecraft: Java Edition**.
  Es gibt keinen Offline-, Crack- oder Umgehungs-Modus – das ist bewusst so.
- RAM ist in der Xbox Dev Mode auf ca. 3–4 GB begrenzt; der Launcher setzt das
  Standard-Limit auf 3072 MB.

## Rechtliches

- Minecraft, Mojang-Assets, Java, Fabric, Forge, NeoForge und Modrinth unterliegen
  ihren eigenen Lizenzen und Nutzungsbedingungen.
- XB-Launcher ist ein eigenständiges Projekt und kopiert keinen Code aus
  anderen Launchern. Es dient ausschließlich dem Start legal erworbener
  Minecraft-Kopien.
- Dieses Projekt ist für den **privaten Gebrauch** gedacht.
