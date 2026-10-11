#!/bin/bash
# XB-Launcher UWP Setup für GitHub Codespaces

echo "==> XB-Launcher UWP Setup..."

# .NET SDK installieren (falls nicht vorhanden)
if ! command -v dotnet &> /dev/null; then
    echo "==> Installiere .NET SDK..."
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel 10.0
    export DOTNET_ROOT=$HOME/.dotnet
    export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools
fi

# Windows SDK für UWP (Referenz-Assemblies)
echo "==> Hinweis: UWP-Build benötigt Windows SDK"
echo "    Für vollständigen Build: Windows-PC oder Windows-Cloud-VM verwenden"
echo ""

# Projekt wiederherstellen
echo "==> Stelle NuGet-Pakete wieder her..."
cd /workspaces/XB-Launcher
dotnet restore src/XBLauncher.Core/XBLauncher.Core.csproj
dotnet restore src/XBLauncher.App/XBLauncher.App.csproj

echo ""
echo "==> Setup abgeschlossen!"
echo "==> Hinweis: UWP/Xbox-Build benötigt Windows-Umgebung"
echo "==> Verwende 'scripts/build-appx.ps1' auf Windows für das finale Paket"