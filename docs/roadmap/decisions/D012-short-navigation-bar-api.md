# D012 — ShortNavigationBar XAML API

Status: Accepted
Date: 2026-07-31
Applies to: M5-NAV-001

AndroidX exposes a `ShortNavigationBar` container and `ShortNavigationBarItem` composable, but its
item is the same destination selection concept already represented by Avalonia's
`MaterialNavigationItem`. Avalonia therefore adds `MaterialShortNavigationBar` and reuses
`MaterialNavigationItem` rather than creating a second item type. `IconPosition=Top|Start` is added
to the shared item and remains explicit per destination, matching the upstream parameter.

`Arrangement=EqualWeight|Centered` maps the two upstream policies. Avalonia also adds `Auto` as a
narrow desktop/adaptive convenience: constrained widths below `ResponsiveBreakpoint` use
EqualWeight and widths at or above it use Centered. The default breakpoint is the Material 600dp
compact-to-medium boundary and is configurable in XAML. `ResolvedArrangement` is a read-only
output. Changing size or arrangement never replaces item instances and `SelectedIndex` remains the
single two-way selection state.

The container is a UI Automation Tab selection provider and its real item controls are TabItem
selection providers. Horizontal arrows wrap in visual/logical order, reverse in RTL, select the
focused destination, and Home/End select the endpoints. Ordinary Tab traversal is not intercepted.
Responsive calculations and item-count padding remain internal implementation details.
