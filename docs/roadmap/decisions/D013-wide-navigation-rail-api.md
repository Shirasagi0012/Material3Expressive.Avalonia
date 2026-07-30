# D013 — WideNavigationRail XAML API

Status: Accepted
Date: 2026-07-31
Applies to: M5-NAV-002

AndroidX separates `WideNavigationRailState`, the rail composable, and each item composable.
Avalonia represents the same hoisted state as the two-way `MaterialWideNavigationRail.IsExpanded`
styled property and exposes its spring value through read-only `ExpansionProgress`. This follows
the library's bindable-control convention without introducing a coroutine-shaped public state
object for one Boolean value.

The control owns a direct `Items` collection of existing `MaterialNavigationItem` controls and a
two-way `SelectedIndex`; expansion never recreates destinations or selection. Items default to
following the upstream state mapping, Collapsed→Top and Expanded→Start.
`FollowWideRailExpansion=False` preserves a caller's explicit `IconPosition`. `Header` is an
explicit optional slot and `Arrangement=Top|Center|Bottom` maps the three relevant upstream
vertical arrangements.

The rail implements the fixed 96dp collapsed width, intrinsic 220–360dp expanded width, 44dp
vertical padding, 40dp header gap, 64/56dp item minimum heights, and 4/0dp item spacing. It does
not choose an expansion breakpoint itself: M5-NAV-003 owns window-size coordination and may bind
`IsExpanded` without replacing this control.

Desktop keyboard behavior uses Up/Down plus Home/End to focus and select enabled destinations;
Tab remains ordinary traversal. UI Automation exposes a Tab + ISelectionProvider with stable real
TabItem + ISelectionItemProvider children and also exposes ExpandCollapse on the rail. Header
controls retain their native peer and precede destinations in traversal order.
