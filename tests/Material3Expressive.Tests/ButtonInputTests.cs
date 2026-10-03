using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class ButtonInputTests
{
    [AvaloniaFact]
    public void Enter_UsesNativeKeyDownActivation_AndKeepsMaterialPressFeedback()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var other = new Button();
        window.Content = null;
        window.UpdateLayout();
        window.Content = new StackPanel { Children = { button, other } };
        window.UpdateLayout();
        var clicks = 0;
        var executions = 0;
        button.Click += (_, _) => clicks++;
        button.Command = new CallbackCommand(() => executions++);
        button.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Assert.Equal(1, clicks);
        Assert.Equal(1, executions);
        Assert.True(MaterialInteraction.GetOrCreate(button).Interaction.HasFlag(InteractionState.Pressed));
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Assert.Equal(2, clicks);
        Assert.Equal(2, executions);
        Assert.True(MaterialInteraction.GetOrCreate(button).Interaction.HasFlag(InteractionState.Pressed));
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Assert.Equal(2, clicks);
        Assert.Equal(2, executions);
        Assert.False(MaterialInteraction.GetOrCreate(button).Interaction.HasFlag(InteractionState.Pressed));
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Assert.Equal(2, clicks);

        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Assert.Equal(3, clicks);
        other.Focus(NavigationMethod.Tab);
        Assert.False(MaterialInteraction.GetOrCreate(button).Interaction.HasFlag(InteractionState.Pressed));
        button.Focus(NavigationMethod.Tab);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Assert.Equal(3, clicks);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Assert.Equal(4, executions);
        button.IsEnabled = false;
        Assert.False(MaterialInteraction.GetOrCreate(button).Interaction.HasFlag(InteractionState.Pressed));
        button.IsEnabled = true;
        button.Focus(NavigationMethod.Tab);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Assert.Equal(4, clicks);
        Assert.Equal(4, executions);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void DragOutAndBack_CancelsNativeClickAndCommand_UntilAnotherPress()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var clicks = 0;
        var executions = 0;
        button.Click += (_, _) => clicks++;
        button.Command = new CallbackCommand(() => executions++);
        var origin = button.TranslatePoint(default, window)!.Value;
        var inside = origin + new Point(button.Bounds.Width / 2, button.Bounds.Height / 2);
        var outside = origin + new Point(button.Bounds.Width + 10, button.Bounds.Height / 2);
        window.MouseMove(inside);
        window.MouseDown(inside, MouseButton.Left);
        Assert.True(button.IsPressed);
        window.MouseMove(outside);
        Assert.False(button.IsPressed);
        Assert.False(MaterialInteraction.GetOrCreate(button).Interaction.HasFlag(InteractionState.Pressed));
        window.MouseMove(inside);
        window.MouseUp(inside, MouseButton.Left);
        Assert.Equal(0, clicks);
        Assert.Equal(0, executions);
        window.MouseDown(inside, MouseButton.Left);
        window.MouseUp(inside, MouseButton.Left);
        Assert.Equal(1, clicks);
        Assert.Equal(1, executions);
        window.Close();
    }

    private sealed class CallbackCommand(Action callback) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => callback();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
