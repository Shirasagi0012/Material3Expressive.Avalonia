# Material3Expressive.Avalonia — Roadmap

> Drafted 2026-07-26. A living document — revise phase boundaries as reality intrudes.

## Vision

A Material 3 Expressive component library for Avalonia that tracks the official
AndroidX Compose implementation in three dimensions:

1. **Visual fidelity** — tokens, color, typography, shape, elevation match the M3E spec.
2. **Behavioral fidelity** — state layers, springs, shape morphing, interaction states.
3. **API fidelity** — the theming surface reads like `MaterialTheme(colorScheme, typography, shapes, motionScheme)`, adapted to Avalonia idiom where XAML forces it.

Strong typing throughout: no string-keyed resource lookup on any library-internal path.

## Strategic decisions (2026-07-26)

| Decision | Choice |
|---|---|
| Component strategy | **Custom TemplatedControls** reading tokens in C# from an inherited theme property. Stock-control restyles only where trivially cheap (TextBlock typography). |
| Motion scope | **Motion/interaction primitives built early** (right after the theming core), before scaling component count. |
| Token pipeline | **Source generator now**, driven by token tables; Button is the proving ground. |
| DesignTokens library | **Retired.** Proven ideas (typed hosts, theme-variant resolution chain) fold into this repo's concrete implementation. Archive the repo with a pointer here. |

## Target architecture (theming core v3)

Replaces both prior generations (the generic `DesignTokens` runtime and the
hand-rolled per-domain resolvers in `Foundation/`).

- **One inherited attached property** carries the whole `MaterialTheme`
  (ColorScheme, TypographyScheme, ShapeScheme, MotionScheme, component-token
  overrides). Scoping a subtree = setting one property. No per-domain host
  properties, no `[local, app]` MultiBinding fallback — the theme is injected at
  each `TopLevel` so plain property inheritance suffices.
- **Components read tokens in code**, Compose-style: subscribe to the inherited
  theme property, re-resolve on theme/variant change. This structurally fixes
  the ControlTheme-setter scoping hole (shared parse-time bindings can't do
  per-target resolution; a control reading its own inherited value can).
- **Component tokens reference system tokens.** `ButtonTokens.ContainerColor`
  defaults to `ColorRole.Primary` (an enum reference resolved against the active
  scheme + variant), not a literal brush. Literal values appear only as user
  overrides. Dark mode and dynamic color then work with zero app wiring.
- **Markup extensions are app-author sugar only** (`{m3:SysColor Primary}`,
  `{m3:TypeStyle TitleLarge}`), reimplemented on the new core. Never used on
  library-internal paths.
- **Precedence:** control-local override → nearest subtree theme → TopLevel/app
  theme → built-in defaults.

## Phases

### Phase 0 — Stabilize the base — **done 2026-07-26** (one item outstanding)

- ~~Fix `MaterialColorUtilities.Avalonia` WIP breakage
  (`ColorTokenResolver.cs` has an incomplete switch arm for `RefPaletteToken.Custom`);
  finish or cut custom-color support for now. Tests green.~~
  **Finished** custom key colors rather than cutting them — see below.
- ~~CI: build + test on push (GitHub Actions), both this repo and the submodule.~~
  Added to all three repos (`.github/workflows/ci.yml`), ubuntu + windows matrix.
- ~~Remove the `DesignTokens.Avalonia` package reference as part of Phase 1.~~
  **Pulled forward into Phase 0**: the package existed only on a local disk feed, so
  CI could not restore it. The five generic types actually used are now vendored
  `internal` under `Tokens/Infrastructure/`; theming core v3 deletes them.
- Archive the DesignTokens repo (README pointing here). **Still outstanding** —
  needs a GitHub settings change.
- ~~Pin the Avalonia version~~ (**12.1.0**, see below); parity referent is pinned:
  **compose-material3 `1.5.0-alpha25`** (androidx @ `ff0a0a99`, checked out at
  `D:\source\androidx`).
  Survey notes: [docs/androidx-reference.md](docs/androidx-reference.md).
  Note: Expressive APIs are stable upstream — we treat Expressive as the default
  behavior, not a variant.

#### Custom key colors (delivered)

`CustomColor` (name + seed color + harmonize flag) is the XAML content property of
`ColorScheme`, so key colors are declared inline:

```xml
<mcu:TonalSpotScheme Color="#6750A4">
    <mcu:CustomColor Name="Brand" Color="#FF5722" />
</mcu:TonalSpotScheme>
```

Each produces a tonal palette — optionally harmonized toward the scheme's source
color — reachable as `{MdRefPalette Custom, Tone=60, CustomKey=Brand}` and as the
four `SysColorToken.Custom*` roles at the M3 spec tones (light 40/100/90/10, dark
80/20/30/90; fixed, i.e. not contrast-responsive). An unknown key resolves to the
binding's fallback rather than throwing.

#### Pinned versions

| What | Pin | Where |
|---|---|---|
| Avalonia | **12.1.0** | `AvaloniaVersion` in all three `Directory.Packages.props` |
| .NET | `net10.0` (MaterialColorUtilities also targets `net8.0`) | per-project `TargetFramework(s)` |
| compose-material3 | **1.5.0-alpha25** | androidx @ `ff0a0a99` |

Avalonia 12 migration notes, for anything still on 11.x: `Avalonia.Data.IBinding`
is gone (use `BindingBase`, which is also what `this[!Property]` now returns);
`ConstructorArgumentAttribute` moved to `Avalonia.Metadata`; `BindingPlugins` is
internal and DataAnnotations validation became opt-in via
`AppBuilder.WithDataAnnotationsValidation()`; DevTools moved from
`Avalonia.Diagnostics` to `AvaloniaUI.DiagnosticsSupport`.

### Phase 1 — Theming core v3 — **done 2026-07-26**

Spec: [docs/architecture.md](docs/architecture.md). Delivered:

- ~~Single-theme-property model; color system's Avalonia binding layer ported onto it.~~
  `MaterialThemeScope.Theme` (authoring) → immutable `ResolvedTheme` snapshot →
  inherited `ResolvedTheme` property. `MaterialColorUtilities.Avalonia` gained
  the same split (`MaterialColor.Scheme` → `ResolvedColorScheme`) and stays
  standalone-usable; the vendored `Tokens/Infrastructure` generics are gone.
- ~~Shape scheme.~~ `CornerSize` is dp-or-percent and resolves against the shape's
  size, so `CornerFull` is genuinely 50% and Phase 3's deferred lerp drops in
  unchanged. 8 configurable slots + 15 roles, per upstream `Shapes`/`ShapeKeyTokens`.
- ~~Typography rides the theme object.~~ `ResolvedTypographyScheme`; `TypographyRole`
  and the attribute-selector TextBlock theme are unchanged.
- **MotionScheme** shipped as data (6 springs, expressive by default); no animator.
- **Component tokens are role references.** `ButtonTokens.ContainerColor` defaults to
  `SysColorToken.Primary`, not a brush — the demo's whole `ButtonTokens` override
  block deleted itself. `ColorTokenValue` is the role-or-literal union.
- **Per-theme memo bag** on the snapshot (`GetButtonColors(isDark)`), mirroring
  upstream's cached default bundles.
- `MaterialControl` reads tokens in C# off the inherited snapshot; markup
  extensions rebuilt as `.Self()`-rooted bindings so they resolve per target.
- **Exit criteria met** — `tests/Material3Expressive.Tests`, 32 tests:
  (a) subtree theme override reaches both a ControlTheme-styled `Button` and a
  C#-reading control, (b) light/dark switch re-resolves live, (c) app-level theme
  replacement re-resolves live. Plus: live scheme mutation, subtree clearing,
  and popup inheritance.

Findings worth carrying forward:

- `BindingBase.CreateInstance` is `internal abstract` in Avalonia 12 — custom
  binding types are impossible; `CompiledBinding.Create(expr)` builds an *unrooted*
  path (resolves against `DataContext`), so per-target resolution must use
  `CompiledBindingPathBuilder().Self()`.
- **Popups inherit subtree themes for free**: `Popup` sets the popup root's logical
  parent to itself, and that sets `InheritanceParent`. No special handling needed.
- `Application` is outside every tree, so the app theme is injected at each
  `TopLevel` by a style in `MaterialThemeStyles`, at `Style` priority.

### Phase 2 — Token pipeline (codegen) — **done 2026-07-26**

Format reference: [docs/token-tables.md](docs/token-tables.md). Delivered:

- ~~Token-table format (JSON) mirroring the M3 token DB.~~ Two kinds:
  `tokenSet` (a verbatim transcription of one upstream `*Tokens.kt`, stamped with
  its token-DB version) and `component` (a token surface plus one value set per
  variant, referencing transcriptions with `@Name.Member`). Sets compose with
  `extends`, so a variant states only what it changes.
- ~~Roslyn incremental generator.~~ `Material3Expressive.TokenGenerator`, with its
  own JSON reader — an analyzer that takes a dependency has to ship it, and the
  tables are read once per build. Comments and trailing commas are accepted
  because every deviation from upstream is annotated in place.
- ~~Regenerate `ButtonTokens` from a table.~~ Replaced by `ButtonColorTokens`
  (5 variants) and `ButtonSizeTokens` (5 sizes) — upstream's own split, and the
  full Phase 4 matrix rather than just the filled/small pair v2 had.
- **The type scale moved into a table too**, closing the Phase 1 loose end. It
  had 30 defaults in a constructor, 30 styled properties, a 30-arm switch, and
  the face/weight of every slot restated in `Typography.axaml`. It is now one
  table, and the slots carry their own `Font`/`Weight` — which fixed a real bug:
  the emphasized styles were rendering at the base weight.
- **Exit criteria met** — a component domain is one table file. The snapshot
  accessors (`ResolvedTheme.GetButtonColors(variant, isDark)`), the sparse
  override bags (`ButtonColorOverrides`) and the theme plumbing are all emitted
  from the set of tables; `ResolvedTheme` never has to be edited.

Findings worth carrying forward:

- **One source generator cannot see another's output.** `TypographyRole` moved
  into a table, so `PropertyGenerator.Avalonia` could no longer resolve it in
  `[GenerateAttachedProperty<…>(DefaultValue = TypographyRole.BodyMedium)]`.
  `TypographyScheme.TextStyle` is written out by hand for that reason; any
  attached property whose type comes from a table must be.
- Component token overrides ride the theme as its content
  (`MaterialTheme.Components`), keyed by variant. Sparseness comes from
  `AvaloniaObject.IsSet`, so a bag replaces only what it names.

### Phase 3 — Motion & interaction foundations — **done 2026-07-26**

- ~~**Springs:** a spring animator on the render clock with Compose `SpringSpec`
  semantics.~~ `SpringSimulation` and `SpringEstimation` are ports of
  `animation-core`. The solver is analytic rather than integrated, so a dropped
  frame changes nothing and 60Hz and 144Hz produce the same trajectory — both
  are tested. Settling is decided by the estimated duration, not by watching for
  a small value, so an overshoot passing through the target is not mistaken for
  arrival.
- ~~**Interaction primitives.**~~ `StateLayer` encodes what a pile of style
  triggers cannot: interactions stack but only the topmost shows, and the fades
  are asymmetric (focus in 45ms / out 15ms, drag in 45ms / out 150ms).
  `RippleAnimation` includes the fast-tap rule — a press released before the
  fade-in finishes jumps to full alpha rather than fading from where it was.
- ~~**Press shape morphing.**~~ `AnimatedShape` ports
  `internal/AnimatedShapeState.kt`, including the interruption cases: returning
  to the shape being left flips progress and negates velocity; a new target
  freezes what is on screen. The freeze avoids nesting a lerp inside the last
  one, which upstream notes would overflow the stack under repeated toggling.
- **Not built: a separate focus ring.** The roadmap called for the spec's
  "inset ring drawn outside the container fill", but compose-material3
  `1.5.0-alpha25` has no such thing for buttons — focus is the state layer at
  `FocusStateLayerOpacity`, and the pinned referent wins over the spec page.
- **Polygon morphing (graphics-shapes): already ported** at
  `modules/MaterialShapes` — full 35-shape catalog, Morph pipeline, 80 tests,
  and the `ShapeUtil.toPath` bridge. Needed only for LoadingIndicator, wavy
  progress, and the public shape catalog; it joins the build in Wave 2.
- **Exit criteria met** — the gallery's Buttons page shows spring-driven state
  layer, ripple and shape morph, and `SquaringItOff_MorphsRatherThanSnapping`
  drives the whole stack through the headless render clock.

Findings worth carrying forward:

- `TopLevel.RequestAnimationFrame` is single-shot, which is exactly right: the
  loop exists only while something moves, so an idle control costs nothing.
- The headless render timer reports a **real** stopwatch, so a test that forces
  ticks in a tight loop replays the same instant. Frames have to be spaced in
  real time, and each tick pumped through the dispatcher.
- Material components are hosted, not inherited: `MaterialThemeWatcher` and
  `MaterialInteractionHost` attach to any `Control`, so `MaterialButton` can
  derive from Avalonia's `Button` and keep content, commands, click modes and
  keyboard activation rather than reimplementing them.

### Phase 4 — First component wave (prove the stack end-to-end)

Token table → generated tokens → templated control → motion → gallery page,
for each of:

- **Button** — 5 color variants (filled, tonal, outlined, elevated, text),
  5 sizes (XS–XL), shape morph on press, toggle variant.
- **IconButton** (+ toggle), **FAB** + Extended FAB, **Card** (3 variants).
- TextBlock typography restyle ships as the one stock-control package.
- Demo app graduates into a proper gallery (navigation, light/dark toggle,
  seed-color picker, per-page theme scoping demo).
- Settle the API-idiom question here with real code: one control + `Variant`
  enum vs. separate classes per variant (Compose uses separate composables;
  pick what's idiomatic in XAML and document the rule).
- **Exit criteria:** v0.1.0-alpha on NuGet + gallery screenshots vs. Compose
  side-by-side.

### Phase 5 — Coverage waves

Grouped by shared primitives, roughly in order:

| Wave | Components |
|---|---|
| 2 | Checkbox, Switch, RadioButton, Slider (new track shape), ProgressIndicator (incl. wavy), LoadingIndicator (shape morph) |
| 3 | TextField (filled/outlined — largest single component), Chips, ButtonGroup (standard + connected), SplitButton, FAB menu |
| 4 | NavigationBar (flexible), NavigationRail, Drawer, App bars / Toolbars (incl. floating), Tabs |
| 5 | Dialog, Bottom sheet, Menu, Snackbar, Tooltip, Search, Badge, Date/Time pickers, Carousel |

Each wave: token tables first, then controls, then motion polish, then gallery.

## Cross-cutting

- **Testing:** Avalonia.Headless for token resolution, scoping, and interaction
  states from Phase 1 onward; visual regression (gallery screenshot diffing)
  once Phase 4 lands.
- **Packaging:** `Material3Expressive` (controls + theming),
  `MaterialColorUtilities` stays independently useful on NuGet; shapes port as
  its own package.
- **Material Symbols icon package (parallel track, separate repo):** variable
  Material Symbols rendered via SkiaSharp 4 (`ICustomDrawOperation` +
  `ISkiaSharpApiLeaseFeature` — path verified). One repo, one package per family
  (Outlined/Rounded/Sharp); `Symbol` enum source-generated from Google's
  `.codepoints` file; `Fill`/`Weight`/`Grade`/`OpticalSize` as animatable
  StyledProperties (opsz auto-tracks size 20–48; grade −25 in dark theme).
  Requires consumers to override SkiaSharp 3.119 → 4.x — pin an exact tested
  version, CI with the override applied + headless render test. **Publish to
  NuGet.org early** (avoids the local-feed CI restore trap DesignTokens hit).
  Core library must never depend on it — components take icon *content*;
  only demo/gallery references the package. Wanted by Phase 4 (icon slots).
- **Docs:** per-component page in the gallery + a parity table vs. Compose
  (what matches, what deviates and why).

## Known risks / spikes

1. **Spring animator** — no prior art in Avalonia core; spike early in Phase 3.
2. **Variable fonts** — M3E leans on Roboto Flex axes; Avalonia's variable-font
   axis support is limited. Spike during Phase 1 typography work; may constrain
   emphasized styles to static weights.
3. **Text metrics** — line-height/letter-spacing semantics differ from Android;
   accept documented deviation rather than fight the text stack.
4. **M3E is a moving target** — track a pinned compose-material3 release;
   upgrade deliberately, not continuously.

## Immediate next steps

1. ~~Phase 0: fix the submodule build break, wire up CI.~~ Done. Remaining Phase 0
   loose end: archive the DesignTokens repo on GitHub with a README pointing here.
2. ~~Write `docs/architecture.md` for theming core v3.~~ Done.
3. ~~Implement the core + the three exit-criteria tests.~~ Done — Phase 1 complete.
4. ~~Spike the token-table JSON format against Button's real token set.~~ Done —
   Phase 2 complete, tables cover Button's full variant × size matrix.
5. ~~Loose end from Phase 1: `TypeScaleTokens.cs` carries no token-DB stamp.~~
   Done — it is a token table now, stamped `v0_103`, and the emphasized slots
   gained the face and weight they were missing.
6. Phase 3: spike the spring animator against `MotionScheme`'s six specs.
