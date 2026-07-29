# DEF-001 — Complex-shape raster residual

Status: Deferred  
Authorized by: User  
Date: 2026-07-29  
Affects: `progress/circular-wavy/*`

The renderer-difference warning is limited to the `complex-shape` expected-edge profile and is
capped below 100 high-delta pixels. It uses no whole-frame mask, ignored region, or structural
relaxation. Review when the tolerance policy is revisited. This does not make a component
otherwise complete and does not replace the open D004 accessibility decision.
