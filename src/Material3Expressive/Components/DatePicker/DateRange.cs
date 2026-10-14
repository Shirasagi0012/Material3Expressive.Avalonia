namespace Material3Expressive.Components;

public enum DatePickerDisplayMode
{
    Picker,
    Input
}

/// <summary>An atomic range selection. A start without an end represents an in-progress selection.</summary>
public readonly record struct DateRange(DateOnly? Start, DateOnly? End);
