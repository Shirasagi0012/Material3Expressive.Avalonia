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

### Phase 2 — Token pipeline (codegen)

- Token-table format (JSON) mirroring the M3 token DB / Compose's generated
  `*Tokens.kt` files: system tokens + per-component tables mapping component
  tokens → system-token references or literals.
- Roslyn incremental generator emitting: role enums, component token classes
  (StyledProperties with role-reference defaults), resolver code. Single source
  of truth — deletes today's triplicated defaults (ctor / resolver switch /
  markup fallback).
- Regenerate `ButtonTokens` from a table as the proof; delete hand-written v2.
- **Exit criteria:** adding a component token domain = writing one table entry,
  no hand-written plumbing.

### Phase 3 — Motion & interaction foundations

- **Springs:** physics-based (no fixed duration) — Avalonia transitions can't
  express this. Build a spring animator on the render clock implementing
  Compose `SpringSpec` semantics (stiffness / damping ratio, initial velocity,
  mid-flight retargeting, velocity-preserving reversal, visibility threshold),
  plus the `MotionScheme` token set (spatial vs. effects × default/fast/slow —
  constants captured in docs/androidx-reference.md).
- **Interaction primitives:** `StateLayer` (hover 0.08 / focus 0.10 /
  pressed 0.10 / dragged 0.16), ripple (engine mechanics + timing constants
  captured in docs/androidx-reference.md from `compose/material/material-ripple`;
  the M3E wrapper node lives in `compose/material3/material3-ripple`, not yet in
  the sparse checkout), focus indicator (Expressive inset ring drawn outside the
  container fill), and a base control class wiring interaction state → animated
  token values.
- **Press shape morphing is NOT polygon morphing** (upstream finding): buttons
  animate `CornerBasedShape` corner sizes with a spring via a small state
  machine (`internal/AnimatedShape.kt`) + deferred CornerSize lerp. Port that —
  it's ~200 lines and covers Button/IconButton/FAB/Chip/Menu press morphs.
- **Polygon morphing (graphics-shapes): already ported** at
  `D:\Shirasagi\Avalonia\MaterialShapes` — full 35-shape catalog, Morph
  pipeline, 73 mirrored tests. Needed only for LoadingIndicator, wavy progress,
  and the public shape catalog. Before shipping on it: fix the
  FeatureMapping.cs concave-corner sentinel bug (task chip filed) and the
  per-frame geometry allocation in MorphShapeView.
- **Exit criteria:** a demo page showing spring-driven state-layer + shape
  transitions matching M3E timing on a prototype button.

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
4. Spike the token-table JSON format against Button's real token set.
5. Loose end from Phase 1: `Foundation/Typography/Tokens/TypeScaleTokens.cs`
   predates the androidx survey and carries no token-DB stamp. Diff it against
   `tokens/TypeScaleTokens.kt` and stamp it — every other token file in the repo
   now records its upstream source and version.
