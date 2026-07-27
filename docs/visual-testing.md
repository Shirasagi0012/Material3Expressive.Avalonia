# Visual testing

The visual framework is local-first. It has two deliberately separate jobs:

1. strict Avalonia-on-Skia regression against user-reviewed candidate images; and
2. diagnostic comparison with a pinned Compose Material 3 reference capture.

Neither NuGet publishing nor GitHub state is involved.

## Avalonia regression

The current full set contains 89 deterministic scenes: 75 static/component and
cross-cutting scenes, 7 settled interaction scenes, and 7 exact animation
keyframes. The standard environment is Windows x64, Avalonia 12.1, real Skia,
premultiplied RGBA, `en-US`, LTR, Roboto preset weights and grayscale text
antialiasing. Every case also records its actual culture, flow direction and
render scaling; representative 1.25×, 2× and RTL cases are part of the set.

The bundled Roboto variable font is valid input. Tests request named weights
such as Normal, Medium and Bold; they do not continuously adjust the `wght`
axis. The normal font SHA-256 is
`B25986D18730960C7B27384AB2BC500856AE7FE9E71C9850019195FF9019F0B2`.

```powershell
dotnet run --project tools/Material3Expressive.VisualBaseline -- record --set full
dotnet run --project tools/Material3Expressive.VisualBaseline -- review --set full --open
dotnet run --project tools/Material3Expressive.VisualBaseline -- verify --set full
```

`record` writes candidate PNGs and invalidates any approval for changed bytes.
Only the user may approve reviewed candidates:

```powershell
dotnet run --project tools/Material3Expressive.VisualBaseline -- approve --set full --confirm-reviewed
```

Approval is bound to each PNG's SHA-256. Missing or unapproved candidates are
reported as pending and skipped by xUnit; they never silently become enforced
baselines. The current approval manifest is intentionally empty: all 89 images
are candidates awaiting user review.

Useful narrower sets include `smoke`, `interaction`, `parity`, `motion`, and
component tags such as `button`:

```powershell
dotnet run --project tools/Material3Expressive.VisualBaseline -- verify --set interaction
dotnet run --project tools/Material3Expressive.VisualBaseline -- review --set motion --open
dotnet run --project tools/Material3Expressive.VisualBaseline -- probe --filter "button/*" --iterations 100
```

Interaction scenes use the real Headless mouse and keyboard pipeline. Stable
states are settled without waiting. Motion scenes switch the component-owned
`MotionGroup` to an internal manual clock before input, then advance by exact
durations. The review page presents those frames as horizontal timelines.
Neither path uses `Task.Delay` or directly sets pseudo-classes.

## Compose reference capture

The executable reference is the released
`androidx.compose.material3:material3-android:1.5.0-alpha24` AAR. Its SHA-256,
Android API 35 environment and every captured PNG hash are versioned under
`visual-references/compose/1.5.0-alpha24`.

```powershell
.\tests\ComposeReference\capture.ps1
dotnet run --project tools/Material3Expressive.VisualBaseline -- compose-status
dotnet run --project tools/Material3Expressive.VisualBaseline -- parity --open
```

The capture uses the `Pixel_9_Pro` API 35 AVD, overrides Compose density to
160 dpi, fixes font scale to 1, sets LTR explicitly and disables system
animations. The same
`Roboto-VariableFont_wdth,wght.ttf` used by Avalonia is staged into the Android
build after verifying SHA-256
`B25986D18730960C7B27384AB2BC500856AE7FE9E71C9850019195FF9019F0B2`;
all 15 standard Compose Typography slots use that family, and components
request named weight presets rather than continuous `wght` values.
`-CaptureOnly -Output <unique-directory>` records a determinism probe without
importing it.

Compose references are imported only when the AAR, color contract, capture
environment, dimensions, PNG hashes and staged reference-manifest hash all
match. The instrumentation registry and captured case set must each match the
reviewed manifest exactly, and every tagged component must have non-empty bounds
inside its viewport. The color-contract, reference-manifest and capture-manifest
hashes are recorded, so a partial import cannot blur provenance. The current
reference set contains 15 valid cases and has been independently captured twice
with identical PNG hashes.

## Color contract

Compose's parameterless `lightColorScheme()` and `darkColorScheme()` are static
baseline token palettes. They do not run a seed through Material Color
Utilities. In contrast, this project feeds `#6750A4` into `TonalSpotScheme`.
Even with the same apparent seed, the former produces light primary `#6750A4`
while the latter produces `#65558F`.

Component parity therefore injects the same generated role colors into both
renderers. The shared file is:

`visual-references/color-contracts/tonal-spot-spec2021-phone-seed-6750a4.json`

It pins:

- seed `#6750A4`;
- Tonal Spot;
- Material Color Utilities `Spec2021`;
- phone platform;
- the Material Color Utilities submodule commit; and
- every light/dark system role plus the file SHA-256.

A .NET test regenerates all roles from the current submodule and compares them
with the contract. Two Foundation swatch scenes then prove that Compose and
Avalonia consume the same primary-role colors; both currently compare at exact
pixel equality.

## Parity interpretation

Cross-renderer comparison is diagnostic until reviewed thresholds are
established. The report includes raw and aligned RMSE, perceptual ΔE76,
dominant foreground color, foreground bounds, edge F1, declared-region
metrics, raw text-ink bounds, overlays and heatmaps.

Alignment is proposed from the foreground centre of a one-component scene and
accepted only when it lowers pixel RMSE. This detects a true translation without
turning asymmetric shadow or size differences into a false offset. Text bounds
remain raw, so a label baseline shift is visible even when an aligned overlay is
also generated.

The current 15-case report shows:

- shared primary-role swatches: exact in light and dark;
- filled card: exact;
- selected checkbox edge F1 `0.9919`;
- small FAB edge F1 `0.9554`, mean ΔE76 `0.262`;
- navigation bar edge F1 `0.7750`, mean ΔE76 `0.561`;
- outlined text field edge F1 `0.6675`, mean ΔE76 `1.469`;
- enabled button dominant colors: exact after the shared color contract; and
- the text-button label is approximately 4 px higher in Avalonia at mdpi.

Those are findings for review, not automatic failures or accepted exceptions.
The framework records these differences without requiring root-cause
attribution or changes to the main Avalonia project.

## Defects exposed while building the suite

The framework has already found and locally fixed several real defects:

- button-derived controls did not start Material press feedback because their
  class handler consumed `PointerPressed` before the interaction host;
- ButtonGroup structural shape overrides could leave clock-dependent
  intermediate corners;
- NavigationRail arranged items horizontally;
- an outlined TextField clipped its floated label and inherited Fluent focus
  chrome;
- Dialog and BottomSheet content presenters could arrange at zero size; and
- a progress/loading-indicator board clipped its final indicator.

Behavioral tests now cover the input-routing and overlay-layout regressions in
addition to the candidate images.
