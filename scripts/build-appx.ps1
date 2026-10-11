# build-appx.ps1 - XB-Launcher APPX packaging
#
# Publishes the .NET app, generates store assets, creates a self-signed cert,
# packs everything with makeappx.exe and signs it with signtool.exe.
#
# The makeappx/signtool binaries come from the Microsoft.Windows.SDK.BuildTools
# NuGet package, so no Visual Studio installation is required.

$ErrorActionPreference = "Stop"

$root     = Split-Path -Parent $PSScriptRoot
$src      = Join-Path $root "src"
$pack     = Join-Path $root "packaging"
$out      = Join-Path $root "output"
$publish  = Join-Path $out "publish"
$stage    = Join-Path $out "appx"
$dotnet   = "dotnet.exe"
$sdkTools = "C:\temp\buildtools_extract\bin\10.0.28000.0\x64"

New-Item -ItemType Directory -Force -Path $out, $publish, $stage | Out-Null

Write-Host "==> 1/6  .NET publish (self-contained win-x64)..."
& $dotnet publish (Join-Path $src "XBLauncher.Uwp\XBLauncher.Uwp.csproj") `
    -c Release -r win-x64 --self-contained true -o $publish -nologo

Write-Host "==> 2/6  Store-Assets generieren..."
$assetsDir = Join-Path $stage "Assets"
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null

# Generate simple solid-color PNG tiles at the required sizes using .NET
Add-Type -AssemblyName System.Drawing
function New-Tile($path, $w, $h, $text) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 14, 17, 22))
    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 61, 220, 132))
    $font = New-Object System.Drawing.Font "Segoe UI", ([math]::Max(8, [int]($w/6))), ([System.Drawing.FontStyle]::Bold)
    $size = $g.MeasureString($text, $font)
    $g.DrawString($text, $font, $brush, (($w - $size.Width)/2), (($h - $size.Height)/2))
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

New-Tile (Join-Path $assetsDir "StoreLogo.png")        50   50   "XB"
New-Tile (Join-Path $assetsDir "Square44x44Logo.png")  44   44   "XB"
New-Tile (Join-Path $assetsDir "Square150x150Logo.png") 150 150  "XB-Launcher"
New-Tile (Join-Path $assetsDir "Wide310x150Logo.png")  310 150  "XB-Launcher"
New-Tile (Join-Path $assetsDir "SplashScreen.png")     620 300  "XB-Launcher"

Write-Host "==> 3/6  Payload zusammenstellen..."
# AppxManifest at package root
Copy-Item (Join-Path $pack "AppxManifest.xml") (Join-Path $stage "AppxManifest.xml") -Force

# Published app payload goes to package ROOT so the manifest can reference XB-Launcher.exe
Get-ChildItem $stage | Where-Object { $_.Name -notin @("AppxManifest.xml","Assets") } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $publish "*") $stage -Recurse -Force

# Native helper dll
$nativeDll = Join-Path $root "native\xb_shim\xb_shim.dll"
if (Test-Path $nativeDll) {
    Copy-Item $nativeDll (Join-Path $stage "xb_shim.dll") -Force
}

Write-Host "==> 4/6  APPX packen (makeappx)..."
$makeappx = Join-Path $sdkTools "makeappx.exe"
$package  = Join-Path $out "XB-Launcher.appx"
if (Test-Path $package) { Remove-Item $package -Force }
& $makeappx pack /d $stage /p $package /o
if ($LASTEXITCODE -ne 0) { throw "makeappx fehlgeschlagen ($LASTEXITCODE)" }

Write-Host "==> 5/6  Zertifikat erstellen..."
$certPath = Join-Path $out "XB-Launcher.pfx"
$certPwd  = ConvertTo-SecureString -String "xblauncher" -Force -AsPlainText
if (-not (Test-Path $certPath)) {
    $cert = New-SelfSignedCertificate -Type Custom `
        -Subject "CN=XB-Launcher" `
        -KeyUsage DigitalSignature `
        -FriendlyName "XB-Launcher Dev Cert" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
    Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $certPwd | Out-Null
    Write-Host "    Zertifikat: $certPath"
} else {
    Write-Host "    Bestehendes Zertifikat wird wiederverwendet."
}

Write-Host "==> 6/6  APPX signieren (signtool)..."
$signtool = Join-Path $sdkTools "signtool.exe"
& $signtool sign /f $certPath /p "xblauncher" /fd SHA256 $package
if ($LASTEXITCODE -ne 0) { throw "signtool fehlgeschlagen ($LASTEXITCODE)" }

Write-Host ""
Write-Host "============================================================"
Write-Host "  XB-Launcher APPX erstellt:"
Write-Host "  $package"
Write-Host "============================================================"
Write-Host ""
Write-Host "Installation auf Xbox (Dev Mode):"
Write-Host "  1. Entwicklermodus aktivieren (Xbox Dev Home)"
Write-Host "  2. Device Portal Ã¶ffnen (https://<xbox-ip>)"
Write-Host "  3. Apps -> Apps installieren -> XB-Launcher.appx hochladen"
Write-Host "  4. Oder WinAppDeployCmd.exe -install -ip <xbox-ip> -file XB-Launcher.appx"

