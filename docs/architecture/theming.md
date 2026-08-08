# Current theming architecture

The current implementation uses mutable authoring objects and immutable resolved snapshots.
`MaterialTheme` and its legacy component override bags are authoring surfaces; a resolved snapshot
is published through inherited `MaterialThemeScope.ResolvedTheme`. The Button family is the first
exception: its component-owned Defaults adapters consume raw tables directly and its templates use
owner-local semantic projections, without markup extensions or component snapshot records.

## Resolution order

For a control and token, resolution proceeds from strongest to weakest:

1. control-local override;
2. nearest subtree theme;
3. application or `TopLevel` theme;
4. built-in resolved defaults.

Consumers resolve against the control that receives the value. Markup extensions use rooted
`.Self()` bindings so a `ControlTheme` does not accidentally capture the application or one
unrelated control during XAML parsing.

## Token pipeline

Token source files are split by ownership under `src/Material3Expressive/Tokens/`: reference tables
use `.ref.jsonc`, shared system tables use `.sys.jsonc`, raw component transcriptions use
`.comp.jsonc`, and legacy component assembly/override tables use `.default.jsonc` under
`Tokens/Defaults/`. The token generator still emits token sets, legacy component variants, internal
resolved accessors, and sparse override bags for unmigrated domains. New domains should keep
upstream values in a raw component table and put runtime assembly or Avalonia corrections in a
component-owned Defaults adapter; do not turn JSON into a behavior language.

The current frozen-baseline coverage is 103 raw component tables, 3 system tables, and 32 legacy
defaults tables. `MaterialColorUtilities` remains the owner of palette and system-colour resolution;
`ShapeScheme`, `MotionScheme`, and `TypographyScheme` remain the owners of their runtime contracts.
Those owners are intentionally not mirrored as JSON reference tables. The explicit exceptions and
the 27 raw-only future component sources are recorded in the
[token coverage inventory](../reference/token-coverage.md).

```mermaid
flowchart LR
    A[AndroidX ff0a0a99\n*Tokens.kt] --> B[.comp.jsonc\nraw component tables]
    A --> C[.sys.jsonc\nElevation / State / TypeScale]
    B --> D[legacy .default.jsonc\nassembly]
    C --> D
    D --> E[TokenTableGenerator]
    E --> F[ResolvedTheme\nlegacy component snapshot]
    F --> G[legacy control adapter]
    B --> J[Button / ToggleButton Defaults\nC# policy]
    C --> J
    J --> H[Avalonia control/template]
    I[MaterialColorUtilities\nShapeScheme / MotionScheme / TypographyScheme] --> F
```

System typography is owned by `TypographyScheme.TypeScaleOverrides`; legacy component defaults stay
in `MaterialTheme.Components`. The old `TypeScaleOverrides` item in `Components` remains only as an
obsolete compatibility path during the alpha migration.

Button and ToggleButton are the first component boundaries being tightened. Their raw
`Tokens/Component/Button/` files remain AndroidX-shaped, while `Controls/Button/` owns independent
`ButtonDefaults` and `ToggleButtonDefaults` adapters. ToggleButton has separate checked, unchecked
and disabled colour contracts plus three shape states; it does not reuse ordinary Button's colour
aggregate. Conditional interaction and Avalonia property binding stay in C#, rather than becoming a
JSON behaviour language.

The same boundary now exists for Switch and Slider. `Controls/Switch/` projects the generated
checked/unchecked/disabled colours into the visual state consumed by `MaterialSwitch`, while
`Controls/Slider/` shares disabled-colour projection between `MaterialSlider` and
`MaterialRangeSlider`. Their JSON tables remain static defaults; endpoint selection, pointer state,
motion and layout policy stay in the controls.

## Scope and history

This page is the current short contract. The detailed v3 investigation, including rejected
approaches and early phase labels, is preserved in
[the archived architecture record](../archive/theming-core-v3-architecture.md).
