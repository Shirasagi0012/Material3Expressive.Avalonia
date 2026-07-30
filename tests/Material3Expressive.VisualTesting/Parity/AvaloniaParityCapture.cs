using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Rendering;
using MaterialSkiaShadow;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>Captures Avalonia pixels and declared node/text layout in the CMP contract coordinate space.</summary>
public sealed class AvaloniaParityCapture
{
    private readonly VisualRenderer _renderer = new();

    public ParityActualCapture Capture(VisualCase source, ParityCaptureResult expected)
    {
        EnsureCanonicalRoboto();
        var physical = source with
        {
            Viewport = new PixelSize(
                checked((int)(source.Viewport.Width * expected.CoordinateSpace.Scale)),
                checked((int)(source.Viewport.Height * expected.CoordinateSpace.Scale))),
            RenderScaling = expected.CoordinateSpace.Scale,
            Culture = "en-US",
            FlowDirection = FlowDirection.LeftToRight
        };

        var replay = expected.Motion is null
            ? null
            : new VisualMotionReplay(
                expected.Motion.Origin,
                expected.Motion.TimeUnit,
                expected.Motion.DurationScale,
                expected.Motion.DeliveryFrameTicks,
                expected.Motion.CaptureFrameIndex,
                expected.Motion.ScheduleSha256);

        return _renderer.Render(physical, replay, (context, image) =>
        {
            var nodes = CaptureNodes(context, expected.Nodes, physical.RenderScaling);
            var layout = new ParityCaptureResult(
                ParityCaptureResult.CurrentSchemaVersion,
                expected.CaseId,
                new ParityCoordinateSpace("physical-pixel", "root-top-left", "right", "down", (float)physical.RenderScaling),
                new ParityViewport(image.Width, image.Height),
                nodes,
                CaptureMotion(physical, context));
            return new ParityActualCapture(image, layout);
        });
    }

    private static ParityMotionCapture? CaptureMotion(VisualCase source, VisualContext context)
    {
        if (source.Motion is not { } motion)
            return null;

        var delivered = context.DeliveredMotionFrameTicks
            ?? throw new InvalidOperationException($"Avalonia parity scene '{source.Id}' did not record delivered motion frames.");
        var captureIndex = context.MotionCaptureFrameIndex
            ?? throw new InvalidOperationException($"Avalonia parity scene '{source.Id}' did not record its capture frame.");
        if (captureIndex < 0 || captureIndex >= delivered.Count)
            throw new InvalidOperationException($"Avalonia parity scene '{source.Id}' recorded an invalid capture frame.");

        return new ParityMotionCapture(
            ParityMotionCapture.CurrentSchemaVersion,
            motion.ScenarioId,
            motion.CheckpointId,
            "first-controlled-motion-frame",
            "100ns-ticks",
            1,
            delivered[captureIndex],
            [new ParityMotionEvent(0, motion.TriggerAction)],
            delivered,
            captureIndex,
            ParityMotionTimeline.ScheduleSha256(delivered),
            motion.ClockCoverage);
    }

    private static void EnsureCanonicalRoboto()
    {
        FontEnvironment.RequirePinnedRobotoFaces();
    }

    private static IReadOnlyList<ParityNode> CaptureNodes(
        VisualContext context,
        IReadOnlyList<ParityNode> expected,
        double scale)
    {
        // Popup children retain a logical owner while living in an overlay visual root. Include
        // both trees so exposed menus can contract their item text without weakening geometry or
        // raster evidence.
        var rootControls = context.Root.GetVisualDescendants()
            .OfType<Control>()
            .Concat(context.Root.GetLogicalDescendants().OfType<Control>())
            .Distinct()
            .ToArray();
        var popupControls = rootControls
            .OfType<Popup>()
            .SelectMany(popup => popup.Child is null
                ? []
                : popup.Child.GetVisualDescendants()
                    .OfType<Control>()
                    .Prepend(popup.Child)
                    .Concat(popup.Child.GetLogicalDescendants().OfType<Control>()))
            .Distinct();
        var allControls = rootControls
            .Concat(popupControls)
            .Distinct()
            .ToArray();
        return expected.Select(node =>
        {
            var control = ResolveControl(node, context.Content, allControls);
            if (control is null && node.Id == "label" && node.TextLayout is not null)
            {
                var textField = allControls.OfType<MaterialTextField>().SingleOrDefault();
                var probe = textField?.GetLabelParityProbe();
                if (textField is not null && probe is not null)
                    return CaptureTextFieldLabel(node, context.Root, textField, probe, scale);

                var dropdown = allControls.OfType<MaterialExposedDropdownMenu>().SingleOrDefault();
                var dropdownProbe = dropdown?.GetLabelParityProbe();
                if (dropdown is not null && dropdownProbe is not null)
                    return CaptureTextFieldLabel(node, context.Root, dropdown, dropdownProbe, scale);
            }
            if (control is null && TryCaptureGeometryProbe(node, context.Root, allControls, scale, out var geometryProbe))
                return geometryProbe;
            if (control is null)
                return node with { BoundsInRootPx = null, Visible = false, TextLayout = null };

            var bounds = BoundsInRoot(context.Root, control, scale);
            var text = control as TextBlock;
            return new ParityNode(
                node.Id,
                node.Kind,
                node.ParentId,
                bounds,
                control.IsVisible,
                text is null ? null : CaptureText(text, context.Root, scale),
                CaptureFocusedState(node, control, allControls),
                ResolveShadowProbe(control) is { } shadowProbe
                    ? CaptureShadow(context.Root, control, shadowProbe, scale)
                    : null);
        }).ToArray();
    }

    private static IParityShadowProbe? ResolveShadowProbe(Control control) => control switch
    {
        IParityShadowProbe probe => probe,
        // A resting ListItem uses 0dp elevation and has no shadow contract in either capture.
        // Keep the wrapper limited to the positive animated elevation that InteractiveListItem
        // passes to graphicsLayer during a drag.
        MaterialListItem listItem when listItem.ParityShadowParameters.Elevation > 0 =>
            new MaterialListItemShadowProbe(listItem),
        MaterialBottomSheetScaffoldSheet sheet =>
            new MaterialBottomSheetScaffoldShadowProbe(sheet),
        MaterialSnackbar snackbar =>
            new MaterialSnackbarShadowProbe(snackbar),
        _ => null
    };

    // MaterialListItem belongs to the production assembly, so it cannot directly implement the
    // visual-testing-only interface. The internal values below are the exact MaterialContainer
    // inputs used for its shadow render; AssemblyInfo grants this test project read-only access.
    private sealed class MaterialListItemShadowProbe(MaterialListItem listItem) : IParityShadowProbe
    {
        public ComposeShadowShape ShadowShape => listItem.ParityShadowShape;

        public ComposeShadowParameters ShadowParameters => listItem.ParityShadowParameters;

        public bool OccluderDrawn => listItem.ParityShadowOccluderDrawn;
    }

    private sealed class MaterialBottomSheetScaffoldShadowProbe(
        MaterialBottomSheetScaffoldSheet sheet) : IParityShadowProbe
    {
        public ComposeShadowShape ShadowShape => sheet.ParityShadowShape;

        public ComposeShadowParameters ShadowParameters => sheet.ParityShadowParameters;

        public bool OccluderDrawn => sheet.ParityShadowOccluderDrawn;
    }

    private sealed class MaterialSnackbarShadowProbe(MaterialSnackbar snackbar) : IParityShadowProbe
    {
        public ComposeShadowShape ShadowShape => snackbar.ParityShadowShape;

        public ComposeShadowParameters ShadowParameters => snackbar.ParityShadowParameters;

        public bool OccluderDrawn => snackbar.ParityShadowOccluderDrawn;
    }

    private static bool TryCaptureGeometryProbe(
        ParityNode expected,
        Control root,
        IReadOnlyList<Control> allControls,
        double scale,
        out ParityNode capture)
    {
        foreach (var control in allControls.Prepend(root).Distinct())
        {
            if (control is not IVisualBoundsProbe probe || !probe.TryGetVisualBounds(expected.Id, out var localBounds))
                continue;

            var transform = control.TransformToVisual(root);
            if (transform is null)
                continue;

            var shadow = expected.Shadow is { } declaredShadow
                && control is MaterialExposedDropdownMenu
                && allControls.OfType<MaterialSurface>()
                    .SingleOrDefault(surface => surface.Name == "PART_MenuSurface") is { } menuSurface
                    ? CapturePopupShadowDeclaration(declaredShadow, menuSurface, scale)
                    : null;

            capture = new ParityNode(
                expected.Id,
                expected.Kind,
                expected.ParentId,
                TransformRectToPhysicalRoot(localBounds, transform.Value, scale),
                control.IsVisible,
                null,
                null,
                shadow);
            return true;
        }

        capture = default!;
        return false;
    }

    private static ParityShadow CapturePopupShadowDeclaration(
        ParityShadow declared,
        MaterialSurface surface,
        double scale)
    {
        // Compose's manual popup probe reports the graphics-layer layout slot and overlay clip,
        // neither of which Avalonia exposes through TransformToVisual across PopupRoot. Preserve
        // those reference coordinate fields while deriving every component-owned shadow input
        // from the mounted MaterialSurface.
        var corners = surface.CornerRadius;
        return declared with
        {
            ShapeBoundsLocalDp = new ParityRect(
                0,
                0,
                (float)surface.Bounds.Width,
                (float)surface.Bounds.Height),
            CornerRadiiDp = new ParityCornerRadii(
                (float)corners.TopLeft,
                (float)corners.TopRight,
                (float)corners.BottomRight,
                (float)corners.BottomLeft),
            ElevationPx = (float)(surface.ShadowElevation * scale),
            Opacity = 1,
            OccluderDrawn = true
        };
    }

    private static ParityShadow CaptureShadow(
        Control root,
        Control control,
        IParityShadowProbe probe,
        double scale)
    {
        var transform = control.TransformToVisual(root)
            ?? throw new InvalidOperationException("Shadow probe is not mounted in the capture root.");
        var shape = probe.ShadowShape;
        var parameters = probe.ShadowParameters;
        var lighting = ComposeShadowLighting.ForViewport(root.Bounds.Size, scale);
        var resolved = ComposeShadowContract.Resolve(shape, parameters, lighting, scale);
        var corners = shape.Corners;
        var localBounds = shape.Bounds;
        var deviceBounds = TransformRectToPhysicalRoot(localBounds, transform, scale);
        var physicalTransform = new ParityAffineTransform(
            (float)(transform.M11 * scale),
            (float)(transform.M12 * scale),
            (float)(transform.M21 * scale),
            (float)(transform.M22 * scale),
            (float)(transform.M31 * scale),
            (float)(transform.M32 * scale));
        var clipBounds = FindParentClipBounds(root, control, scale);
        var flags = resolved.TransparentOccluder
            ? new[] { "transparentOccluder" }
            : Array.Empty<string>();

        return new ParityShadow(
            ComposeShadowContract.SkiaBehaviorVersion,
            ClassifyShape(shape),
            new ParityRect(
                (float)localBounds.Left,
                (float)localBounds.Top,
                (float)localBounds.Right,
                (float)localBounds.Bottom),
            deviceBounds,
            new ParityCornerRadii(
                (float)corners.TopLeft,
                (float)corners.TopRight,
                (float)corners.BottomRight,
                (float)corners.BottomLeft),
            physicalTransform,
            resolved.ElevationPx,
            FormatArgb(resolved.AmbientColor),
            FormatArgb(resolved.SpotColor),
            lighting.AmbientAlpha,
            lighting.SpotAlpha,
            resolved.Opacity,
            resolved.TransparentOccluder,
            new ParityPoint3(lighting.LightX, lighting.LightY, lighting.LightZ),
            lighting.LightRadius,
            flags,
            clipBounds,
            probe.OccluderDrawn);
    }

    private static string ClassifyShape(ComposeShadowShape shape)
    {
        var corners = shape.Corners;
        if (corners == default)
            return "rectangle";
        var radius = shape.Bounds.Width / 2;
        if (Math.Abs(shape.Bounds.Width - shape.Bounds.Height) <= 1e-6
            && Math.Abs(corners.TopLeft - radius) <= 1e-6
            && Math.Abs(corners.TopRight - radius) <= 1e-6
            && Math.Abs(corners.BottomRight - radius) <= 1e-6
            && Math.Abs(corners.BottomLeft - radius) <= 1e-6)
            return "circle";
        return "roundedRectangle";
    }

    private static ParityRect? FindParentClipBounds(
        Control root,
        Control control,
        double scale)
    {
        for (var ancestor = control.GetVisualParent(); ancestor is not null && ancestor != root; ancestor = ancestor.GetVisualParent())
        {
            if (ancestor is Control parent && parent.ClipToBounds)
                return BoundsInRoot(root, parent, scale);
        }
        return null;
    }

    private static string FormatArgb(Color color) => color.A == 0
        ? "#00000000"
        : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static Control? ResolveControl(
        ParityNode expected,
        Control content,
        IReadOnlyList<Control> all)
    {
        var named = all.Prepend(content).FirstOrDefault(item => item.Name == expected.Id);
        if (named is not null)
            return named;
        var generatedListItemText = ResolveGeneratedListItemStringSlot(expected, all);
        if (generatedListItemText is not null)
            return generatedListItemText;
        if (expected.TextLayout is null)
            return expected.Id == "component" ? content : null;

        // Text parity is intentionally opt-in through a stable node name. The sole template alias
        // is the declared TextField supporting-text part; its label has a dedicated control probe.
        if (expected.Id == "supporting")
            return all.FirstOrDefault(item => item.Name == "PART_Supporting");
        return null;
    }

    private static TextBlock? ResolveGeneratedListItemStringSlot(
        ParityNode expected,
        IReadOnlyList<Control> all)
    {
        if (expected.TextLayout is null || expected.Id is not ("headline" or "supporting" or "trailing" or "overline"))
            return null;

        // A ContentPresenter creates the TextBlock only after its parent ListItem is styled, so
        // Avalonia forbids setting StyledElement.Name during VisualCase.Prepare. For a declared
        // string slot, both the slot value and expected text are stable parity inputs; resolve
        // the generated TextBlock from those exact facts and reject ambiguity rather than using
        // template order or a broad text-tree heuristic.
        var candidates = all.OfType<MaterialListItem>()
            .Where(item => string.Equals(
                ListItemStringSlot(item, expected.Id),
                expected.TextLayout.Text,
                StringComparison.Ordinal))
            .ToArray();
        if (candidates.Length == 0)
            return null;
        if (candidates.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected one MaterialListItem string slot for parity node '{expected.Id}', found {candidates.Length}.");
        }

        var textBlocks = candidates[0].GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(textBlock => string.Equals(textBlock.Text, expected.TextLayout.Text, StringComparison.Ordinal))
            .ToArray();
        return textBlocks.Length switch
        {
            1 => textBlocks[0],
            _ => throw new InvalidOperationException(
                $"Expected one generated TextBlock for ListItem parity node '{expected.Id}', found {textBlocks.Length}.")
        };
    }

    private static string? ListItemStringSlot(MaterialListItem item, string nodeId) => nodeId switch
    {
        "headline" => item.Content as string,
        "supporting" => item.SupportingContent as string,
        "trailing" => item.TrailingContent as string,
        "overline" => item.OverlineContent as string,
        _ => null
    };

    private static ParityRect? BoundsInRoot(Control root, Control control, double scale)
    {
        var transform = control.TransformToVisual(root);
        if (transform is null)
            return null;
        return TransformRectToPhysicalRoot(
            new Rect(control.Bounds.Size),
            transform.Value,
            scale);
    }

    private static ParityNode CaptureTextFieldLabel(
        ParityNode expected,
        Control root,
        Control textField,
        MaterialTextFieldLabelProbe probe,
        double scale)
    {
        var transform = textField.TransformToVisual(root)
            ?? throw new InvalidOperationException("MaterialTextField label probe is not mounted.");
        var bounds = ComposeSnappedTextLayoutSlotBounds(
            new Rect(probe.Origin, new Size(probe.Layout.Width, probe.Layout.Height)),
            transform,
            scale);
        return new ParityNode(
            expected.Id,
            expected.Kind,
            expected.ParentId,
            bounds,
            textField.IsVisible,
            CaptureTextLayout(
                probe.Layout,
                expected.TextLayout!.Text,
                probe.Origin,
                transform,
                scale,
                styleSignature: textField is MaterialSecureTextField
                    or MaterialDateInput
                    or MaterialExposedDropdownMenu
                    ? CaptureTextStyleSignature(
                        probe.Typeface,
                        probe.FontSize,
                        probe.LineHeight,
                        probe.LetterSpacing,
                        scale)
                    : null,
                paintSignature: CaptureTextPaintSignature(probe.Foreground)));
    }

    private static bool? CaptureFocusedState(
        ParityNode expected,
        Control control,
        IReadOnlyList<Control> allControls)
    {
        if (expected.Focused is null)
            return null;

        // MaterialTextField delegates focus to its internal editor. Its outer control is
        // deliberately non-focusable, so Control.IsFocused would not describe the same semantic
        // state as Compose's interactionSource.collectIsFocusedAsState().
        if (control is MaterialTextField)
            return allControls.OfType<TextBox>()
                .SingleOrDefault(item => item.Name == "PART_TextBox")
                ?.IsFocused;

        return control.IsFocused;
    }

    private static ParityRect ComposeSnappedTextLayoutSlotBounds(
        Rect rawLayoutBounds,
        Matrix transform,
        double scale)
    {
        var rawBounds = TransformRectToPhysicalRoot(rawLayoutBounds, transform, scale);

        // Modifier.onGloballyPositioned reports Compose Text's integer layout slot (the
        // TextLayoutResult IntSize), while line and character data is intentionally floating
        // point. The custom Avalonia text-field label has no framework layout node, so expose the
        // equivalent minimum enclosing physical-pixel slot here instead of incorrectly using its
        // floating TextLayout.Width as the node bounds.
        return new ParityRect(
            MathF.Floor(rawBounds.Left),
            MathF.Floor(rawBounds.Top),
            MathF.Ceiling(rawBounds.Right),
            MathF.Ceiling(rawBounds.Bottom));
    }

    private static ParityTextLayout? CaptureText(
        TextBlock textBlock,
        Control root,
        double scale)
    {
        if (textBlock.TextLayout is not TextLayout layout)
            return null;
        var transform = textBlock.TransformToVisual(root);
        if (transform is null)
            return null;
        var drawOrigin = GetTextDrawOrigin(textBlock, layout);
        return CaptureTextLayout(
            layout,
            textBlock.Text ?? string.Empty,
            drawOrigin,
            transform.Value,
            scale,
            CaptureTextStyleSignature(textBlock, scale),
            CaptureTextPaintSignature(textBlock.Foreground));
    }

    private static ParityTextLayout CaptureTextLayout(
        TextLayout layout,
        string text,
        Point drawOrigin,
        Matrix transform,
        double scale,
        ParityTextStyleSignature? styleSignature,
        ParityTextPaintSignature? paintSignature)
    {
        var drawOriginInRoot = TransformPointToPhysicalRoot(drawOrigin, transform, scale);
        var lineTop = 0d;
        var textOffset = 0;
        var lines = new List<ParityTextLine>();
        foreach (var line in layout.TextLines)
        {
            var top = lineTop;
            lineTop += line.Height;
            var start = textOffset;
            var end = Math.Min(text.Length, start + line.Length - line.NewLineLength);
            var localBounds = new Rect(line.Start, top, line.Width, line.Height);
            var rootBounds = TransformRectToPhysicalRoot(Offset(localBounds, drawOrigin), transform, scale);
            var localBaseline = top + line.Baseline;
            var rootBaseline = TransformPointToPhysicalRoot(
                new Point(drawOrigin.X + line.Start, drawOrigin.Y + localBaseline),
                transform,
                scale).Y;
            lines.Add(new ParityTextLine(
                lines.Count, start, end, end,
                rootBounds.Left,
                rootBounds.Right,
                rootBounds.Top,
                rootBounds.Bottom,
                rootBaseline,
                false,
                ScaleRect(localBounds, scale),
                (float)(localBaseline * scale)));
            textOffset += line.Length;
        }
        var characters = Enumerable.Range(0, text.Length).Select(offset =>
        {
            var rect = layout.HitTestTextPosition(offset);
            var line = lines.LastOrDefault(item => offset >= item.StartUtf16 && offset < item.EndUtf16)?.Index ?? 0;
            var localBounds = new Rect(rect.X, rect.Y, rect.Width, rect.Height);
            return new ParityCharacterBox(
                offset,
                ((int)text[offset]).ToString("X4"),
                line,
                TransformRectToPhysicalRoot(Offset(localBounds, drawOrigin), transform, scale),
                ScaleRect(localBounds, scale));
        }).ToArray();
        var first = lines.FirstOrDefault()?.BaselinePx ?? drawOriginInRoot.Y;
        var last = lines.LastOrDefault()?.BaselinePx ?? drawOriginInRoot.Y;
        return new ParityTextLayout(
            text,
            "utf-16",
            first,
            last,
            lines.Count,
            false,
            lines,
            characters,
            drawOriginInRoot,
            lines.FirstOrDefault()?.BaselineInTextPx,
            lines.LastOrDefault()?.BaselineInTextPx,
            styleSignature,
            paintSignature);
    }

    private static Point GetTextDrawOrigin(TextBlock textBlock, TextLayout layout)
    {
        var padding = textBlock.Padding;
        if (textBlock.UseLayoutRounding)
        {
            var layoutScale = LayoutHelper.GetLayoutScale(textBlock);
            padding = LayoutHelper.RoundLayoutThickness(padding, layoutScale);
        }

        var top = padding.Top;
        if (textBlock.Bounds.Height < layout.Height)
        {
            top += textBlock.VerticalAlignment switch
            {
                VerticalAlignment.Center => (textBlock.Bounds.Height - layout.Height) / 2,
                VerticalAlignment.Bottom => textBlock.Bounds.Height - layout.Height,
                _ => 0
            };
        }

        return new Point(padding.Left, top);
    }

    private static ParityTextStyleSignature CaptureTextStyleSignature(TextBlock textBlock, double scale)
        => CaptureTextStyleSignature(
            new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch),
            textBlock.FontSize,
            textBlock.LineHeight,
            textBlock.LetterSpacing,
            scale);

    private static ParityTextStyleSignature CaptureTextStyleSignature(
        Typeface typeface,
        double fontSize,
        double lineHeight,
        double letterSpacing,
        double scale)
    {
        var requestedFontFamily = typeface.FontFamily.Name;
        var resolvedFontFamily = requestedFontFamily;
        GlyphTypeface? glyphTypeface = null;
        try
        {
            glyphTypeface = typeface.GlyphTypeface;
            resolvedFontFamily = string.IsNullOrWhiteSpace(glyphTypeface.TypographicFamilyName)
                ? glyphTypeface.FamilyName
                : glyphTypeface.TypographicFamilyName;
        }
        catch (InvalidOperationException)
        {
            // The shaped TextLayout remains the source of truth if the platform cannot expose a
            // glyph typeface. Keep the requested family so the comparison stays diagnostic.
        }

        var pinnedRobotoHash = glyphTypeface is null
            ? null
            : FontEnvironment.GetCanonicalRobotoHash(glyphTypeface);
        return new ParityTextStyleSignature(
            requestedFontFamily,
            resolvedFontFamily,
            pinnedRobotoHash,
            (int)typeface.Weight,
            (float)(fontSize * scale),
            ToPhysicalPixelsOrNull(lineHeight, scale),
            (float)(letterSpacing * scale));
    }

    private static ParityTextPaintSignature CaptureTextPaintSignature(IBrush? foreground) =>
        foreground switch
        {
            ISolidColorBrush solid => new(
                "solid",
                FormattableString.Invariant(
                    $"#FF{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}"),
                QuantizeOpacity((solid.Color.A / 255d) * solid.Opacity)),
            null => new("unspecified", null, 1),
            _ => new("brush", null, QuantizeOpacity(foreground.Opacity))
        };

    private static float QuantizeOpacity(double opacity) =>
        (float)(Math.Round(Math.Clamp(opacity, 0, 1) * 255, MidpointRounding.AwayFromZero) / 255);

    private static float? ToPhysicalPixelsOrNull(double value, double scale) =>
        double.IsNaN(value) ? null : (float)(value * scale);

    private static ParityPoint TransformPointToPhysicalRoot(Point point, Matrix transform, double scale)
    {
        var rootPoint = point.Transform(transform);
        return new ParityPoint((float)(rootPoint.X * scale), (float)(rootPoint.Y * scale));
    }

    private static ParityRect TransformRectToPhysicalRoot(Rect rect, Matrix transform, double scale)
    {
        var points = new[]
        {
            TransformPointToPhysicalRoot(rect.TopLeft, transform, scale),
            TransformPointToPhysicalRoot(rect.TopRight, transform, scale),
            TransformPointToPhysicalRoot(rect.BottomLeft, transform, scale),
            TransformPointToPhysicalRoot(rect.BottomRight, transform, scale)
        };
        return new ParityRect(
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));
    }

    private static ParityRect ScaleRect(Rect rect, double scale) => new(
        (float)(rect.Left * scale),
        (float)(rect.Top * scale),
        (float)(rect.Right * scale),
        (float)(rect.Bottom * scale));

    private static Rect Offset(Rect rect, Point offset) => new(
        rect.X + offset.X,
        rect.Y + offset.Y,
        rect.Width,
        rect.Height);
}

public sealed record ParityActualCapture(RenderedImage Image, ParityCaptureResult Layout);
