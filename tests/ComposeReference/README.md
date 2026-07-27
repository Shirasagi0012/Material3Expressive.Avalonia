# Compose reference capture

This local-only Android instrumentation project renders the fixed parity scenes
with the released Compose Material 3 `1.5.0-alpha24` artifact. Its build
verifies the exact `material3-android` AAR SHA-256 before compiling. The reviewed
`visual-references/compose/1.5.0-alpha24/manifest.json` is staged into the test
APK with its SHA-256, and is the source of truth for case IDs, viewports, themes,
images and capture-environment requirements.

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

The instrumentation registry must match the manifest case set exactly. Each
scene also exposes a tagged component whose non-empty bounds are checked against
the declared viewport before the screenshot is accepted. This catches missing,
off-screen and accidentally clipped scene roots at capture time.

The current 40-case set includes the original color/button/component probes plus
the first expansion wave: all 15 standard typography slots on six light/dark
boards, twelve filled/outlined text-field states, and nine navigation bar, rail
and tab scenes. Scene identity is keyed by manifest case ID so adjacent input
states cannot reuse focus or remembered Compose state. Focused fields perform and
assert the upstream screenshot test's split touch gesture before capture.

Component scenes read the shared, hash-pinned Tonal Spot role palette from
`visual-references/color-contracts`. This avoids treating Compose's static
parameterless color scheme and Material Color Utilities seed generation as if
they were the same algorithm. The build also verifies and packages the exact
Roboto variable-font file used by Avalonia. All 15 standard Compose Typography
slots use that family, while components request only named weights such as
Normal, Medium and Bold.
