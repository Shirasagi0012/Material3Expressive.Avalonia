# Token tables

Raw reference, system and component facts are defined once in JSONC under
`src/Material3Expressive/Tokens/` and turned into C# at build time by
`Material3Expressive.TokenGenerator`. The directories keep those upstream transcriptions separate
from the remaining legacy component-default assembly tables.

The project is migrating away from generated `componentDefaults`: ordinary `Button` and
`ToggleButton` are the first Button-family components on the new path. Their raw `.comp.jsonc`
tables remain generated, while variant/size/state mapping, Compose corrections, Avalonia adaptation
and template projection live in their own C# Defaults/adapters. The `.default.jsonc` tables and
their generated snapshot/override surfaces remain only for controls not yet migrated.

The filename suffix communicates the table category and determines discovery:

| suffix | root `kind` | purpose |
|---|---|---|
| `.ref.jsonc` | `referenceTokens` | reference-layer primitives such as palettes |
| `.sys.jsonc` | `systemTokens` | shared system roles and schemes |
| `.comp.jsonc` | `componentTokens` | verbatim upstream component token tables |
| `.default.jsonc` | `componentDefaults` | legacy component assembly, variants and overrides for unmigrated controls |

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
103 raw component tables, 3 system tables, 32 legacy component-defaults tables, and no duplicated
reference palette tables. The raw component set covers all 103 component-domain `*Tokens.kt`
files from the 120-file AndroidX token directory. The remaining 17 names are either owned by
`MaterialColorUtilities`, `ShapeScheme`, `MotionScheme`, or `TypographyScheme`, represented by a
system table, or intentionally deferred because `PrimaryNavigationTabTokens` contains a custom
3dp shape that the current raw schema cannot express faithfully.

The complete file-by-file inventory is in [Token coverage and ownership](token-coverage.md). Keep
that inventory in sync when the AndroidX baseline or a component's owner changes.

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
not stable package API; use the component's defaults policy and public control properties at
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

### `componentDefaults` — legacy generated assembly

Declares a component token surface once (`properties`) and fills it once per variant (`sets`). This
is the legacy layer that composes transcriptions into a generated control surface. It remains
supported for unmigrated controls. New component-owned Defaults code should instead keep runtime
corrections, variant selection, resolved brushes and Avalonia policy beside the control; none of
that belongs in a raw `componentTokens` transcription.

```json
{
  "kind": "componentDefaults",
  "name": "IconButtonColorTokens",
  "namespace": "Material3Expressive.Foundation",
  "resolvedName": "IconButtonColors",
  "variantEnum": "IconButtonVariant",

  "properties": {
    "ContainerColor": { "type": "color", "summary": "The container fill." },
    "DisabledContainerColor": { "type": "color", "opacity": "DisabledContainerOpacity" },
    "DisabledContainerOpacity": { "type": "opacity" }
  },

  "sets": [
    { "name": "Standard", "values": { "ContainerColor": "@IconButtonTokens.ContainerColor", "…": "…" } },
    { "name": "Filled", "extends": "Standard", "values": { "ContainerColor": "@FilledIconButtonTokens.ContainerColor" } }
  ]
}
```

A set must give every property a value; `extends` inherits from a set declared above it, so a
variant states only what it changes.

Ordinary Button and ToggleButton intentionally have no corresponding generated defaults tables.
`ButtonVariant` and `ButtonSize` are handwritten component API; `ButtonDefaults` and
`ToggleButtonDefaults` map them directly to the raw Button tables and keep state policy in C#.
Their generated color/size override bags have therefore been removed.

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

For an **unmigrated** component-defaults table with a `variantEnum` and `resolvedName`, the
generator still emits:

- a public variant enum, one stable member per set;
- an internal token record with a static property per set and `For(variant)`;
- an internal resolved record for one theme variant;
- a `ComponentTokenOverrides` subclass when the table is overridable;
- internal accessors on the legacy component portion of `ResolvedTheme`.

Those are migration compatibility artifacts, not the model for new component APIs. The Button
family prototypes instead have handwritten axis API, internal raw-table consumption, independent
semantic records, and public Avalonia properties for visual customization. ToggleButton still has
unchecked/checked/disabled slots and three shape states, but it now reuses `ButtonDefaults.Metrics`
and `ButtonDefaults.Shapes` for the shared size/shape axis.

For an unmigrated component, its generated override bag is still added to `MaterialTheme.Components`.
Do not use that mechanism as a Button customization API: use `Background`, `Foreground`, `Padding`,
`BorderBrush`, `BorderThickness`, `MinHeight`, styles or a custom ControlTheme instead.

Set `"overridable": false` on a legacy table that should not get a bag.

## Directory layout

Keep the domain visible in the path as well as in the table name:

```text
Tokens/
  Reference/
  System/
  Component/<Domain>/
  Defaults/<Domain>/        # legacy, until its component migrates
```

The generator includes the four suffix families recursively. Every component has raw table(s) in
`Tokens/Component/<Domain>/`; only a legacy component also has a matching defaults table under
`Tokens/Defaults/<Domain>/`.

## Adding a component

Write the raw upstream transcription first. Then give the control a component-owned Defaults
adapter when it needs variant mapping, Compose corrections, theme resolution or template
projection. Add a `.default.jsonc` assembly table only when deliberately using the remaining legacy
generator path; it is not the target architecture and new code should not rely on generated
component overrides or `ResolvedTheme.Components`.
