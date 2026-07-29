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
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity"
```

The test fixture launches Gradle once, generates the complete fresh Compose bundle, validates its
manifest and hashes, and shares it across one independently discoverable xUnit theory row per
case. Each row captures only its matching Avalonia scene, writes its artifacts and manifest entry,
and reports its own result. A non-zero result caused by visual differences is meaningful: the
harness has run successfully and found implementation work to do.

Run one Avalonia case while still generating the complete Compose expected bundle:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity&ParityCase=button/filled/medium/enabled/light"
```

Select a family with xUnit substring matching:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity&ParityCase~button/"
```

The corresponding xUnit data rows carry a `ParityCase` trait, so IDE and `dotnet test` filtering
can address the same cases directly.

The `dotnet test` console summary may not print accepted `rasterWarning` details. Treat the fresh
`actual/manifest.json` and per-case `comparison.json` artifacts as the authoritative warning report.

To inspect only the reference producer, run the commands in
[`tests/ComposeReference/README.md`](../tests/ComposeReference/README.md). Its `--case` argument
accepts a case ID or a prefix wildcard such as `button/*`.

## What is compared

The current catalog contains 96 representative foundation, button, card, FAB, checkbox, text-field,
navigation, tab, slider, surface, divider, ListItem, and segmented-list cases. Every case has the same stable ID,
viewport, theme, and named probe contract on both sides.

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
2. Named component bounds are compared as un-clipped root-relative physical pixels. The Compose
   producer records each probe's `positionInRoot` plus its measured size, rather than the
   scroll-clipped `boundsInRoot()` rectangle, so it matches Avalonia visual bounds. `TextBlock` nodes retain
   their framework bounds; the custom Material text-field label is projected to the equivalent
   snapped Compose layout slot. Floating line and caret/advance data is compared separately.
   Images are never auto-aligned or recentered.
3. Registered text nodes compare content, line ranges, baselines, and one advance/caret box per
   UTF-16 offset. Nodes that explicitly exercise focus also compare the observed focus state.
4. The rendered pixels are compared with the profile selected for the case.

Motion cases add a structural check between steps 1 and 2. Compose records the trigger, logical
time origin, complete frame-delivery schedule, capture index, schedule hash, and controlled clock
coverage in the current capture schema v10. Avalonia creates a fresh tree and replays that exact schedule through
component-owned `MotionGroup` instances. A timeline mismatch fails before raster classification;
neither semantic equivalence nor a warning policy can accept it. See
[animation-parity.md](animation-parity.md) for the lifecycle and authoring rules.

Report a motion case with separate timeline and captured-frame conclusions. The timeline contract
is exact structural evidence; the captured frame then receives the ordinary raster classification.
Consequently, `rasterWarning` means the tested timeline matched exactly but the frame retains a
Deferred rendering residual. It does not mean that timestamp alignment or clock coverage was
accepted approximately.

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
is a version-7, centralized semantic raster policy. It has no viewport-sized allowance and no
global `Δ<=1` floor. Profile JSON rejects unknown fields, duplicate/unreachable prefix rules, and
invalid quota ranges so a typo cannot silently make the comparison looser.

The version lines are independent: the top-level capture-result and comparison-report contract is
v10, the centralized comparison-profile policy is v7, and the nested motion record is v1. A motion field
change bumps the top-level capture contract even if profile coefficients and policy schema do not
change.

Use [`comparison-profiles.md`](comparison-profiles.md) as the authoring and calibration contract
when selecting a profile, adding a case-prefix rule, or changing an owner budget. This section is
the operational overview; the JSON and comparer source remain the runtime authority.

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

Thus a matched text node can absorb any channel delta inside its expected-derived glyph owner,
while a wrong text/style/paint contract or a difference outside that owner still fails. This
deliberately favors avoiding false mismatches and can accept a raster-only glyph defect contained
entirely by the owner. Generic shape edges use a separate expected-gradient threshold; the
`component-with-text-and-elevation` profile used by elevated buttons and dragged ListItems uses
the lower threshold `4` solely to cover its low-contrast rounded boundary, with only `Δ<=2`
receiving the large edge allocation and `Δ<=8` still sparse. Its shadow allowances still require
an exact complete shadow contract before they can own a pixel.

Elevation has two separate allocations, and only receives them after its full `ParityShadow`
contract matches:

- diffuse support: `Δ<=1`, with `16 + ceil(5% * supportPixels)` pixels, capped at 4096;
- expected support boundary: `Δ<=48`, with a sparse perimeter-derived allocation capped at 48,
  component area at most 12px, and component thickness at most 2px.

The capture schema is now v10. The shadow contract introduced before v9 includes `occluderDrawn`, in addition to shape,
corners, local-to-device matrix, elevation, colors/alpha, opacity/transparent-occluder flag,
lighting, and clip. This prevents an opaque component interior from consuming shadow allowance,
while preserving valid `shadow-only` cases where the expected shadow mesh is visible inside the
shape; `transparentOccluder` continues to describe whether that painted occluder lets the mesh
remain visible through it.

Schema v8 introduced optional motion metadata, retained unchanged by v10. Static cases omit it. Motion cases compare scenario,
checkpoint, event, origin/unit/scale, elapsed time, every frame timestamp, capture index, schedule
hash, and clock coverage exactly before geometry or raster comparison.

Schema v10 adds an exact text paint signature containing paint kind, opaque foreground RGB, and
8-bit-quantized effective opacity. Normalizing color alpha and brush opacity this way makes
equivalent paint representations compare equally. The signature is compared before the
expected-derived text owner becomes eligible.

The v10 `comparison.json` snapshots the selected profile, matched rule prefix, and profile schema,
then reports `exactMatch`, `semanticRasterEquivalent`, `rasterWarning`, or `mismatch` through its
`kind` field. It separates raw, floor-ignored, allowed, and rejected pixels; records owner priority,
eligible/observed pixels and energy, dynamic quotas, rejection reasons, rejected bounds, and bounded
same-coordinate RGBA samples. A `rasterWarning` means the normal semantic
policy rejected an otherwise structurally valid case, but a narrowly scoped temporary warning
policy accepted it; the strict and warning diagnostics are both retained. Any renderer-specific
allowance must be narrow, justified, and changed in the policy file rather than embedded in an
individual test.

The standard text profiles use a full-range `Δ<=255` final bin inside the matched expected glyph
edge mask. This intentionally accepts high-contrast antialiasing and one-physical-pixel glyph phase
differences after text layout, shaping inputs, and paint match exactly. It is not a line-rectangle
allowance: pixels outside the expected glyph owner remain strict.

The currently user-authorized temporary `complex-shape` profile is an explicit exception to the
normal sparse-edge calibration: it is selected only by `progress/circular-wavy/*`, preserves all
structural checks, and assigns only expected shape-edge pixels. Its normal policy keeps the
bounded `Δ<=8` container-edge budget; a secondary warning policy may then accept a capped
`Δ<=255` residual of at most 99 pixels, with a 99-pixel/99-thickness component cap. The 99/100
boundary is covered by a comparer mutation test. It is not a global tolerance, is reported as a
warning rather than semantic equivalence, and is recorded as Deferred in the roadmap pending a
user decision to recalibrate or remove it.

`foundation/typography/display-headline/*` is deliberately marked as skipped—not passed by a
tolerance—because Avalonia currently lacks an equivalent baseline API. It remains in both scene
catalogs and is visible in test output. Conversely,
`textfield/*/focused-empty/light` remains an active failure when its focused caret endpoints or
length differ; no broad text-field exception hides that defect.

The selected profile ID, complete selected-profile snapshot, ordered matching rule, and the
SHA-256 of the centralized profile file are retained across `comparison.json` and the actual
manifest. This makes a tolerance-policy change both visible and reconstructable from every run.

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
      rejected-mask.png
      comparison.json
      comparison.txt
```

Artifacts are deliberately not committed. `actual/manifest.json` identifies the Avalonia runtime,
the profile configuration hash, and the per-case outputs.

Use the failure category to start triage:

- `Infrastructure` or `Contract`: producer environment, manifest, catalog, or probe mismatch.
- `Geometry`: container, icon, or child placement/size is wrong.
- `TextLayout`: typography, line breaking, baseline, or character advance differs.
- `Raster`: inspect `rejected-mask.png` first for the official normal-policy rejection set, then
  use `expected.png`, `image.png`, and `diff.png` to explain its paint, shape, color, shadow, or
  glyph context.

`comparison.json` contains the complete machine-readable result; `comparison.txt` is the concise
failure summary emitted with the xUnit assertion. `rejected-mask.png` is always generated at the
expected viewport size: rejected pixels are opaque red and every other pixel is transparent. It is
empty for exact and normal semantic passes. For `rasterWarning`, it preserves the pixels rejected by
the normal policy even though the secondary warning policy accepts the case. It never contains raw
differences which the normal semantic policy accepted.

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
   For intermediate animation frames, follow [animation-parity.md](animation-parity.md): author one
   case per checkpoint with `MotionCaseContract` and `VisualMotionSpec`, and let the producer-owned
   schedule drive both sides.
3. Resolve the case to an existing comparison profile whenever possible. If the family has a
   distinct renderer contract, follow [`comparison-profiles.md`](comparison-profiles.md) to add a
   reviewed prefix rule, profile, and boundary mutation tests.
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
