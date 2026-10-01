param(
    [Parameter(Position=0)][ValidateSet('run','list','compare','inspect','verify-reference','build','test','self-test','capture-actual')][string]$Command = 'run',
    [Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $PSScriptRoot 'Material3Expressive.Parity/Material3Expressive.Parity.csproj'
Push-Location $taskRoot
try {
    if ($Command -in @('build','run','self-test')) {
        Push-Location (Join-Path $PSScriptRoot 'reference')
        try {
            & ./gradlew.bat installDist --console plain --dependency-verification strict
            if ($LASTEXITCODE -ne 0) { throw 'Compose reference build failed.' }
        } finally { Pop-Location }
    }
    if ($Command -in @('build','run','self-test','capture-actual')) {
        & dotnet restore $project --locked-mode
        if ($LASTEXITCODE -ne 0) { throw 'Locked NuGet restore failed.' }
        & dotnet build $project --no-restore --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw 'Parity build failed.' }
    }
    if ($Command -eq 'build') { exit 0 }
    if ($Command -eq 'test') {
        & dotnet test (Join-Path $PSScriptRoot 'Material3Expressive.Parity.Tests/Material3Expressive.Parity.Tests.csproj') --verbosity quiet
    } else {
        & dotnet run --project $project --no-build -- $Command @Arguments
    }
    exit $LASTEXITCODE
} finally { Pop-Location }
