#!/bin/bash
# XB-Launcher Release Upload Script
# Lädt die Release-Dateien zu verschiedenen Hosting-Diensten hoch

echo "========================================="
echo "  XB-Launcher Release Upload"
echo "========================================="
echo ""
echo "Datei: XB-Launcher-Release.zip"
echo "Größe: $(du -h XB-Launcher-Release.zip | cut -f1)"
echo ""

# Option 1: transfer.sh (kostenlos, einfach)
echo "==> Upload zu transfer.sh..."
curl --upload-file XB-Launcher-Release.zip https://transfer.sh/XB-Launcher-Release.zip
echo ""
echo ""

# Option 2: file.io (kostenlos, einmal-Download)
echo "==> Upload zu file.io..."
curl -F "file=@XB-Launcher-Release.zip" https://file.io
echo ""
echo ""

# Option 3: 0x0.st (kostenlos)
echo "==> Upload zu 0x0.st..."
curl -F "file=@XB-Launcher-Release.zip" https://0x0.st
echo ""
echo ""

echo "========================================="
echo "  Upload abgeschlossen!"
echo "========================================="
echo ""
echo "Die Links oben können für den Download verwendet werden."
echo ""
echo "Alternativ:"
echo "1. GitHub Release erstellen (https://github.com)"
echo "2. Google Drive / OneDrive / Dropbox"
echo "3. USB-Stick für die Xbox"