# Token tables

Every token in the library is defined once, in a JSON table under
`src/Material3Expressive/Tokens/`, and turned into C# at build time by
`Material3Expressive.TokenGenerator`. Nothing about a token is written by hand twice — that
triplication (constructor default, resolver arm, markup-extension fallback) is what Phase 2
existed to delete.

A table is picked up by its file name: `*.tokens.json`, included as `AdditionalFiles`. A malformed
table is a build error (`M3ETOK001`); two tables claiming the same type name is `M3ETOK002`.

The reader accepts `//` and `/* */` comments and trailing commas, because these files are
maintained by hand and want annotating — every deviation from upstream is a comment next to the
value it explains.

## Two kinds

### `tokenSet` — a verbatim transcription

One table per upstream `*Tokens.kt` object. It becomes a `public static class` of constants in
`Material3Expressive.Foundation.Tokens`, carrying the upstream file and token-DB stamp in its
header so the transcription stays checkable.

```json
{
  "kind": "tokenSet",
  "name": "ButtonSmallTokens",
  "namespace": "Material3Expressive.Foundation.Tokens",
  "version": "v0_11_0",
  "source": "androidx compose/material3 tokens/ButtonSmallTokens.kt",
  "summary": "Metrics for the small button size, which is the default.",

  "tokens": {
    "ContainerHeight": { "type": "dimension", "value": 40 },
    "PressedContainerShape": { "type": "shape", "value": "CornerSmall" }
  }
}
```

### `component` — a value record plus its variants

Declares a token surface once (`properties`) and fills it once per variant (`sets`). This is the
layer that composes transcriptions into something a control consumes.

```json
{
  "kind": "component",
  "name": "ButtonColorTokens",
  "namespace": "Material3Expressive.Foundation",
  "resolvedName": "ButtonColors",
  "variantEnum": "ButtonVariant",

  "properties": {
    "ContainerColor": { "type": "color", "summary": "The container fill." },
    "DisabledContainerColor": { "type": "color", "opacity": "DisabledContainerOpacity" },
    "DisabledContainerOpacity": { "type": "opacity" }
  },

  "sets": [
    { "name": "Filled", "values": { "ContainerColor": "@FilledButtonTokens.ContainerColor", "…": "…" } },
    { "name": "Tonal", "extends": "Filled", "values": { "ContainerColor": "@TonalButtonTokens.ContainerColor" } }
  ]
}
```

A set must give every property a value; `extends` inherits from a set declared above it, so a
variant states only what it changes.

## Values

`"@Name.Member"` references a generated token-set member — that is how a component set points at
the transcription instead of restating its value. Anything else is read according to the
property's type:

| `type` | C# type | Value syntax |
|---|---|---|
| `color` | `ColorTokenValue` | `"Primary"`, `"Custom:Brand"`, `"#FF5722"` |
| `shape` | `ShapeRole` | `"CornerFull"` |
| `typography` | `TypographyRole` | `"LabelLarge"` |
| `motion` | `MotionRole` | `"FastSpatial"` |
| `dimension`, `number`, `opacity`, `elevation` | `double` | `40`, `0.38` |
| `bool` | `bool` | `true` |

`opacity` on a colour property names the number property that scales its alpha. Upstream specifies
disabled states as a role plus an opacity rather than as a second role, so the two have to travel
together to resolve into one brush.

## What gets generated

For a component table named `ButtonColorTokens` with `variantEnum: "ButtonVariant"` and
`resolvedName: "ButtonColors"`:

- `enum ButtonVariant` — one member per set.
- `record ButtonColorTokens` — the token values, with a `static` property per set and
  `For(ButtonVariant)`.
- `record ButtonColors` — the colour tokens resolved to brushes for one theme variant, built by
  `ButtonColorTokens.Resolve(scheme, isDark)`.
- `class ButtonColorOverrides : ComponentTokenOverrides` — a sparse authoring bag. Only the tokens
  actually set on it replace the generated defaults, so a theme can retune one colour without
  restating the set around it.
- Accessors on the theme snapshot: `ResolvedTheme.GetButtonColorTokens(variant)` and the memoised
  `ResolvedTheme.GetButtonColors(variant, isDark)`.

Overrides ride the theme as its content:

```xml
<foundation:MaterialTheme ColorScheme="{mcu:TonalSpotScheme '#6750A4'}">
    <foundation:ButtonColorOverrides Variant="Filled" ContainerColor="Custom:Brand" />
</foundation:MaterialTheme>
```

Set `"overridable": false` on a table that should not get a bag.

## Adding a component

Write one table file. There is no other step — the snapshot accessors and the override plumbing
are emitted from the set of tables, so `ResolvedTheme` never has to be edited.
