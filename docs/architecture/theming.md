# Current theming architecture

`MaterialTheme` is the mutable authoring model. `ResolvedTheme` is its immutable, inherited
system snapshot, published through `MaterialThemeScope.ResolvedTheme`. Component defaults do not
live in either object: each control family owns a small, stateless C# Defaults policy.

## Resolution order

For a public Avalonia property, the effective value follows Avalonia precedence:

1. control-local value;
2. application or subtree `Style` / `ControlTheme`;
3. library Material `ControlTheme` binding;
4. component-owned default projection;
5. raw component tokens plus `ResolvedTheme` system schemes.

The library default is therefore a binding in the Material control theme, never a local write to
`Background`, `Foreground`, `Padding`, or another public styling property. A consumer can retune
an ordinary visual value with normal Avalonia styling without needing a token override bag.

## Token pipeline

The generator accepts three JSONC input kinds under `src/Material3Expressive/Tokens/`:

```text
AndroidX *Tokens.kt
    -> .comp.jsonc                raw component provenance
    -> .sys.jsonc                 shared system data
    -> .ref.jsonc                 future reference primitives

.comp.jsonc + ResolvedTheme + component axis/state policy
    -> XxxDefaults semantic records
    -> control, ControlTheme, presenter, or adapter
```

`.comp.jsonc` is intentionally a faithful, internal transcription. It records token facts,
provenance, system-role references, dimensions, opacity and elevation; it does not describe
variant assembly, interaction state, Avalonia bindings, template behavior, or public
customisation. The generator emits raw token sets and the system TypeScale record only. It does
not emit component default records, resolved component brushes, `ResolvedTheme.Components`, or
generic token override types.

`ResolvedTheme` contains only resolved system schemes:

```text
ColorScheme
Typography
Shapes
Motion
Ripple
```

It deliberately has no component tokens, defaults, overrides, light/dark selection, variant,
size, interaction state, bounds, or pointer state. The consuming control supplies those late
inputs to its Defaults policy.

## Component-owned Defaults

A Defaults class maps raw tables and a system snapshot into semantic records, for example:

```csharp
var colors = ButtonDefaults.Colors(theme, variant, isDark);
var metrics = ButtonDefaults.Metrics(size);
```

Defaults are pure policy: they do not subscribe to a theme, hold a control, write an
`AvaloniaProperty`, or manage detach lifecycle. They are also where a Compose correction or an
intentional Avalonia adaptation is documented beside the control that needs it.

An adapter/control owns lifecycle and late state. Native Avalonia controls use internal attached
semantic outputs projected by their Material `ControlTheme`; self-rendering controls can retain a
semantic record or DirectProperty directly. Presenters combine those profiles with interaction,
bounds and RTL to choose the final render value.

## Customisation boundary

Use the smallest ordinary Avalonia mechanism that reaches the desired result:

- a `MaterialTheme` scheme for a brand-wide system change;
- a subtree `Style` or `ControlTheme` for a family appearance;
- a local styled property for one control;
- a pseudo-class style for enabled/disabled/checked state;
- a component-specific semantic property only when standard styling cannot express the required
  presenter contract.

There is no `MaterialTheme.Components`, `ComponentTokenOverrides`, or generic component token
override bag. That omission is deliberate: component axes and state contracts vary too much for a
uniform JSON-shaped API to stay understandable or correctly scoped.

## Scope and history

This page is the current contract. The detailed v3 investigation, including rejected approaches
and early phase labels, is preserved in
[the archived architecture record](../archive/theming-core-v3-architecture.md).
