# Material Shape for Avalonia

`androidx\graphics\graphics-shapes` ported to Avalonia.

This module is maintained in the Material3Expressive repository and referenced directly by its projects.
Package versions and publishing are described in the [root README](../../README.md#版本与发布).

## Features

- **Material 3 Expressive Shapes** Access the complete set of predefined Material 3 shapes, or customize your own shapes. 
- **Shape Morphing** Create smooth, high-performance animations that morph between any two arbitrary shapes.
- **Avalonia Integration**:
  - Generate standard Avalonia Geometry objects for custom use.
  - Use MaterialShapeView to render static shapes.
  - Use MorphShapeView to implement morphing animations between two shapes.

## Geometry conversion

`RoundedPolygon.ToGeometry()` and `Morph.ToGeometry(progress)` mirror androidx's
`compose/material3/internal/ShapeUtil.kt`, including its `startAngle`, `repeatPath` and
`closePath` parameters.

Two deviations, both deliberate:

- **`startAngle` defaults to `0` here, not `270`.** Upstream's `toPath` is internal and every
  caller re-centres the result, so its default is harmless there. On a public API a non-zero
  default would silently rotate — and, since the rotation is about the origin rather than the
  shape's centre, translate — every shape drawn through it. Pass
  `AvaloniaExtensions.StartAngleTop` when porting material3 drawing code.
- **The rotation is applied about the origin**, exactly as upstream does; the pivot only decides
  where the first anchor's angle is measured from.

## Known limitations

- `MorphShapeView` builds a fresh `StreamGeometry` for every `Progress` change, i.e. once per
  frame while morphing. This is not an oversight: Avalonia 12's `StreamGeometry` has no rewind —
  `_strokePath` is readonly and re-`Open()`ing *appends* — so a geometry cannot be rebuilt in
  place. Avoiding the per-frame allocation means dropping to a custom draw operation with a
  reused `SKPath`, which trades away the non-Skia backends.

## Gallery

You can explore the capabilities and play with different Material Shapes by building and running the included Gallery App.

<img width="656" height="1035" alt="MaterialShape.Gallery Demo" src="https://github.com/user-attachments/assets/ea687ad3-3101-4d48-8ed7-ae572550dafd" />
