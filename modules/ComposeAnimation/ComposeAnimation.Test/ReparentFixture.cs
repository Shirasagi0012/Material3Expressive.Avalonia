using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

/// <summary>
/// The shared D17 reparent scene: two sibling slots that each own a <see cref="AnimationScope.Get"/>
/// scope, one content control, a manual frame source. <see cref="Move"/> reparents the content from
/// <see cref="Left"/> to <see cref="Right"/> in one tree edit; <see cref="Detach"/> removes it.
/// With <c>lookahead: true</c> the slots are lookahead panels inside a <see cref="LookaheadRoot"/>.
/// </summary>
internal sealed class ReparentFixture : IDisposable
{
    public ManualFrameSource Frames { get; } = new();
    public Panel Left { get; }
    public Panel Right { get; }
    public Control Content { get; }
    public LookaheadRoot? Root { get; }
    public Window Window { get; }

    /// <param name="content">The control under test, placed in <see cref="Left"/>.</param>
    /// <param name="lookahead">Use lookahead slots inside a LookaheadRoot.</param>
    /// <param name="addContentAfterSlotScopes">
    /// Create the slot scopes before the content arrives (so a host scope created on attach parents
    /// to Left's) and leave the content's own scope to the host; otherwise the content's scope is
    /// created after the first layout.
    /// </param>
    public ReparentFixture(Control content, bool lookahead = false, bool addContentAfterSlotScopes = false)
    {
        Content = content;
        if (lookahead)
        {
            Left = new LookaheadStackPanel { Orientation = Orientation.Vertical };
            Right = new LookaheadStackPanel { Orientation = Orientation.Vertical };
            var outer = new LookaheadStackPanel { Orientation = Orientation.Vertical, Children = { Left, Right } };
            Root = new LookaheadRoot { Width = 500, Height = 400, Child = outer, UseLayoutRounding = false };
            Window = new AnimationTestWindow(Frames) { Width = 600, Height = 500, Content = Root };
        }
        else
        {
            Left = new StackPanel();
            Right = new StackPanel();
            Window = new AnimationTestWindow(Frames) { Width = 400, Height = 400, Content = new StackPanel { Children = { Left, Right } } };
        }
        if (addContentAfterSlotScopes)
        {
            Window.Show();
            Dispatcher.UIThread.RunJobs();
            AnimationScope.Get(Left); AnimationScope.Get(Right);
            Left.Children.Add(content);
            Dispatcher.UIThread.RunJobs();
            return;
        }
        Left.Children.Add(content);
        Window.Show();
        Layout();
        AnimationScope.Get(Left); AnimationScope.Get(Right);
        AnimationScope.Get(content);
    }

    public void Move() { Left.Children.Remove(Content); Right.Children.Add(Content); }
    public void MoveBack() { Right.Children.Remove(Content); Left.Children.Add(Content); }
    public void Detach() { Left.Children.Remove(Content); }
    public Point Position(Control node) => node.TranslatePoint(default, (Visual?)Root ?? Window)!.Value;
    public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    public void Step(int ms) { Layout(); Frames.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
    public void Dispose() => Window.Close();
}
