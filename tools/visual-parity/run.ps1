$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Push-Location $repoRoot
try {
    $arguments = @(
        'test',
        'tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj',
        '--filter',
        'Category=VisualParity'
    )
    & dotnet @arguments
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
