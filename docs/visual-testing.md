# Compose parity testing

Visual testing has one purpose: compare the current Avalonia implementation directly with a pinned
official `compose-material3` reference capture. Avalonia screenshots are not recorded, approved,
or used as regression baselines.

## Reference contract

The executable reference is the released
`androidx.compose.material3:material3-android:1.5.0-alpha24` AAR. Its SHA-256, Android API 35
environment, case manifest and every reference PNG hash are versioned under
`visual-references/compose/1.5.0-alpha24`.

The capture uses the `Pixel_9_Pro` API 35 AVD at 160 dpi, font scale 1, `en-US`, LTR, disabled
system animations, and the bundled Roboto font. Both implementations consume the same pinned
Tonal Spot color contract:

`visual-references/color-contracts/tonal-spot-spec2021-phone-seed-6750a4.json`

Each Compose reference case has the same stable ID, viewport, theme, font and content in the
Avalonia visual-case catalog. Contract tests reject missing or mismatched cases before parity is
run.

## Commands

```powershell
.\tests\ComposeReference\capture.ps1
dotnet run --project tools/Material3Expressive.VisualBaseline -- compose-status
dotnet run --project tools/Material3Expressive.VisualBaseline -- parity --open
```

`compose-status` verifies that checked-in references are complete and hash-valid.
`compose-import` accepts only a capture whose AAR, environment, color contract, dimensions, case
set and PNG hashes match the manifest. `parity` renders Avalonia and writes an HTML comparison
report containing Compose, Avalonia, overlay, heatmap and region metrics.

## Interpretation

Parity is cross-renderer conformance evidence. Containers, geometry, colors, icon placement and
text bounds should be evaluated against the Compose image; text rasterization and shadows may need
small, explicitly documented tolerances. The report is diagnostic until per-region pass/fail
limits are defined. It never accepts an Avalonia image as a new source of truth.
