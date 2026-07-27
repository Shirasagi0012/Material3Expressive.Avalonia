[CmdletBinding()]
param(
    [string] $Avd = "Pixel_9_Pro",
    [string] $AndroidSdk = "$env:LOCALAPPDATA\Android\Sdk",
    [string] $JavaHome = "$env:LOCALAPPDATA\Programs\Android Studio\jbr",
    [string] $Output,
    [switch] $CaptureOnly,
    [switch] $KeepEmulator
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $projectRoot "..\..")
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $repositoryRoot "artifacts\compose-capture"
}

$adb = Join-Path $AndroidSdk "platform-tools\adb.exe"
$emulator = Join-Path $AndroidSdk "emulator\emulator.exe"
$gradle = Join-Path $projectRoot "gradlew.bat"
$java = Join-Path $JavaHome "bin\java.exe"
$referenceFont = Join-Path `
    $env:LOCALAPPDATA `
    "Microsoft\Windows\Fonts\Roboto-VariableFont_wdth,wght.ttf"
foreach ($required in @($adb, $emulator, $gradle, $java)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Required tool is missing: $required"
    }
}
$expectedFontHash = "B25986D18730960C7B27384AB2BC500856AE7FE9E71C9850019195FF9019F0B2"
if (-not (Test-Path -LiteralPath $referenceFont)) {
    throw "The shared Roboto reference font is missing: $referenceFont"
}
$actualFontHash = (Get-FileHash -LiteralPath $referenceFont -Algorithm SHA256).Hash
if ($actualFontHash -ne $expectedFontHash) {
    throw "Roboto reference font hash mismatch. Expected $expectedFontHash, got $actualFontHash."
}

$env:ANDROID_HOME = $AndroidSdk
$env:ANDROID_SDK_ROOT = $AndroidSdk
$env:JAVA_HOME = $JavaHome
$startedEmulator = $false

function Get-BootedEmulator {
    $serials =
        & $adb devices |
        Select-Object -Skip 1 |
        ForEach-Object { ($_ -split "\s+")[0] } |
        Where-Object { $_ -like "emulator-*" }
    foreach ($serial in $serials) {
        $response = & $adb -s $serial emu avd name 2>$null | Select-Object -First 1
        if (-not $response) {
            continue
        }
        $name = $response.Trim()
        if ($name -eq $Avd) {
            return $serial
        }
    }
    return $null
}

$serial = Get-BootedEmulator
if (-not $serial) {
    Start-Process `
        -FilePath $emulator `
        -ArgumentList @(
            "-avd", $Avd,
            "-no-window",
            "-no-audio",
            "-no-boot-anim",
            "-no-snapshot-load",
            "-no-snapshot-save",
            "-gpu", "swiftshader_indirect"
        ) `
        -WindowStyle Hidden
    $startedEmulator = $true
}

$deadline = [DateTimeOffset]::Now.AddMinutes(4)
do {
    Start-Sleep -Seconds 2
    $serial = Get-BootedEmulator
    $bootResponse =
        if ($serial) {
            & $adb -s $serial shell getprop sys.boot_completed 2>$null |
                Select-Object -First 1
        }
        else {
            $null
        }
    $booted = $bootResponse -and ($bootResponse.Trim() -eq "1")
} until ($booted -or [DateTimeOffset]::Now -ge $deadline)

if (-not $booted) {
    throw "The '$Avd' emulator did not finish booting within four minutes."
}

$env:ANDROID_SERIAL = $serial
$apiResponse =
    & $adb -s $serial shell getprop ro.build.version.sdk 2>$null |
        Select-Object -First 1
if (-not $apiResponse) {
    throw "The '$Avd' emulator stopped responding before API verification."
}
$api = $apiResponse.Trim()
if ($api -ne "35") {
    throw "Compose references require API 35; '$Avd' reported API $api."
}

& $adb -s $serial shell settings put global window_animation_scale 0
& $adb -s $serial shell settings put global transition_animation_scale 0
& $adb -s $serial shell settings put global animator_duration_scale 0
& $adb -s $serial shell settings put system font_scale 1.0

try {
    & $gradle `
        --project-dir $projectRoot `
        :capture-app:connectedDebugAndroidTest `
        "-PreferenceFontPath=$referenceFont" `
        "-Pandroid.testInstrumentationRunnerArguments.class=material3expressive.compose.reference.ComposeReferenceCaptureTest"
    if ($LASTEXITCODE -ne 0) {
        throw "Compose reference instrumentation failed with exit code $LASTEXITCODE."
    }

    $deviceCapture = "/data/local/tmp/material3expressive-compose-reference"
    $outputParent = Split-Path -Parent $Output
    if (-not (Test-Path -LiteralPath $outputParent)) {
        New-Item -ItemType Directory -Path $outputParent | Out-Null
    }
    if (Test-Path -LiteralPath $Output) {
        throw "Capture output already exists; move or remove it before recording again: $Output"
    }
    & $adb -s $serial pull $deviceCapture $Output
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path (Join-Path $Output "capture.json"))) {
        throw "Could not pull Compose capture metadata from the emulator."
    }

    if (-not $CaptureOnly) {
        dotnet run `
            --project (Join-Path $repositoryRoot "tools\Material3Expressive.VisualBaseline") `
            -- compose-import --source $Output
        if ($LASTEXITCODE -ne 0) {
            throw "Compose reference import failed with exit code $LASTEXITCODE."
        }
        dotnet run `
            --project (Join-Path $repositoryRoot "tools\Material3Expressive.VisualBaseline") `
            -- parity
        if ($LASTEXITCODE -ne 0) {
            throw "Compose parity report generation failed with exit code $LASTEXITCODE."
        }
    }
}
finally {
    if ($startedEmulator -and -not $KeepEmulator -and $serial) {
        & $adb -s $serial emu kill | Out-Null
    }
}
