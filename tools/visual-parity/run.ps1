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
    $artifactRoot = Join-Path $repoRoot 'artifacts\visual-parity\xunit'
    $existingRuns = @(Get-ChildItem $artifactRoot -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)
    & dotnet @arguments
    $testExitCode = $LASTEXITCODE

    $latestRun = Get-ChildItem $artifactRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notin $existingRuns } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if ($null -ne $latestRun) {
        $manifestPath = Join-Path $latestRun.FullName 'actual\manifest.json'
        if (Test-Path -LiteralPath $manifestPath) {
            $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            foreach ($warningCase in @($manifest.cases | Where-Object { $_.comparisonKind -eq 'rasterWarning' })) {
                $message = @($warningCase.warnings) -join ' '
                Write-Warning "Visual parity warning for '$($warningCase.id)': $message"
            }
        }
    }

    exit $testExitCode
}
finally {
    Pop-Location
}
