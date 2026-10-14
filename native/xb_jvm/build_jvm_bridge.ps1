# build_jvm_bridge.ps1 - compiles the native xb_jvm.dll JVM-embed bridge.
#
# Requires a Visual Studio x64 Native Tools command prompt (MSVC + Windows SDK)
# and a JDK (for jni.h + jvm.lib). Set JAVA_HOME to a JDK 21+ install.
#
# Output: native\xb_jvm\xb_jvm.dll

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$src  = Join-Path $here "xb_jvm.c"
$def  = Join-Path $here "xb_jvm.def"
$out  = Join-Path $here "xb_jvm.dll"

$jdk = $env:JAVA_HOME
if (-not $jdk) { throw "JAVA_HOME is not set. Point it at a JDK 21+ install." }

$include = Join-Path $jdk "include"
$wininc  = Join-Path $jdk "include\win32"
$jvmLib  = Join-Path $jdk "lib\jvm.lib"
if (-not (Test-Path $jvmLib)) {
    # Some JDKs keep jvm.lib under a different layout.
    $alt = Get-ChildItem $jdk -Recurse -Filter jvm.lib -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($alt) { $jvmLib = $alt.FullName }
}
if (-not (Test-Path $jvmLib)) { throw "jvm.lib not found under $jdk" }

Write-Host "==> Compiling xb_jvm.dll"
& cl /nologo /LD /O2 /I"$include" /I"$wininc" "$src" /link /DEF:"$def" "$jvmLib" /OUT:"$out"
if ($LASTEXITCODE -ne 0) { throw "cl failed ($LASTEXITCODE)" }

Write-Host "Built: $out"