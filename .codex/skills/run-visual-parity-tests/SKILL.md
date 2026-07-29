---
name: run-visual-parity-tests
description: Run, filter, inspect, and diagnose the Compose-versus-Avalonia visual-parity suite in the Material3Expressive repository. Use when executing the complete suite, one case, or a component family; locating fresh expected/actual artifacts; interpreting exact, semantic-equivalent, warning-accepted, mismatch, contract, geometry, text-layout, shadow, and raster results; or performing same-coordinate image-difference analysis without changing the official acceptance policy.
---

# Run Visual Parity Tests

Run the smallest relevant parity selection, locate the artifact root created by that invocation,
and diagnose failures in contract-first order. Treat the repository's C# comparer and versioned
profile file as the only authority for classification and acceptance. Use Python image analysis
only to explain a result, never to replace it.

Keep this skill focused on execution and evidence-based diagnosis. If the task is to add or modify
a Compose reference scene, Avalonia `VisualCase`, structural probe, or comparison-profile test,
hand that authoring work to `$create-visual-parity-tests`; return here to validate the result.

## Refresh the current contract

Before running or interpreting results, read:

- `docs/visual-parity.md` for the current environment, commands, and artifact layout;
- `docs/upstream-baselines.md` for the implementation-source baseline versus executable parity
  reference boundary;
- `docs/animation-parity.md` for motion-origin, replay, clock-coverage, and unsupported-path rules;
- `docs/comparison-profiles.md` when the task involves profile selection, owner budgets, or policy
  review;
- `tests/VisualParity/comparison-profiles.json` for current policy values and case-prefix rules;
- `tests/Material3Expressive.VisualTests/ComposeParityFixture.cs` for fresh bundle and artifact
  lifecycle;
- `tests/Material3Expressive.VisualTests/ComposeVisualParityTests.cs` for xUnit case traits and
  warning reporting;
- `tests/Material3Expressive.VisualTesting/Parity/ParityComparer.cs` for structural comparison;
- `tests/Material3Expressive.VisualTesting/Parity/SemanticRasterComparer.cs` for raster behavior.

Do this on every policy/debugging task. The formal policy is profile schema version 7 and the
capture/comparison contract is schema version 10. Keep the checked-in profile and source authoritative if a
later schema deliberately changes coefficients, masks, or report fields.

When the run is part of a ROADMAP task, read `ROADMAP.md`, the active task document under
`docs/roadmap/backlog/`, and referenced records in `docs/roadmap/decisions.md`. Use those documents
to label the result as task evidence. Do not choose a task, change Current focus, or close a task
from this diagnostic skill.

## Run the tests

Run all commands from the repository root.

Run the complete parity suite:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity"
```

Run one exact case ID:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity&ParityCase=button/filled/medium/enabled/light"
```

Run a case family by xUnit substring matching:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity&ParityCase~button/"
```

Run the deterministic motion prototype:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity&ParityCase~foundation/motion/linear-tween/"
```

Run the component motion families:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity&ParityCase~progress/circular-wavy/motion/"
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity&ParityCase~progress/loading-indicator/indeterminate/"
```

Use an exact direct xUnit filter when extra `dotnet test` options are needed:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter 'Category=VisualParity&ParityCase=button/filled/medium/enabled/light'
```

Use substring matching for a family:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter 'Category=VisualParity&ParityCase~button/'
```

Run focused comparer/policy unit tests without launching the Compose producer:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter 'FullyQualifiedName~SemanticRasterBudgetTests'
```

One xUnit invocation creates one fresh Compose expected bundle and shares it across the selected
theory rows. Filtering to one Avalonia case still generates and validates the complete Compose
bundle in the fixture. Prefer a family invocation over many separate case invocations when the
same expected bundle can be reused.

The `dotnet test` console summary may not print accepted `rasterWarning` details. After every run,
locate the newest `artifacts/visual-parity/xunit/<timestamp>/` directory and inspect the actual
manifest and per-case comparison reports. Do not rely on an older console log or infer warning
status from the xUnit pass count.

To debug only the reference producer, follow `tests/ComposeReference/README.md` and pass `--case`
to Gradle. That verifies Compose capture; it does not run the Avalonia comparison.

A non-zero process exit caused by a parity mismatch means the harness ran successfully and found
a conformance failure. Distinguish that from build, Gradle, manifest, font, or catalog failures.
`rasterWarning` keeps `isMatch=true` and therefore does not fail xUnit, but it must remain visible
and separately reported as temporary implementation debt.
Treat an xUnit skipped row separately from both pass and mismatch. The current display/headline
baseline cases are explicitly skipped because the required Avalonia structural API is unavailable;
they are not tolerance-approved passes.

Remember the dual-baseline distinction in every report: parity results describe the pinned
Compose Multiplatform executable reference, while implementation completeness is assessed against
the AndroidX source baseline and the active roadmap task. Do not report an alpha22 parity pass as
completion of an alpha25-only behavior.

## Locate the fresh result

Never assume the last path mentioned in an older console log is current. Find the newest artifact
directory after the command completes:

```powershell
$parityRun = Get-ChildItem -Directory 'artifacts\visual-parity\xunit' |
  Sort-Object LastWriteTimeUtc -Descending |
  Select-Object -First 1
$parityRun.FullName
```

The important files are:

| Path | Meaning |
|---|---|
| `expected/manifest.json` | Pinned Compose environment, catalog, hashes, and expected paths |
| `expected/cases/<id>/image.png` | Fresh Compose image |
| `expected/cases/<id>/layout.json` | Compose structural contract |
| `actual/manifest.json` | Avalonia environment, profile hash, and completed case results |
| `actual/cases/<id>/expected.png` | Per-case copy of the expected image |
| `actual/cases/<id>/image.png` | Avalonia capture |
| `actual/cases/<id>/layout.json` | Avalonia structural capture |
| `actual/cases/<id>/comparison.txt` | Concise assertion summary |
| `actual/cases/<id>/comparison.json` | Full structural and raster report |
| `actual/cases/<id>/diff.png` | Built-in visual diff preview |
| `actual/cases/<id>/rejected-mask.png` | Official normal-policy rejected pixels only; opaque red over transparency |

The actual manifest can be partial when only some rows ran, a row was skipped, or the process
stopped early. A skipped row has no official comparison `kind`. An infrastructure/contract failure
may occur before a per-case `comparison.json` exists; in that case use the test output and expected
manifest rather than searching for a missing raster report.

Read results in this order:

1. test output and `comparison.txt`;
2. `comparison.json` kind, failures, warnings, and selected profile;
3. expected and actual `layout.json`, comparing `motion` first when present and then the named node;
4. normal `raster` rejection reasons/owners/budgets and, for warnings, secondary `warningRaster`;
5. `rejected-mask.png`, then expected, actual, built-in `diff.png`, and optional diagnostics.

Inspect `rejected-mask.png` first whenever the normal policy rejected pixels. It contains only the
official normal-policy rejection set, not raw differences accepted by semantic budgets. It is empty
for exact/semantic passes and remains non-empty for a `rasterWarning` so the temporary debt stays
visible. Then inspect `diff.png` for context. `ParityRun.CreateDiff` maps the largest same-coordinate RGB
channel delta to a simple green-to-red preview: exact pixels are green and larger differences move
toward red. It is deliberately a quick spatial overview, not the semantic acceptance mask or a
complete statistics report. The official comparer uses all four premultiplied RGBA channels, so
an alpha-only difference can be absent from this RGB preview and still affect the official result.

## Interpret the verdict

Use `comparison.json.kind` as the official completed-case classification:

- **`exactMatch`**: every same-coordinate premultiplied RGBA pixel is exact and all contracts pass;
- **`semanticRasterEquivalent`**: contracts pass and every non-exact pixel is accepted by its
  normal expected-derived semantic mask and budget; treat this as fully accepted parity;
- **`rasterWarning`**: every structural check passes and the normal raster policy rejects, but a
  narrowly scoped secondary warning policy accepts; xUnit accepts it, while the issue remains
  visible Deferred work;
- **`mismatch`**: one or more structural or raster checks fail.

Keep **skipped** outside this enum: it is an xUnit discovery/execution status with an explicit
reason, not a comparer result.

For a motion case, report the timeline and frame result separately. Scenario/checkpoint identity,
trigger, origin, elapsed ticks, delivery timestamps, capture index, schedule hash, and clock
coverage are an exact structural verdict. Only after that verdict passes does `kind` classify the
captured frame. A `rasterWarning` is therefore Deferred rendering debt on an exactly matched tested
timeline, not approximate animation-clock acceptance.

Use `comparison.json.isMatch` only as the execution-gate projection: it is true for exact,
semantic-equivalent, and warning outcomes, and false for mismatch. Do not use it as the quality
classification. Classify the first failing layer within `mismatch` as:

- **Infrastructure / Contract failure**: environment, manifest, font, catalog, viewport, identity,
  or required probe is invalid;
- **Geometry / TextLayout / ShadowContract failure**: structural facts differ; raster allowance
  cannot turn the case into a pass;
- **RasterMismatch**: structure passes but one or more pixel differences are outside a mask,
  outside a delta bin, over count/energy budget, or over connected-component limits.

For text, a high channel delta is not independently suspicious when it is assigned to a matched
`text-ink` owner: the standard final `Δ<=255` bin intentionally accepts opaque foreground versus
background transitions caused by a one-physical-pixel glyph phase difference. Confirm that the
text geometry, shaping/style inputs, and normalized paint signature all pass, and that
`rejectedPixels` is zero. A wrong glyph color or opacity must appear as a paint-signature failure;
a changed pixel outside the expected-derived glyph owner must remain rejected.

Do not derive a different outcome from a diagnostic image statistic when `kind` is present. For
older artifacts without `kind`, derive only a clearly marked legacy fallback; a warning can be
recovered only when the artifact also contains warning evidence.

Do not interpret `pixels.differentPixels` as the raw number of unequal image pixels. In the current
model it is based on `raster.rejectedPixels`; accepted semantic differences appear under
`raster.allowedPixels`, strict-floor differences under `raster.ignoredFloorPixels`, and the raw
count under `raster.rawDifferentPixels`. Use the bundled image script for additional distributions.

## Understand semantic raster acceptance

Raster budgets apply only after structural evidence passes. The comparer assigns each non-exact
pixel to at most one expected-derived owner, then checks its delta bin's count, energy, connected-
component area, and thickness. Unowned pixels remain exact.

In v10, read `profileRulePrefix` and the embedded `profile` snapshot before interpreting the dynamic
report. Every rejection carries a reason, inclusive bounds, and bounded RGBA samples. Unowned
differences appear in `strict-outside-semantic-mask`; they are no longer recoverable only by
subtracting owner totals. Count/energy overages use a deterministic highest-delta residual witness,
while a component-limit failure rejects the complete violating component. The union is exactly the
set rendered by `rejected-mask.png`.

The normal `rasterPolicy` decides exact versus semantic equivalence. Only when it rejects and all
structural checks passed may `warningRasterPolicy` run against the same semantic context. If it
accepts, inspect the rejected normal raster report, accepted warning raster report, and documented
warning reason together; do not describe the result as semantic equivalence.

Read `references/semantic-raster-model.md` to interpret owner and budget fields in a report. Read
`docs/comparison-profiles.md` for profile design, schema rules, calibration, and mutation-test
requirements. If policy must change, hand authoring to `$create-visual-parity-tests`, then return
here to validate the affected cases.

## Diagnose a mismatch or warning

Use this sequence:

1. If no case artifact exists, diagnose Gradle/.NET startup, manifest/schema, version, font hash,
   catalog, or case selection first.
2. Read every structural failure. For motion cases, compare scenario/checkpoint, event, origin,
   elapsed ticks, every delivery timestamp, capture index, schedule hash, and clock coverage before
   input/viewport/node/focus/layout/text/shadow facts. Fix those before studying raster thresholds.
3. Compare the same named node in expected and actual `layout.json`. Do not infer its position from
   screenshot pixels when a probe exists.
4. If structure passes, inspect `raster.regions[].budgets[]`: compare observed/allowed count and
   energy, rejection reasons/bounds/samples, maximum rejected delta, largest component area, and
   thickness. Confirm the alpha-nonzero pixel count in `rejected-mask.png` equals
   `raster.rejectedPixels`.
5. For `rasterWarning`, identify exactly why normal `raster` rejected, confirm `warningRaster`
   accepted the same expected-derived owner, record every warning message, and state the profile's
   removal/recalibration condition.
6. For a motion warning, inspect every checkpoint in the scenario and identify the worst residual;
   do not infer scenario-wide acceptability from one clean frame. Before accepting calibration
   evidence, run the complete scenario twice and confirm identical expected and actual PNG,
   motion, schedule, and layout hashes.
7. Run the bundled image diagnostic for raw pixel distribution and visual evidence.
8. Form one hypothesis, inspect the relevant AndroidX/Avalonia/rendering source, change one variable,
   rerun the smallest case, then rerun the family.

Run the diagnostic script on a case artifact directory:

```powershell
python .codex/skills/run-visual-parity-tests/scripts/analyze_parity_diff.py `
  'artifacts/visual-parity/xunit/<run>/actual/cases/<case-id>'
```

Generate reusable visual diagnostics:

```powershell
python .codex/skills/run-visual-parity-tests/scripts/analyze_parity_diff.py `
  'artifacts/visual-parity/xunit/<run>/actual/cases/<case-id>' `
  --output-dir 'artifacts/visual-parity/diagnostics/<case-name>'
```

The script validates and summarizes `rejected-mask.png` when a v10 case artifact provides it, and
also complements `diff.png` with raw diagnostics. Its primary statistics use
the same-coordinate four-channel RGBA delta used by the official comparer. It also reports a
backdrop-composited visual RGB view to explain alpha/storage behavior. It provides delta histogram,
energy, bbox, row/column concentration, color transitions, and connected components. With an output
directory it writes a JSON report, heatmap, mask, side-by-side strip, nearest-neighbor crop, and
blink GIF. `--threshold` and `--bins` are diagnostic controls only; they do not modify the official
profile or verdict.

The script requires Pillow, NumPy, and OpenCV. Check availability before use. If any dependency is
missing, tell the user exactly which package is needed and ask before installing it; do not install
packages implicitly.

Read `references/image-diagnostics.md` for visual patterns and image-comparison techniques.

## Comparison guardrails

- Compare expected and actual at identical dimensions and coordinates. Do not align, recenter,
  resize, blur, or search neighboring pixels for an acceptance result.
- Inspect at 100% physical pixels and with nearest-neighbor magnification. Smooth zoom hides 1px
  endpoint and rounding defects.
- Use a blink view to reveal displacement; use a heatmap and bbox to reveal magnitude and extent.
- Treat SSIM, perceptual hashes, PSNR, or feature matching as optional exploration only. Never use
  them as a parity gate; a high score can hide a wrong token color, missing border, or shifted icon.
- For transparent scenes, remember that the formal version-7 comparer compares all four
  premultiplied RGBA channels directly. Use backdrop-composited visual RGB only as a supplemental
  explanation; it does not override `kind`.
- Do not add a baseline, ignore rectangle, or broader profile merely because the diff looks small.

For roadmap-scoped runs, hand the artifact path, official kind, structural/raster evidence, and
remaining failure list back to the active task. Leave task status and Current focus unchanged;
the Goal/roadmap orchestration workflow owns those transitions.

## Report back

Summarize a run with:

- the exact command and artifact root;
- selected and completed case counts;
- separate counts for `exactMatch`, `semanticRasterEquivalent`, `rasterWarning`, `mismatch`, and
  skipped rows;
- the official `kind` for every non-exact or non-accepted case;
- for warnings, the normal-policy rejection, warning-policy acceptance, warning reason, and
  removal/recalibration condition;
- for motion scenarios, the exact timeline-contract verdict, checkpoint counts by official kind,
  the worst warning checkpoint when present, and the two-run hash-repeatability result;
- for mismatches, the first failing layer and concrete node/metric/budget evidence;
- for raster rejection, the selected rule/profile snapshot, rejection reasons, rejected bounds,
  samples, and rejected-mask/report pixel-count agreement;
- one next hypothesis or smallest rerun command.

If asked only to run or diagnose, do not change implementation or comparison policy without an
explicit request.
