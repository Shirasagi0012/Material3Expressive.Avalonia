# Semantic raster model

Use this reference when reading `comparison.json`, explaining a raster pass/failure, or reviewing a
profile change. Treat source code and `comparison-profiles.json` as authoritative when they differ.

## Contents

- [Decision pipeline](#decision-pipeline)
- [Expected-derived ownership](#expected-derived-ownership)
- [Canonical version-6 policy](#canonical-version-6-policy)
- [Dynamic budget](#dynamic-budget)
- [Current report interpretation](#current-report-interpretation)
- [RGBA and visual RGB](#rgba-and-visual-rgb)
- [Policy review rules](#policy-review-rules)

## Decision pipeline

```text
pinned environment + expected bundle + hashes
  -> manifest/catalog/viewport contract
  -> node identity, visibility, parent, bounds, focus
  -> text content/layout/style and shadow input contract
  -> same-coordinate raster comparison
     -> exact
     -> expected-derived semantic budgets
        -> equivalent or raster mismatch
```

A structural failure remains a failure even if the raster happens to be close. Raster budgets
explain backend rendering differences only after the two scenes represent the same semantics and
geometry.

## Expected-derived ownership

Build permissive masks only from the Compose expected image and structural facts that matched.
Never derive an allowance from actual edges, because a shifted or malformed actual shape would then
create its own permission.

Assign each pixel to at most one owner. Current source order is:

1. one `text:<node-id>` owner per matched text node;
2. one `shadow-edge:<node-id>` owner per matched shadow when configured;
3. one generic shape-edge owner;
4. one `shadow:<node-id>` support owner per matched shadow;
5. no owner, which means strict.

Earlier owners win overlaps. This is an allocation priority, not the UI's paint order.

### Text ink

Clip to matched expected text lines, detect expected glyph/ink edges, and dilate only by the profile
radius. Do not use the whole line rectangle as a tolerance region. Require matching text content,
line ranges, baselines, character boxes, origins, and style signatures before creating the owner.

### Shape edge

Detect edges from the expected image and use a narrow dilation band. This can model antialiasing at
rounded corners, icons, paths, indicators, and thin strokes. Keep high-delta and thick connected
components out of this category so a shifted opaque edge cannot pass.

### Shadow support and edge

Require a matched `ParityShadow` first. Derive an expected envelope/support region from shape,
transform, elevation, light, clip, `occluderDrawn`, `transparentOccluder`, and expected shadow
signal. Exclude the painted opaque occluder from support and edge owners. Give diffuse support a
broad but low-delta budget. If a separate boundary owner exists, reserve any rare higher-delta
allowance for the expected shadow boundary, not the support interior.

### Strict

Keep background, fills, shape interiors, and all unowned pixels strict. A scene containing text or
shadow does not change this default.

## Canonical version-6 policy

Use these as the settled policy shape; read `comparison-profiles.json` for complete coefficients
and family mappings:

- keep `strictChannelDelta` at zero for every profile;
- dilate expected text and generic shape edges by one physical pixel;
- give text ink broad bounded bins through delta 20 and only a sparse tail through delta 35;
- give generic shape edges broad delta-at-most-2 coverage and sparse delta-at-most-8 coverage;
- use expected edge-detection delta 16 normally and 4 only for the low-contrast elevated-button
  boundary profile;
- let the text-field shape owner cover its renderer-specific edge deltas through 18, only inside
  the expected shape-edge mask;
- dilate shadow support by two physical pixels and allow only delta 1, with
  `16 + ceil(5% * eligible support pixels)` capped at 4096;
- allow shadow-boundary deltas through 48 only in a sparse perimeter-derived quota capped at 48,
  with each component at most 12 pixels and 2 pixels thick.

No number grants a whole-frame or whole-line allowance. The expected owner mask, count, energy,
hard cap, and component constraints all have to pass.

## Dynamic budget

For each owner and delta bin, compute count and energy allowances from the expected mask size:

```text
scaled = ceil(EligiblePixels * RatePer10k / 10000)
AllowedCount = min(HardCap, max(Minimum, Base + scaled))
AllowedEnergy = min(EnergyHardCap, max(EnergyMinimum, EnergyBase + scaledEnergy))
Energy = sum(max-channel delta for observed pixels in the bin)
```

The exact rounding and fields come from `SemanticRasterComparer.cs`. Fixed versioned coefficients
make policy reviewable; expected eligible pixels make the per-case allowance proportional to the
semantic owner rather than viewport size.

For every bin, inspect:

- `observedPixels` versus `allowedPixels`;
- `observedEnergy` versus `allowedEnergy`;
- `rejectedPixels` and `maximumRejectedChannelDelta`;
- `largestConnectedComponentPixels`;
- `largestConnectedComponentThicknessPx`.

Count stops a large number of discrepancies. Energy stops many pixels parked just below a delta
ceiling. Component area and thickness stop a compact missing fill, border segment, or shifted solid
shape from consuming a dispersed-AA budget.

## Current report interpretation

`comparison.json` contains:

- `profileId`: profile selected by the first matching ordered case-prefix rule; validation rejects
  a later rule shadowed by an earlier broader prefix;
- `kind`: official `exactMatch`, `semanticRasterEquivalent`, or `mismatch` classification;
- `isMatch`: official case result after all structural and enabled raster checks;
- `failures`: structural and/or raster failure messages;
- `pixels.differentPixels`: currently rejected pixel accounting, not raw unequal pixels;
- `raster.ignoredFloorPixels`: nonzero deltas at or below the strict floor;
- `raster.allowedPixels`: pixels accepted inside semantic budgets;
- `raster.rejectedPixels`: rejected accounting used by raster pass/fail;
- `raster.regions`: owner-level eligible-pixel and bin reports.

Interpret reporting labels directly:

```text
kind=exactMatch                -> structural contract passed and no raster budget was consumed
kind=semanticRasterEquivalent  -> structural contract passed and allowedPixels > 0
kind=mismatch                  -> structural and/or raster failure; classify the first failing layer
```

Keep `kind` authoritative. Do not infer exactness from `pixels.differentPixels == 0`, because that
field reports rejected accounting and accepted semantic differences also produce zero there.

## RGBA and visual RGB

The formal version-6 comparer takes the maximum same-coordinate delta across all four premultiplied
RGBA channels. It does not composite onto a backdrop and does not search neighboring pixels. The
built-in `diff.png` visualizes RGB only, so consult `comparison.json` or the diagnostic script for
alpha-only differences.

The diagnostic script additionally composites decoded images over an explicit backdrop and reports
visual RGB. Use that result to explain storage/alpha behavior, not to override the official
four-channel result or `kind`.

## Policy review rules

When changing a profile:

1. state the renderer difference being modeled;
2. scope it to the smallest semantic owner and case family;
3. keep geometry/text/shadow contracts enabled;
4. add a mutation test showing the intended difference passes;
5. add a boundary mutation showing the next larger/wider/thicker difference fails;
6. run affected cases, the complete family, comparer unit tests, and the full suite;
7. review the versioned profile hash in the new manifest.

Keep profile schema validation strict: reject unknown fields, invalid/overlapping bins, duplicate
profiles, duplicate prefixes, and unreachable prefix rules rather than silently ignoring them.
