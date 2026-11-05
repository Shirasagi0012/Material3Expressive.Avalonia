using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Catalog;

public partial class TextFieldDemo : Window
{
    private readonly MaterialInteractionSource _fieldInteractions = new();
    private MaterialInteractionEvent.Focus? _previewFocus;
    public TextFieldDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        MaterialTextField.SetInteractionSource(InteractionInput, _fieldInteractions);
        _fieldInteractions.Interaction += interaction => InteractionStatus.Text = interaction.GetType().Name;
    }

    private void ClearSearch(object? sender, RoutedEventArgs e) => SearchInput.Clear();
    private void TogglePreviewFocus(object? sender, RoutedEventArgs e)
    {
        if (_previewFocus is { } focus)
        {
            _previewFocus = null;
            _fieldInteractions.Emit(new MaterialInteractionEvent.Unfocus(focus));
        }
        else _fieldInteractions.Emit(_previewFocus = new MaterialInteractionEvent.Focus());
    }
    private void ToggleTheme(object? sender, RoutedEventArgs e) =>
        RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    private void ToggleDirection(object? sender, RoutedEventArgs e) =>
        FlowDirection = FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
