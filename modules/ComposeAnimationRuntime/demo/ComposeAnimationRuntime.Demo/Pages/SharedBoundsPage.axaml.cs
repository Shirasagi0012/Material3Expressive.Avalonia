using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class SharedBoundsPage : MotionPageBase
{
    private SharedBounds? _source;
    private SharedBounds? _destination;

    public SharedBoundsPage() => InitializeComponent();

    protected override void OnAttached()
    {
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        _source = CreateBounds("Source", "#4F7CFF", SharedParticipantRole.Source);
        _destination = CreateBounds("Destination", "#E87532", SharedParticipantRole.Destination);
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
        Status.Text = pairs.Count == 0 ? "No matching participants" : "Animating source → destination";
    }

    private static SharedBounds CreateBounds(string text, string color, SharedParticipantRole role)
        => new()
        {
            Key = "card",
            Role = role,
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
