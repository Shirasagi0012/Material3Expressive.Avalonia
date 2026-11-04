using Avalonia;
using System.Globalization;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
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
        internal void Advance(int ms) { Time.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Draw(); }
        public void Dispose()
        {
            Window.Close(); Draw();
            Assert.Null(Window.AnimationRoot.Diagnostics.LastError);
            Assert.Equal(0, Time.ActiveTimerCount); Assert.Equal(0, Clock.PendingFrameCount); Clock.Dispose();
        }
    }
}
