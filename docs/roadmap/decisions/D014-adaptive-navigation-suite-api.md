# D014 — Adaptive navigation suite XAML API

Status: Accepted
Date: 2026-07-31
Applies to: M5-NAV-003

AndroidX `NavigationSuiteScaffold` owns responsive component selection, body placement, optional
primary action placement, visibility state, and a movable destination declaration. Avalonia maps
that contract to `MaterialNavigationSuiteScaffold` in the existing
`Shirasagi0012.Material3Expressive.Avalonia` assembly and `Material3Expressive.Controls`
namespace, as required by D001.

`LayoutType=Auto` uses the upstream window-size rule: width below 600dp resolves to
`ShortNavigationBarCompact`; otherwise height below 480dp resolves to
`ShortNavigationBarMedium`; all other windows resolve to `WideNavigationRailCollapsed`.
`ShortNavigationBarCompact`, `ShortNavigationBarMedium`, `WideNavigationRailCollapsed`,
`WideNavigationRailExpanded`, and `None` are also explicit values. The legacy
NavigationBar/NavigationRail/NavigationDrawer aliases are intentionally omitted because the
upstream suite recommends the new short/wide types and this task's dependencies implement those
types.

The scaffold owns one explicit `Items` collection of real `MaterialNavigationItem` controls and a
two-way `SelectedIndex`. It moves those same instances between one internal
`MaterialShortNavigationBar` and one internal `MaterialWideNavigationRail`; it never projects
duplicate destination controls. Compact maps items to Top, medium and expanded rail to Start, and
collapsed rail to Top. Body content remains the inherited implicit `Content` slot.

`PrimaryAction` is placed 16dp above a horizontal bar at logical end and becomes the wide rail
header. `VerticalArrangement` passes through to the rail. `IsNavigationVisible` is two-way and
uses the repository DefaultSpatial spring; `NavigationVisibilityProgress` is read-only.
Breakpoint calculation remains internal, while the resolved result is observable through
read-only `ResolvedLayoutType`. Applications with a fold posture or custom policy set an explicit
`LayoutType`; no desktop-global posture service or new adaptive namespace is introduced.

Changing the resolved type is an atomic responsive re-layout, matching Compose's type selection;
show/hide and wide-rail expansion retain their respective springs. Selection, destination
instances, enabled state, automation identity, and focused destination are retained. The scaffold
adds only a neutral Pane automation node; the active Tab/TabItem navigation subtree and body
descendants preserve their established providers.
