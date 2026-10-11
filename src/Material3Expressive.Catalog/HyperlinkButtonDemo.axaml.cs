using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class HyperlinkButtonDemo : Window
{
    private int _clicks;

    public HyperlinkButtonDemo()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
        VisitedLink.PropertyChanged += (_, e) =>
        {
            if (e.Property == HyperlinkButton.IsVisitedProperty) UpdateVisitedState();
        };
        UpdateVisitedState();
    }

    private void UpdateVisitedState() =>
        VisitedState.Text = VisitedLink.Classes.Contains(":visited") ? ":visited is set" : ":visited is not set";

    private void OnClick(object? sender, RoutedEventArgs e) => ClickText.Text = (++_clicks).ToString();
}
