# AndroidX Material3 — reference notes for the Avalonia port

> Historical source survey, compiled 2026-07-26 from the local androidx checkout:
> `D:\source\androidx` @ `ff0a0a99` (androidx-main, 2026-07-26), where the library was one
> unreleased revision ahead at `1.5.0-alpha25`. The current executable visual-parity reference is
> the Compose Multiplatform producer documented in
> [visual-parity.md](visual-parity.md), pinned to Material 3 `1.5.0-alpha22` lineage. This survey
> remains useful for implementation research, but is not the versioned test contract. The two
> roles and their update policy are defined in [upstream-baselines.md](upstream-baselines.md). Common
> source root (abbreviated `<m3>` below):
> `D:\source\androidx\compose\material3\material3\src\commonMain\kotlin\androidx\compose\material3\`
>

## Headline facts

1. **Expressive is stable upstream.** Only 17 `@ExperimentalMaterial3ExpressiveApi`
   usages remain in all of commonMain; the annotation now gates only `MaterialShapes`
   (the 35-shape catalog), `LoadingIndicator`, and a few deprecated shims.
   `MaterialExpressiveTheme`, `MotionScheme`, `ButtonShapes`, XS–XL sizes,
   `ToggleButton`, `ButtonGroup`, `SplitButton`, `FloatingToolbar` are all stable.
   **We treat Expressive as default behavior, not a variant.**
2. **Theme plumbing collapsed to a single CompositionLocal** holding
   `MaterialTheme.Values(colorScheme, typography, shapes, motionScheme)`
   (`<m3>MaterialTheme.kt:307`). This matches our v3 single-inherited-property design.
3. **Button press shape-morph is corner-radius lerp, not polygon morphing.**
   `graphics-shapes` `Morph` is used by exactly two components (LoadingIndicator,
   circular wavy progress). Everything else animates `CornerBasedShape` corner sizes
   with a spring.

## Token system

### Four layers

1. **Primitive/palette** — literal values. `tokens\PaletteTokens.kt` (tonal palettes),
   `ShapeTokens.kt`, `ElevationTokens.kt`, `StateTokens.kt`, `TypeScaleTokens.kt`,
   `TypefaceTokens.kt`, `StandardMotionTokens.kt`, `ExpressiveMotionTokens.kt`.
2. **Semantic/system** — palette → role: `ColorLightTokens.kt` / `ColorDarkTokens.kt`
   (`val Background = PaletteTokens.Neutral98`), `TypographyTokens.kt`.
3. **Key-token enums** (the theme slots): `ColorSchemeKeyTokens` (47),
   `ShapeKeyTokens` (15), `TypographyKeyTokens` (30 = 15 + 15 `*Emphasized`),
   `MotionSchemeKeyTokens` (6, hand-written).
4. **Component token objects** — reference key tokens for color/shape/type; hold raw
   `Dp`/`Float` for metrics and opacities.

### The variant/size split (Button exemplar)

- **Color & state tokens per variant:** `FilledButtonTokens`, `TonalButtonTokens`,
  `OutlinedButtonTokens`, `ElevatedButtonTokens`, `TextButtonTokens`
  (e.g. `ContainerColor = ColorSchemeKeyTokens.Primary`,
  `DisabledContainerOpacity = 0.1f`, `DisabledLabelTextOpacity = 0.38f`).
- **Metrics per size:** `ButtonXSmallTokens` … `ButtonXLargeTokens` — height, icon
  size, spacing, and the shape triple
  `ContainerShapeRound` / `ContainerShapeSquare` / `PressedContainerShape`.
- Our token tables should mirror this split.

### Version drift warning

Generated files carry per-file token-DB stamps (`// VERSION: v0_11_0` etc.) and the
tree is a patchwork of ~19 distinct stamps in two formats (`v0_NNN` old,
`NN_N_N`/`NN.N.N` new/Expressive-era). Examples: `Button*Tokens` = `v0_11_0`,
`ColorLightTokens` = `v0_210`, `ShapeKeyTokens` = `14_1_0`, `Fab*` + motion = `v0_14_0`.
**Our token tables must record the per-file source stamp** or future androidx syncs
become undiffable. Some values are hard-coded upstream with TODOs (XS button padding,
icon sizes) — mark those `provisional`.

### Resolution & caching

- Per subsystem: `internal fun X.fromToken(key)` `when`-switch + an internal
  `Key.value` accessor reading the ambient theme
  (`ColorScheme.kt:1477`, `Shapes.kt:371`, `Typography.kt:748`, `MotionScheme.kt:272`).
  All internal — public API only exposes resolved values. `DisabledAlpha = 0.38f`
  (`ColorScheme.kt:1538`).
- Shape key tokens derive directional variants (`CornerExtraSmallTop` etc.) via
  `top()/end()` copies; `CornerFull` returns `CircleShape` directly (not overridable).
- **Load-bearing perf pattern:** `ColorScheme` has **60** and `Shapes` **15** mutable
  memo fields (`defaultButtonColorsCached` …) so each component's default bundle is
  built once per theme instance (`ColorScheme.kt:637`, `Shapes.kt:243`), consumed via
  `internal val ColorScheme.defaultButtonColors get() = cached ?: build().also{…}`
  extension properties scoped inside `ButtonDefaults` (`Button.kt:1222`).
  C# equivalent: lazily-populated field bag on the immutable theme object, fresh per
  theme instance.

## Theme plumbing

- `MaterialTheme.Values` is `@Immutable`; accessors read
  `LocalMaterialTheme.current.<x>`. Legacy per-subsystem locals are internal/vestigial;
  `LocalMotionScheme` is deprecated.
- `MaterialTheme {}` also provides: default indication (`ripple()`), text-selection
  colors (primary @ 0.4), and `bodyLarge` as ambient text style.
- `MaterialExpressiveTheme` (`MaterialTheme.kt:260`): nullable params; **outermost**
  call fills unset params with hard defaults (`expressiveLightColorScheme()`,
  `MotionScheme.expressive()`); **nested** calls inherit (guard:
  `LocalUsingExpressiveTheme`). No `expressiveDarkColorScheme()` exists; expressive
  Typography/Shapes defaults are still TODO upstream (Emphasized styles exist only as
  key tokens).

## Motion system

### MotionScheme interface

Six generic spec methods — `default/fast/slow` × `Spatial/Effects` — plus
`standard()` / `expressive()` factories (`MotionScheme.kt`). Impls build 6 springs
once and unchecked-cast per call. Rule: **spatial** for bounds/position/size/corner
changes, **effects** for color/alpha ("non-spatial").

### Spring constants (transcription targets, `v0_14_0`)

| Spec | Standard damping/stiffness | Expressive damping/stiffness |
|---|---|---|
| DefaultSpatial | 0.9 / 700 | 0.8 / 380 |
| FastSpatial | 0.9 / 1400 | 0.6 / 800 |
| SlowSpatial | 0.9 / 300 | 0.8 / 200 |
| DefaultEffects | 1.0 / 1600 | 1.0 / 1600 |
| FastEffects | 1.0 / 3800 | 1.0 / 3800 |
| SlowEffects | 1.0 / 800 | 1.0 / 800 |

**Invariant: all effects specs are identical between schemes and critically damped**
(damping 1.0 ⇒ never bounce). Only spatial differs — expressive is softer + slower,
which is where the bounce personality lives. Preserve this invariant.

### Consumption idioms (for our control base class)

- Size/offset (`animateDpAsState`) → spatial; color (`animateColorAsState`) → effects
  (`RadioButton.kt:83,243`; `ToggleButton.kt:1186`).
- **Snap when disabled** — no animation between enabled/disabled states
  (`RadioButton.kt:243` comment).
- Asymmetric in/out: enter = `DefaultEffects`/`SlowEffects`, exit = `FastEffects`
  (Tab, Checkbox, Chip); FAB expand: width = `FastSpatial`, alpha = `FastEffects`.
- 141 motion call sites across 38 files; heaviest: DatePicker, SearchBar (12 each).
- Legacy duration/easing tokens (`MotionTokens.kt`) survive for non-spring cases
  (e.g. LoadingIndicator's linear global rotation).

## Interaction & state layers

### State layer opacities (`StateTokens.kt`, `v0_210`)

`Hover 0.08 · Focus 0.10 · Pressed 0.10 · Dragged 0.16`
⚠️ Our current prototype uses pressed = 0.12 — that is the *old* M3 value; update.

### Ripple / indication model (`Ripple.kt`, 933 lines)

- `ripple(bounded, radius, color, focusRingShape, enable{Press,Focus,Hover,Drag}Indication)`
  returns an `IndicationNodeFactory`; fast-paths to two shared singletons when all
  params are default (allocation win worth copying).
- Three-level config: `LocalRippleThemeConfiguration` (app-wide, incl. focus style) →
  `LocalRippleConfiguration` (subtree, nullable = ripple off) → per-call-site params.
- **Expressive focus = inset ring**, not opacity: `Focus.InsetRing(outerInset 0dp,
  outerWidth 2dp, innerInset 1dp, innerWidth 3dp)`; outer color = `secondary`,
  inner = `onSecondary`. Focus-in animates `fastSpatialSpec`, focus-out
  `fastEffectsSpec`. Default remains opacity-focus for now.
- Draw-order trick (`Surface.kt:222`): when inset rings are enabled, focus indication
  is a separate modifier applied **before** the surface background (ring draws outside
  the fill); press/hover layers apply after it via `clickable`.
- Interaction bookkeeping: exit interactions carry a reference to their entry
  (`Release.press`, `Exit.enter`) — removal by identity, so overlapping pointers work.
  Elevation resolves from `interactions.lastOrNull()` (a priority stack), not booleans
  (`Button.kt:1729`). ButtonGroup's press-expand holds release until the animation is
  ≥75% visible (`waitUntil { value > 0.75f }`) so fast taps still read.

### Ripple engine mechanics (`material-ripple` commonMain)

Module layout: `androidMain` is the hardware-accelerated `RippleDrawable`/
`RippleHostView` path (not relevant to us); **`commonMain`'s `CommonRippleNode` +
`RippleAnimation` is the portable canvas-drawn path** used by non-Android platforms
(JetBrains' desktop actuals build on it) — this is the Avalonia reference.
`RippleNode` (abstract base, `Ripple.kt:309`) owns the state layer + measurement;
`CommonRippleNode` (`CommonRipple.kt:64`) owns the press ripples.

**Press ripple** (`RippleAnimation.kt`) — three animations run in parallel on press:

| Property | From → To | Duration / easing |
|---|---|---|
| Alpha | 0 → 1 | 75 ms linear |
| Radius | start → end radius | 225 ms FastOutSlowIn |
| Center | touch point → layout center | 225 ms linear |

- Start radius = `max(w, h) * 0.3`. End radius: half-diagonal of the bounds,
  **+ 10dp if bounded**; explicit `radius` param overrides.
- Fade-out (release): alpha → 0 over 150 ms linear, but only **after fade-in
  completes** — a quick tap holds the ripple until it's fully visible. If release
  arrives mid-fade-in, drawn alpha snaps to 1 immediately.
- Bounded ripples originate at the press position and clip to bounds; unbounded
  originate at (and stay in) the center.
- Ripples are keyed by `PressInteraction.Press` identity in a map; a new press
  calls `finish()` on all existing ripples (multi-touch and rapid taps overlap
  gracefully). Interactions arriving before first measure are queued
  (`pendingInteractions`) and flushed in `onRemeasured`.
- Draw order per frame: content → state layer → ripples; press alpha is applied
  at draw time (`color.copy(alpha = pressedAlpha)`).

**State layer** (non-press states, `Ripple.kt:433`): a single `Animatable` alpha —
deliberately *one* layer, not incoming+outgoing pairs (upstream comment: outgoing
transitions are ~15 ms ≈ under one frame; two layers aren't worth it). Interactions
stack in a list, `lastOrNull()` wins, exits remove by identity. Tween specs:
incoming hover 15 ms, focus 45 ms, drag 45 ms; outgoing drag 150 ms, others 15 ms —
all linear. (The M3E-era focus *ring* animates with motionScheme springs instead —
see the ripple config section above; these tweens govern the classic opacity layers.)

## Surface and Divider primitives

`Surface.kt` separates tonal elevation from shadow elevation: nested surface color resolution
uses accumulated tonal elevation, while drawing elevation stays independent. `Divider.kt` exposes
`HorizontalDivider` and `VerticalDivider`, both defaulting to `DividerTokens.Thickness` and
`DividerTokens.Color`; the pinned token file is `tokens/DividerTokens.kt` (VERSION `v0_117`).
`Dp.Hairline` is a special stroke-width input, not a 1dp layout measurement: it reserves zero
cross-axis layout size and paints one physical pixel centred on the canvas boundary
(`Divider.kt`, `DividerTest.kt`, and `DividerScreenshotTest.kt`).

Avalonia adapts the horizontal/vertical composables through `MaterialDivider.Orientation` and
exposes the special case explicitly through `IsHairline`. This preserves the zero-layout
contract; `Thickness=0` is intentionally not overloaded to mean hairline and remains no line.
The alpha22 executable reference can exercise the default, custom-thickness, orientation, and
hairline paths, so those rows have normal Compose parity evidence rather than a version-gap claim.

## List items, segmentation, and interaction modes

`<m3>ListItem.kt` at the implementation-source baseline contains four standard and four
segmented overloads: no interaction, an action click, radio-like single selection, and
checkbox-like multi-selection (`ListItem.kt:262-544` and `552-847`). Each has the same five
content slots: leading, trailing, overline, supporting, and headline/content. The Avalonia API
therefore adapts those overloads through one explicit `MaterialListItem.InteractionMode`
(`None`, `Click`, `SingleSelection`, `MultiSelection`) rather than exposing an unrelated control
for each Kotlin function. This is an intentional XAML API decision, not an omitted overload.

Every interactive Compose overload also accepts optional `onLongClick` and `onLongClickLabel`.
The same `InteractionMode` decision now governs Avalonia's optional `LongClick` routed event,
`LongClickCommand`/`LongClickCommandParameter`, and `LongClickLabel`: `Click`,
`SingleSelection`, and `MultiSelection` support the secondary action, while `None` keeps no
gesture path. Avalonia maps this to `Holding` (including deliberate mouse holding support) and
uses the label as fallback automation help text because UI Automation has no secondary-invoke
pattern. A successful hold consumes the following primary click, matching `combinedClickable`.

The shared `InteractiveListItem` body (`ListItem.kt:1450-1669`) is the behavioral reference:

- it collects pressed, focused, hovered, and dragged interactions; `dragged` selects reorder
  colors, shape, state layer, and elevation;
- its `updateTransition(InteractiveListColorState)` animates all resolved colors with
  `MotionSchemeKeyTokens.DefaultEffects`, while shape and dragged elevation use
  `FastSpatial` (`ListItem.kt:1492-1588`);
- `ListItemDefaults.ListItemColors` resolves disabled before dragged before selected, but
  `ListItemShapes.shapeForInteraction` and the dragged elevation are not enabled-gated:
  `ListTokens` selects `CornerLarge` (16dp) and `ElevationTokens.Level4` (8dp) while the row
  has a drag interaction (`ListItemDefaults.kt:750-835`, `ListTokens.kt:48-49`);
- `InteractiveListItem` lifts every positive animated dragged elevation with `zIndex(1f)`, leaves
  the outer shadow unclipped, and clips row content to the animated shape. Its ripple path uses
  `color.copy(alpha = animatedAlpha)`, so a disabled dragged state layer replaces alpha with the
  drag opacity instead of multiplying the 38% disabled content alpha (`ListItem.kt:1563-1587`,
  `material3-ripple/Ripple.kt:377-388`);
- the alpha25 height fix maps one/two/three text lines to `56/72/88dp`; the interactive measure
  policy uses the text baseline test for multiline supporting text and aligns the remaining inner
  content at the top above its 60dp breakpoint (`ListItem.kt:1650-1990`);
- `ListItemDefaults.ContentPadding` resolves to logical `16dp` start/end, `10dp` top/bottom, and
  `ListTokens.ItemBetweenSpace` supplies the conditional `12dp` side-slot gap
  (`ListItemDefaults.kt:40-64`, `ListTokens.kt`, VERSION `29.0.0`).

The desktop executable reference has an important limitation here: its alpha22 Compose
Multiplatform implementation cannot execute alpha25's modern no-interaction overload or its
expressive line-height flag. It can execute the older click/selection overloads and inject the
same external `DragInteraction`, including disabled drag; use those cases as alpha22 conformance
evidence only. Record the alpha25 height behavior as **parity unavailable** and prove it with
source review plus deterministic Avalonia layout and motion tests.

The pinned alpha22 interactive overloads also accept the long-click parameters, but a static
reference image has no observable post-invocation state. Long-click behavior is therefore covered
by source review and deterministic Avalonia pointer/command/automation tests rather than claimed
as a visual-parity result.

`SegmentedListItem` is not just a differently colored ordinary row. It requires caller-supplied
`ListItemShapes`; `ListItemDefaults.segmentedShapes(index, count)` rounds only the outer group
corners, `segmentedColors()` begins from `ItemSegmentedContainerColor`, and the group gap is 2dp
(`ListItemDefaults.kt:246-399,438-536`, `ListTokens.kt:87,143`). `MaterialSegmentedList` is the
explicit Avalonia/XAML adaptation: it accepts only direct `MaterialListItem` children, infers
single/first/middle/last from visible rows, applies the tokenised segmented colour and
physical-pixel-rounded 2dp gap, and clears an internal position when a row leaves the group.
Position is not a public row variant, so group indices and layout cannot drift. Each child keeps
the same `InteractionMode`, long-click contract, and application-owned selection; the container
adds only a List automation role. The pinned alpha22 executable contains the same segmented
overloads/defaults, so focused light/dark scenes provide normal alpha22 conformance evidence.

## Press shape morphing (buttons et al.)

- `ButtonShapes(shape, pressedShape)` — 2 slots; `ToggleButtonShapes` adds
  `checkedShape` (3 slots). Selector `shapeByInteraction` (`Button.kt:1947`) picks the
  target and wraps it in `rememberAnimatedShape`; non-corner-based shapes snap.
- `AnimatedShapeState` (`internal\AnimatedShape.kt`) is the whole state machine
  (184 lines): single `Animatable(progress)`, memoized lerped shape, and two gotchas
  to port faithfully:
  1. **Velocity-preserving reversal** — release mid-press flips progress
     (`snapTo(1-p)`) and negates velocity.
  2. **Lerp-nesting guard** — retargeting mid-flight freezes the current interpolated
     shape as the new start, with exact 0/1 boundary checks to avoid recursively
     nested lerp shapes (upstream comment cites StackOverflowError).
- `CornerSize` lerp is **deferred**: it wraps both operands and interpolates inside
  `toPx(size, density)` — this is what makes `CircleShape (50%) ↔ 8dp` correct at any
  size (`foundation\shape\RoundedCornerShape.kt:121`).
- **Spec choice is deliberate:** plain Button/IconButton press-morph uses
  `DefaultEffects` ("intentional… to prevent any bounce"); **ToggleButton uses
  `FastSpatial`** and does bounce. Canonical default morph: `CircleShape → 8dp`
  (CornerFull → CornerSmall), spring 1.0/1600.
- Shape scale (`ShapeTokens.kt`): XS 4 · S 8 · M 12 · L 16 · L-increased 20 ·
  XL 28 · XL-increased 32 · XXL 48 · Full = circle.

## Button anatomy & sizing idioms

- Five variants = five top-level composables (Filled/Elevated/Tonal/Outlined/Text),
  each with a static-shape and a morphing (`shapes: ButtonShapes`) overload; bodies
  are identical inlined `Surface → Row` structures differing only in defaults.
- **Sizes are not an enum**: caller sets height, and `ButtonDefaults` resolvers map
  `Dp → x` (`shapesFor`, `contentPaddingFor(height, hasStartIcon, hasEndIcon)`,
  `iconSizeFor`, `iconSpacingFor`, `textStyleFor`: 32/40dp → `LabelLarge`, 56dp →
  `TitleMedium`, 96dp → `HeadlineSmall`, 136dp → `HeadlineLarge`). But **IconButton uses named
  factories** + `IconButtonWidthOption(Narrow/Uniform/Wide)` — two idioms coexist
  upstream. The Avalonia API adapts both through named size and width properties while retaining
  upstream's resolved metrics.
- `PrecisionPointer.kt`: mouse-vs-touch density switch (small button 36dp with
  precision pointers vs 40dp touch) — very relevant for desktop Avalonia.
- `ButtonColors/ButtonElevation/ButtonShapes`: public `@Immutable` classes with
  internal constructors + `copy()`; defaults cached on the theme (see caching above).

## graphics-shapes usage map

Only 4 files in material3 import `androidx.graphics.shapes`:

| File | Role |
|---|---|
| `internal\ShapeUtil.kt` | Polygon/Morph → Path bridge (`toPath(startAngle=270, repeatPath, closePath)`, matrix transform). Port first. |
| `MaterialShapes.kt` | 35-shape catalog (lazy + `normalized()`), `customPolygon`/`doRepeat(mirroring)` generators, `toShape()` (scale-to-size + recenter, cached workPath). |
| `LoadingIndicator.kt` | Morph *sequences*: indeterminate cycles SoftBurst→Cookie9→Pentagon→Pill→Sunny→Cookie4→Oval (circular), hand-rolled spring 0.6/200 w/ visibilityThreshold 0.1, 650 ms morph interval, +90° per morph, global linear rotation 4666 ms; `calculateScaleFactor` uses bounds vs maxBounds so rotation never clips. |
| `internal\CircularWavyProgressModifiers.kt` | Wave amplitude = circle↔star morph, allocated only when `0 < amplitude < 1`. |

Implication: our MaterialShapes port (D:\Shirasagi\Avalonia\MaterialShapes) is needed
for LoadingIndicator, wavy progress, and the public catalog — **not** for button/FAB
press morphs.

## Desktop surface hints

`src\commonStubsMain\` lists what upstream itself couldn't make common — the
components needing real per-platform work in Avalonia too: AlertDialog, DatePickerDialog,
ExposedDropdownMenu, ModalBottomSheet, SearchBar, TimePicker, menus, WideNavigationRail,
PrecisionPointer. `DynamicTonalPalette.android.kt` (Material You wallpaper color) is
Android-only — our equivalent is the existing `TonalSpotScheme{DynamicResource SystemAccentColor}`.

## Implications for the Avalonia port

- **Theming:** the single-theme-property design is confirmed by the upstream refactor. Preserve
  `MaterialExpressiveTheme` outer-vs-nested default semantics, the theme-instance cache bag, and
  the upstream state-layer opacities.
- **Token pipeline:** retain the variant/size split, per-file version stamps, and explicit marking
  of upstream-hard-coded or provisional values.
- **Motion and interaction:** springs need damping/stiffness parameters, initial velocity,
  mid-flight retargeting, velocity-preserving reversal, and visibility thresholds. AnimatedShape
  and deferred CornerSize interpolation remain the behavioral reference.
- **Ripple:** the classic engine mechanics are captured above; obtain
  `compose/material3/material3-ripple` when auditing the M3E wrapper-node configuration and inset
  focus-ring behavior.
- **API adaptation:** Compose size resolvers and separate variant composables are adapted to named
  Avalonia size and variant properties where that produces a coherent XAML API.

## Pinned paths

```
D:\source\androidx\libraryversions.toml
<m3>MaterialTheme.kt          <m3>ColorScheme.kt        <m3>Shapes.kt
<m3>Typography.kt             <m3>MotionScheme.kt       <m3>MaterialShapes.kt
<m3>Button.kt                 <m3>ToggleButton.kt       <m3>IconButtonDefaults.kt
<m3>Ripple.kt                 <m3>Surface.kt            <m3>PrecisionPointer.kt
<m3>LoadingIndicator.kt       <m3>ButtonGroup.kt        <m3>SplitButton.kt
<m3>internal\AnimatedShape.kt <m3>internal\ShapeUtil.kt
<m3>internal\MappedInteractionSource.kt
<m3>tokens\  (120 generated files; see stamps above)
D:\source\androidx\compose\foundation\foundation\src\commonMain\kotlin\androidx\compose\foundation\shape\RoundedCornerShape.kt
D:\source\androidx\graphics\graphics-shapes\src\commonMain\kotlin\androidx\graphics\shapes\
```
