[CmdletBinding(PositionalBinding=$false)]
param(
    [Parameter(Position=0)][ValidateSet('prepare-reference','run','recompare','list','report','verify-reference','build','test','self-test','capture-actual')][string]$Command = 'run',
    [string]$NuGetConfig = $env:M3E_NUGET_CONFIG,
    [switch]$NoRestore,
    [Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments
)
$ErrorActionPreference = 'Stop'
if ($NuGetConfig) { $NuGetConfig = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($NuGetConfig) }
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $PSScriptRoot 'Material3Expressive.Parity/Material3Expressive.Parity.csproj'
Push-Location $taskRoot
try {
    if ($Command -eq 'report') {
        Push-Location (Join-Path $PSScriptRoot 'report')
        try {
            if (-not (Test-Path 'node_modules')) {
                & npm ci --no-audit --no-fund
                if ($LASTEXITCODE -ne 0) { throw 'Report dependency installation failed.' }
            }
            & npm run dev -- @Arguments
            exit $LASTEXITCODE
        } finally { Pop-Location }
    }
    if ($Command -in @('build','prepare-reference','self-test')) {
        Push-Location (Join-Path $PSScriptRoot 'reference')
        try {
            if ($IsWindows -or $env:OS -eq 'Windows_NT') {
                & ./gradlew.bat installDist --console plain --dependency-verification strict
            } else {
                & sh ./gradlew installDist --console plain --dependency-verification strict
            }
            if ($LASTEXITCODE -ne 0) { throw 'Compose reference build failed.' }
        } finally { Pop-Location }
    }
    if ($Command -in @('build','list','prepare-reference','run','recompare','self-test','capture-actual')) {
        if (-not $NoRestore) {
            $restoreArguments = @('--locked-mode')
            if ($NuGetConfig) { $restoreArguments += @('--configfile', $NuGetConfig) }
            & dotnet restore $project @restoreArguments
            if ($LASTEXITCODE -ne 0) { throw 'Locked NuGet restore failed.' }
        }
        & dotnet build $project --configuration Release --no-restore --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw 'Parity build failed.' }
    }
    if ($Command -eq 'build') { exit 0 }
    if ($Command -eq 'test') {
        $testProject = Join-Path $PSScriptRoot 'Material3Expressive.Parity.Tests/Material3Expressive.Parity.Tests.csproj'
        if (-not $NoRestore) {
            $restoreArguments = @()
            if ($NuGetConfig) { $restoreArguments += @('--configfile', $NuGetConfig) }
            & dotnet restore $testProject @restoreArguments
            if ($LASTEXITCODE -ne 0) { throw 'Test NuGet restore failed.' }
        }
        & dotnet test $testProject --configuration Release --no-restore --verbosity quiet
    } else {
        & dotnet run --project $project --configuration Release --no-build -- $Command @Arguments
    }
    exit $LASTEXITCODE
} finally { Pop-Location }
