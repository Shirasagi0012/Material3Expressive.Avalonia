param(
    [string]$Case
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Push-Location $repoRoot
try {
    $filter = 'Category=VisualParity'
    if (-not [string]::IsNullOrWhiteSpace($Case)) {
        if ($Case.EndsWith('*')) {
            $filter += '&ParityCase~' + $Case.TrimEnd('*')
        }
        else {
            $filter += '&ParityCase=' + $Case
        }
    }
    $arguments = @(
        'test',
        'tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj',
        '--filter',
        $filter
    )
    & dotnet @arguments
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
