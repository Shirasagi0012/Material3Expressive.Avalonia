using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

internal sealed class DirectRawPlan<T> : IMaterialValuePlan<T>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;

    internal DirectRawPlan(MaterialToken<T> token)
    {
        Token = token ?? throw new ArgumentNullException(nameof(token));
        _dependencies = Array.AsReadOnly<MaterialToken>([token]);
    }

    internal MaterialToken<T> Token { get; }

    public Type ValueType => typeof(T);

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public T Evaluate(MaterialValueContext context) => context.Get(Token);

    object? IMaterialValuePlan.Evaluate(MaterialValueContext context) => Evaluate(context);
}

internal interface IRawValueRecipe<out T>
{
    IReadOnlyList<MaterialToken> Dependencies { get; }

    T Evaluate(MaterialValueContext context);
}

internal sealed class RawRecipePlan<T> : IMaterialValuePlan<T>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;

    internal RawRecipePlan(IRawValueRecipe<T> recipe)
    {
        Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
        _dependencies = SnapshotDependencies(recipe.Dependencies, nameof(recipe));
    }

    internal IRawValueRecipe<T> Recipe { get; }

    public Type ValueType => typeof(T);

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public T Evaluate(MaterialValueContext context) => Recipe.Evaluate(context);

    object? IMaterialValuePlan.Evaluate(MaterialValueContext context) => Evaluate(context);

    private static IReadOnlyList<MaterialToken> SnapshotDependencies(
        IReadOnlyList<MaterialToken>? dependencies,
        string parameterName)
    {
        if (dependencies is null)
            throw new ArgumentException("Value recipe dependencies cannot be null.", parameterName);

        var copy = new MaterialToken[dependencies.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = dependencies[i] ??
                throw new ArgumentException(
                    "Value recipe dependencies cannot contain null tokens.", parameterName);
        }

        return Array.AsReadOnly(copy);
    }
}

internal sealed class SolidBrushRecipe : IRawValueRecipe<IImmutableBrush>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _color;
    private readonly MaterialToken<double>? _opacity;

    internal SolidBrushRecipe(MaterialToken<Color> color, MaterialToken<double>? opacity)
    {
        _color = color ?? throw new ArgumentNullException(nameof(color));
        _opacity = opacity;
        _dependencies = opacity is null
            ? Array.AsReadOnly<MaterialToken>([color])
            : Array.AsReadOnly<MaterialToken>([color, opacity]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public IImmutableBrush Evaluate(MaterialValueContext context)
    {
        var color = context.Get(_color);
        if (_opacity is not null)
        {
            var opacity = Math.Clamp(context.Get(_opacity), 0d, 1d);
            color = new Color(
                (byte)Math.Round(color.A * opacity),
                color.R,
                color.G,
                color.B);
        }

        return new ImmutableSolidColorBrush(color);
    }
}

internal sealed class ConstantSolidBrushRecipe : IRawValueRecipe<IImmutableBrush>
{
    private static readonly IReadOnlyList<MaterialToken> NoDependencies =
        Array.Empty<MaterialToken>();
    private readonly IImmutableBrush _brush;

    internal ConstantSolidBrushRecipe(Color color)
    {
        _brush = new ImmutableSolidColorBrush(color);
    }

    public IReadOnlyList<MaterialToken> Dependencies => NoDependencies;

    public IImmutableBrush Evaluate(MaterialValueContext context) => _brush;
}

internal sealed class ThicknessRecipe : IRawValueRecipe<Thickness>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _left;
    private readonly MaterialToken<double> _top;
    private readonly MaterialToken<double> _right;
    private readonly MaterialToken<double> _bottom;

    internal ThicknessRecipe(
        MaterialToken<double> left,
        MaterialToken<double> top,
        MaterialToken<double> right,
        MaterialToken<double> bottom)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _top = top ?? throw new ArgumentNullException(nameof(top));
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _bottom = bottom ?? throw new ArgumentNullException(nameof(bottom));
        _dependencies = Array.AsReadOnly<MaterialToken>([left, top, right, bottom]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public Thickness Evaluate(MaterialValueContext context) => new(
        context.Get(_left),
        context.Get(_top),
        context.Get(_right),
        context.Get(_bottom));
}

internal sealed class HorizontalTokenThicknessRecipe : IRawValueRecipe<Thickness>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _left;
    private readonly double _vertical;
    private readonly MaterialToken<double> _right;

    internal HorizontalTokenThicknessRecipe(
        MaterialToken<double> left,
        double vertical,
        MaterialToken<double> right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        if (!double.IsFinite(vertical))
            throw new ArgumentOutOfRangeException(
                nameof(vertical), vertical, "The fixed vertical thickness must be finite.");
        _vertical = vertical;
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _dependencies = Array.AsReadOnly<MaterialToken>([left, right]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public Thickness Evaluate(MaterialValueContext context) => new(
        context.Get(_left),
        _vertical,
        context.Get(_right),
        _vertical);
}

internal sealed class ConstantThicknessRecipe : IRawValueRecipe<Thickness>
{
    private static readonly IReadOnlyList<MaterialToken> NoDependencies =
        Array.Empty<MaterialToken>();
    private readonly Thickness _value;

    internal ConstantThicknessRecipe(double left, double top, double right, double bottom)
    {
        if (!double.IsFinite(left))
            throw NonFinite(nameof(left), left);
        if (!double.IsFinite(top))
            throw NonFinite(nameof(top), top);
        if (!double.IsFinite(right))
            throw NonFinite(nameof(right), right);
        if (!double.IsFinite(bottom))
            throw NonFinite(nameof(bottom), bottom);

        _value = new Thickness(left, top, right, bottom);
    }

    public IReadOnlyList<MaterialToken> Dependencies => NoDependencies;

    public Thickness Evaluate(MaterialValueContext context) => _value;

    private static ArgumentOutOfRangeException NonFinite(string name, double value) =>
        new(name, value, "A fixed thickness component must be finite.");
}
