# Comparison profile authoring

This document is the repository authority for designing, selecting, calibrating, and reviewing
visual-parity comparison profiles. The live policy is
[`tests/VisualParity/comparison-profiles.json`](../tests/VisualParity/comparison-profiles.json), and
the C# loader/comparer remains authoritative for exact validation and runtime behavior.

Profiles model narrowly understood renderer differences after the Compose and Avalonia scenes have
already matched structurally. They are not regression baselines and must not make a wrong layout,
token, glyph, fill, border, caret, or shadow contract pass.

## Non-negotiable invariants

- Compare the same viewport at the same coordinates; never crop, resize, recenter, blur, or search
  neighboring pixels for acceptance.
- Keep `strictChannelDelta` at `0`. Every non-exact accepted pixel must belong to an
  expected-derived semantic owner.
- Structural comparison runs first. Viewport, node identity, visibility, parent, bounds, focus,
  text layout/style, and shadow-contract failures remain failures regardless of raster budgets.
- Derive owners from the Compose expected image and matched structural facts, never from the actual
  image. An incorrect actual edge must not create its own permission.
- Assign each non-exact pixel to at most one owner and one delta bin.
- Scope policy by reusable semantic profile and ordered case prefix, not by individual test-body
  tolerances or stored Avalonia screenshots.
- Prove every new allowance with a passing intended mutation and a failing boundary mutation.
- A `warningRasterPolicy` is a temporary, second-level decision only. It runs only after the
  normal raster policy rejects a structurally valid case; it records a warning rather than turning
  that residual into semantic equivalence.

## Temporary complex-shape warning

On 2026-07-29 the user authorized a temporary, narrowly scoped exception for unavoidable desktop
raster residuals on complex Material paths (including non-rounded-rectangle shapes and wave
lines). The `complex-shape` profile preserves the normal structural gates and derives its only
owner from the expected `shapeEdge` mask. Its normal policy is the ordinary bounded container-edge
policy. Only after that policy rejects can its warning policy accept up to 99 high-delta pixels,
25,245 energy, and a 99-pixel/99-thickness connected component. It is currently mapped
exclusively to `progress/circular-wavy/*`. `SemanticRasterBudgetTests` proves that 99 high-delta
expected-edge pixels produce `rasterWarning` and 100 still fail.

This is a user-recorded Deferred calibration decision, not a generic profile-selection shortcut.
Do not add other prefixes without renewed evidence and an explicit roadmap decision; do not use
it for fills, missing content, geometry, or pixels outside the expected edge.

## Decide whether policy should change

Classify the difference before editing JSON:

| Evidence | Action |
|---|---|
| Viewport, bounds, baseline, caret, text range, style, focus, or shadow inputs differ | Fix the scene, probe, adapter, or implementation. Do not change raster policy. |
| Structure matches and differences follow expected glyph ink | Reuse or calibrate `textInk`. |
| Structure matches and differences form a thin expected non-text edge | Reuse or calibrate `shapeEdge`. |
| The full shadow contract matches and differences are diffuse, low-delta shadow signal | Reuse or calibrate `shadowSupport`. |
| The full shadow contract matches and rare higher deltas stay on the expected shadow perimeter | Reuse or calibrate `shadowEdge`. |
| A fill, glyph, border, or shadow is shifted, missing, too thick, or outside the expected mask | Fix the implementation. Do not add an allowance. |
| A required structural fact cannot be observed on Avalonia | Add a narrowly scoped skipped theory row with a reason until a probe/API exists. |

Use this order of preference:

1. Reuse the profile already selected for the nearest semantic family.
2. Add a narrow prefix rule that points to an existing profile when the contract is already
   represented.
3. Add a new profile only when the component family has a distinct, repeatable renderer contract
   that cannot be expressed by an existing profile.
4. Change comparer code or the schema only when the owner model itself is insufficient; accompany
   that change with contract tests and a documented schema migration.

Do not clone a profile merely to turn one failing case green. First show that multiple
representative cases share the same expected-derived difference and that the nearest existing
profile cannot describe it without weakening unrelated families.

## Select owners

The runtime owner priority is specific to general:

1. `textInk`, one owner per structurally matched text node;
2. `shadowEdge`, one owner per fully matched shadow;
3. `shapeEdge`, the generic expected non-text edge owner;
4. `shadowSupport`, one owner per fully matched shadow;
5. strict, for every remaining pixel.

Earlier owners win overlaps. This is allocation priority, not UI paint order.

### `textInk`

Use only for backend glyph rasterization and antialiasing differences. The comparer clips to
matched expected text lines, detects expected glyph/ink edges, and applies the configured physical-
pixel dilation. `compareGeometry` and `compareTextLayout` must both be enabled. Do not use an entire
line rectangle or text control rectangle as an allowance.

### `shapeEdge`

Use for thin antialiasing differences at expected rounded corners, paths, icons, indicators,
dividers, and strokes. A generous low-delta bin may cover the eligible edge mask, but higher-delta
bins should remain sparse and tightly bounded by connected-component area and thickness.

### `shadowSupport`

Use for broad, low-delta quantization inside an expected shadow envelope. It requires
`compareGeometry`. The owner is created only after the complete `ParityShadow` contract matches,
including shape, elevation, colors/alpha, light, transform, scale, clip, `occluderDrawn`, and
`transparentOccluder`.

### `shadowEdge`

Use only when a separate sparse budget is needed at the expected shadow boundary. It requires both
`compareGeometry` and `shadowSupport`. Keep component area and thickness small so a shifted or
solid shadow cannot consume a perimeter budget.

## Schema reference

The root object contains:

| Field | Contract |
|---|---|
| `schemaVersion` | Must match the loader's current version; currently `7`. |
| `profiles` | Unique profile definitions. Unknown fields are rejected. |
| `caseProfileRules` | Ordered first-prefix-match mapping from case ID to profile. |

Each profile contains:

| Field | Contract |
|---|---|
| `id` | Stable unique semantic name, not a single case ID. |
| `layoutTolerance` | Non-negative finite physical-pixel tolerances for `geometryPx`, `textPx`, `textBaselinePx`, and `textLineBoundsPx`. |
| `rasterPolicy.algorithm` | Must be `semantic-raster-v1`. |
| `rasterPolicy.strictChannelDelta` | Must remain `0`. |
| `warningRasterPolicy` | Optional secondary semantic policy. It is evaluated only when the normal raster policy fails and every structural check passed. |
| `warningReason` | Required non-empty Deferred/review reason whenever `warningRasterPolicy` is present; forbidden otherwise. |
| `edgeDetectionChannelDelta` | Non-zero threshold used to derive expected edge masks. Lower only with evidence for a repeatable low-contrast boundary. |
| `shadowSignalChannelDelta` | Expected shadow-signal threshold used when deriving shadow masks. |
| `shadowPaddingPerElevationPx` | Non-negative finite envelope padding per elevation unit. |
| `minimumShadowPaddingPx` | Non-negative minimum shadow-envelope padding. |
| `textInk`, `shapeEdge`, `shadowSupport`, `shadowEdge` | Optional owner policies containing `dilationPx` and one or more ordered budgets. |
| `compareGeometry` | Enables structural geometry comparison; required by `textInk`, `shadowSupport`, and `shadowEdge`. Keep it enabled for normal component profiles. |
| `compareTextLayout` | Enables text structure/style comparison; required by `textInk`. |
| `compareRaster` | Enables the official same-coordinate raster gate. |

Each owner policy contains a non-negative `dilationPx` and one or more budgets. Budget
`maxChannelDelta` values must be positive and strictly increasing. A pixel enters the first bin
whose ceiling covers its maximum premultiplied-RGBA channel delta.

| Budget field | Meaning |
|---|---|
| `minimumPixelsPerOwner` | Count floor for small owners. |
| `basePixelsPerOwner` | Fixed count allowance before proportional scaling. |
| `pixelsPer10kEligiblePixels` | Count rate per 10,000 expected eligible owner pixels; range `0..10000`. |
| `maxPixelsPerOwner` | Hard count cap; must be at least the minimum and base. |
| `minimumEnergyPerOwner` | Delta-energy floor for small owners. |
| `baseEnergyPerOwner` | Fixed energy allowance before proportional scaling. |
| `energyPer10kEligiblePixels` | Energy rate per 10,000 eligible pixels. |
| `maxEnergyPerOwner` | Hard energy cap; must be at least the minimum and base. |
| `maxConnectedComponentPixels` | Maximum accepted area of every connected difference component. |
| `maxConnectedComponentThicknessPx` | Maximum accepted thickness of every connected component. |

Count and energy allowances scale with the expected owner mask, not viewport size:

```text
scaledCount = ceil(EligiblePixels * PixelsPer10k / 10000)
AllowedCount = min(MaxPixels, max(MinimumPixels, BasePixels + scaledCount))

scaledEnergy = ceil(EligiblePixels * EnergyPer10k / 10000)
AllowedEnergy = min(MaxEnergy, max(MinimumEnergy, BaseEnergy + scaledEnergy))
Energy = sum(maximum premultiplied-RGBA channel delta of each observed pixel)
```

A bin passes only when count, energy, maximum delta, and every connected component's area and
thickness all pass. Do not rely on a high delta ceiling alone.

## Author a rule or profile

### Reuse an existing profile

Add the narrowest stable family prefix to `caseProfileRules`:

```json
{ "prefix": "newcomponent/", "profileId": "component-with-text" }
```

Rules use first-prefix-match semantics. Put a specific rule before a broader rule:

```json
{ "prefix": "newcomponent/elevated/", "profileId": "component-with-text-and-elevation" },
{ "prefix": "newcomponent/", "profileId": "component-with-text" }
```

Reversing those rows makes the specific row unreachable and validation must reject it. Prefixes
must be unique and every referenced profile must exist.

### Add a new profile

Before editing, write a short calibration note containing:

1. the renderer difference being modeled;
2. the structural evidence proving the scenes are equivalent;
3. the smallest owner that contains the difference;
4. representative affected families and why existing profiles are insufficient;
5. observed eligible pixels, delta histogram, energy, component area, and thickness;
6. the intended passing mutation and the first failing boundary mutation.

Start from the semantically nearest profile, then justify every changed field. Keep profile IDs
semantic, such as `component-with-text`, rather than component- or case-specific. Add only the
owners that the family needs; omitted owners remain strict.

### Calibrate budgets

1. Run repeated identical captures to confirm determinism and establish the renderer noise floor.
2. Confirm all structural contracts pass before collecting raster evidence.
3. Inspect several representative cases, including small and large eligible masks.
4. Choose the narrowest owner and lowest delta bins that contain the repeatable difference.
5. Set proportional rates from expected eligible pixels; use base/minimum values only for genuine
   small-owner behavior and hard caps to prevent growth on large cases.
6. Bound connected-component area and thickness from the observed renderer pattern.
7. Add positive and negative mutation tests before accepting the new values.

Profile values must be based on checked-in tests and reproducible artifacts, not a single visual
inspection. Diagnostic thresholds used by image scripts never become acceptance policy
automatically.

## Required validation

At minimum, run:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter 'FullyQualifiedName~SemanticRasterBudgetTests'

dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter 'FullyQualifiedName~ParityContractTests'

.\tools\visual-parity\run.ps1 -Case "affected-family/*"
```

Then run the full parity suite before treating a profile change as complete. Review the selected
profile ID and profile SHA-256 in the new actual manifest.

Mutation coverage must demonstrate both sides of the boundary:

- the intended backend difference passes inside the expected owner;
- the next larger delta, wider/thicker component, excess count/energy, or out-of-mask mutation
  fails;
- structural mutations still fail before raster acceptance;
- a warning policy cannot downgrade a structural mismatch, and its first over-budget mutation
  still fails;
- owner overlap consumes only the documented higher-priority owner.

## Review checklist

- [ ] The difference is a renderer effect, not a scene, probe, token, layout, text, caret, or shadow
      implementation defect.
- [ ] The nearest existing profile was evaluated before adding a new one.
- [ ] The rule prefix is stable, narrow, ordered before broader prefixes, and reaches every intended
      case without catching unrelated families.
- [ ] Geometry remains enabled for normal component profiles; text and shadow owners have their
      required structural contracts.
- [ ] `strictChannelDelta` remains zero and every allowance is expected-derived.
- [ ] Delta bins are increasing and bounded by count, energy, component area, and thickness.
- [ ] Positive and negative mutation tests prove the intended boundary.
- [ ] Focused comparer tests, affected family tests, and the full suite pass.
- [ ] Any warning policy has a specific removal/recalibration reason and is visible as
      `rasterWarning` in run artifacts; it is not counted as semantic equivalence.
- [ ] No baseline image, whole-frame ignore region, per-case tolerance, recentering, cropping, or
      diagnostic-only metric was added to acceptance.
