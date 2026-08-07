# Token tables

Every token in the library is defined once, in a JSONC table under `src/Material3Expressive/Tokens/`,
and turned into C# at build time by `Material3Expressive.TokenGenerator`. The directories mirror
the ownership boundary: reference, system and raw component transcriptions are kept apart from
component assembly and Avalonia overrides. Nothing about a default is written by hand three times
— that triplication (constructor default, resolver arm, markup-extension fallback) is what this
pipeline exists to delete. Generated component records and snapshot accessors are internal
implementation details; public customization goes through the generated override bags and stable
component enums.

The filename suffix communicates the table category and determines discovery:

| suffix | root `kind` | purpose |
|---|---|---|
| `.ref.jsonc` | `referenceTokens` | reference-layer primitives such as palettes |
| `.sys.jsonc` | `systemTokens` | shared system roles and schemes |
| `.comp.jsonc` | `componentTokens` | verbatim upstream component token tables |
| `.default.jsonc` | `componentDefaults` | component assembly, variants and overrides |

The root `kind` remains the semantic authority; a suffix/kind mismatch is a build error
(`M3ETOK004`). Other `AdditionalFile` names are ignored. A malformed table is a build error
(`M3ETOK001`); two tables claiming the same type name is `M3ETOK002`; an unknown, incompatible or
cyclic `@Table.Member` reference is `M3ETOK003`.

`provenance` is optional metadata with one of `androidx-token`, `compose-default`, `m3-spec` or
`avalonia-policy`. If omitted, raw/reference/system tables default to `androidx-token` and component
defaults default to `compose-default`; use an explicit value for spec-only domains such as Carousel
or for a deliberate Avalonia policy table. The generator carries it into the generated header and
XML documentation without treating it as runtime behaviour.

The generator parses each `AdditionalFile` independently. Its validation catalog is built from a
declaration-only projection (names, types, set shape and references), while source emission keeps
the full parsed body. Changing a literal value therefore regenerates only that table; changing a
member, set or reference also invalidates the catalog and the aggregate theme accessors.

The reader accepts `//` and `/* */` comments and trailing commas, because these files are
maintained by hand and want annotating — every deviation from upstream is a comment next to the
value it explains.

## Current coverage snapshot

At the frozen AndroidX baseline `ff0a0a99c4f0cfe95d75d5df615e51d399575922`, the repository contains
103 raw component tables, 3 system tables, 35 component-defaults tables, and no duplicated
reference palette tables. The raw component set covers all 103 component-domain `*Tokens.kt`
files from the 120-file AndroidX token directory. The remaining 17 names are either owned by
`MaterialColorUtilities`, `ShapeScheme`, `MotionScheme`, or `TypographyScheme`, represented by a
system table, or intentionally deferred because `PrimaryNavigationTabTokens` contains a custom
3dp shape that the current raw schema cannot express faithfully.

The complete file-by-file inventory, including the 27 raw tables that do not yet have a defaults
assembly, is in [Token coverage and ownership](token-coverage.md). Keep that inventory in sync
when the AndroidX baseline or a component's owner changes.

## Four table categories

### `referenceTokens` — design-system reference values

Reference tables are reserved for palette and other design-system primitives when they are added.
They emit a token set in `Material3Expressive.Foundation.Tokens` and do not contain component
assembly logic.

### `systemTokens` — the system scheme

System tables hold shared Material roles such as elevation, state-layer opacity and typography.
Simple system tables use the same `tokens` shape as a transcription; scheme-shaped system tables
may use `properties` and `sets` (TypeScale is the current example). They are consumed by the
system theme layer, not authored as a component default.

### `componentTokens` — a verbatim upstream transcription

One table per upstream `*Tokens.kt` object. It becomes an `internal static class` of constants in
`Material3Expressive.Foundation.Tokens`, carrying the upstream file and token-DB stamp in its
header so the transcription stays checkable. Raw classes are implementation/provenance artifacts,
not stable package API; use the component defaults, theme overrides and control properties at
runtime instead.

```json
{
  "kind": "componentTokens",
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

### `componentDefaults` — a value record plus its variants

Declares a component token surface once (`properties`) and fills it once per variant (`sets`). This
is the layer that composes transcriptions into something a control consumes. Runtime corrections,
`extends`, variant selection, resolved brushes and sparse overrides belong here; they must not be
copied into a raw `componentTokens` table.

```json
{
  "kind": "componentDefaults",
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

The four JSON kinds are the artifact contract. Internally, the generator also records two
orthogonal axes: `layer` (`Reference`, `System`, `Component`) and `artifact` (`Raw`, `Defaults`).
The root `kind` is authoritative; directory names are organizational only. This distinction keeps
component-owned Avalonia policy out of raw AndroidX transcriptions and leaves room for a future
reference palette without treating it as a component default.

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

- `public enum ButtonVariant` — one stable member per set.
- `internal record ButtonColorTokens` — the token values, with a `static` property per set and
  `For(ButtonVariant)`; this is consumed by the component adapter, not application code.
- `internal record ButtonColors` — the colour tokens resolved to brushes for one theme variant,
  built by `ButtonColorTokens.Resolve(scheme, isDark)`.
- `public class ButtonColorOverrides : ComponentTokenOverrides` — a sparse authoring bag. Only the
  tokens actually set on it replace the generated defaults, so a theme can retune one colour
  without restating the set around it.
- Internal accessors on the theme snapshot are intentionally implementation details; controls and
  markup resolve through their component-owned adapter instead of coupling user code to generated
  record types.

ToggleButton is intentionally a separate defaults table (`ToggleButtonColorTokens` with
`ToggleButtonVariant`) and runtime adapter. Its resolved record has unchecked/checked/disabled
content and container slots; it does not make a toggle depend on the ordinary Button resolver's
conditional fields.

Overrides ride the theme as its content:

```xml
<foundation:MaterialTheme ColorScheme="{mcu:TonalSpotScheme '#6750A4'}">
    <foundation:ButtonColorOverrides Variant="Filled" ContainerColor="Custom:Brand" />
</foundation:MaterialTheme>
```

Set `"overridable": false` on a table that should not get a bag.

## Directory layout

Keep the domain visible in the path as well as in the table name:

```text
Tokens/
  Reference/
  System/
  Component/<Domain>/
  Defaults/<Domain>/
```

The generator includes the four suffix families recursively. A component normally has at least one
raw table in `Tokens/Component/<Domain>/` and one matching defaults table in
`Tokens/Defaults/<Domain>/`.

## Adding a component

Write the raw upstream transcription and its component-defaults assembly table. There is no hand
written accessor step — the snapshot accessors and override plumbing are emitted from the set of
defaults tables, so `ResolvedTheme` never has to be edited.
