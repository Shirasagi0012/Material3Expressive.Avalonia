# Current theming architecture

The current implementation uses mutable authoring objects and immutable resolved snapshots.
`MaterialTheme` and its component override bags are authoring surfaces; a resolved snapshot is
published through inherited `MaterialThemeScope.ResolvedTheme` and consumed by controls and
markup extensions.

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

Token source files live in `src/Material3Expressive/Tokens/*.tokens.json`. The token generator
emits token sets, component variants, resolved accessors, and sparse override bags. New token
domains should be added through one table; do not duplicate constructor defaults, resolver arms,
and markup fallbacks by hand.

## Scope and history

This page is the current short contract. The detailed v3 investigation, including rejected
approaches and early phase labels, is preserved in
[the archived architecture record](../archive/theming-core-v3-architecture.md).
