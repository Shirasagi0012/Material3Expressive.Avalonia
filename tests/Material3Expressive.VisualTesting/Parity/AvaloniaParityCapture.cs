using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Rendering;

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

        return _renderer.Render(physical, (context, image) =>
        {
            var nodes = CaptureNodes(context, expected.Nodes, physical.RenderScaling);
            var layout = new ParityCaptureResult(
                ParityCaptureResult.CurrentSchemaVersion,
                expected.CaseId,
                new ParityCoordinateSpace("physical-pixel", "root-top-left", "right", "down", (float)physical.RenderScaling),
                new ParityViewport(image.Width, image.Height),
                nodes);
            return new ParityActualCapture(image, layout);
        });
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
        var allControls = context.Root.GetVisualDescendants().OfType<Control>().ToArray();
        return expected.Select(node =>
        {
            var control = ResolveControl(node, context.Content, allControls);
            if (control is null && node.Id == "label" && node.TextLayout is not null)
            {
                var textField = allControls.OfType<MaterialTextField>().SingleOrDefault();
                var probe = textField?.GetLabelParityProbe();
                if (textField is not null && probe is not null)
                    return CaptureTextFieldLabel(node, context.Root, textField, probe, scale);
            }
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
                CaptureFocusedState(node, control, allControls));
        }).ToArray();
    }

    private static Control? ResolveControl(
        ParityNode expected,
        Control content,
        IReadOnlyList<Control> all)
    {
        var named = all.Prepend(content).FirstOrDefault(item => item.Name == expected.Id);
        if (named is not null)
            return named;
        if (expected.TextLayout is null)
            return expected.Id == "component" ? content : null;

        // Text parity is intentionally opt-in through a stable node name. The sole template alias
        // is the declared TextField supporting-text part; its label has a dedicated control probe.
        if (expected.Id == "supporting")
            return all.FirstOrDefault(item => item.Name == "PART_Supporting");
        return null;
    }

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
        MaterialTextField textField,
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
                styleSignature: null));
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
            CaptureTextStyleSignature(textBlock, scale));
    }

    private static ParityTextLayout CaptureTextLayout(
        TextLayout layout,
        string text,
        Point drawOrigin,
        Matrix transform,
        double scale,
        ParityTextStyleSignature? styleSignature)
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
            styleSignature);
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
    {
        var requestedFontFamily = textBlock.FontFamily.Name;
        var resolvedFontFamily = requestedFontFamily;
        GlyphTypeface? glyphTypeface = null;
        try
        {
            glyphTypeface = new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch).GlyphTypeface;
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
            (int)textBlock.FontWeight,
            (float)(textBlock.FontSize * scale),
            ToPhysicalPixelsOrNull(textBlock.LineHeight, scale),
            (float)(textBlock.LetterSpacing * scale));
    }

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
