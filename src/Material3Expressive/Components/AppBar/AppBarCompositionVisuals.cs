using System.Globalization;
using Avalonia;
using Avalonia.Rendering.Composition;
using SmoothScroll.Avalonia.Composition;

namespace Material3Expressive.Components;

// UI-side bindings only. The scroll participant receives numeric geometry and output writers,
// never these visuals or expressions.
internal sealed class AppBarCompositionVisuals(
    CompositionVector3Output offsets,
    CompositionVector3Output appearance,
    CompositionColorOutput? color) : IDisposable
{
    private readonly Dictionary<(CompositionVisual Visual, string Property), string> _bindings = [];

    internal static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    internal void Bind(Visual element, string property, string expression, Size? arrangedSize = null)
    {
        var visual = ElementComposition.GetElementVisual(element);
        if (visual is null)
            return;

        var size = arrangedSize ?? element.Bounds.Size;
        // Match the values that Visual.SynchronizeCompositionProperties will send before
        // installing the animation. A later different native value cancels that animation
        // in Avalonia 12.1.2, including the first opacity update from stock title alpha.
        var changed = property switch
        {
            "Opacity" => visual.Opacity != (float)element.Opacity,
            "Size" => visual.Size != new Vector(size.Width, size.Height),
            "ClipToBounds" => visual.ClipToBounds != element.ClipToBounds,
            _ => false,
        };
        if (changed)
        {
            switch (property)
            {
                case "Opacity":
                    visual.Opacity = (float)element.Opacity;
                    break;
                case "Size":
                    visual.Size = new Vector(size.Width, size.Height);
                    break;
                case "ClipToBounds":
                    visual.ClipToBounds = element.ClipToBounds;
                    break;
            }

            _bindings.Remove((visual, property));
        }

        Bind(visual, property, expression);
    }

    internal void Bind(CompositionVisual visual, string property, string expression)
    {
        var key = (visual, property);
        if (_bindings.TryGetValue(key, out var previous) && previous == expression)
            return;

        var animation = visual.Compositor.CreateExpressionAnimation(expression);
        animation.SetReferenceParameter("offsets", offsets);
        animation.SetReferenceParameter("appearance", appearance);
        if (color is { })
            animation.SetReferenceParameter("colorOutput", color);
        visual.StartAndEvaluateAnimation(property, animation);
        _bindings[key] = expression;
    }

    public void Dispose()
    {
        foreach (var (visual, property) in _bindings.Keys)
        {
            visual.StopAnimation(property);
            // StopAnimation leaves the server's last evaluated value in place. Force a
            // direct value into the next batch; only the final value is serialized.
            switch (property)
            {
                case "Translation":
                    var translation = visual.Translation;
                    visual.Translation = translation + new Vector3D(0, 1, 0);
                    visual.Translation = translation;
                    break;
                case "Size":
                    var size = visual.Size;
                    visual.Size = size + new Vector(0, 1);
                    visual.Size = size;
                    break;
                case "Opacity":
                    var opacity = visual.Opacity;
                    visual.Opacity = opacity == 0 ? 1 : 0;
                    visual.Opacity = opacity;
                    break;
                case "ClipToBounds":
                    var clip = visual.ClipToBounds;
                    visual.ClipToBounds = !clip;
                    visual.ClipToBounds = clip;
                    break;
            }
        }

        _bindings.Clear();
    }
}
