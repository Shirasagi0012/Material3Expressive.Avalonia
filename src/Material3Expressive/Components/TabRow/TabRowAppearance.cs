using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Material3Expressive.Components;

public enum TabRowVariant { Primary, Secondary }
public enum TabContentLayout { Stacked, Leading, Custom }

/// <summary>A tab's start-relative layout, in DIP. ContentWidth excludes the 16 DIP text padding on each side.</summary>
public readonly record struct TabPosition(double Left, double Width, double ContentWidth)
{
    public double Right => Left + Width;
}

/// <summary>Shared Material row settings for TabRow and the native TabControl theme.</summary>
public static class TabRowAppearance
{
    public static readonly AttachedProperty<TabRowVariant> VariantProperty = AvaloniaProperty.RegisterAttached<Control, TabRowVariant>("Variant", typeof(TabRowAppearance));
    public static readonly AttachedProperty<bool> IsScrollableProperty = AvaloniaProperty.RegisterAttached<Control, bool>("IsScrollable", typeof(TabRowAppearance));
    public static readonly AttachedProperty<double> EdgePaddingProperty = AvaloniaProperty.RegisterAttached<Control, double>("EdgePadding", typeof(TabRowAppearance), 52);
    public static readonly AttachedProperty<double> MinTabWidthProperty = AvaloniaProperty.RegisterAttached<Control, double>("MinTabWidth", typeof(TabRowAppearance), 90);
    public static readonly AttachedProperty<IBrush?> ContentColorProperty = AvaloniaProperty.RegisterAttached<Control, IBrush?>("ContentColor", typeof(TabRowAppearance));
    public static readonly AttachedProperty<IControlTemplate?> IndicatorProperty = AvaloniaProperty.RegisterAttached<Control, IControlTemplate?>("Indicator", typeof(TabRowAppearance));
    public static readonly AttachedProperty<IControlTemplate?> DividerProperty = AvaloniaProperty.RegisterAttached<Control, IControlTemplate?>("Divider", typeof(TabRowAppearance));
    public static readonly AttachedProperty<IReadOnlyList<TabPosition>> TabPositionsProperty = AvaloniaProperty.RegisterAttached<Control, IReadOnlyList<TabPosition>>("TabPositions", typeof(TabRowAppearance), Array.Empty<TabPosition>());

    public static TabRowVariant GetVariant(Control owner) => owner.GetValue(VariantProperty);
    public static void SetVariant(Control owner, TabRowVariant value) => owner.SetValue(VariantProperty, value);
    public static bool GetIsScrollable(Control owner) => owner.GetValue(IsScrollableProperty);
    public static void SetIsScrollable(Control owner, bool value) => owner.SetValue(IsScrollableProperty, value);
    public static double GetEdgePadding(Control owner) => owner.GetValue(EdgePaddingProperty);
    public static void SetEdgePadding(Control owner, double value) => owner.SetValue(EdgePaddingProperty, value);
    public static double GetMinTabWidth(Control owner) => owner.GetValue(MinTabWidthProperty);
    public static void SetMinTabWidth(Control owner, double value) => owner.SetValue(MinTabWidthProperty, value);
    public static IBrush? GetContentColor(Control owner) => owner.GetValue(ContentColorProperty);
    public static void SetContentColor(Control owner, IBrush? value) => owner.SetValue(ContentColorProperty, value);
    public static IControlTemplate? GetIndicator(Control owner) => owner.GetValue(IndicatorProperty);
    public static void SetIndicator(Control owner, IControlTemplate? value) => owner.SetValue(IndicatorProperty, value);
    public static IControlTemplate? GetDivider(Control owner) => owner.GetValue(DividerProperty);
    public static void SetDivider(Control owner, IControlTemplate? value) => owner.SetValue(DividerProperty, value);
    public static IReadOnlyList<TabPosition> GetTabPositions(Control owner) => owner.GetValue(TabPositionsProperty);
}

/// <summary>Tab content layout and colors, also consumed by native TabItem headers.</summary>
public static class TabAppearance
{
    public static readonly AttachedProperty<TabContentLayout> ContentLayoutProperty = AvaloniaProperty.RegisterAttached<Control, TabContentLayout>("ContentLayout", typeof(TabAppearance));
    public static readonly AttachedProperty<Color?> SelectedContentColorProperty = AvaloniaProperty.RegisterAttached<Control, Color?>("SelectedContentColor", typeof(TabAppearance));
    public static readonly AttachedProperty<Color?> UnselectedContentColorProperty = AvaloniaProperty.RegisterAttached<Control, Color?>("UnselectedContentColor", typeof(TabAppearance));
    public static TabContentLayout GetContentLayout(Control owner) => owner.GetValue(ContentLayoutProperty);
    public static void SetContentLayout(Control owner, TabContentLayout value) => owner.SetValue(ContentLayoutProperty, value);
    public static Color? GetSelectedContentColor(Control owner) => owner.GetValue(SelectedContentColorProperty);
    public static void SetSelectedContentColor(Control owner, Color? value) => owner.SetValue(SelectedContentColorProperty, value);
    public static Color? GetUnselectedContentColor(Control owner) => owner.GetValue(UnselectedContentColorProperty);
    public static void SetUnselectedContentColor(Control owner, Color? value) => owner.SetValue(UnselectedContentColorProperty, value);
}
