# Image diagnostics

Use these techniques after contract, geometry, text layout, and shadow inputs have been checked.
They explain a warning or mismatch; they do not alter the official result.

## High-value views

Start with the case's existing `diff.png`. The current C# helper uses the maximum same-coordinate
RGB channel delta and renders an exact-green to high-delta-red overview. Use it to locate the
affected area quickly; use `comparison.json` and the bundled script when magnitude, ownership,
component shape, alpha, or raw counts matter. The official comparer includes premultiplied alpha;
the built-in RGB preview does not.

| View/statistic | Best use |
|---|---|
| Blink expected/actual | Spot translation, size changes, missing elements, unstable animation |
| Nearest-neighbor 8x crop | Inspect 1px strokes, half-pixel endpoints, glyph edges, and corners |
| Delta heatmap | Separate sparse high-delta defects from broad low-delta noise |
| Difference bbox | Determine whether the problem is local, component-wide, or full-frame |
| Row/column projection | Reveal one displaced baseline, divider, caret, or vertical edge |
| Delta bins + energy | Distinguish a few outliers from accumulated low-amplitude error |
| Connected components | Distinguish dispersed AA from a solid missing/shifted region |
| Color transition counts | Find one wrong token/state color or systematic opacity difference |
| RGBA versus composited RGB | Separate the official stored-channel difference from visual output |

Always view at native physical resolution first. If magnifying, use nearest-neighbor resampling so
the viewer does not introduce smoothing that resembles antialiasing.

## Common visual signatures

| Signature | Likely hypothesis | Verify next |
|---|---|---|
| Two opposite-color lobes around one shape | Translation or size mismatch | named bounds, physical rounding, centering remainder |
| Thin parallel lines along a border | 1px shift or different stroke inclusion | stroke width, half-pixel endpoint, scale |
| Only glyph edge speckles | rasterizer/hinting difference | text layout and style signature must already match |
| Glyph interior or whole text uniformly different | wrong color, opacity, font weight, or glyph | resolved text style and token |
| One horizontal concentration | baseline, divider, indicator, caret endpoint | row projection and named probe |
| One vertical concentration | width, notch endpoint, caret, or centering | column projection and physical pixel math |
| Broad diffuse halo, mostly delta 1 | shadow quantization | `ParityShadow`, support owner, clip |
| Shifted or clipped halo | shadow input/layout defect | light, elevation, transform, parent clip |
| Large solid rectangle | fill/token/state/template mismatch | expected/actual color transitions and template resources |
| Full-frame low delta | theme/backdrop/color-space mismatch | manifest, theme contract, alpha composition |
| Diff changes between identical reruns | nondeterministic time/focus/font/environment | capture clock, settling, hashes |

These are hypotheses, not diagnoses. Confirm them with layout JSON, source, and the smallest rerun.

## Bundled script

Analyze a case directory containing `expected.png`, `image.png`, and optionally `comparison.json`:

```powershell
python .codex/skills/run-visual-parity-tests/scripts/analyze_parity_diff.py '<case-dir>'
```

Analyze arbitrary images:

```powershell
python .codex/skills/run-visual-parity-tests/scripts/analyze_parity_diff.py `
  '<expected.png>' '<actual.png>'
```

Write views and machine-readable statistics:

```powershell
python .codex/skills/run-visual-parity-tests/scripts/analyze_parity_diff.py `
  '<case-dir>' --output-dir '<diagnostics-dir>' --scale 8 --padding 3
```

For transparent-image diagnostics, pass the backdrop you want to inspect explicitly:

```powershell
python .codex/skills/run-visual-parity-tests/scripts/analyze_parity_diff.py `
  '<case-dir>' --backdrop '#FFFFFFFF'
```

Use `--threshold` to hide tiny four-channel RGBA deltas in diagnostic masks and `--bins` to change
printed histogram boundaries. These flags never reproduce or modify the official semantic budgets.

## Tool choices

Use Pillow for deterministic PNG decoding/encoding and blink/crop artifacts. Use NumPy for exact
same-coordinate arithmetic, histograms, energy, bbox, and color transitions. Use OpenCV only for
binary connected-component statistics.

Do not require SSIM or perceptual metrics for this workflow. They can answer whether two full
images look broadly similar, but parity needs to reject locally important defects even when their
global score is excellent. Likewise, feature matching and image registration are useful only as a
private hypothesis about displacement; never align images before the official comparison.

If Pillow, NumPy, or OpenCV is unavailable, report the missing import and ask before installing the
corresponding `pillow`, `numpy`, or `opencv-python` package.
