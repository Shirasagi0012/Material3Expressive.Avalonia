using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ComposeAnimation;

namespace Material3Expressive.Components;

/// <summary>Single-line search input for the Boolean expanded-state search bar API.</summary>
/// <remarks>Text, selection and editing remain native TextBox state. Applications handle requests.</remarks>
public class SearchBarInputField : TextBox
{
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<SearchBarInputField, bool>(nameof(IsExpanded));
    public static readonly RoutedEvent<RoutedEventArgs> ExpansionRequestedEvent =
        RoutedEvent.Register<SearchBarInputField, RoutedEventArgs>(nameof(ExpansionRequested), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<RoutedEventArgs> SearchRequestedEvent =
        RoutedEvent.Register<SearchBarInputField, RoutedEventArgs>(nameof(SearchRequested), RoutingStrategies.Bubble);
    internal static readonly StyledProperty<double> PreferredMinimumWidthProperty =
        AvaloniaProperty.Register<SearchBarInputField, double>(nameof(PreferredMinimumWidth));
    internal static readonly StyledProperty<double> PreferredMaximumWidthProperty =
        AvaloniaProperty.Register<SearchBarInputField, double>(nameof(PreferredMaximumWidth));
    internal static readonly StyledProperty<TimeSpan> ClearFocusDelayProperty =
        AvaloniaProperty.Register<SearchBarInputField, TimeSpan>(nameof(ClearFocusDelay));
    internal double PreferredMinimumWidth { get => GetValue(PreferredMinimumWidthProperty); set => SetValue(PreferredMinimumWidthProperty, value); }
    internal double PreferredMaximumWidth { get => GetValue(PreferredMaximumWidthProperty); set => SetValue(PreferredMaximumWidthProperty, value); }
    internal TimeSpan ClearFocusDelay { get => GetValue(ClearFocusDelayProperty); set => SetValue(ClearFocusDelayProperty, value); }
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public event EventHandler<RoutedEventArgs>? ExpansionRequested { add => AddHandler(ExpansionRequestedEvent, value); remove => RemoveHandler(ExpansionRequestedEvent, value); }
    public event EventHandler<RoutedEventArgs>? SearchRequested { add => AddHandler(SearchRequestedEvent, value); remove => RemoveHandler(SearchRequestedEvent, value); }
    private CancellationTokenSource? _clearFocus;
    static SearchBarInputField() => AffectsMeasure<SearchBarInputField>(PreferredMinimumWidthProperty, PreferredMaximumWidthProperty);
    protected override Type StyleKeyOverride => typeof(SearchBarInputField);
    protected override AutomationPeer OnCreateAutomationPeer() => new SearchInputAutomationPeer(this);

    protected override Size MeasureOverride(Size availableSize)
    {
        var constrained = new Size(Math.Min(availableSize.Width, PreferredMaximumWidth), availableSize.Height);
        var measured = base.MeasureOverride(constrained);
        return new(Math.Min(availableSize.Width, Math.Max(PreferredMinimumWidth, measured.Width)), measured.Height);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (IsFocused) RaiseEvent(new RoutedEventArgs(ExpansionRequestedEvent));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.Key == Key.Enter && IsEffectivelyEnabled)
        {
            RaiseEvent(new RoutedEventArgs(SearchRequestedEvent));
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // LaunchedEffect(expanded): focus changes alone do not restart the effect.
        if (change.Property != IsExpandedProperty) return;
        CancelClearFocus();
        if (!IsExpanded && IsFocused && this.IsAttachedToVisualTree())
        {
            var cancellation = _clearFocus = new CancellationTokenSource();
            var scope = AnimationScope.Get(this);
            _ = scope.Launch(async _ => {
                try
                {
                    await scope.DelayAsync(ClearFocusDelay, cancellation.Token);
                    if (!IsExpanded && this.IsAttachedToVisualTree()) TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested || scope.Lifetime.IsCancellationRequested) { }
                finally
                {
                    if (ReferenceEquals(_clearFocus, cancellation)) _clearFocus = null;
                    cancellation.Dispose();
                }
            }, "Search input clear focus");
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelClearFocus();
        base.OnDetachedFromVisualTree(e);
    }
    private void CancelClearFocus() { _clearFocus?.Cancel(); _clearFocus = null; }

    // Keep the native editable value/selection provider, adding the two upstream semantics.
    private sealed class SearchInputAutomationPeer(SearchBarInputField owner) : TextBoxAutomationPeer(owner)
    {
        protected override string? GetNameCore() => base.GetNameCore() ?? SearchBarStrings.Get("SearchBarSearch");
        protected override string? GetItemStatusCore() => base.GetItemStatusCore() ?? (owner.IsExpanded ? SearchBarStrings.Get("SuggestionsAvailable") : null);
        protected override void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            base.OwnerPropertyChanged(sender, e);
            if (e.Property == IsExpandedProperty && AutomationProperties.GetItemStatus(owner) is null)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty,
                    e.GetOldValue<bool>() ? SearchBarStrings.Get("SuggestionsAvailable") : null, GetItemStatus());
        }
    }
}
