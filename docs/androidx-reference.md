# AndroidX Material3 — reference notes for the Avalonia port

> Compiled 2026-07-26 from a survey of the local androidx checkout.
> Checkout: `D:\source\androidx` @ `ff0a0a99` (androidx-main, 2026-07-26).
> Library version under development: **compose-material3 `1.5.0-alpha25`** — this is our
> pinned parity referent. Common source root (abbreviated `<m3>` below):
> `D:\source\androidx\compose\material3\material3\src\commonMain\kotlin\androidx\compose\material3\`
>
> Note the checkout is partial: `compose/{animation,foundation,material3,ui}`,
> `compose/material/material-ripple` (added 2026-07-26 — the classic drawn-ripple
> engine, surveyed below), and `graphics/graphics-shapes` are present.
> Still absent: `compose/material3/material3-ripple` — the M3E node
> (`androidx.compose.material3.ripple` package: `RippleNodeConfiguration`,
> `createRippleModifierNode`, inset-focus-ring drawing) that material3's
> `Ripple.kt` delegates to. Add that path to the sparse checkout before building
> our StateLayer/ripple in Phase 3.

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
  `iconSizeFor`, `iconSpacingFor`, `textStyleFor` — label sizes: labelLarge →
  titleMedium → headlineSmall → headlineLarge). But **IconButton uses named
  factories** + `IconButtonWidthOption(Narrow/Uniform/Wide)` — two idioms coexist
  upstream; we must pick one for the C# API (decide in Phase 4).
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

## Implications mapped to our phases

- **Phase 1:** single-theme-property design confirmed by upstream refactor. Mirror
  `MaterialExpressiveTheme` outer-vs-nested default semantics. Add the theme-instance
  cache bag. Fix state-layer opacities (pressed 0.10).
- **Phase 2:** token tables get variant/size file split + per-file version stamps +
  `provisional` flag for upstream-hard-coded values.
- **Phase 3:** spring animator must support: damping/stiffness params, initial
  velocity, mid-flight retarget, velocity-preserving reversal, visibility threshold.
  AnimatedShape state machine + deferred CornerSize lerp are the port targets.
  Ripple engine mechanics are captured above (CommonRippleNode + RippleAnimation);
  fetch `compose/material3/material3-ripple` for the M3E wrapper node (config
  plumbing + inset focus ring drawing) before finalizing the StateLayer design.
- **Phase 4:** decide size-selection idiom (Dp-lookup vs named factories); button
  variants likely separate ControlThemes over one control class or variant enum —
  settle with real code.

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
