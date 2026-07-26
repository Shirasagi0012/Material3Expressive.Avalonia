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

### Phase 0 — Stabilize the base

- Fix `MaterialColorUtilities.Avalonia` WIP breakage
  (`ColorTokenResolver.cs` has an incomplete switch arm for `RefPaletteToken.Custom`);
  finish or cut custom-color support for now. Tests green.
- CI: build + test on push (GitHub Actions), both this repo and the submodule.
- Archive the DesignTokens repo (README pointing here); remove the
  `DesignTokens.Avalonia` package reference as part of Phase 1.
- Pin the Avalonia version; pin the compose-material3 version being tracked
  (e.g. 1.4.x) so "parity" has a concrete referent.

### Phase 1 — Theming core v3

- Implement the single-theme-property model described above; port the color
  system's Avalonia binding layer onto it (`MaterialColorUtilities` core — HCT,
  DynamicScheme, variants — is untouched; only the host/resolver layer changes).
- Shape scheme: corner-radius token scale (extra-small → full) with a shape
  token type designed to admit morphable shapes later.
- Typography rides through the theme object; keep `TypographyRole` +
  attribute-selector TextBlock theme.
- Written spec in `docs/architecture.md`: resolution order, variant handling,
  override semantics.
- **Exit criteria:** headless UI tests proving (a) subtree theme override
  affects a control styled via ControlTheme (the case broken today),
  (b) light/dark switch re-resolves live, (c) app-level theme replacement
  re-resolves live.

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
  Compose `SpringSpec` semantics (stiffness / damping ratio), plus the
  `MotionScheme` token set (spatial vs. effects × default/fast/slow).
- **Interaction primitives:** `StateLayer` (hover/focus/press opacity from
  tokens), ripple (optional), focus indicator ring, and a base control class
  wiring interaction state → animated token values.
- **Shape morphing:** port androidx `graphics-shapes` (RoundedPolygon, Morph)
  as a standalone module (sibling to MaterialColorUtilities) — needed for
  button press morph, loading indicator, FAB menu. Meaty but isolated; can
  proceed in parallel. Fallback if it slips: animated corner radius only.
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
- **Docs:** per-component page in the gallery + a parity table vs. Compose
  (what matches, what deviates and why).

## Known risks / spikes

1. **Spring animator** — no prior art in Avalonia core; spike early in Phase 3.
2. **graphics-shapes port** — nontrivial geometry (cubic features, morph
   matching). Parallel track with a corner-radius-only fallback.
3. **Variable fonts** — M3E leans on Roboto Flex axes; Avalonia's variable-font
   axis support is limited. Spike during Phase 1 typography work; may constrain
   emphasized styles to static weights.
4. **Text metrics** — line-height/letter-spacing semantics differ from Android;
   accept documented deviation rather than fight the text stack.
5. **M3E is a moving target** — track a pinned compose-material3 release;
   upgrade deliberately, not continuously.

## Immediate next steps

1. Phase 0: fix the submodule build break, wire up CI.
2. Write `docs/architecture.md` for theming core v3 (the spec is the review
   feedback from 2026-07-26 distilled).
3. Implement the core + the three exit-criteria tests.
4. Spike the token-table JSON format against Button's real token set.
