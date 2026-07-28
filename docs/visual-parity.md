# Visual parity testing

## Purpose

Visual parity testing compares the Avalonia implementation directly with the pinned official
Compose Multiplatform Material 3 implementation. Compose is the only reference: each run creates
a new expected bundle, then captures and compares the matching Avalonia scenes.

This executable parity reference is intentionally older than the AndroidX implementation source
baseline. It defines test output, not the complete feature target. See
[upstream-baselines.md](upstream-baselines.md) for the distinction and version-gap policy.

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
| Render scale | Canonical 2.0 physical pixels per logical dp/DIP; explicit scale cases may override it |
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
manifest and hashes, and shares it across one independently discoverable xUnit theory row per
case. Each row captures only its matching Avalonia scene, writes its artifacts and manifest entry,
and reports its own result. A non-zero result caused by visual differences is meaningful: the
harness has run successfully and found implementation work to do.

Run one Avalonia case while still generating the complete Compose expected bundle:

```powershell
.\tools\visual-parity\run.ps1 -Case "button/filled/medium/enabled/light"
```

Select a family by suffixing the case prefix with `*`:

```powershell
.\tools\visual-parity\run.ps1 -Case "button/*"
```

The corresponding xUnit data rows carry a `ParityCase` trait, so IDE and `dotnet test` filtering
can address the same cases directly.

To inspect only the reference producer, run the commands in
[`tests/ComposeReference/README.md`](../tests/ComposeReference/README.md). Its `--case` argument
accepts a case ID or a prefix wildcard such as `button/*`.

## What is compared

The current catalog contains 61 representative color, typography, button, card, FAB, checkbox,
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
2. Named component bounds are compared in root-relative physical pixels. `TextBlock` nodes retain
   their framework bounds; the custom Material text-field label is projected to the equivalent
   snapped Compose layout slot. Floating line and caret/advance data is compared separately.
   Images are never auto-aligned or recentered.
3. Registered text nodes compare content, line ranges, baselines, and one advance/caret box per
   UTF-16 offset. Nodes that explicitly exercise focus also compare the observed focus state.
4. The rendered pixels are compared with the profile selected for the case.

For standard Avalonia text, data comes from `TextBlock.TextLayout` and
`HitTestTextPosition`. The custom Material text-field label uses its dedicated internal
`TextLayout` probe so it has the same line and character-box data. The editable Compose text-field
input has no independent root drawing-origin probe yet; it remains covered by the text-field image
region, while the label and supporting text are measured structurally.

Focused cases capture the terminal focus state, not a transition frame. Compose requests real
focus only after the component's own interaction collector has been composed, advances a bounded
simulated clock until the requested focus state is observed, then advances synchronization,
terminal, and commit frames (including a 5-second terminal animation tick). Avalonia uses real
pointer input and settles its control-owned motion. The producer asserts the observed Compose
interaction-source focus state and the Avalonia scene asserts its editor focus state. Both test
environments keep the focused caret visibly on without blinking, so a wall-clock blink phase
cannot be accepted or mistaken for a parity difference.

## Comparison profiles and raster acceptance

[`tests/VisualParity/comparison-profiles.json`](../tests/VisualParity/comparison-profiles.json)
is a version-6, centralized semantic raster policy. It has no viewport-sized allowance and no
global `Δ<=1` floor. Profile JSON rejects unknown fields, duplicate/unreachable prefix rules, and
invalid quota ranges so a typo cannot silently make the comparison looser.

Raster comparison happens only after the structural contract has been evaluated. The comparer
constructs masks from the expected image plus facts which already matched between Compose and
Avalonia. A changed pixel can consume one, and only one, region budget, in this priority order:

1. matched text glyph/ink edge;
2. matched shadow edge;
3. expected generic shape edge;
4. matched shadow support;
5. strict rejection outside those masks.

The comparer never registers, translates, crops, or locally re-matches screenshots. It compares all
four premultiplied RGBA channels at the same physical coordinate. The pixel quota for each owner
and delta bin is derived from that owner’s expected mask, not from the screenshot size:

```text
quota = min(maximum, max(minimum, base + ceil(eligiblePixels * per10k / 10000)))
```

Thus a matched text node can absorb its own bounded low/mid-delta grayscale anti-aliasing, while a
large flat color change, a missing glyph, or a difference outside its expected ink boundary still
fails. Generic shape edges use a separate expected-gradient threshold; the elevated-button profile
uses the lower threshold `4` solely to cover its low-contrast rounded boundary, with only `Δ<=2`
receiving the large edge allocation and `Δ<=8` still sparse.

Elevation has two separate allocations, and only receives them after its full `ParityShadow`
contract matches:

- diffuse support: `Δ<=1`, with `16 + ceil(5% * supportPixels)` pixels, capped at 4096;
- expected support boundary: `Δ<=48`, with a sparse perimeter-derived allocation capped at 48,
  component area at most 12px, and component thickness at most 2px.

The capture schema is now v5. Its shadow contract includes `occluderDrawn`, in addition to shape,
corners, local-to-device matrix, elevation, colors/alpha, opacity/transparent-occluder flag,
lighting, and clip. This prevents an opaque component interior from consuming shadow allowance,
while preserving valid `shadow-only` cases where the expected shadow mesh is visible inside the
shape; `transparentOccluder` continues to describe whether that painted occluder lets the mesh
remain visible through it.

`comparison.json` reports `exactMatch`, `semanticRasterEquivalent`, or `mismatch` through its
`kind` field, plus each region’s eligible pixels, observed pixels/energy, dynamic quota, and
largest connected component. Any renderer-specific allowance must be narrow, justified, and
changed in the policy file rather than embedded in an individual test.

`foundation/typography/display-headline/*` is deliberately marked as skipped—not passed by a
tolerance—because Avalonia currently lacks an equivalent baseline API. It remains in both scene
catalogs and is visible in test output. Conversely,
`textfield/*/focused-empty/light` remains an active failure when its focused caret endpoints or
length differ; no broad text-field exception hides that defect.

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

Rectangle failures use a single `(left, top, right, bottom)` tuple instead of one message per edge:

```text
Text 'label' character 4 bounding differs (LTRB px): expected (249.728, 76.000, 265.896, 116.000), actual (250.528, 72.000, 266.896, 112.000), tolerance (0.500, 0.500, 0.500, 0.500).
```

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
