# Token coverage and ownership

This is the auditable inventory for the token split. It describes the checked-in source state at
AndroidX commit `ff0a0a99c4f0cfe95d75d5df615e51d399575922` (the project-frozen Material 3
baseline). The AndroidX token directory contains 120 `*Tokens.kt` files at that commit.

## Current file counts

| Input family | Count | Meaning |
|---|---:|---|
| `Tokens/Reference/*.ref.jsonc` | 0 | No duplicated palette/reference source is checked in. |
| `Tokens/System/*.sys.jsonc` | 3 | `ElevationTokens`, `StateTokens`, and the scheme-shaped `TypeScale`. |
| `Tokens/Component/**/*.comp.jsonc` | 103 | Same-name, verbatim AndroidX component token tables. |

The 103 raw component tables account for every AndroidX component token file that belongs to a
component domain. The 17-name exception list below includes the three names represented by the
system tables, so the unique total remains 120. A raw table is allowed to be a sub-token source
without a one-to-one public control: a component-owned C# Defaults policy may combine several
size, colour, state, and item tables, or preserve a raw table for a future control family.

There are no `.default.jsonc` inputs. Card, FAB, Form, IconButton, Misc, Navigation, Overlay,
ProgressIndicator, TextInput and Button policies now live in their owning C# Defaults classes.
This keeps Compose defaults/corrections and Avalonia adaptation with the controls that consume
them, rather than generating a global component snapshot or a generic override bag.

## AndroidX files intentionally not represented as raw component tables

The 17 AndroidX names below are not omissions from the component transcription count. They belong
to a different owner or need a representation that is not currently part of the raw-table schema.

| AndroidX token file | Current owner or decision |
|---|---|
| `ElevationTokens`, `StateTokens`, `TypeScaleTokens` | represented by the three `.sys.jsonc` tables; `TypeScale` is the Avalonia scheme record built from the upstream slots. |
| `PaletteTokens`, `ColorLightTokens`, `ColorDarkTokens`, `ColorSchemeKeyTokens` | `MaterialColorUtilities` owns tonal palettes, dynamic schemes, and `SysColorToken`; duplicating the generated palette would create a second colour source of truth. |
| `ShapeTokens`, `ShapeKeyTokens` | `ShapeScheme` and `ShapeRole` own the theme-resolved shape roles; a raw Kotlin shape object would not add a new runtime source. |
| `MotionTokens`, `StandardMotionTokens`, `ExpressiveMotionTokens`, `MotionSchemeKeyTokens` | `MotionScheme`, `MotionRole`, and the spring implementation own the Avalonia motion contract. |
| `TypefaceTokens`, `TypographyKeyTokens`, `TypographyTokens` | `TypographyScheme`, `TypeScale`, and the Roboto module own font family, weight, and text-style resolution. |
| `PrimaryNavigationTabTokens` | kept as a documented gap: its `ActiveIndicatorShape` is an inline `RoundedCornerShape(3.dp)`, while the current raw schema only represents a named `ShapeRole`. `TabDefaults` owns the explicit 3dp component policy. |

These ownership decisions are deliberate. A future reference-layer table is appropriate only when
it can be backed by an independent, stable reference owner; it must not mirror
`MaterialColorUtilities`, `ShapeScheme`, or `MotionScheme` merely to make the file count look like
AndroidX.

## Raw tables without a component policy yet

The following 27 raw tables are already transcribed, but no current component-owned policy
references them. They are the next candidates when the corresponding controls or variants are
implemented; they are not safe to delete because they preserve the upstream provenance needed for
that work. Button and ToggleButton raw tables are intentionally not listed: their C# adapters
consume them directly.

```text
BaselineButtonTokens             BottomAppBarTokens
DateInputModalTokens              DockedToolbarTokens
DragHandleTokens                  ExpandedListTokens
ExtendedFabLargeTokens            ExtendedFabMediumTokens
ExtendedFabPrimaryTokens          ExtendedFabSmallTokens
FabMenuBaselineTokens             FabSmallTokens
FilledAutocompleteTokens          FilledTonalButtonTokens
MenuTokens                        NavigationRailExpandedTokens
NavigationRailHorizontalItemTokens
OutlinedAutocompleteTokens        RevealListTokens
SegmentedMenuTokens               SplitButtonLargeTokens
SplitButtonMediumTokens           SplitButtonXLargeTokens
SplitButtonXSmallTokens           StandardMenuTokens
TimeInputTokens                   VibrantMenuTokens
```

The practical completion order is:

1. Add component-owned Defaults records and control adapters for the raw-only families that are already visible
   in the component coverage matrix (menu, bottom-app-bar, extended-FAB/menu, autocomplete, and
   date/time input/list variants).
2. Decide whether the schema should gain a first-class custom shape primitive before attempting
   to transcribe `PrimaryNavigationTabTokens`; do not encode its 3dp shape as a misleading named
   role.
3. Add reference tables only if a future colour/shape/motion owner explicitly exports a stable
   reference contract. Until then, the existing C# owners remain the single source of truth.

## Verification rule

The inventory is checked against the frozen AndroidX checkout, not against a moving upstream
branch. When the baseline changes, update the commit, counts, and the explicit exception tables in
the same change as the new transcriptions. The generator's filename/kind checks and cross-table
reference diagnostics then catch stale or unowned additions at build time.
