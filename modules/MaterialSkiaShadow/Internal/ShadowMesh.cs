/*
 * Copyright 2017 Google LLC
 *
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE file.
 *
 * Managed adaptation of Skia m150 SkShadowTessellator.
 */

using SkiaSharp;

namespace MaterialSkiaShadow.Internal;

internal sealed class ShadowMesh
{
    public List<SKPoint> Positions { get; } = [];

    public List<SKColor> Colors { get; } = [];

    public List<ushort> Indices { get; } = [];

    public bool IsEmpty => Positions.Count == 0 || Indices.Count == 0;

    public SKVertices ToVertices() => SKVertices.CreateCopy(
        SKVertexMode.Triangles,
        Positions.ToArray(),
        null,
        Colors.ToArray(),
        Indices.ToArray());
}
