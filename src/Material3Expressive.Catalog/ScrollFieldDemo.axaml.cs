using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Data.Converters;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class ScrollFieldDemo : Window
{
    private static readonly string[] UnitNames = ["Pixels", "Points", "Inches", "cm", "mm"];
    public static FuncValueConverter<int, string> UnitName { get; } = new(index => UnitNames[index]);
    public ScrollFieldDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        Numbers.State = new ScrollFieldState(60, 12);
        Units.State = new ScrollFieldState(5, 2);
        Units.OptionDescription = index => UnitNames[index];
        Numbers.State.PropertyChanged += (_, _) => UpdateStatus();
        UpdateStatus();
    }
    private void UpdateStatus() => Status.Text = $"Selected: {Numbers.State!.SelectedOption:D2} · Target: {Numbers.State.TargetOption:D2} · Scrolling: {Numbers.State.IsScrollInProgress}";
    private async void Animate(object? sender, RoutedEventArgs e)
    {
        try { await Numbers.State!.AnimateScrollToOptionAsync(45); }
        catch (OperationCanceledException) { }
    }
    private void Reset(object? sender, RoutedEventArgs e) => Numbers.State!.SelectedOption = 0;
    private void ToggleRtl(object? sender, RoutedEventArgs e) => FlowDirection = FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
