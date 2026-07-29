---
name: run-visual-parity-tests
description: Execute, filter, inspect, and diagnose the Compose-versus-Avalonia visual-parity suite. Use create-visual-parity-tests for authoring scenes, cases, probes, or comparison policy.
---

# Run visual parity tests

Run the smallest useful selection, locate its fresh artifacts, and diagnose in contract-first
order. The C# comparer and checked-in profile JSON are the only acceptance authority.

## Load only the task package

Always read:

1. the active backlog task;
2. `docs/visual-parity-core.md`;
3. the relevant fixture, test, comparer, and case files for the selected run.

Follow the task's `decisions` and `docs` fields. Additionally read `docs/animation-parity.md` only
for motion, `docs/comparison-profiles.md` plus the profile JSON for policy work,
`docs/upstream-baselines.md` for baseline conflicts, and `docs/visual-parity-test-plan.md` for
infrastructure/schema changes. Do not read ROADMAP or all decision history merely because a run is
roadmap-scoped; the task is the evidence boundary.

## Commands

Run from the repository root:

```powershell
dotnet test tests/Material3Expressive.VisualTests/Material3Expressive.VisualTests.csproj `
  --filter "Category=VisualParity&ParityCase=<exact-or-prefix>"
```

Use `ParityCase=<id>` for one case and `ParityCase~<family>/` for a family. Use
`Category=VisualParity` for the full suite. For comparer-only checks, filter the relevant unit
test, such as `FullyQualifiedName~SemanticRasterBudgetTests`. Prefer one family invocation when
the fixture can share its fresh Compose bundle. A filtered xUnit run can still generate the full
expected bundle.

After every run, locate the newest `artifacts/visual-parity/xunit/<timestamp>/`; never reuse a path
from an older log. Read `comparison.txt`, then `comparison.json`, expected/actual `layout.json`,
normal raster evidence, and finally `rejected-mask.png` and `diff.png`.

## Official verdicts

Use `comparison.json.kind`:

- `exactMatch`: exact pixels and contracts;
- `semanticRasterEquivalent`: contracts pass and normal semantic raster policy passes;
- `rasterWarning`: structure passes, normal policy rejects, and a narrow temporary warning accepts;
- `mismatch`: a contract or raster layer fails.

Keep skipped rows outside this enum. `isMatch` is only the execution gate. For motion, report the
exact timeline contract separately from the frame kind; a warning never means approximate timing.

Classify mismatches in this order: infrastructure/manifest/font/catalog, motion identity and clock
metadata, geometry/text/shadow contract, then raster policy. Fix earlier layers before tuning
pixels. For raster rejection, confirm the profile snapshot/rule, rejection reasons and bounds, and
that opaque pixels in `rejected-mask.png` equal `raster.rejectedPixels`.

## Diagnosis

Compare expected and actual at identical physical coordinates. Do not align, resize, blur, search
neighboring pixels, or use SSIM/perceptual hashes as a gate. For motion, compare scenario,
checkpoint, trigger, origin, elapsed ticks, delivery timestamps, capture index, schedule hash, and
clock coverage before images. For text and shadows, trust named structural probes before studying
raster residuals.

Use the bundled script only as supplemental explanation:

```powershell
python .codex/skills/run-visual-parity-tests/scripts/analyze_parity_diff.py `
  artifacts/visual-parity/xunit/<run>/actual/cases/<case-id>
```

It requires Pillow, NumPy, and OpenCV; do not install packages implicitly. If authoring is wrong,
return to `$create-visual-parity-tests`, change one variable, and rerun the smallest case then its
family. Do not change implementation or policy when the user asked only to diagnose.

## Report and roadmap boundary

Report the exact command, fresh artifact root, selected/completed counts, each non-accepted kind,
structural/raster evidence, warning reason and removal condition, motion two-run hash result when
relevant, and the smallest next hypothesis. Return this as task evidence. Leave task status and
Current focus unchanged; only the roadmap orchestration workflow changes them.

Do not add a baseline, ignore rectangle, or broader profile to make a result pass. Read
`references/semantic-raster-model.md` or `references/image-diagnostics.md` only when the report
fields or image explanation require that detail.
