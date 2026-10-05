using System.ComponentModel;
using System.Globalization;
using Avalonia;

namespace Catalog.Framework;

/// <summary>A lazily sampled value. Sampling is owned by the expanded diagnostics panel.</summary>
public sealed class DemoDiagnostic(string name, Func<object?> current, Func<object?>? target, string unit)
    : INotifyPropertyChanged
{
    public string Name { get; } = name;
    public string Unit { get; } = unit;
    public string Current { get; private set; } = "—";
    public string Target { get; private set; } = "—";
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Refresh()
    {
        var next = Format(current());
        if (Current != next)
        {
            Current = next;
            PropertyChanged?.Invoke(this, new(nameof(Current)));
        }
        next = Format(target?.Invoke());
        if (Target != next)
        {
            Target = next;
            PropertyChanged?.Invoke(this, new(nameof(Target)));
        }
    }

    private static string Number(double value) => double.IsFinite(value)
        ? value.ToString("0.#", CultureInfo.InvariantCulture) : "—";

    private static string Format(object? value) => value switch
    {
        null => "—",
        bool flag => flag ? "是" : "否",
        double number => Number(number),
        float number => Number(number),
        Size size => $"{Number(size.Width)} × {Number(size.Height)}",
        Point point => $"{Number(point.X)}, {Number(point.Y)}",
        Vector vector => $"{Number(vector.X)}, {Number(vector.Y)}",
        TimeSpan time => Number(time.TotalMilliseconds),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "—"
    };
}
