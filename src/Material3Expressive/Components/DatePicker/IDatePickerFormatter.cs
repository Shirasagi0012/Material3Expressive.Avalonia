using System.Globalization;

namespace Material3Expressive.Components;

/// <summary>Formats calendar headings and date descriptions without changing numeric input parsing.</summary>
/// <remarks>Corresponds to the fixed Material3 DatePickerFormatter. Dates have no time zone;
/// they represent the same calendar day as the upstream UTC-midnight value. Replace the formatter
/// property when its configuration changes. A null result requests the component's placeholder.</remarks>
public interface IDatePickerFormatter
{
    string? FormatMonthYear(DateOnly? month, CultureInfo culture);
    string? FormatDate(DateOnly? date, CultureInfo culture, bool forContentDescription = false);
}
