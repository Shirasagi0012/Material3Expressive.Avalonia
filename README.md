# Material 3 Expressive for Avalonia

A Material 3 Expressive component library for Avalonia that tracks the official AndroidX Compose
implementation — currently through Compose Multiplatform Material 3 `1.12.0-alpha03` (Jetpack
Material 3 `1.5.0-alpha22`) — in visual, behavioural and API
fidelity. Every token is transcribed from the Material token database and stamped with the version
it came from.

> `0.1.0-alpha`. The local roadmap is delivered — theming core, token pipeline, motion,
> all five component waves, and local visual verification. NuGet publishing and repository
> archival are deliberately deferred. The Compose reference target and parity workflow are
> documented in [docs/visual-parity.md](docs/visual-parity.md).

## What is here

| | |
|---|---|
| **Dynamic colour** | Any seed colour to a full Material scheme, light and dark, plus custom key colours. Built on [MaterialColorUtilities.Avalonia](modules/MaterialColorUtilities). |
| **Type scale** | Fifteen slots and their emphasized counterparts, each naming its own face and weight. |
| **Shape** | Corner sizes that are dp *or percent*, so `CornerFull` is genuinely half the height at any size. |
| **Motion** | A spring animator ported from `animation-core` — analytic, retargetable, velocity-preserving. |
| **Interaction** | State layers with Material's asymmetric fades, ripples, and the corner-morph press feedback. |
| **Symbols** | Material Symbols Rounded as the first-party icon family, including animatable fill, weight, grade and optical-size axes. |
| **Components** | Buttons (5 variants × 5 sizes, plus toggle, icon, toggle icon, group, split), FAB and FAB menu, card, checkbox, radio button, switch, slider, progress (linear, circular, wavy) and loading indicator, chips, text field, navigation bar and rail, tabs, app bar, drawer, dialog, bottom sheet, snackbar, tooltip, badge, menu, search, carousel, date and time pickers. |

## Getting started

Register the bundled Rounded variable font in the application builder:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseMaterial3Expressive();
```

Then load the component styles and theme:

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
<m3:MaterialFab Label="Compose"><ms:MaterialSymbol Symbol="Edit" /></m3:MaterialFab>
<m3:MaterialCard Variant="Outlined">…</m3:MaterialCard>
```

Material Symbols use the Rounded family by default. Toggle, navigation, tab and chip selection
states drive the variable font's `Fill` axis instead of swapping glyphs. The current tested render
stack is Avalonia 12.1 with SkiaSharp 4.150.1.

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

The theming core is documented in [docs/architecture.md](docs/architecture.md). The current
Compose comparison workflow is in [docs/visual-parity.md](docs/visual-parity.md).

## Layout

```
src/Material3Expressive              the library
src/Material3Expressive.TokenGenerator   the token-table source generator
src/Material3Expressive.Catalog         the gallery
modules/MaterialColorUtilities       dynamic colour (independently useful)
modules/MaterialShapes               polygon shapes and morphing (the loading indicator)
modules/MaterialSymbols              variable Material Symbols and bundled font families
```

## Licence

Apache-2.0, matching the AndroidX sources this is ported from.
