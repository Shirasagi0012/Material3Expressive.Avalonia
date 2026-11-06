using Avalonia;
using System.Globalization;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using ComposeAnimation.Runtime;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class SearchBarInputFieldTests
{
    [AvaloniaTheory]
    [InlineData("en", "Search", "Suggestions below")]
    [InlineData("zh-CN", "搜索", "以下是搜索建议")]
    public void SearchSemanticsPreserveNativeValueAndExplicitAccessibility(string culture, string name, string status)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            using var host = new Host();
            var peer = ControlAutomationPeer.CreatePeerForElement(host.Field)!;
            Assert.Equal(name, peer.GetName()); Assert.Null(peer.GetItemStatus());
            var notifications = 0;
            peer.PropertyChanged += (_, e) => { if (e.Property == AutomationElementIdentifiers.ItemStatusProperty) notifications++; };
            host.Field.IsExpanded = true;
            Assert.Equal(status, peer.GetItemStatus()); Assert.Equal(1, notifications);
            ((IValueProvider)peer).SetValue("query"); Assert.Equal("query", host.Field.Text);
            AutomationProperties.SetName(host.Field, "custom name"); AutomationProperties.SetItemStatus(host.Field, "custom status");
            host.Field.IsExpanded = false;
            Assert.Equal("custom name", peer.GetName()); Assert.Equal("custom status", peer.GetItemStatus());
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [AvaloniaTheory]
    [InlineData(220, 220)]
    [InlineData(600, 360)]
    [InlineData(900, 360)]
    public void SearchInputUsesSoftWidthAndNativeSingleLineEditing(double parentWidth, double expected)
    {
        using var host = new Host(parentWidth);
        Assert.Equal(expected, host.Field.Bounds.Width);
        Assert.True(host.Field.Bounds.Height >= 56);
        Assert.False(host.Field.AcceptsReturn);
        Assert.Equal(TextWrapping.NoWrap, host.Field.TextWrapping);
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)host.Field.Background!).Color);
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)host.Field.BorderBrush!).Color);
        Assert.Equal(Avalonia.Input.TextInput.TextInputReturnKeyType.Search, Avalonia.Input.TextInput.TextInputOptions.GetReturnKeyType(host.Field));
    }

    [AvaloniaFact]
    public void FocusAndEnterRequestApplicationActionsWithoutWritingExpandedOrText()
    {
        using var host = new Host();
        var expands = 0; string? submitted = null;
        host.Field.ExpansionRequested += (_, _) => expands++;
        host.Field.SearchRequested += (_, _) => submitted = host.Field.Text;
        host.Field.Focus(NavigationMethod.Tab); host.Draw();
        Assert.Equal(1, expands); Assert.False(host.Field.IsExpanded);
        host.Field.Text = "material";
        var key = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter };
        host.Field.RaiseEvent(key);
        Assert.True(key.Handled); Assert.Equal("material", submitted); Assert.Equal("material", host.Field.Text);
        Assert.False(host.Field.IsExpanded);
        submitted = null; host.Field.IsEnabled = false;
        host.Field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        Assert.Null(submitted);
    }

    [AvaloniaFact]
    public void CollapseClearsFocusAfterDelayAndReexpansionCancelsTheEffect()
    {
        using var host = new Host();
        host.Field.Focus(); host.Field.IsExpanded = true; host.Draw();
        host.Field.IsExpanded = false; host.Draw();
        host.Advance(99); Assert.True(host.Field.IsFocused);
        host.Field.IsExpanded = true; host.Draw();
        host.Advance(200); Assert.True(host.Field.IsFocused);
        host.Field.IsExpanded = false; host.Draw();
        host.Advance(99); Assert.True(host.Field.IsFocused);
        host.Advance(1); Assert.False(host.Field.IsFocused);
        Assert.Equal(0, host.Time.ActiveTimerCount);
    }

    [AvaloniaFact]
    public void DetachingCancelsClearFocusWithoutClearingAnotherControlsFocus()
    {
        using var host = new Host();
        host.Field.Focus(); host.Field.IsExpanded = true;
        host.Field.IsExpanded = false; host.Draw();
        var other = new Button { Content = "other" };
        host.Window.Content = other; host.Draw(); other.Focus();
        host.Advance(200);
        Assert.True(other.IsFocused); Assert.Equal(0, host.Time.ActiveTimerCount);
    }

    [AvaloniaFact]
    public void FocusChangesDoNotRestartAnExpandedKeyedEffect()
    {
        using var host = new Host();
        host.Field.Focus(); host.Draw();
        host.Advance(200);
        Assert.True(host.Field.IsFocused); // Initial collapsed focus alone does not schedule a clear.
        var other = new Button { Content = "other" };
        host.Window.Content = null; host.Draw();
        host.Window.Content = new StackPanel { Children = { host.Field, other } }; host.Draw();
        host.Field.Focus(); host.Field.IsExpanded = true; host.Field.IsExpanded = false; host.Draw();
        other.Focus(); host.Advance(100);
        Assert.False(other.IsFocused); // Upstream captured focused at collapse, then clears the focus manager.
        Assert.Null(host.Window.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    public void ExternalSourceFocusDoesNotRequestExpansionButParticipatesInCollapseEffect()
    {
        using var host = new Host();
        var source = new MaterialInteractionSource();
        host.Field.InteractionSource = source;
        Assert.Same(source, MaterialTextField.GetInteractionSource(host.Field));
        var other = new Button();
        host.Window.Content = null;
        host.Window.Content = new StackPanel { Children = { host.Field, other } }; host.Draw();
        other.Focus();
        var requests = 0;
        host.Field.ExpansionRequested += (_, _) => requests++;
        source.Emit(new MaterialInteractionEvent.Focus());
        Assert.False(host.Field.IsFocused);
        Assert.False(host.Field.IsExpanded);
        Assert.Equal(0, requests);
        host.Field.IsExpanded = true;
        host.Field.IsExpanded = false;
        host.Advance(99); Assert.True(other.IsFocused);
        host.Advance(1); Assert.False(other.IsFocused);
    }

    [AvaloniaFact]
    public void SourceUnfocusPreventsClearEffectEvenWhenEditorRemainsFocused()
    {
        using var host = new Host();
        var source = new MaterialInteractionSource();
        MaterialInteractionEvent.Focus? focus = null;
        source.Interaction += interaction => { if (interaction is MaterialInteractionEvent.Focus f) focus = f; };
        host.Field.InteractionSource = source;
        host.Field.Focus();
        host.Field.IsExpanded = true;
        Assert.NotNull(focus);
        source.Emit(new MaterialInteractionEvent.Unfocus(focus));
        Assert.True(host.Field.IsFocused);
        host.Field.IsExpanded = false;
        host.Advance(200);
        Assert.True(host.Field.IsFocused);
        Assert.Equal(0, host.Time.ActiveTimerCount);
    }

    [AvaloniaFact]
    public void SourceReplacementDoesNotRestartOrCancelAnAlreadyLaunchedCollapseEffect()
    {
        using var host = new Host();
        host.Field.InteractionSource = new();
        host.Field.Focus();
        host.Field.IsExpanded = true;
        host.Field.IsExpanded = false;
        host.Advance(60);
        var next = new MaterialInteractionSource();
        host.Field.InteractionSource = next;
        next.Emit(new MaterialInteractionEvent.HoverEnter());
        host.Advance(39); Assert.True(host.Field.IsFocused);
        host.Advance(1); Assert.False(host.Field.IsFocused);
        Assert.Equal(0, host.Time.ActiveTimerCount);
    }

    [AvaloniaFact]
    public void IconOffsetsAndExplicitColorsSurviveTheSharedTextFieldTheme()
    {
        using var host = new Host();
        MaterialTextField.SetLeadingIcon(host.Field, new Border { Width = 24, Height = 24 });
        MaterialTextField.SetTrailingIcon(host.Field, new Border { Width = 24, Height = 24 });
        host.Field.Foreground = Brushes.Red; host.Field.Background = Brushes.Blue; host.Draw();
        var leading = host.Field.GetVisualDescendants().OfType<ContentPresenter>().Single(c => c.Name == "Leading");
        var trailing = host.Field.GetVisualDescendants().OfType<ContentPresenter>().Single(c => c.Name == "Trailing");
        Assert.Equal(4, ((TranslateTransform)leading.RenderTransform!).X);
        Assert.Equal(-4, ((TranslateTransform)trailing.RenderTransform!).X);
        host.Field.IsEnabled = false; host.Draw();
        Assert.Equal(Brushes.Red, host.Field.Foreground); Assert.Equal(Brushes.Blue, host.Field.Background);
    }

    [AvaloniaFact]
    public void StateKeyboardFocusIsIndependentAndDownKeyExpands()
    {
        using var host = new Host();
        var state = host.Field.State = new SearchBarState();
        var requests = 0;
        host.Field.ExpansionRequested += (_, _) => requests++;
        host.Field.Focus(NavigationMethod.Tab); host.Draw();
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        host.Field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
        host.Draw();
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
        Assert.Equal(0, requests);
        Assert.False(host.Field.IsExpanded); // Boolean API is not overwritten by State.
        host.Frames(16, 1008);
        Assert.Equal(1, state.Progress);
        _ = state.AnimateToCollapsedAsync(); host.Frames(1024, 2016);
        host.Advance(200);
        Assert.True(host.Field.IsFocused); // Keyboard collapse keeps editing focus.
    }

    [AvaloniaFact]
    public void StateTextGrowthTracksLengthAndSourceFocus()
    {
        using var host = new Host();
        var state = host.Field.State = new SearchBarState();
        host.Field.Text = "aa"; host.Draw();
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        host.Field.Focus(NavigationMethod.Tab);
        host.Field.Text = "bb"; host.Draw(); // Equal-length replacement does not expand.
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        host.Field.Text = "b"; host.Draw();
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        host.Field.Text = "bc"; host.Draw();
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
    }

    [AvaloniaFact]
    public void InitiallyExpandedFieldDoesNotStartTextObserverOnCollapse()
    {
        using var host = new Host();
        var state = host.Field.State = new SearchBarState(SearchBarValue.Expanded); host.Frames(0, 0);
        host.Field.Focus(NavigationMethod.Tab);
        _ = state.SnapToAsync(0); host.Draw();
        host.Field.Text = "typed"; host.Draw();
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        host.Field.State = new SearchBarState(); host.Frames(16, 16); // The new effect key installs its collector at commit.
        host.Field.Text = "typed more"; host.Draw();
        Assert.Equal(SearchBarValue.Expanded, host.Field.State.TargetValue);
    }

    [AvaloniaFact]
    public void SharedSourceReleaseExpandsButCancelAndOldSourceDoNot()
    {
        using var host = new Host();
        var state = host.Field.State = new SearchBarState();
        var source = host.Field.InteractionSource = new MaterialInteractionSource();
        host.Frames(16, 16);
        var press = new MaterialInteractionEvent.Press(new Point(4, 4));
        source.Emit(press); source.Emit(new MaterialInteractionEvent.CancelPress(press)); host.Draw();
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        var next = host.Field.InteractionSource = new MaterialInteractionSource();
        host.Frames(32, 32);
        source.Emit(new MaterialInteractionEvent.Release(press)); host.Draw();
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        next.Emit(new MaterialInteractionEvent.Release(press)); host.Draw();
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
    }

    [AvaloniaFact]
    public void PrivateSourceClickExpandsAnAlreadyKeyboardFocusedField()
    {
        using var host = new Host();
        var state = host.Field.State = new SearchBarState();
        host.Field.Focus(NavigationMethod.Tab); host.Draw();
        host.Window.MouseDown(new Point(30, 25), MouseButton.Left);
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        host.Window.MouseUp(new Point(30, 25), MouseButton.Left); host.Draw();
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
        host.Frames(16, 1008);
        _ = state.SnapToAsync(0); host.Draw();
        host.Advance(99); Assert.True(host.Field.IsFocused);
        host.Advance(1); Assert.False(host.Field.IsFocused);
    }

    [AvaloniaFact]
    public void ExpandedArrowMovesFocusToSuggestionAndReadOnlyStillExpands()
    {
        using var host = new Host();
        var state = host.Field.State = new SearchBarState(SearchBarValue.Expanded); host.Frames(0, 0);
        host.Field.IsReadOnly = true;
        var suggestion = new Button { Content = "suggestion" };
        host.Window.Content = null;
        host.Window.Content = new StackPanel { Children = { host.Field, suggestion } }; host.Draw();
        host.Field.Focus(NavigationMethod.Tab);
        host.Field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
        Assert.True(suggestion.IsFocused);
        _ = state.SnapToAsync(0); host.Field.Focus(NavigationMethod.Tab);
        host.Field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Down }); host.Draw();
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
    }

    [AvaloniaFact]
    public void StateSemanticsFollowProgressThresholdAndRespectExplicitStatus()
    {
        using var host = new Host();
        var state = host.Field.State = new SearchBarState();
        var peer = ControlAutomationPeer.CreatePeerForElement(host.Field)!;
        var notifications = 0;
        peer.PropertyChanged += (_, e) => { if (e.Property == AutomationElementIdentifiers.ItemStatusProperty) notifications++; };
        _ = state.SnapToAsync(.02f); Assert.Null(peer.GetItemStatus());
        _ = state.SnapToAsync(.021f); Assert.NotNull(peer.GetItemStatus());
        Assert.Equal(1, notifications);
        AutomationProperties.SetItemStatus(host.Field, "custom");
        _ = state.SnapToAsync(0); Assert.Equal("custom", peer.GetItemStatus());
    }

    [AvaloniaFact]
    public void StateReplacementCancelsInputOperationAndDetachReleasesObserver()
    {
        using var host = new Host();
        var old = host.Field.State = new SearchBarState(); host.Frames(0, 0);
        host.Field.Focus(NavigationMethod.Tab);
        host.Field.Text = "a"; host.Draw(); host.Frames(16, 80);
        Assert.True(old.IsAnimating);
        var next = host.Field.State = new SearchBarState(); host.Draw();
        Assert.False(old.IsAnimating);
        host.Window.Content = null; host.Draw();
        var progress = next.Progress;
        host.Field.Text = "ab"; host.Draw();
        Assert.Equal(progress, next.Progress); Assert.False(next.IsAnimating);
        Assert.Equal(0, host.Clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedStateInputsOpenAndCloseARealPopupWithoutReopening(bool pointer)
    {
        using var host = new Host();
        var state = host.Field.State = new SearchBarState();
        var expanded = new SearchBarInputField { State = state };
        var popup = new ExpandedDockedSearchBar { State = state, InputField = expanded, Content = new Button { Content = "result" } };
        host.Window.Content = null;
        host.Window.Height = 500;
        host.Window.Content = new Grid { Children = { new SearchBar { State = state, Content = host.Field }, popup } };
        host.Draw();
        if (pointer)
        {
            host.Window.MouseDown(new Point(30, 25), MouseButton.Left);
            host.Window.MouseUp(new Point(30, 25), MouseButton.Left);
        }
        else
        {
            host.Field.Focus(NavigationMethod.Tab);
            host.Field.Text = "query";
        }
        host.Draw(); host.Frames(16, 1008);
        Assert.True(popup.IsPresented); Assert.True(expanded.IsFocused);
        _ = state.AnimateToCollapsedAsync(); host.Frames(1024, 2400); host.Advance(200);
        Assert.False(popup.IsPresented); Assert.Equal(SearchBarValue.Collapsed, state.CurrentValue);
        Assert.Equal(!pointer, host.Field.IsFocused);
    }

    [AvaloniaFact]
    public void BatchedTextEditsObserveOnlyTheAppliedSnapshot()
    {
        using var host = new Host();
        var state = host.Field.State = new SearchBarState(); host.Frames(0, 0);
        host.Field.Focus(NavigationMethod.Tab); host.Draw();
        host.Field.Text = "temporary"; host.Field.Text = ""; host.Draw();
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        host.Field.Text = "a"; host.Field.Text = "ab"; host.Draw();
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
    }

    [AvaloniaFact]
    public void TextBeforeReplacementCollectorStartsBecomesItsInitialValue()
    {
        using var host = new Host();
        host.Field.State = new SearchBarState(); host.Frames(0, 0);
        host.Field.Focus(NavigationMethod.Tab); host.Draw();
        var state = host.Field.State = new SearchBarState();
        host.Field.Text = "seed"; host.Draw(); host.Frames(16, 16);
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        host.Field.Text = "seeded"; host.Draw();
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
    }

    [AvaloniaFact]
    public void StateInputRingUsesPrivateFocusAndPreservesLayout()
    {
        using var host = new Host();
        host.Field.State = new();
        MaterialInteraction.SetFocusFeedback(host.Field, FocusFeedback.InsetRing); host.Draw();
        var size = host.Field.Bounds.Size;
        var ring = Assert.Single(host.Field.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
        Assert.Equal(host.Field.Bounds.Size, ring.Bounds.Size);
        host.Field.Focus(NavigationMethod.Tab); host.Frames(16, 160);
        Assert.InRange(ring.Progress, 0.01f, 1.1f);
        host.Window.FocusManager!.Focus(null); host.Frames(176, 1008);
        Assert.Equal(0, ring.Progress); Assert.Equal(size, host.Field.Bounds.Size);
        Assert.Equal(SearchBarValue.Collapsed, host.Field.State.CurrentValue);
    }

    [AvaloniaFact]
    public void PointerModeRemovesRingAndKeyboardModeCreatesAFreshCollector()
    {
        using var host = new Host();
        host.Field.State = new();
        MaterialInteraction.SetFocusFeedback(host.Field, FocusFeedback.InsetRing); host.Draw();
        host.Field.Focus(NavigationMethod.Tab); host.Frames(16, 400);
        var ring = Assert.Single(host.Field.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
        Assert.InRange(ring.Progress, .99f, 1.01f);
        host.Window.MouseDown(new Point(30, 25), MouseButton.Left); host.Frames(416, 416);
        Assert.Empty(host.Field.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
        host.Window.MouseUp(new Point(30, 25), MouseButton.Left);
        host.Window.FocusManager!.Focus(null);
        host.Field.Focus(NavigationMethod.Tab); host.Frames(432, 432);
        var fresh = Assert.Single(host.Field.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
        Assert.NotSame(ring, fresh); Assert.Equal(0, fresh.Progress);
        // The collector was created after this focus event; upstream flows do not replay it.
        host.Window.FocusManager.Focus(null); host.Field.Focus(NavigationMethod.Tab); host.Frames(448, 1008);
        // Core InputModeFilterInteractionSource preserves signed counts: the unmatched
        // old Unfocus leaves this new collector unable to pair the subsequent Focus.
        Assert.Equal(0, fresh.Progress);
    }

    [AvaloniaFact]
    public void StateInputRingUsesExternalSourceAndReleasesOnModeOrTemplateChange()
    {
        using var host = new Host();
        host.Field.State = new();
        MaterialInteraction.SetFocusFeedback(host.Field, FocusFeedback.InsetRing);
        var source = host.Field.InteractionSource = new(); host.Frames(16, 16);
        var focus = new MaterialInteractionEvent.Focus(); source.Emit(focus); host.Frames(32, 400);
        var ring = Assert.Single(host.Field.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
        Assert.InRange(ring.Progress, .99f, 1.01f); Assert.False(host.Field.IsFocused);
        host.Field.CornerRadius = new(8, 12, 16, 20); host.Draw();
        Assert.Equal(host.Field.CornerRadius, ring.Corners);
        MaterialInteraction.SetFocusFeedback(host.Field, FocusFeedback.Opacity); host.Draw();
        Assert.Empty(host.Field.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
        MaterialInteraction.SetFocusFeedback(host.Field, FocusFeedback.InsetRing); host.Draw();
        var fresh = Assert.Single(host.Field.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
        Assert.Equal(0, fresh.Progress);
        var template = host.Field.Template;
        host.Field.Template = null; host.Draw();
        source.Emit(new MaterialInteractionEvent.Focus()); host.Frames(416, 432);
        Assert.Equal(0, host.Clock.PendingFrameCount);
        host.Field.Template = template; host.Draw();
        Assert.Single(host.Field.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
        host.Field.State = null; host.Draw();
        Assert.Empty(host.Field.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
    }

    private sealed class Host : IDisposable
    {
        internal ManualFrameSource Clock { get; } = new(Dispatcher.UIThread.VerifyAccess);
        internal ManualTimeProvider Time { get; } = new(Dispatcher.UIThread.VerifyAccess);
        internal AnimationTestWindow Window { get; }
        internal SearchBarInputField Field { get; } = new();
        internal Host(double width = 600)
        {
            Window = new(Clock, Time) { Width = width, Height = 200 };
            Window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
            Window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
            Window.Content = Field; Window.Show(); Draw(); Clock.SendFrame(TimeSpan.Zero);
        }
        internal void Draw() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        internal void Frames(int start, int end) { for (var ms = start; ms <= end; ms += 16) { Clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Draw(); } }
        internal void Advance(int ms) { Time.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Draw(); }
        public void Dispose()
        {
            Window.Close(); Draw();
            Assert.Null(Window.AnimationRoot.Diagnostics.LastError);
            Assert.Equal(0, Time.ActiveTimerCount); Assert.Equal(0, Clock.PendingFrameCount); Clock.Dispose();
        }
    }
}
