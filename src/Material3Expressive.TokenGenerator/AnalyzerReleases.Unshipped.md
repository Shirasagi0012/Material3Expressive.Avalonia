; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
M3ETOK001 | Material3Expressive | Error | A `*.tokens.json` table is malformed and could not be turned into code.
M3ETOK002 | Material3Expressive | Error | Two token tables declare the same name, so one would overwrite the other.
