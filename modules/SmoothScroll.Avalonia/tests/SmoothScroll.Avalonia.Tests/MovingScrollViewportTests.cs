using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using SmoothScroll.Avalonia.Interaction;

namespace SmoothScroll.Avalonia.Tests;

public sealed class MovingScrollViewportTests
{
    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void MovingAncestorDoesNotChangeTranslationOrReleaseVelocity(double scale)
    {
        var stationary = Capture(moving: false, scale);
        var moving = Capture(moving: true, scale);
        Assert.Equal(new[] { 32d, 82, 82, 132 }, stationary.Positions);
        Assert.Equal(stationary.Positions, moving.Positions);
        Assert.True(stationary.Velocity > 0);
        Assert.Equal(stationary.Velocity, moving.Velocity, 9);
    }

    [AvaloniaFact]
    public void LayoutMovementBeforeTouchSlopDoesNotStartScrolling()
    {
        using var host = new Host(1);
        host.Press(100, 300);
        host.Translation.Y = -50;
        host.Render();
        host.Move(116, 295);
        Assert.Equal(0, host.Tracker.Position.Y);
        host.Move(132, 260);
        Assert.Equal(22, host.Tracker.Position.Y);
    }

    [AvaloniaTheory]
    [InlineData(16, false)]
    [InlineData(40, false)]
    [InlineData(41, true)]
    [InlineData(128, true)]
    public void ReleaseAfterPointerStoppedDoesNotRevivePreviousVelocity(int pauseMilliseconds, bool stopped)
    {
        using var host = new Host(1);
        var preVelocities = new List<double>();
        var postVelocities = new List<(double Consumed, double Available)>();
        host.Tracker.ConfigureVerticalScroll(new Factory(), null, (available, _) =>
        {
            preVelocities.Add(available);
            return Task.FromResult(available);
        }, (consumed, available, _) =>
        {
            postVelocities.Add((consumed, available));
            return Task.FromResult(0d);
        });
        host.Render();
        host.Press(100, 300);
        host.Move(116, 250);
        host.Move(132, 200);
        var releasePosition = host.Tracker.Position.Y;
        host.Release((ulong)(132 + pauseMilliseconds), 200);

        var velocity = Assert.Single(preVelocities);
        if (stopped)
            Assert.Equal(0, velocity);
        else
            Assert.True(velocity > 0);
        Assert.Equal((0d, 0d), Assert.Single(postVelocities));
        Assert.Equal(releasePosition, host.Tracker.Position.Y);
    }

    private static (double[] Positions, double Velocity) Capture(bool moving, double scale)
    {
        using var host = new Host(scale);
        var positions = new List<double>();
        var velocity = 0d;
        host.Tracker.ConfigureVerticalScroll(new Factory(), null, (available, _) =>
        {
            velocity = available;
            return Task.FromResult(available);
        });
        host.Render();
        host.Press(100, 300);
        host.Move(116, 300 - 50 * scale); positions.Add(host.Tracker.Position.Y);
        if (moving) host.Translation.Y = -30;
        host.Render();
        host.Move(132, 300 - 100 * scale); positions.Add(host.Tracker.Position.Y);
        if (moving) host.Translation.Y = 70;
        host.Render();
        host.Move(140, 300 - 100 * scale); positions.Add(host.Tracker.Position.Y);
        host.Move(148, 300 - 150 * scale); positions.Add(host.Tracker.Position.Y);
        host.Release(164, 300 - 150 * scale);
        return (positions.ToArray(), velocity);
    }

    private sealed class Factory : IScrollMovementParticipantFactory
    {
        public IScrollMovementParticipant Create() => new Participant();
    }
    private sealed class Participant : IScrollMovementParticipant
    {
        public double PreScroll(in ScrollMovementContext context) => 0;
        public double PostScroll(in ScrollMovementContext context, double selfConsumed) => 0;
    }

    private sealed class Host : IDisposable
    {
        private readonly Window _window;
        private readonly Border _source;
        private readonly Pointer _pointer = new(Pointer.GetNextFreeId(), PointerType.Touch, true);
        private readonly InputElementInteractionSource _input;
        internal InteractionTracker Tracker { get; }
        internal TranslateTransform Translation { get; } = new();

        internal Host(double scale)
        {
            _source = new Border { Width = 160, Height = 400, Background = Brushes.Transparent,
                RenderTransformOrigin = RelativePoint.TopLeft, RenderTransform = new ScaleTransform(scale, scale) };
            _window = new Window { Width = 400, Height = 600, Content = new Border
                { Child = _source, RenderTransform = Translation, RenderTransformOrigin = RelativePoint.TopLeft } };
            _window.Show();
            Tracker = ElementComposition.GetElementVisual(_window)!.Compositor.CreateInteractionTracker(null);
            Tracker.MaxPosition = new Vector3D(10000, 10000, 0);
            _input = new InputElementInteractionSource(_source, Tracker)
            {
                PositionXSourceMode = InteractionSourceMode.Disabled,
                PositionYSourceMode = InteractionSourceMode.EnabledWithInertia,
                VerticalTouchSlop = 18
            };
            Render();
        }

        internal void Press(ulong time, double y)
        {
            _pointer.Capture(_source);
            _source.RaiseEvent(new PointerPressedEventArgs(_source, _pointer, _window, new Point(100, y), time,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            Render();
        }
        internal void Move(ulong time, double y)
        {
            _source.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, _source, _pointer, _window,
                new Point(100, y), time, new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
            Render();
        }
        internal void Release(ulong time, double y)
        {
            _source.RaiseEvent(new PointerReleasedEventArgs(_source, _pointer, _window, new Point(100, y), time,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            Render();
        }
        internal void Render()
        {
            for (var i = 0; i < 5; i++)
            {
                Dispatcher.UIThread.RunJobs();
                using var frame = _window.CaptureRenderedFrame();
            }
            Dispatcher.UIThread.RunJobs();
        }
        public void Dispose()
        {
            _pointer.Capture(null);
            _input.Dispose();
            Tracker.Dispose(); Render();
            _window.Close();
        }
    }
}
