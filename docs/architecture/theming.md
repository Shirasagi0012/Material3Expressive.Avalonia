# Current theming architecture

The active theming core is a boundary-relative Raw-token graph built on Avalonia resources. The
old `ResolvedTheme`, `MaterialThemeScope`, requester-relative `TokenReader`, and scheme snapshot
transport have been removed.

The normative design is split across the [token refactor documentation](../refactor/README.md).
This page records the currently active production path.

## Resolution pipeline

```text
canonical MaterialToken<T> identity
    -> MaterialTheme.Children sparse sources
    -> Avalonia owner resource chain
    -> composed DefaultRawTokens catalog
    -> one MaterialResolutionSession
    -> MaterialValueKey<T>
    -> DynamicResource / ControlTheme
```

`MaterialTheme` is a `ResourceProvider` placement. A Flat request fixes that placement's Owner as
the boundary for every Raw dependency in the same resolution session. Each alias or recipe hop
restarts at that Owner and preserves the original `ThemeVariant`.

There is no persistent Raw or Flat cache. One request memoizes common Raw dependencies only for
the lifetime of its `MaterialResolutionSession`.

## Raw identities and definitions

Raw identities are immutable, canonical `MaterialToken<T>` instances. M3E owns its Foundation and
component identities; MCU Avalonia owns Reference/System color identities. Identity descriptors do
not carry defaults.

Definitions use the closed shared model:

- literal;
- same-type alias;
- pure recipe with declared Raw dependencies.

The composed `DefaultRawTokens` catalog is queried only after the complete Avalonia resource chain
misses. This lets a nearer sparse placement inherit an explicit ancestor definition instead of
accidentally replacing it with a built-in default.

## Authoring

`MaterialTheme.Children` contains sparse `IMaterialTokenSource` instances. Later children have
higher priority. MCU `ColorScheme` objects can participate directly; a one-off definition uses
`TokenOverride`:

```xml
<m3:MaterialTheme>
  <m3:MaterialTheme.Children>
    <mcu:TonalSpotScheme Color="#6750A4" />
    <m3:TokenOverride
        Key="{x:Static m3:SystemShapeTokens.CornerSmall}"
        Value="10" />
  </m3:MaterialTheme.Children>
</m3:MaterialTheme>
```

The generator emits canonical descriptors and immutable default-catalog contributions. It does not
emit per-table override facades. `TokenRef` creates an alias definition targeting another canonical
identity.

## Material values and controls

Controls consume public typed `MaterialValueKey<T>` endpoints through `DynamicResource`; Raw keys
are not a ControlTheme consumption protocol. Material Value plans may combine Raw values into
consumer-ready brushes, thicknesses, shapes, and profiles, but cannot inspect a requester, bounds,
pointer state, or other hidden runtime input.

During the current migration, token-dependent component families are excluded from the production
assembly. Only the token-independent Scaffold slice remains enabled. A component returns to the
build only after its complete Raw -> Material Value -> DynamicResource path no longer depends on
the deleted runtime.
