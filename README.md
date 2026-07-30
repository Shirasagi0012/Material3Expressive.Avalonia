# Material 3 Expressive for Avalonia

A Material 3 Expressive component library for Avalonia that tracks the official AndroidX Compose
implementation in visual, behavioural and API fidelity. The implementation source baseline is
AndroidX Compose Material3 `1.5.0-alpha25` at commit `ff0a0a99`; the executable desktop visual
reference is Compose Multiplatform Material3 `1.12.0-alpha03`, corresponding to Jetpack Material3
`1.5.0-alpha22`. The split is intentional and documented in
[docs/reference/upstream-baselines.md](docs/reference/upstream-baselines.md). Every token is transcribed from the
Material token database and stamped with the version it came from.

> `0.1.0-alpha` development line. The theming core, token pipeline, motion foundations, broad
> component coverage, and local visual-parity workflow are implemented. Current completeness work
> is tracked in [ROADMAP.md](ROADMAP.md); no publication date is currently planned.

## What is here

| | |
|---|---|
| **Dynamic colour** | Any seed colour to a full Material scheme, light and dark, plus custom key colours. Built on [MaterialColorUtilities.Avalonia](modules/MaterialColorUtilities). |
| **Type scale** | Fifteen slots and their emphasized counterparts, each naming its own face and weight. |
| **Shape** | Corner sizes that are dp *or percent*, so `CornerFull` is genuinely half the height at any size. |
| **Motion** | A spring animator ported from `animation-core` — analytic, retargetable, velocity-preserving. |
| **Interaction** | State layers with Material's asymmetric fades, ripples, and the corner-morph press feedback. |
| **Symbols** | Material Symbols Rounded as the first-party icon family, including animatable fill, weight, grade and optical-size axes. |
| **Components** | Surface, divider, list-row, segmented-list, and scaffold primitives; buttons (5 variants × 5 sizes, plus toggle, icon, toggle icon, group, split, and segmented), FAB and FAB menu, card, checkbox, radio button, switch, slider, progress (linear, circular, wavy) and loading indicator, chips, text field, navigation bar and rail, tabs, app bar, drawer, dialog, bottom sheet, snackbar, tooltip, badge, menu, search, carousel, date and time pickers. |

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

## Segmented buttons

`MaterialSegmentedButtonRow` adapts Compose's separate single- and multi-choice row scopes through
one XAML container with an explicit `SelectionMode`. Children bind their ordinary nullable
`IsChecked` state. Single mode keeps at most one checked item and exposes radio/selection
automation; multiple mode leaves each item independent and exposes toggle automation.

```xml
<m3:MaterialSegmentedButtonRow Width="300">
    <m3:MaterialSegmentedButton Content="Day" />
    <m3:MaterialSegmentedButton Content="Month" IsChecked="True" />
    <m3:MaterialSegmentedButton Content="Week" />
</m3:MaterialSegmentedButtonRow>

<m3:MaterialSegmentedButtonRow SelectionMode="Multiple" Width="300">
    <m3:MaterialSegmentedButton Content="Email" IsChecked="True" />
    <m3:MaterialSegmentedButton Content="Calendar" />
    <m3:MaterialSegmentedButton Content="Tasks" IsChecked="True" />
</m3:MaterialSegmentedButtonRow>
```

Visible direct children receive equal widths, one-outline overlap, and inferred first/middle/last
shapes. A checked item without custom `Icon` content uses the same fixed 24×24 Filled Check vector
as AndroidX; `SelectionIcon` accepts a replacement Avalonia `Geometry` (the XAML equivalent of
Compose's `ImageVector`), and `ShowSelectionIcon="False"` disables the default. Arrow, Home, and
End keys move and select in single mode, while Space retains ordinary toggle behavior in multiple
mode.

## Surfaces and list rows

`MaterialSurface` separates Material tonal and shadow elevation; `MaterialDivider` supplies the
one control adaptation of horizontal and vertical dividers through `Orientation`. Set
`IsHairline="True"` when the Compose `Dp.Hairline` contract is required: it paints one physical
pixel while retaining zero cross-axis layout size. `Thickness="0"` deliberately remains no line.
A standard
`MaterialListItem` has named leading, headline (`Content`), overline, supporting, and trailing
slots. Its `InteractionMode` is the explicit Avalonia/XAML adaptation of the four Compose
overloads:

```xml
<m3:MaterialSurface TonalElevation="3" ShadowElevation="3" Padding="16">
    <m3:MaterialListItem InteractionMode="MultiSelection"
                         IsSelected="True"
                         Content="Keep offline"
                         SupportingContent="Download new content automatically." />
</m3:MaterialSurface>

<m3:MaterialDivider Orientation="Horizontal" />
<m3:MaterialDivider Orientation="Vertical" IsHairline="True" />

<m3:MaterialSegmentedList>
    <m3:MaterialListItem InteractionMode="Click" Content="Account" />
    <m3:MaterialListItem InteractionMode="MultiSelection"
                         IsSelected="True"
                         Content="Offline notifications" />
    <m3:MaterialListItem InteractionMode="Click" Content="Privacy" />
</m3:MaterialSegmentedList>
```

`None` is enabled static content: it neither consumes pointer/keyboard activation nor lets an
inherited `Command.CanExecute` disable the row. `Click` invokes an action, `SingleSelection` is
radio-like, and `MultiSelection` toggles `IsSelected`. A host owns single-selection coordination and an owning
reorder surface may report `IsDragged`; disabled rows retain disabled colours but receive the
upstream dragged shape, elevation, and state layer. The control does not invent a drag/drop system. The
`MaterialSegmentedList` container is the XAML adaptation of Compose segmented-list grouping: it
infers single/first/middle/last shapes from its visible direct `MaterialListItem` children, applies
the 2dp token gap and segmented container colour, and leaves each row's `InteractionMode` and
selection ownership unchanged. See [docs/coverage/component-matrix.md](docs/coverage/component-matrix.md) for
the executable-reference and alpha25 version-gap status.

## Scaffold layout

`MaterialScaffold` coordinates body `Content`, `TopBar`, `BottomBar`, `SnackbarHost`, and
`FloatingActionButton`. The body always fills the complete scaffold. Apply the read-only
`ContentPadding` to the body child; for scrolling content, apply it inside the `ScrollViewer`
rather than to the viewer itself.

`MaterialBottomSheetScaffold` adds the standard persistent sheet contract around the same slots.
Its `BottomSheetState` starts at `PartiallyExpanded`, exposes vetoable `Expand()`,
`PartiallyExpand()`, and `Hide()` commands, and retains the alpha25 `56dp` peek and `640dp`
maximum width defaults. `SheetContent` is measured at full height and clipped by the partial
anchor. `SheetSwipeEnabled` coordinates delayed pointer capture, precision-wheel input, and
`ScrollViewer` chaining: upward motion expands the sheet before scrolling its content, while
downward motion returns to the peek anchor only after the child reaches its top boundary. Its
state follows the shared complex-state contract: the getter exposes the non-null effective
`BottomSheetState`, each instance creates a default, and assigning an external state transfers
observation. Applications restore an anchor by constructing `BottomSheetState` with its initial
value; reset uses the control's explicit reset operation. Snackbar queue state remains the separate
`SnackbarHost` responsibility. Static and expressive-spring parity contracts are present; the
current light-theme result retains a documented one-channel shadow framework-parameter mismatch.

`MaterialSnackbarHost` provides the queue boundary used by a scaffold's snackbar slot. Call
`SnackbarHostState.ShowSnackbarAsync(...)` to enqueue a message; the returned task completes with
`SnackbarResult.ActionPerformed` or `SnackbarResult.Dismissed`. The host creates a per-instance
non-null state by default, accepts an application-owned replacement, and exposes `ResetState()` for
restoration to a fresh queue. Short, long, and indefinite durations follow the alpha25 defaults;
the settled message and generated `Undo` action visuals have executable alpha22 parity, while
appearance/disappearance motion parity and recommended accessibility timeout calculation remain
documented gaps.

```xml
<m3:MaterialScaffold x:Name="Scaffold"
                     FloatingActionButtonPosition="End">
    <m3:MaterialScaffold.TopBar>
        <m3:MaterialAppBar Content="Inbox" />
    </m3:MaterialScaffold.TopBar>
    <m3:MaterialScaffold.BottomBar>
        <m3:MaterialNavigationBar />
    </m3:MaterialScaffold.BottomBar>
    <m3:MaterialScaffold.FloatingActionButton>
        <m3:MaterialFab><ms:MaterialSymbol Symbol="Add" /></m3:MaterialFab>
    </m3:MaterialScaffold.FloatingActionButton>
    <m3:MaterialScaffold.SnackbarHost>
        <m3:MaterialSnackbarHost x:Name="SnackbarHost" />
    </m3:MaterialScaffold.SnackbarHost>

    <ScrollViewer>
        <StackPanel Margin="{Binding ContentPadding, ElementName=Scaffold}">
            <!-- scrolling body -->
        </StackPanel>
    </ScrollViewer>
</m3:MaterialScaffold>
```

```csharp
_ = SnackbarHost.State.ShowSnackbarAsync("Conversation archived", actionLabel: "Undo");
```

`ContentWindowInsets` defaults to `null`, which observes the containing `TopLevel` safe area and
updates when it changes. Set an explicit `Thickness` to replace that source; use `0` when an
ancestor already consumed the safe area. The scaffold positions a snackbar host but deliberately
does not own its queue, timeout, or result state.

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
[docs/reference/token-tables.md](docs/reference/token-tables.md).

The theming core is documented in [docs/architecture/theming.md](docs/architecture/theming.md). The current
Compose comparison workflow is in [docs/testing/visual-parity/README.md](docs/testing/visual-parity/README.md). Feature-level
support is tracked in [docs/coverage/component-matrix.md](docs/coverage/component-matrix.md), while future work
and its completion criteria live in [ROADMAP.md](ROADMAP.md).

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
