; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
M3ETOK001 | Material3Expressive | Error | A `*.jsonc` token table is malformed and could not be turned into code.
M3ETOK002 | Material3Expressive | Error | Two token tables declare the same name, so one would overwrite the other.
M3ETOK003 | Material3Expressive | Error | A token table contains an unknown, incompatible, reverse-layer, or cyclic reference.
M3ETOK004 | Material3Expressive | Error | A token table's filename suffix does not match its root `kind`.
M3ETOK005 | Material3Expressive | Error | External canonical declaration metadata is missing, malformed, incompatible, cannot resolve a structured reference, or overlaps a generated descriptor identity/family.
