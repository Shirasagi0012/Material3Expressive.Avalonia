# Transition preparation lease

`Transition.BeginPreparation()` and `AnimatedVisibilityScope.BeginPreparation()` reserve
completion while layout/pairing data is unavailable. This replaces duration-based placeholder
animations. A lease has no animation value, does not appear in `Animations`, and contributes
nothing to `TotalDuration`.

```csharp
using var preparation = outgoingScope.BeginPreparation();
// Acquire before changing visibility; otherwise Exit=None may already have completed.
outgoingHost.Visible = false;

await PrepareTargetLayoutAsync(cancellationToken); // Resume on the owning UI thread.
if (!preparation.IsActive) return; // Detached, disposed, or superseded by manual seeking.

// Register the real shared animation on the visibility Transition before leaving this block.
// The lease's Dispose releases only preparation; registered animations retain their own lifetime.
```

## Completion and ownership

- Each lease is independent. All leases on a branch must be released before that branch and
  its ancestors can complete. `IsPreparing` includes descendants and notifies ancestors.
- Acquisition marks the branch running immediately, including when current equals target.
  Existing animations and the normal frame clock keep advancing. This is a completion gate,
  not a timeline pause; a root running only for preparation continues requesting frames.
- Release does not synchronously complete or tear down content. The next frame evaluates the
  branch, including real animations registered during preparation. Late registration uses the
  existing Transition timeline semantics.
- Zero-duration animations and `DurationScale=0` do not bypass preparation. Leases have no
  timeout. The presentation coordinator owns any timeout, pairing failure, and fallback policy.
- Release is idempotent and must occur on the owning clock thread. Dispose the lease in
  `using`/`finally` on cancellation or failure. Cancelling `RunAsync` stops playback only; it
  does not revoke caller-owned leases. Disposing the transition revokes all descendant leases.
- The visibility scope owns its Transition, so detach, source replacement, or scope teardown
  revoke reservations. A delayed callback must check `IsActive` before touching its old scope.
- Target reversal does not revoke preparation. The coordinator releases superseded work;
  generation checks are still needed when an old async request races a newer one.

## Manual timeline operations

Entering manual seeking revokes outstanding preparations throughout the affected tree.
Acquiring during seeking throws `InvalidOperationException`. Explicit `SnapToAsync` also
revokes preparations, including a snap to the already-current state. Seekable animation's
entry into manual timeline mode follows the same policy. No fake duration participates in
fraction calculation, and no pending layout callback can hold an explicit timeline operation.

This policy does not provide layout preparation during interactive seeking: a future shared
transition coordinator must prepare geometry before seeking, or discard/rebuild its pending
session when the lease is revoked. Completed/revoked leases are never reused.

## Avalonia integration and validation

AnimatedVisibility's initial layout gate uses the same lease; measured readiness remains
separate so a seeking preview still waits for actual measurement. Its idle/disposal checks
also account for `IsPreparing`. `Exit=None` therefore keeps live content while a shared
transition reservation is outstanding.

The SharedTransitionProbe now transfers its lease to real bounds/child-arc animations registered
on the exiting scope. Window callbacks observe target layout and read Runtime samples. Scope
cleanup commits the final drawing handoff before the source content is removed. See
[Shared bounds integration](SHARED_BOUNDS_RUNTIME.md) for the protocol and validation limits.

Regression tests cover nested/multiple reservations, long waits, zero duration scale, late
animation registration, continued existing animation, reversal, disposal/detach, manual seek
and snap, reentrant release, frame-loop release, and cancellation cleanup. Render cost remains
separate work; bounds retargeting now has numerical continuity and lifecycle regression coverage.
