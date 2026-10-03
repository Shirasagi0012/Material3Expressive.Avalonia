using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;

namespace Material3Expressive.Components;

/// <summary>Positions a badge at the top end of an anchor without enlarging the anchor's layout.</summary>
public class BadgedBox : ContentControl
{
    public static readonly StyledProperty<object?> BadgeProperty =
        AvaloniaProperty.Register<BadgedBox, object?>(nameof(Badge));
    public static readonly StyledProperty<IDataTemplate?> BadgeTemplateProperty =
        AvaloniaProperty.Register<BadgedBox, IDataTemplate?>(nameof(BadgeTemplate));

    public object? Badge { get => GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }
    public IDataTemplate? BadgeTemplate { get => GetValue(BadgeTemplateProperty); set => SetValue(BadgeTemplateProperty, value); }

    protected override bool RegisterContentPresenter(ContentPresenter presenter) =>
        presenter.Name == "PART_BadgePresenter" || base.RegisterContentPresenter(presenter);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BadgeProperty)
        {
            if (change.OldValue is ILogical oldChild) LogicalChildren.Remove(oldChild);
            if (change.NewValue is ILogical newChild) LogicalChildren.Add(newChild);
        }
    }
}
