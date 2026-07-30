# DEF-002 — Snackbar accessibility recommended timeout

Status: Deferred
Authorized by: User
Date: 2026-07-30
Affects: `M4-SNACKBAR-001`

Defer adapting Compose
`AccessibilityManager.calculateRecommendedTimeoutMillis` until Avalonia exposes an equivalent
platform accessibility preference or the library adopts a shared cross-platform timeout
abstraction. Implementing it now would require platform-specific preference plumbing and public
API design beyond SnackbarHost's queue, motion, and automation scope.

The delivered host retains the alpha25 Short, Long, and Indefinite defaults. Applications can
select `Indefinite` for content that must remain available, while the host already exposes its
message as a Polite live Pane with Invoke dismissal. This authorized platform gap does not block
`M4-SNACKBAR-001` completion; revisit it when an appropriate Avalonia or library-wide
accessibility-timeout source exists.
