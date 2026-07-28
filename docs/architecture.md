# Theming core v3 — architecture

> Historical architecture specification, written 2026-07-26 during the first roadmap. Phase
> labels and then-future scope notes are preserved as design history; see
> [roadmap-history.md](roadmap-history.md). The current implementation source baseline and older
> executable parity reference are defined separately in
> [upstream-baselines.md](upstream-baselines.md). The tested Avalonia baseline is `12.1.0`.
> Reference notes on the upstream design live in [androidx-reference.md](androidx-reference.md).

This replaces both prior generations: the generic `DesignTokens` runtime (vendored under
`Tokens/Infrastructure/`) and the hand-rolled per-domain resolvers in `Foundation/`.

## 1. The problem being fixed

The v2 markup extensions resolved their host **at XAML parse time**: an extension walked
`IAvaloniaXamlIlParentStackProvider.Parents`, captured the first `StyledElement`, and built an
observable against it. A `ControlTheme` setter is parsed *once*, so every control styled by that
theme shared one observable bound to one captured parent — usually `Application`. Consequences:

- a subtree theme override never reached a `ControlTheme`-styled control;
- a `[local, app]` `MultiBinding` was needed to fake a fallback chain;
- token defaults were written three times (constructor, resolver switch, markup fallback).

Two Avalonia facts make the fix straightforward, and both were verified against the 12.1 source:

- `Setter` instantiates a binding **per target**: `Value is BindingBase` →
  `binding.CreateInstance(target, Property, null)` (`Avalonia.Base/Styling/Setter.cs:113`).
- A compiled binding path starting with `.Self()` is *rooted*, and with `Source` unset the binding
  source becomes the target itself (`Avalonia.Base/Data/CompiledBinding.cs:207-213`).

So a setter-borne binding **can** do per-target resolution, as long as the binding describes
"read this inherited property off whatever control you are attached to" rather than naming a
source object up front.

Note on API shape: `BindingBase.CreateInstance` is `internal abstract` in Avalonia 12, so a custom
`BindingBase` subclass is not possible. `CompiledBinding.Create<TIn,TOut>(x => ...)` exists and
takes an expression tree, but the path it builds is **unrooted** — with no `source` argument it
resolves against the target's `DataContext`. It is therefore the right tool only where the source
object is known up front (see §6). For per-target resolution use
`CompiledBindingPathBuilder().Self().Property(...)`.

## 2. Layer model

```
authoring (mutable, bindable, XAML)      MaterialTheme, ColorScheme, TypographyScheme,
        │                                ShapeScheme, MotionScheme, component token bags
        │  any change ⇒ rebuild
        ▼
resolved (immutable snapshot)            ResolvedTheme { ResolvedColorScheme, typography,
        │                                shape, motion, component tokens, lazy memo bag }
        │  pushed onto the element that declared the theme
        ▼
distribution (property inheritance)      MaterialThemeScope.ResolvedTheme  (inherits: true)
        │
        ├────────────▶ consumption in C#    MaterialControl reads the inherited value
        └────────────▶ consumption in XAML  markup extensions → .Self() bindings
```

The authoring objects stay mutable `AvaloniaObject`s so XAML keeps working the way app authors
expect — `ColorScheme.Color` can carry `{DynamicResource SystemAccentColor}`, a scheme's seed can
be changed in place, and a `CustomColor` can be added to a live scheme. Every such change raises a
change event; the theme rebuilds its snapshot and re-publishes it. Consumers therefore only ever
see immutable values and can use reference equality to skip work — the same contract Compose gets
from `@Immutable`.

## 3. Properties

Two attached properties, both declared on `MaterialThemeScope`:

| Property | Type | Inherits | Role |
|---|---|---|---|
| `Theme` | `MaterialTheme?` | no | Authoring surface. Set in XAML on `Application`, a `Window`, or any subtree root. |
| `ResolvedTheme` | `ResolvedTheme` | **yes** | What every consumer reads. Never set by app authors. |

A class handler on `Theme` subscribes to the authoring object and writes the snapshot into
`ResolvedTheme` on the *same* element; property inheritance carries it to the subtree. Setting
`Theme` to null clears both.

`MaterialColorUtilities.Avalonia` mirrors this shape one layer down and stays independently
usable: `MaterialColor.Scheme` (authoring, not inherited) → `MaterialColor.ResolvedScheme`
(immutable `ResolvedColorScheme`, inherited). When a `MaterialTheme` is applied, it also publishes
its colour scheme through `MaterialColor.ResolvedScheme` on the same element, so `{MdSysColor}`
works inside an M3E theme scope without MCU knowing that M3E exists.

## 4. Resolution order

For a consumer `C` and token `T`:

1. **Control-local override** — a value set directly on `C` (local value beats everything below).
2. **Nearest subtree theme** — the closest ancestor with `Theme` set.
3. **TopLevel / app theme** — injected at each `TopLevel` (§6), at `Style` priority.
4. **Built-in defaults** — `ResolvedTheme.Default`, the property's default value.

Steps 2 and 3 are the same mechanism at different depths: whichever ancestor is nearest wins,
because that is what property inheritance does. Step 3 sits at `Style` priority precisely so that
a `Theme` set locally on a `Window` (local value) outranks it. Step 4 needs no lookup — it is the
`ResolvedTheme` property's default value, so an app that never declares a theme still renders.

## 5. Theme variants

Colour is the only variant-dependent subsystem. Rules:

- The variant is read from the **consuming element**, via
  `ThemeVariantScope.ActualThemeVariantProperty`. That is the same property instance Avalonia
  inherits onto every `StyledElement` (`StyledProperty.AddOwner` returns `this`), so it can be
  observed per-target like any other.
- `ResolvedColorScheme` precomputes **both** the light and dark `DynamicScheme` when it is built.
  A light/dark switch is then a field read, not a scheme regeneration.
- Custom variants resolve through `ColorScheme.IsDark`, which walks `ThemeVariant.InheritVariant`
  until it reaches Light or Dark.
- A markup extension may pin the variant (`{MdSysColor Primary, Theme=Dark}`); a pinned variant
  drops the `ActualThemeVariant` input from the binding entirely.

Because a colour token depends on *two* per-target inputs — the inherited scheme and the element's
variant — colour bindings are `MultiBinding`s over two `.Self()` bindings. Both re-evaluate live.

## 6. TopLevel injection

`Application` is not part of any visual or logical tree, so a theme declared on it cannot inherit
anywhere. `MaterialStyles` therefore carries one style:

```
Style(x => x.Is<TopLevel>())
    .Setter(MaterialThemeScope.ResolvedThemeProperty, <binding to Application's ResolvedTheme>)
```

The source object here *is* known up front (`Application.Current`), so this binding is built with
`CompiledBinding.Create(app => app[MaterialThemeScope.ResolvedThemeProperty], source: app)` — the
expression-tree API, no string path. Every `TopLevel` — `Window`, `PopupRoot`, embedded roots —
picks the app theme up at `Style` priority as it is styled.

**Popups inherit subtree themes.** `Popup` sets the popup root's logical parent to the `Popup`
itself (`Avalonia.Controls/Primitives/Popup.cs:455`), and `SetParent` assigns `InheritanceParent`
when it is null (`Avalonia.Base/StyledElement.cs:458`). Property inheritance therefore flows from
the popup's placement context into the popup root, and a flyout opened inside a themed subtree
gets that subtree's theme rather than the app's. The `TopLevel` style still applies to
`PopupRoot`, but only at `Style` priority, so the inherited subtree value wins. This is covered by
a test rather than documented as a gap.

## 7. Token values: role references, not literals

A component token holds a **reference to a system token**, not a resolved brush:

```
ButtonTokens.ContainerColor = ColorTokenValue.Role(SysColorToken.Primary)   // default
ButtonTokens.ContainerColor = ColorTokenValue.Literal(Colors.Red)           // user override
```

`ColorTokenValue` is a readonly struct union of *role reference* (a `SysColorToken` plus an
optional custom-colour key) and *literal colour*, with a `TypeConverter` so both `"Primary"` and
`"#FF5722"` parse from XAML. Consequences:

- dark mode and dynamic colour work with zero app wiring — the role resolves against whichever
  scheme and variant are live at the consumer;
- there is exactly one place a default is written (the token bag's initializer), which is what
  Phase 2's generator will emit;
- an override is a value like any other, so subtree precedence applies to it unchanged.

Metrics (`CornerRadius`, `Thickness`, opacities) stay plain values — they have no variant
dimension. Shape tokens reference `ShapeRole`; typography tokens reference `TypographyRole`.

## 8. Caching

`ResolvedTheme` is built once per authoring mutation and holds a **lazy memo bag**: per-component
resolved bundles are computed on first request and stored on the snapshot. This mirrors upstream's
60 colour + 15 shape memo fields (`ColorScheme.kt:637`, `Shapes.kt:243`), which exist for
performance rather than convenience. Because the snapshot is immutable and replaced wholesale, a
stale cache is not representable.

`ResolvedColorScheme` caches resolved `Color`s and immutable brushes per (token, variant) in flat
arrays indexed by enum ordinal — colour resolution is the hot path for every control.

## 9. Rules for library code

1. **No string-keyed resource lookup on any library-internal path.** Tokens are enums; bindings
   name `AvaloniaProperty` objects.
2. **Library-internal XAML never captures a parse-time source.** If a setter or template needs a
   token, the binding must be `.Self()`-rooted so it resolves per target.
3. **Markup extensions are app-author sugar.** They may appear in app XAML and (until Phase 4
   replaces it) in the stock-control `ControlTheme`s; they are never the mechanism by which a
   custom control reads its own tokens — that is C# against the inherited snapshot.
4. **Authoring objects are mutable; everything downstream of them is immutable.** Never hand a
   consumer a reference to an authoring object.
5. **Every subsystem resolves against the consumer**, not against the element that declared the
   theme. Variant, density, and later text-scale are properties of where a control *is*.

## 10. Out of scope for Phase 1

- The spring animator and any animated token (Phase 3). `MotionScheme` ships as data only: the six
  spring specs, no evaluator.
- Polygon/morphable shapes (Phase 3). `CornerSize` is modelled so the deferred lerp upstream uses
  can be added without changing the token type: a percent corner has no value until it is given a
  size, and `CornerSize.Lerp` interpolates *after* both operands resolve, which is the only way
  `CornerFull ↔ 8dp` is correct at every size.
  One caveat on the binding-driven path: a `Setter` has no access to the target's bounds, so
  `{m3:MdButtonToken ContainerCornerRadius}` resolves the shape against the button's own token
  height (40dp ⇒ a 20dp pill). A control that reads its tokens in code resolves against its actual
  bounds. The discrepancy disappears with the ControlTheme in Phase 4.
- Token-table codegen (Phase 2). Token bags are hand-written now; their *shape* is what the
  generator will emit.
- Real M3E controls (Phase 4). Phase 1 ships `MaterialControl`, the base class that reads the
  snapshot and re-resolves, plus the existing stock-control `ControlTheme`s ported onto the new
  core. Its contract: `OnThemeChanged` fires once when the control attaches — including when the
  theme in scope happens to be the default — and again on every theme or variant change, never
  spuriously.
- Control-local token override surface (attached bags vs. control properties) — Phase 4 decides it
  with real components; §4 step 1 today means "a property set directly on the control".
