# MaterialSkiaShadow

`MaterialSkiaShadow` is a Skia-only Avalonia renderer for the point-light elevation shadow used by
Compose Multiplatform. Its behavior is pinned to Skia m150, Skiko `RenderNode::drawShadow`, and
Compose Desktop's `SkiaGraphicsContext.setLightingInfo` contract.

The first implementation supports one flat simple contour made from lines, quadratics, and conics.
It reproduces Skiko's GPU-as-extension build behavior, including the coarse non-Ganesh quadratic
path, convex and concave shadow rings, Skia's cache-compatible canonical spot-mesh placement, and
Skia's blur fallback when tessellation fails. The canonical placement is significant for low
elevations: generating a mesh at its final translation is mathematically equivalent, but produces
different float rounding and raster edge coverage from `SpotVerticesFactory`.

This covers Material rectangles, rounded rectangles, and circles. Cubics, perspective, tilted Z planes,
directional lights and non-Skia Avalonia backends are not supported
yet and fail explicitly.

The managed tessellator is adapted from these Skia m150 sources under Skia's BSD license:

- `src/utils/SkShadowTessellator.cpp`
- `src/utils/SkPolyUtils.cpp`
- `src/core/SkDrawShadowInfo.h/.cpp`
- `src/gpu/ganesh/geometry/GrPathUtils.cpp`
- `src/opts/SkRasterPipeline_opts.h`

No custom SkiaSharp native build is required. Rasterization remains in the repository-pinned
SkiaSharp 4.150.1 native library through `SKVertices` and `SKCanvas.DrawVertices`.

Renderer-level parity covers rectangles, rounded rectangles, circles, fractional pills, isolated
ambient/spot meshes, parent clipping, layer opacity, high elevation, and 1.5×/2× render scales.
Every elevation case also emits and compares the complete resolved shadow input contract. Raster
comparison treats a per-channel delta of one as Skia quantization noise; a delta above one fails.
