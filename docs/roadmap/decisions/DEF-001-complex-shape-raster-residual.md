# DEF-001 — Complex-shape raster residual

Status: Deferred
Authorized by: User
Date: 2026-07-29
Renewed: 2026-07-31
Affects: `progress/circular-wavy/*`

The renderer-difference warning is limited to the `complex-shape` expected-edge profile and is
capped below 100 high-delta pixels. It uses no whole-frame mask, ignored region, or structural
relaxation. Review when the tolerance policy is revisited. This does not make a component
otherwise complete.

The user renewed this authorization after D004 accessibility, dark coverage, and indeterminate
coverage were completed. It now explicitly covers all seven supported rows:

- `progress/circular-wavy/determinate/light`
- `progress/circular-wavy/determinate/dark`
- `progress/circular-wavy/indeterminate/start/light`
- `progress/circular-wavy/indeterminate/start/dark`
- `progress/circular-wavy/motion/start/light`
- `progress/circular-wavy/motion/half-wave/light`
- `progress/circular-wavy/motion/one-wave/light`

The renewal changes no profile, threshold, mask, ignored region, or structural/timeline contract.
It is the final disposition of the renderer residual for this roadmap program; future renderer or
comparison-policy work should reevaluate the Deferred record rather than silently broadening it.
