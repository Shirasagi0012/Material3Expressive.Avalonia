# Visual parity testing

## Purpose

Visual parity testing compares the Avalonia implementation directly with the pinned official
Compose Multiplatform Material 3 implementation. Compose is the only reference: each run creates
a new expected bundle, then captures and compares the matching Avalonia scenes.

There are no checked-in Avalonia screenshot baselines, approval manifests, candidate recording,
or self-regression fallback. A mismatch is a failed parity test even while the job is used as a
diagnostic during early development. Making that job a required CI gate is a delivery-policy
decision, not a reason to accept a new image.

## Pinned experiment

| Setting | Value |
|---|---|
| Compose Multiplatform plugin/runtime | `1.12.0-beta02` |
| Compose Material 3 | `1.12.0-alpha03` |
| Jetpack Material 3 lineage | `1.5.0-alpha22` |
| Kotlin / JDK | `2.3.20` / 17 |
| Render scale | 2.0 physical pixels per logical dp/DIP |
| Locale and direction | `en-US`, LTR |
| Font scale and color space | 1.0, sRGB |
| Font | repository-pinned Roboto variable font, hash-validated on both sides |

The Compose dependency lock file and checksum metadata live in
[`tests/ComposeReference`](../tests/ComposeReference). Version upgrades are intentional source
changes, not baseline refreshes.

## Run the suite

From the repository root:

```powershell
.\tools\visual-parity\run.ps1
```

This is equivalent to:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj --filter Category=VisualParity
```

The test fixture launches Gradle once, generates the complete fresh Compose bundle, validates its
manifest and hashes, captures all Avalonia cases, writes artifacts, and then reports the aggregate
result. A non-zero result caused by visual differences is meaningful: the harness has run
successfully and found implementation work to do.

To inspect only the reference producer, run the commands in
[`tests/ComposeReference/README.md`](../tests/ComposeReference/README.md). Its `--case` argument
accepts a case ID or a prefix wildcard such as `button/*`.

## What is compared

The initial catalog contains 40 representative color, typography, button, card, FAB, checkbox,
text-field, navigation, and tab cases. Every case has the same stable ID, viewport, theme, and
named probe contract on both sides.

```text
Compose scene + probes  -->  expected PNG, layout JSON, manifest
                                      |
                                      v
Avalonia matching scene + probes --> actual PNG, layout JSON
                                      |
                                      v
                         structural, geometry, text, and raster comparison
```

Checks happen in this order:

1. The producer manifest, versions, environment, font hash, catalog, and image hashes must match
   the contract.
2. Named component bounds are compared in root-relative physical pixels. Images are never
   auto-aligned or recentered.
3. Registered text nodes compare content, line ranges, baselines, and one advance/caret box per
   UTF-16 offset.
4. The rendered pixels are compared with the profile selected for the case.

For standard Avalonia text, data comes from `TextBlock.TextLayout` and
`HitTestTextPosition`. The custom Material text-field label uses its dedicated internal
`TextLayout` probe so it has the same line and character-box data. The editable Compose text-field
input has no independent root drawing-origin probe yet; it remains covered by the text-field image
region, while the label and supporting text are measured structurally.

## Comparison profiles

[`tests/VisualParity/comparison-profiles.json`](../tests/VisualParity/comparison-profiles.json)
is the versioned, centralized tolerance policy. It selects a profile by case-ID prefix for color
swatches, containers, components with text, and text fields. Current profiles use strict raster
comparison; any later renderer-specific allowance must be narrow, justified, and changed in that
file rather than embedded in a test.

The selected profile ID and the SHA-256 of this file are included in the actual manifest. That
makes a tolerance-policy change visible in every run artifact.

## Find the artifacts

Each invocation writes a unique directory below:

```text
artifacts/visual-parity/xunit/<timestamp>/
  expected/
    manifest.json
    cases/<case-id>/image.png
    cases/<case-id>/layout.json
  actual/
    manifest.json
    cases/<case-id>/
      expected.png
      image.png
      layout.json
      diff.png
      comparison.json
      comparison.txt
```

Artifacts are deliberately not committed. `actual/manifest.json` identifies the Avalonia runtime,
the profile configuration hash, and the per-case outputs.

Use the failure category to start triage:

- `Infrastructure` or `Contract`: producer environment, manifest, catalog, or probe mismatch.
- `Geometry`: container, icon, or child placement/size is wrong.
- `TextLayout`: typography, line breaking, baseline, or character advance differs.
- `Raster`: inspect `expected.png`, `image.png`, and `diff.png` for paint, shape, color, shadow,
  or glyph rendering differences.

`comparison.json` contains the complete machine-readable result; `comparison.txt` is the concise
failure summary emitted with the xUnit assertion.

## Add or change a parity case

1. Add a Compose scene and `CaseContract` in `tests/ComposeReference`, with explicit viewport,
   theme, and required named probes.
2. Add the matching Avalonia `VisualCase` with the identical ID and scene state. Name every node
   that needs structural comparison. For custom-drawn text, add a direct layout probe instead of
   estimating its bounds from pixels.
3. If the new case family needs a distinct comparison class, add a reviewed prefix rule and
   profile to `comparison-profiles.json`; otherwise it must resolve to an existing profile.
4. Run the parity command and inspect the new artifact directory. Add focused unit tests for any
   new contract, capture adapter, or comparer behavior.

Do not add a stored expected screenshot or an allow-list entry to make a difference pass.

## Upgrade the Compose reference

An upgrade changes the experiment itself. Update the Compose pins, Gradle lock and verification
metadata, and the expected-version validation in the .NET bundle loader together. Then run the
full suite: it will generate the new Compose expected bundle automatically. Review the resulting
systematic differences and update the Avalonia implementation and case contracts as needed.

See [visual-parity-test-plan.md](visual-parity-test-plan.md) for the architectural rationale and
the remaining coverage roadmap.
