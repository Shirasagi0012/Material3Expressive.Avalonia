# Token tables

JSONC tables under `src/Material3Expressive/Tokens/` are the checked-in provenance layer for
reference, system, and raw component token facts. `Material3Expressive.TokenGenerator` turns them
into C# during the library build. Component default policy is intentionally not a JSON artifact:
it lives beside the owning control in C#.

## Input contract

| Suffix | Root `kind` | Purpose |
|---|---|---|
| `.ref.jsonc` | `referenceTokens` | Reference-layer primitives, when the project owns an independent source. |
| `.sys.jsonc` | `systemTokens` | Shared system roles and scheme-shaped system data such as `TypeScale`. |
| `.comp.jsonc` | `componentTokens` | Verbatim upstream component token tables. |

The root `kind` is semantic authority and must match the file suffix (`M3ETOK004`). Other
`AdditionalFile` names are ignored. A malformed table produces `M3ETOK001`; duplicate table names
produce `M3ETOK002`; an unknown, incompatible, reverse-layer, or cyclic `@Table.Member` reference
produces `M3ETOK003`.

The reader accepts comments and trailing commas. Keep provenance and any intentional transcription
note adjacent to the raw value that needs it.

## Generated output

A raw reference or component table emits an `internal static` token class in
`Material3Expressive.Foundation.Tokens`. System tables emit their raw class too; a scheme-shaped
system table such as `TypeScale` additionally emits its system record and variant enum.

The generator does not emit any of the following:

```text
componentDefaults
ResolvedXxxTokens
XxxTokensOverrides
ComponentTokenOverrides
ResolvedTheme.Components
```

Raw generated types are implementation/provenance artifacts, not a stable package customisation
API. A component's C# Defaults policy selects raw tables, resolves colour roles against a
`ResolvedTheme`, applies Compose corrections and Avalonia adaptations, and returns only the
semantic records its control needs.

## `componentTokens`

One `.comp.jsonc` table corresponds to one upstream `*Tokens.kt` object. It should preserve the
upstream values and source/version metadata without encoding behavior:

```jsonc
{
  "kind": "componentTokens",
  "name": "ButtonSmallTokens",
  "namespace": "Material3Expressive.Foundation.Tokens",
  "version": "v0_11_0",
  "source": "androidx compose/material3 tokens/ButtonSmallTokens.kt",
  "tokens": {
    "ContainerHeight": { "type": "dimension", "value": 40 },
    "PressedContainerShape": { "type": "shape", "value": "CornerSmall" }
  }
}
```

Variant axes, size axes, enabled/checked/pressed selection, state-layer behavior, template
projection and user-facing properties belong in `ButtonDefaults`, `SwitchDefaults`,
`IconButtonDefaults`, or the relevant component owner—not in this table.

## Values and references

A string beginning with `@` refers to another generated token-set member. References are normally
used inside raw tables when an upstream token object reuses another static token; the catalog
checks existence, type, permitted layer direction and cycles.

| `type` | C# type | Value syntax |
|---|---|---|
| `color` | `ColorTokenValue` | `"Primary"`, `"Custom:Brand"`, `"#FF5722"` |
| `shape` | `ShapeRole` | `"CornerFull"` |
| `typography` | `TypographyRole` | `"LabelLarge"` |
| `motion` | `MotionRole` | `"FastSpatial"` |
| `dimension`, `number`, `opacity`, `elevation` | `double` | `40`, `0.38` |
| `bool` | `bool` | `true` |
| `enum` | declared enum | a named enum member |

`provenance` may be `androidx-token`, `compose-default`, `m3-spec`, or `avalonia-policy`. Raw
tables default to `androidx-token`; use an explicit value when the table's source genuinely differs.
The generator carries the value into generated comments; it does not turn provenance into runtime
behavior.

## Directory layout

```text
Tokens/
  Reference/
  System/
  Component/<Domain>/
```

There is no `Tokens/Defaults/` input tree. A C# Defaults owner is the corresponding assembly
layer. This preserves a clean division: JSON holds static source facts; code owns policy, bindings,
state selection, lifecycle and rendering adaptation.

## Adding or changing a component

1. Transcribe the upstream raw token object into `.comp.jsonc`, including source/version and
   comments for deviations.
2. Add or update the component-owned Defaults policy to choose raw tables and resolve system roles.
3. Keep normal visual customisation on existing Avalonia properties and styles. Add a
   component-specific semantic property only when a template/presenter genuinely needs one.
4. Cover mappings and corrections with deterministic tests; run the visual-parity workflow when
   its infrastructure is available.

Do not add a generic override bag or a component snapshot to bridge a one-off need. Those layers
would blur static token provenance with component state and Avalonia behavior.
