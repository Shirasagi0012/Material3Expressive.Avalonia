---
name: create-visual-parity-tests
description: Create, extend, migrate, and review component visual-parity test definitions in the Avalonia Material3Expressive repository by mirroring pinned Compose Multiplatform Material 3 scenes, defining structural probes and deterministic animation checkpoints, and authoring narrowly justified semantic or temporary warning comparison profiles. Use when adding coverage for a control or state, migrating a remaining Material component, modifying a Compose reference or Avalonia VisualCase, adding probes, or reviewing test authoring against D:\source\androidx and D:\source\compose-multiplatform-core. Use run-visual-parity-tests instead when the task is primarily test execution, artifact inspection, or failure diagnosis.
---

# Create Visual Parity Tests

Use this skill to add a reproducible Compose-versus-Avalonia conformance case. Treat the pinned
Compose implementation as the executable reference and Avalonia as the implementation under test.
The goal is a failure that explains what differs, not a screenshot that merely looks close.

Keep this skill focused on authoring test definitions and their comparison contract. After making
the case runnable, hand execution, artifact inspection, and result diagnosis to
`$run-visual-parity-tests`. Do not duplicate its command catalog or report-analysis workflow here.

## Load the repository contract first

Before editing, read:

- `docs/visual-parity.md` for capture contracts, environment assumptions, and current comparison
  semantics relevant to authoring.
- `docs/upstream-baselines.md` for the implementation-source baseline versus executable parity
  reference boundary.
- `docs/animation-parity.md` whenever an intermediate animation frame, delay, repeated phase, or
  motion-clock declaration is involved.
- `docs/comparison-profiles.md` before selecting, adding, or changing a comparison profile.
- `docs/visual-parity-test-plan.md` for architectural boundaries and migration decisions.
- `tests/ComposeReference/README.md` for the producer protocol.
- `tests/VisualParity/comparison-profiles.json` before adding or changing a profile.
- The existing nearest-neighbor case in `tests/Material3Expressive.VisualTesting/Cases/` and its
  Compose scene under `tests/ComposeReference/capture-app/src/main/kotlin/.../scenes/`.

When the case belongs to a ROADMAP task, also read `ROADMAP.md`, the task document under
`docs/roadmap/backlog/`, and every referenced record in `docs/roadmap/decisions.md`. `ROADMAP.md`
controls task selection and status; the task document controls scope, dependencies, exit criteria,
and evidence. The parity documents control parity architecture. Do not treat one as a substitute
for another, and do not select or close a different roadmap task from this skill.

The two parity registries are currently separate and the stable case ID is their matching key. Do
not introduce a third registry or a checked-in screenshot baseline for one new case.

## Workflow

### 1. Derive the behavior and state matrix

Start from the component's public API and upstream implementation, not from the target screenshot.
Inspect the complete AndroidX call chain in `D:\source\androidx`, usually including:

1. the public composable's default parameters;
2. `*Defaults` and token definitions;
3. internal layout and measurement code;
4. the actual default child composables, indicators, dividers, labels, state layers, and shadows;
5. screenshot/behavior tests under `src/androidDeviceTest` and relevant unit tests.

Use `D:\source\compose-multiplatform-core` only when scene setup, `ImageComposeScene`, Skiko,
font handling, or desktop rendering behavior is relevant. It is not a replacement for AndroidX
semantics or Material defaults.

Write a compact state matrix before coding. Cover independent behavior axes rather than generating
every Cartesian combination:

- visual variant and size;
- light/dark theme;
- enabled/disabled;
- empty/populated or selected/unselected;
- focused, pressed, hovered, error, indeterminate, or expanded state when supported;
- leading/trailing icons, label, placeholder, supporting text, badge, or other slots;
- long text, multiline, RTL, or non-default density only when they expose a distinct contract.

Choose cases that expose layout boundaries and state transitions. Give each case a stable lowercase
ID such as `textfield/outlined/error/light`, and keep the same ID, viewport, content, theme, and
state on both sides.

Before trusting a reference image, verify the reference scene's outer constraints, required size,
padding, density, insets, and background. A `requiredSize` can clip a component and make a correct
Compose screenshot look wrong. Separate the composable's total bounds from its visible field or
content area. For example, an outlined field's label-cutout overhang and supporting slot are part
of its total layout even when the visible outline is shorter.

### 2. Register the Compose expected scene

Add the scene in the appropriate file under
`tests/ComposeReference/capture-app/src/main/kotlin/material3expressive/compose/reference/scenes/`.
Register a `CaseContract` in the reference case registry with:

- the exact case ID;
- logical viewport width/height in dp;
- theme and render scale;
- required nodes with stable IDs, kinds, and parent IDs;
- expected focus only for cases that explicitly exercise focus.

Use `ProbeRegistry` rather than estimating geometry from pixels:

- add `geometry(id)` / `text(id)` to important layout nodes;
- add `onTextLayout(id)` for text content, lines, baselines, origins, and character boxes;
- record focus only after the requested semantic state is actually observed;
- record a shadow contract for renderer-level elevation cases;
- probe custom-drawn elements such as an indicator, notch, floating label, caret, divider, or
  selection marker whenever a stable renderer-independent structural fact can be exposed. If a
  continuously changing path has no independent layout node or stable capture adapter, document
  that limitation instead of inventing surrogate geometry.

For static cases, capture a deterministic settled state. For motion parity, author one case per
checkpoint with `MotionCaseContract`: keep the initial target at rest, give every checkpoint the
same scenario ID and trigger action, select an exact elapsed time in 100ns ticks, and supply one
deterministic `ReferenceScene.trigger`. Set `usesComposeCoroutineClock=true` when `delay` or another
coroutine-time operation contributes to the frame. Do not call `ImageComposeScene.render`, advance
a private clock, sleep, or calculate another frame schedule in scene code; the producer owns the
registration transaction and complete replay schedule.

Keep reference colors and typography complete. Construct both themes from the same full token
contract; an omitted role can silently fall back to a framework default. Resolve defaults from
AndroidX source even when the token name or visual intuition suggests another color.

### 3. Register the Avalonia scene

Add one matching `VisualCase` to the appropriate file under
`tests/Material3Expressive.VisualTesting/Cases/`, then ensure it is included exactly once through
`VisualCaseCatalog`.

Mirror the Compose contract exactly:

- keep `VisualCase.Viewport` in logical dp/DIP values even though its current type is `PixelSize`;
  do not paste the expected PNG dimensions into the case;
- use the same theme, locale, flow direction, text, slots, enabled/focus state, and background;
- name every control or internal element that the contract compares;
- use `Prepare` for static input/focus setup and settled-state capture;
- use `VisualContext.Require<T>` to target controls instead of searching by incidental visual order.

For a stable terminal state, call `context.SettleAnimations()` before capture when the component
owns motion. For a first-class intermediate frame, add `VisualMotionSpec` with the exact matching
scenario/checkpoint/trigger action and put the state change or input in its `Trigger`. The renderer
enters `MotionTestClockScope` before `CreateContent` and replays the Compose-produced schedule; do
not call `UseManualMotionClock()` or `AdvanceAnimations(...)` from that case. Those helpers remain
valid for focused unit tests and legacy static setup, not for manifest-backed motion parity.

Every animated Avalonia value in a motion case must be owned by `MotionGroup`. Native Avalonia
`Transition`, composition animations, wall-clock timers, and MaterialSymbol `DoubleTransition`
are unsupported until a controlled-clock adapter exists. Declare the limitation or refactor the
component instead of claiming clock coverage. Motion metadata is structural: a changed event,
timestamp, schedule hash, or coverage declaration remains a mismatch and cannot be accepted by
`semanticRasterEquivalent` or `rasterWarning`.

If the component draws an important element without a child `Visual`, add a named internal probe
or capture adapter when it can expose a stable renderer-independent structural fact. Never invent
a surrogate probe or present the outer control bounds as if they measured the internal path. When
a continuously changing path has no independent `LayoutNode`/`Visual` counterpart, record the
reason explicitly, keep the public component bounds and exact motion metadata structural, and
treat only the moving path as raster evidence.

When using a native Avalonia control inside a Material template, audit its resolved template and
pseudo-class/resource setters. Check `Foreground`, placeholder color and opacity, caret brush,
focused/disabled/pointer-over resources, border/background, and alignment. Public Material
properties may be overridden by the platform template.

### 4. Choose the comparison profile

Follow `docs/comparison-profiles.md`. Reuse the nearest semantic profile first; add a narrow ordered
prefix rule when an existing contract fits, and create a new profile only for a distinct,
repeatable renderer difference supported by several representative cases.

Do not edit policy until viewport, geometry, text, focus, and shadow contracts pass. Any new owner
or budget requires focused positive and negative mutation tests. Keep structural defects active;
use a narrowly reasoned skipped row when the platform cannot expose a required structural fact.

Treat the normal `rasterPolicy` as the durable semantic-equivalence boundary. Add a
`warningRasterPolicy` only for an explicitly accepted Deferred issue: keep its prefix narrow,
provide `warningReason`, record a removal or recalibration condition, and prove that the first
out-of-warning mutation still fails. Never use warning policy to downgrade a structural defect.

For a motion scenario, evaluate a proposed warning across every registered checkpoint and
calibrate against the worst repeatable residual rather than one convenient frame. Run the same
selected scenario twice first; expected and actual PNGs, motion JSON, schedule hashes, and layout
hashes must repeat. Keep one coherent Deferred owner, reason, scope, and removal/recalibration
condition for the scenario.

### 5. Hand off validation

Once the Compose scene, Avalonia case, probes, and any required profile tests are complete, invoke
`$run-visual-parity-tests` to run the smallest case, its family, and the full suite as appropriate.
Use that skill to locate fresh artifacts, classify `comparison.json.kind`, and diagnose failures.

If this work is attached to a roadmap task, return the command, artifact, classification, and
remaining gaps as task evidence. Do not mark the task complete from parity alone: the task's other
completion dimensions and exit criteria still apply. A passing alpha22 executable case is parity
evidence for the pinned reference, not proof that an alpha25-only implementation behavior is complete.

Treat `exactMatch` and `semanticRasterEquivalent` as fully accepted parity outcomes. A
`rasterWarning` may be integrated because xUnit accepts it, but it remains visible implementation
debt and does not close the underlying parity work unless the roadmap explicitly records that
temporary disposition. A `mismatch` remains incomplete; a skipped row remains unevaluated.
For any normal-policy rejection, require the v10 report's owner/reason/bounds evidence and confirm
`rejected-mask.png` contains exactly `raster.rejectedPixels`; do not calibrate from raw `diff.png`.

For motion, record two conclusions instead of compressing them into one label: the timeline
contract either matches exactly or structurally fails, and only then does the captured frame receive
its raster classification. Never describe `rasterWarning` as approximate timestamp, duration,
easing, or clock parity.

If validation reveals an authoring defect—such as mismatched scene input, missing probes, an
incorrect viewport, nondeterministic state, or an unjustified profile—return here, change one
authoring variable, and hand the smallest relevant case back for another run. A case is not complete
until the run skill confirms its structural contract and official comparer result.

## Rules for physical pixels, text, and effects

Compose layout frequently resolves in integer physical pixels while Avalonia retains logical
fractional coordinates. Reproduce the Compose rounding explicitly at the render scale:

- use `roundToPx` semantics for dimensions;
- use `ceil` where the Compose placeable path does;
- use integer division for centering and document how remainders are assigned;
- inspect weighted layout remainder distribution;
- convert back to Avalonia logical coordinates only after physical geometry is decided;
- check stroke width, half-pixel endpoints, corner radii, and device scale for borders, indicators,
  dividers, and notches.

Treat text as two contracts. First compare the font hash, requested/resolved family, weight, size,
line height, letter spacing, draw origin, line bounds, baseline, UTF-16 caret/advance boxes,
content, and normalized paint signature. The paint signature uses paint kind, opaque foreground
RGB, and 8-bit-quantized effective opacity so color alpha and brush opacity compare by rendered
meaning. Only then permit the standard full-range `Δ<=255` raster allowance inside the matched
expected glyph-edge owner. This accommodates high-contrast antialiasing and a one-physical-pixel
glyph phase difference; it is not permission to use the whole line/control rectangle. Do not move
an entire text run to hide a caret endpoint difference. Record a precise case-level gap when an
Avalonia API cannot express the reference line-height or baseline semantics, and quarantine that
case as an explicit skip until a structural probe exists.

Recognize the intentional false-negative tradeoff: a raster-only missing or malformed stroke can
be accepted when all residual pixels stay inside the expected-derived glyph owner. Do not claim
that this policy proves glyph coverage identity; it proves the captured text/layout/paint contract
and constrains renderer differences to the expected ink neighborhood.

Measure the actual laid-out text object (`TextLayout`, `TextLayoutResult`, or the framework adapter)
rather than assuming all content is a `string` or substituting an outer control width for intrinsic
text width. Material slots may accept text composables, controls, templates, or arbitrary content.

Model elevation in the shared rendering/capture layer. Compose/Skia Material shadows use a point-
light shadow contract; a simple Avalonia `BoxShadow` offset/blur fit will not remain correct across
shape, size, clip, scale, and theme. Compare shape, elevation, ambient/spot colors and alpha,
opacity/flags, light position/radius, transform, device scale, parent clip, `occluderDrawn`, and
`transparentOccluder` independently from the shadow raster.

## Review checklist

Before considering a new case complete, verify:

- [ ] AndroidX defaults, internal layout, child composables, and relevant screenshot tests were
      inspected and any surprising source fact is documented near the implementation.
- [ ] The reference scene is not accidentally clipped by its outer constraints.
- [ ] The case ID, logical viewport, theme, font, scale, content, state, and time agree on both
      sides.
- [ ] Every diagnostically important non-Visual element has a named probe or explicit reason why it
      cannot be probed.
- [ ] Every motion scenario reports the exact timeline-contract result separately from each
      checkpoint's captured-frame classification.
- [ ] A proposed motion warning was checked at every checkpoint, calibrated from the worst
      repeatable frame, and verified by two same-input scenario runs with stable hashes.
- [ ] Text is measured from the actual layout object and has verifiable style and normalized paint
      signatures.
- [ ] Native Avalonia templates and state resources were audited where applicable.
- [ ] Profile selection or changes satisfy `docs/comparison-profiles.md`.
- [ ] If roadmap-scoped, the active task, referenced decisions, and task-specific evidence are
      updated without changing task status beyond the task's actual completion.
- [ ] The expected outcome is recorded as exact, semantic, warning, mismatch, or skipped; warning
      cases include a reason, narrow scope, and removal/recalibration condition.
- [ ] Validation was handed to `$run-visual-parity-tests`, with the smallest case and relevant
      broader selections run successfully.
- [ ] Any new allowance has the required positive and negative mutation coverage.
- [ ] Any raster rejection has a v10 reason and rejected-mask/report count agreement.
- [ ] No generated PNG, baseline, or blanket allow-list was added to source control.

For copyable Compose/C# registration skeletons and a state/probe worksheet, read
`references/case-authoring-template.md`.
