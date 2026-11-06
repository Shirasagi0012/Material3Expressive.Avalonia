# MaterialSkiaShadow

`MaterialSkiaShadow` draws Compose Multiplatform's point-light elevation shadows through
Avalonia's Skia backend, without modifying SkiaSharp or adding a native library. The existing
public API supports rectangles, rounded rectangles with individual corner radii, circles, and closed paths of lines, quadratics and conics through `ComposeShadowShape.FromPath(SKPath)`. This snapshots managed commands, preserving conic weights and releasing callers from native path lifetime requirements.

On a Ganesh GPU canvas, rectangles, circles and nearly simple rounded rectangles with circular
corners use the managed port of `SurfaceDrawContext::drawFastShadow`. As in Skia, this branch
requires a rect-preserving similarity transform: translation, uniform scale, axis reflection
or an axis-aligned quarter turn. It submits fixed meshes of 9–28 vertices per shadow component
and evaluates the native analytic falloff with `SKRuntimeEffect` and a shared 128×1 alpha lookup
texture. It does not tessellate the general contour or rasterize a shadow bitmap on the CPU.

Other GPU transforms and shapes use the general managed tessellator on the same GPU canvas.
Software canvases use that tessellator with Skia's raster Gaussian transfer. Both retain the
native blur fallback when contour construction fails. The geometric path preserves canonical
ambient/spot mesh placement, point-light occluder classification, simple-polygon sweep
validation, and ear clipping for transparent concave umbras. Canonical placement and the native
endpoint comparisons matter at low elevations: an equivalent geometric formula can change
float rounding, fallback selection and raster edge coverage.

Callers continue to supply DIP geometry. The renderer constructs physical-pixel outlines and
removes only density from the canvas transform, matching Core's recorded outline coordinates.
The general contour follows the locked Skiko artifact's non-Ganesh `handleQuad` branch
(control point, then endpoint), including its conic conversion and convex/concave classification.
This is an artifact-specific source branch, not a choice to approximate curves or a claim that
every raster backend lacks Ganesh. The analytic GPU branch above is separate.

Multiple contours use the existing whole-path blur fallback: the pinned native tessellator rejects a second Move verb. Cubics, perspective, tilted Z planes, directional lights and non-Skia backends remain outside the supported contract. Path inputs are local DIP, just like rounded rectangles.

## Source and framework adaptation

The algorithms are pinned to Skia m150 commit `1f14f1166a847cb0e148f981600bf1e3e6ca6e46`,
Skiko `RenderNode::drawShadow`, and Compose Desktop's `SkiaGraphicsContext.setLightingInfo`.
The shadow algorithms were also compared with Skia m152 commit
`2ca5fe6a8170e2f55f519d9de083be4dccc06e70` used by Skiko `0.152.0-alpha02`; the relevant
changes move includes without changing these algorithms. Rasterization uses the repository's
SkiaSharp `4.150.1` native library.

The managed adaptations follow these Skia sources under the included [BSD license](LICENSE.Skia.txt):

- `src/utils/SkShadowTessellator.cpp` and `src/utils/SkPolyUtils.cpp`
- `src/core/SkDrawShadowInfo.h/.cpp`
- `src/gpu/ganesh/SurfaceDrawContext.cpp`
- `src/gpu/ganesh/ops/ShadowRRectOp.cpp`
- `src/gpu/ganesh/GrShadowGeoProc.cpp`
- `src/opts/SkRasterPipeline_opts.h`

SkiaSharp exposes two vertex texture coordinates; the native third varying is constant per
draw and becomes a shader uniform. The shader multiplies premultiplied color by the native
coverage for the existing SrcOver draw contract. Native destination-space RGBA8888 color
quantization and the original vertex/index ordering are preserved. Skia's internal shadow
cache and specialized Ganesh draw-op batching are not exposed; this port uses `SKVertices`
and `SKCanvas.DrawVertices`, with a shared immutable falloff image/shader.

## Validation boundary

The earlier FAB port selected Ganesh subdivision and required every circle to tessellate.
On 2026-11-06, independent calls to the locked native artifact disproved that assumption:
quad shadows exactly match control/end-point polylines, and a 72 DIP circle at density 2
uses the native ambient blur fallback. `NativeShadowTests.kt` in VisualParity's `hostTest`
retains both checks without Avalonia or stored expected images. Module tests preserve both
coordinate routes for that fallback; all 8 module tests pass.

Current Windows raster captures (`surface-native-curve`) cover 55 real Surface cases:
26 equal, 29 within the existing tolerance, no differences/errors, all repeatable. They include
transparent low-elevation contours and eight static 16/40 DIP high-elevation cases. The latter
isolate failures previously seen during FAB elevation changes. Changing both the curve branch
and coordinate representation removes all 10 previously failing Surface cases. The decision
and alternatives are recorded in [DECISIONS §20](../../docs/DECISIONS.md#20-阴影坐标接入与固定原生曲线分支).
The 33 `fab-source-native-curve` cases have exact structured facts and no raster failures;
7 retain native repeatability failures. Selected Button/Chip draws exercise additional callers;
Chip's remaining double elevation sample differences are separate from its passing images.

The standalone probe counts below predate these corrections and have not been rerun. They
are historical evidence only, particularly for GPU paths changed by physical-pixel outlines.

Direct drawing was compared against native `ShadowUtils.drawShadow` in Skiko
`0.152.0-alpha02` using temporary probes outside the project, independently of the renderer
parity infrastructure under development:

- GPU: 115/115 cases pixel-exact on ANGLE/D3D11 with an NVIDIA RTX 4070 Laptop GPU.
- Raster: 111/115 cases pixel-exact; all 115 have a maximum per-channel difference of one or less.
- Polygon geometry: 1005/1005 convex/simple classifications, triangulation results and triangle
  index sequences match a C++ probe built from the pinned `SkPolyUtils.cpp`.
- Existing module tests: 7 passed.

Cases include isolated ambient/spot shadows, fractional positions, pills, nonuniform scale,
reflection, rotation, nonuniform corner radii, high/low elevations, clipping, opacity,
`SaveLayer`, colored translucent shadows, linear color space and 1×/1.5×/2× render scales.
These checks do not establish full Avalonia renderer parity or equivalence on other GPU backends.

A Release submission microbenchmark on that GPU measured approximately 15–29 µs per analytic
ambient+spot draw versus 38–51 µs for general meshes, and about 3.5–4.1 KB versus 8.9–42.4 KB
of managed allocations, across rectangles, rounded rectangles and pills. It used varying
elevations, warmed shaders, 2000 draws per sample, periodic flushes and four alternating samples.
This measures drawing submission overhead, not completed GPU time or application frame rate.
