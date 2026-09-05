using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class SharedElementPage : MotionPageBase
{
    private SharedElement? _source;
    private SharedElement? _destination;

    public SharedElementPage() => InitializeComponent();

    protected override void OnAttached()
    {
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        _source = CreateElement("Source", "#7657D9", SharedParticipantRole.Source);
        _destination = CreateElement("Destination", "#2D9D78", SharedParticipantRole.Destination);
        Grid.SetColumn(_source, 0);
        Grid.SetColumn(_destination, 1);
        layout.Children.Add(_source);
        layout.Children.Add(_destination);
        Root.ContentLayer.Children.Add(layout);
    }

    protected override void OnDetached()
    {
        Root.ContentLayer.Children.Clear();
        _source = null;
        _destination = null;
    }

    private void OnRun(object? sender, RoutedEventArgs e)
    {
        if (_source is null || _destination is null)
            return;
        var pairs = Root.RunTransition(static () => { });
        Status.Text = pairs.Count == 0 ? "No matching participants" : "Animating shared visual";
    }

    private static SharedElement CreateElement(string text, string color, SharedParticipantRole role)
        => new()
        {
            Key = "element",
            Role = role,
            Mode = SharedElementMode.MoveVisual,
            InputPolicy = SharedElementInputPolicy.TransferToDestination,
            Width = role == SharedParticipantRole.Source ? 150 : 230,
            Height = role == SharedParticipantRole.Source ? 76 : 130,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new Border
            {
                Background = Brush.Parse(color),
                CornerRadius = new CornerRadius(16),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontSize = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            },
        };
}
