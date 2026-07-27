# Compose reference capture

This local-only Android instrumentation project renders the shared 240×96 parity
cases with the released Compose Material 3 `1.5.0-alpha24` artifact. Its build
verifies the exact `material3-android` AAR SHA-256 before compiling.

The alpha24 AAR advertises `minCompileSdk 37`, although the official screenshot
tests and this contract execute on API 35. To keep the local setup API-35-only,
the capture shell disables only AGP's AAR metadata preflight. Resource linking,
Kotlin compilation, D8, installation and the API 35 instrumentation run remain
active, so a real incompatibility still fails the capture.

Prerequisites:

- Android Studio JBR 21
- Android SDK/API 35
- the local `Pixel_9_Pro` API 35 AVD

Run from the repository root:

```powershell
.\tests\ComposeReference\capture.ps1
```

The script starts the AVD headlessly when needed, disables system animations,
runs only `ComposeReferenceCaptureTest`, pulls `capture.json` and its PNGs,
imports the hash-verified reference set, then writes the local parity report.
The test stages its output under shell-owned `/data/local/tmp` before AGP
uninstalls the test APK, so app teardown cannot delete the capture before pull.
Use `-CaptureOnly` with a unique `-Output` directory for determinism probes or
inspection without changing the imported reference set.
The Compose scene overrides `LocalDensity` to 160 dpi and font scale 1.0, so
the reference pixels remain 1 dp = 1 px even though the Pixel 9 Pro hardware
profile has a native density of 480 dpi.

Component scenes read the shared, hash-pinned Tonal Spot role palette from
`visual-references/color-contracts`. This avoids treating Compose's static
parameterless color scheme and Material Color Utilities seed generation as if
they were the same algorithm. The build also verifies and packages the exact
Roboto variable-font file used by Avalonia, then requests only its named
Normal, Medium and Bold weights.
