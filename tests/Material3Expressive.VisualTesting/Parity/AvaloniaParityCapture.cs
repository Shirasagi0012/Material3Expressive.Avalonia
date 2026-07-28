using Avalonia;
using Avalonia.Controls;
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
        const string CanonicalRobotoSha256 = "B25986D18730960C7B27384AB2BC500856AE7FE9E71C9850019195FF9019F0B2";
        if (!FontEnvironment.CaptureRobotoFiles().Any(file =>
                string.Equals(file.Sha256, CanonicalRobotoSha256, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Avalonia parity capture requires the same pinned Roboto font file as the Compose producer.");
        }
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
                text is null ? null : CaptureText(text, context.Root, scale, bounds));
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
        var origin = control.TranslatePoint(default, root);
        if (origin is null)
            return null;
        return new ParityRect(
            (float)(origin.Value.X * scale),
            (float)(origin.Value.Y * scale),
            (float)((origin.Value.X + control.Bounds.Width) * scale),
            (float)((origin.Value.Y + control.Bounds.Height) * scale));
    }

    private static ParityNode CaptureTextFieldLabel(
        ParityNode expected,
        Control root,
        MaterialTextField textField,
        MaterialTextFieldLabelProbe probe,
        double scale)
    {
        var fieldOrigin = textField.TranslatePoint(default, root)
            ?? throw new InvalidOperationException("MaterialTextField label probe is not mounted.");
        var origin = new Point(fieldOrigin.X + probe.Origin.X, fieldOrigin.Y + probe.Origin.Y);
        var bounds = new ParityRect(
            (float)(origin.X * scale),
            (float)(origin.Y * scale),
            (float)((origin.X + probe.Layout.Width) * scale),
            (float)((origin.Y + probe.Layout.Height) * scale));
        return new ParityNode(
            expected.Id,
            expected.Kind,
            expected.ParentId,
            bounds,
            textField.IsVisible,
            CaptureTextLayout(probe.Layout, expected.TextLayout!.Text, bounds.Left, bounds.Top, scale));
    }

    private static ParityTextLayout? CaptureText(
        TextBlock textBlock,
        Control root,
        double scale,
        ParityRect? bounds)
    {
        if (bounds is null || textBlock.TextLayout is not TextLayout layout)
            return null;
        return CaptureTextLayout(layout, textBlock.Text ?? string.Empty, bounds.Left, bounds.Top, scale);
    }

    private static ParityTextLayout CaptureTextLayout(
        TextLayout layout,
        string text,
        float originLeft,
        float originTop,
        double scale)
    {
        var lineTop = 0d;
        var textOffset = 0;
        var lines = new List<ParityTextLine>();
        foreach (var line in layout.TextLines)
        {
            var top = lineTop;
            lineTop += line.Height;
            var start = textOffset;
            var end = Math.Min(text.Length, start + line.Length - line.NewLineLength);
            lines.Add(new ParityTextLine(
                lines.Count, start, end, end,
                originLeft + (float)(line.Start * scale),
                originLeft + (float)((line.Start + line.Width) * scale),
                originTop + (float)(top * scale),
                originTop + (float)((top + line.Height) * scale),
                originTop + (float)((top + line.Baseline) * scale), false));
            textOffset += line.Length;
        }
        var characters = Enumerable.Range(0, text.Length).Select(offset =>
        {
            var rect = layout.HitTestTextPosition(offset);
            var line = lines.LastOrDefault(item => offset >= item.StartUtf16 && offset < item.EndUtf16)?.Index ?? 0;
            return new ParityCharacterBox(
                offset,
                ((int)text[offset]).ToString("X4"),
                line,
                new ParityRect(
                    originLeft + (float)(rect.X * scale),
                    originTop + (float)(rect.Y * scale),
                    originLeft + (float)((rect.X + rect.Width) * scale),
                    originTop + (float)((rect.Y + rect.Height) * scale)));
        }).ToArray();
        var first = lines.FirstOrDefault()?.BaselinePx ?? originTop;
        var last = lines.LastOrDefault()?.BaselinePx ?? originTop;
        return new ParityTextLayout(text, "utf-16", first, last, lines.Count, false, lines, characters);
    }
}

public sealed record ParityActualCapture(RenderedImage Image, ParityCaptureResult Layout);
