# Material 3 Expressive for Avalonia

A Material 3 Expressive component library for Avalonia that tracks the official AndroidX Compose
implementation — pinned to **compose-material3 `1.5.0-alpha25`** — in visual, behavioural and API
fidelity. Every token is transcribed from the Material token database and stamped with the version
it came from.

> `0.1.0-alpha`. The theming core, the token pipeline, motion and the first component wave are
> done; most components are not yet. See [ROADMAP.md](ROADMAP.md).

## What is here

| | |
|---|---|
| **Dynamic colour** | Any seed colour to a full Material scheme, light and dark, plus custom key colours. Built on [MaterialColorUtilities.Avalonia](modules/MaterialColorUtilities). |
| **Type scale** | Fifteen slots and their emphasized counterparts, each naming its own face and weight. |
| **Shape** | Corner sizes that are dp *or percent*, so `CornerFull` is genuinely half the height at any size. |
| **Motion** | A spring animator ported from `animation-core` — analytic, retargetable, velocity-preserving. |
| **Interaction** | State layers with Material's asymmetric fades, ripples, and the corner-morph press feedback. |
| **Components** | Button, toggle button, icon button, toggle icon button, FAB and extended FAB, card, checkbox, radio button, switch, slider, linear and circular progress (including the wavy form), loading indicator. |

## Getting started

```xml
<Application.Styles>
    <FluentTheme />
    <m3:MaterialStyles />
</Application.Styles>

<foundation:MaterialThemeScope.Theme>
    <foundation:MaterialTheme ColorScheme="{mcu:TonalSpotScheme '#6750A4'}" />
</foundation:MaterialThemeScope.Theme>
```

That is the whole setup. Component tokens are *references* to system roles — a button's container
follows `Primary`, its label `OnPrimary` — so dark mode and a change of seed colour both work with
no further wiring.

```xml
<m3:MaterialButton Variant="Tonal" Size="Medium">Send</m3:MaterialButton>
<m3:MaterialFab Label="Compose"><PathIcon Data="{StaticResource EditIcon}" /></m3:MaterialFab>
<m3:MaterialCard Variant="Outlined">…</m3:MaterialCard>
```

## Theming a subtree

One attached property re-themes everything below it, including controls that read their tokens in
C#:

```xml
<Border>
    <m3:MaterialThemeScope.Theme>
        <m3:MaterialTheme ColorScheme="{mcu:TonalSpotScheme '#FF6D00'}" />
    </m3:MaterialThemeScope.Theme>
    …
</Border>
```

Precedence runs control-local override → nearest subtree theme → application theme → built-in
defaults. Popups inherit the theme of the subtree that opened them.

## Retuning a token

Overrides ride the theme as its content and are sparse: only what they name replaces the generated
default.

```xml
<foundation:MaterialTheme>
    <foundation:ButtonColorOverrides Variant="Filled" ContainerColor="Custom:Brand" />
    <foundation:ButtonSizeOverrides Variant="Small" ContainerHeight="48" />
</foundation:MaterialTheme>
```

## How it is built

Every token in the library is defined once, in a JSON table under
[`src/Material3Expressive/Tokens`](src/Material3Expressive/Tokens), and turned into C# at build
time. Adding a component token domain is one table file — the value record, the variant enum, the
override bag and the theme accessors are all generated. See
[docs/token-tables.md](docs/token-tables.md).

The theming core is documented in [docs/architecture.md](docs/architecture.md); deliberate
deviations from upstream are listed in [docs/parity.md](docs/parity.md).

## Layout

```
src/Material3Expressive              the library
src/Material3Expressive.TokenGenerator   the token-table source generator
src/Material3Expressive.Demo         the gallery
modules/MaterialColorUtilities       dynamic colour (independently useful)
modules/MaterialShapes               polygon shapes and morphing (the loading indicator)
```

## Licence

Apache-2.0, matching the AndroidX sources this is ported from.
