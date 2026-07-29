# D009 — MaterialScaffold layout API

Status: Accepted
Date: 2026-07-30

`MaterialScaffold` exposes `TopBar`, `BottomBar`, `SnackbarHost`, `FloatingActionButton`, inherited
body `Content`, and `ScaffoldFabPosition`. Read-only bindable `ContentPadding` is caller-applied
layout output. Null `ContentWindowInsets` observes the current `TopLevel` safe area; explicit
`Thickness`, including zero, overrides it. Snackbar queue and timeout state remain outside this
layout control.
