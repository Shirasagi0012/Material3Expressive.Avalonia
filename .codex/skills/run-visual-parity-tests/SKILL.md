---
name: run-visual-parity-tests
description: Run, filter, inspect, and diagnose the Compose-versus-Avalonia visual-parity suite in the Material3Expressive repository. Use when executing the complete suite, one case, or a component family; locating fresh expected/actual artifacts; interpreting contract, geometry, text-layout, shadow, and semantic-raster results; or performing same-coordinate image-difference analysis without changing the official acceptance policy.
---

# Run Visual Parity Tests

Run the smallest relevant parity selection, locate the artifact root created by that invocation,
and diagnose failures in contract-first order. Treat the repository's C# comparer and versioned
profile file as the only authority for pass/fail. Use Python image analysis only to explain a
result, never to replace it.

Keep this skill focused on execution and evidence-based diagnosis. If the task is to add or modify
a Compose reference scene, Avalonia `VisualCase`, structural probe, or comparison-profile test,
hand that authoring work to `$create-visual-parity-tests`; return here to validate the result.

## Refresh the current contract

Before running or interpreting results, read:

- `docs/visual-parity.md` for the current environment, commands, and artifact layout;
- `docs/comparison-profiles.md` when the task involves profile selection, owner budgets, or policy
  review;
- `tools/visual-parity/run.ps1` for filter behavior;
- `tests/VisualParity/comparison-profiles.json` for current policy values and case-prefix rules;
- `tests/Material3Expressive.VisualTesting/Parity/ParityComparer.cs` for structural comparison;
- `tests/Material3Expressive.VisualTesting/Parity/SemanticRasterComparer.cs` for raster behavior.

Do this on every policy/debugging task. The formal policy is profile schema version 6 and the
capture contract is schema version 5. Keep the checked-in profile and source authoritative if a
later schema deliberately changes coefficients, masks, or report fields.

## Run the tests

Run all commands from the repository root.

Run the complete parity suite:

```powershell
.\tools\visual-parity\run.ps1
```

Run one exact case ID:

```powershell
.\tools\visual-parity\run.ps1 -Case "button/filled/medium/enabled/light"
```

Run a case family by prefix:

```powershell
.\tools\visual-parity\run.ps1 -Case "button/*"
```

Use the equivalent direct xUnit filter only when extra `dotnet test` options are needed:

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

To debug only the reference producer, follow `tests/ComposeReference/README.md` and pass `--case`
to Gradle. That verifies Compose capture; it does not run the Avalonia comparison.

A non-zero process exit caused by a parity mismatch means the harness ran successfully and found
a conformance failure. Distinguish that from build, Gradle, manifest, font, or catalog failures.
Treat an xUnit skipped row separately from both pass and mismatch. The current display/headline
baseline cases are explicitly skipped because the required Avalonia structural API is unavailable;
they are not tolerance-approved passes.

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

The actual manifest can be partial when only some rows ran, a row was skipped, or the process
stopped early. A skipped row has no official comparison `kind`. An infrastructure/contract failure
may occur before a per-case `comparison.json` exists; in that case use the test output and expected
manifest rather than searching for a missing raster report.

Read results in this order:

1. test output and `comparison.txt`;
2. `comparison.json` failures and selected profile;
3. expected and actual `layout.json` for the named node;
4. raster region/budget report;
5. expected, actual, built-in `diff.png`, and optional diagnostic images.

Inspect `diff.png` first. `ParityRun.CreateDiff` currently maps the largest same-coordinate RGB
channel delta to a simple green-to-red preview: exact pixels are green and larger differences move
toward red. It is deliberately a quick spatial overview, not the semantic acceptance mask or a
complete statistics report. The official comparer uses all four premultiplied RGBA channels, so
an alpha-only difference can be absent from this RGB preview and still affect the official result.

## Interpret the verdict

Use `comparison.json.kind` as the official completed-case classification:

- **`exactMatch`**: every same-coordinate premultiplied RGBA pixel is exact and all contracts pass;
- **`semanticRasterEquivalent`**: contracts pass and every non-exact pixel is accepted by its
  expected-derived semantic mask and budget;
- **`mismatch`**: one or more structural or raster checks fail.

Keep **skipped** outside this enum: it is an xUnit discovery/execution status with an explicit
reason, not a comparer result.

Use `comparison.json.isMatch` as the boolean pass/fail projection. Classify the first failing layer
within `mismatch` as:

- **Infrastructure / Contract failure**: environment, manifest, font, catalog, viewport, identity,
  or required probe is invalid;
- **Geometry / TextLayout / ShadowContract failure**: structural facts differ; raster allowance
  cannot turn the case into a pass;
- **RasterMismatch**: structure passes but one or more pixel differences are outside a mask,
  outside a delta bin, over count/energy budget, or over connected-component limits.

Do not derive a different outcome from a diagnostic image statistic when `kind` is present. For
older artifacts without `kind`, derive only a clearly marked legacy fallback.

Do not interpret `pixels.differentPixels` as the raw number of unequal image pixels. In the current
model it is based on `raster.rejectedPixels`; accepted semantic differences appear under
`raster.allowedPixels`, and strict-floor differences under `raster.ignoredFloorPixels`. Use the
bundled image script for raw same-coordinate statistics.

## Understand semantic raster acceptance

Raster budgets apply only after structural evidence passes. The comparer assigns each non-exact
pixel to at most one expected-derived owner, then checks its delta bin's count, energy, connected-
component area, and thickness. Unowned pixels remain exact.

Read `references/semantic-raster-model.md` to interpret owner and budget fields in a report. Read
`docs/comparison-profiles.md` for profile design, schema rules, calibration, and mutation-test
requirements. If policy must change, hand authoring to `$create-visual-parity-tests`, then return
here to validate the affected cases.

## Diagnose a failure

Use this sequence:

1. If no case artifact exists, diagnose Gradle/.NET startup, manifest/schema, version, font hash,
   catalog, or case selection first.
2. Read every structural failure. Fix or explain input/viewport/node/focus/layout/text/shadow facts
   before studying raster thresholds.
3. Compare the same named node in expected and actual `layout.json`. Do not infer its position from
   screenshot pixels when a probe exists.
4. If structure passes, inspect `raster.regions[].budgets[]`: compare observed/allowed count and
   energy, rejected pixels, maximum rejected delta, largest component area, and thickness.
5. Run the bundled image diagnostic for raw pixel distribution and visual evidence.
6. Form one hypothesis, inspect the relevant AndroidX/Avalonia/rendering source, change one variable,
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

The script complements the existing `diff.png`; it does not replace it. Its primary statistics use
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
- For transparent scenes, remember that the formal version-6 comparer compares all four
  premultiplied RGBA channels directly. Use backdrop-composited visual RGB only as a supplemental
  explanation; it does not override `kind`.
- Do not add a baseline, ignore rectangle, or broader profile merely because the diff looks small.

## Report back

Summarize a run with:

- the exact command and artifact root;
- selected and completed case counts;
- skipped case count and reasons, separately from pass/fail;
- official pass/fail results;
- the official `kind` (`exactMatch`, `semanticRasterEquivalent`, or `mismatch`);
- the first failing layer and concrete node/metric/budget evidence;
- one next hypothesis or smallest rerun command.

If asked only to run or diagnose, do not change implementation or comparison policy without an
explicit request.
