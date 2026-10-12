using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimatedPropertyTest
{
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan TweenDuration = TimeSpan.FromMilliseconds(100);

    private static TweenSpec<double> LinearTween(TimeSpan? duration = null)
        => new(duration ?? TweenDuration, easing: new LinearEasing());

    private static Task OnUi(Action body)
        => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    private sealed class WindowCloser(Window window) : IDisposable
    {
        public void Dispose() => window.Close();
    }

    private static (Window window, Border border, ManualFrameSource clock) ShowBorder(
        IFiniteAnimationSpec<double>? spec = null,
        double opacity = 1)
    {
        var clock = new ManualFrameSource();
        var border = new Border
        {
            Width = 80,
            Height = 80,
            Background = Brushes.Red,
            Opacity = opacity
        };

        Animated.SetProperties(border, new AnimatedPropertyCollection
        {
            new AnimatedDouble
            {
                Property = Visual.OpacityProperty,
                Spec = spec ?? LinearTween()
            }
        });
        var window = new AnimationTestWindow(clock)
        {
            Width = 200,
            Height = 200,
            Content = border
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, border, clock);
    }

    private static void StepUntilIdle(AvaloniaObject target, AvaloniaProperty property,
        ManualFrameSource clock, int maxFrames = 200)
    {
        for (var i = 0; i < maxFrames && target.IsAnimating(property); i++)
        {
            clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        }
        Assert.False(target.IsAnimating(property), "The animation did not finish within the allotted frames.");
    }

    [Fact]
    public Task Attachment_DoesNotAnimateInitialValue() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var _ = new WindowCloser(window);
        Assert.Equal(1, border.Opacity);
        Assert.False(border.IsAnimating(Visual.OpacityProperty));
        Assert.Equal(0, clock.PendingFrameCount);
    });

    [Fact]
    public Task LocalValueChange_WritesAnimationLayerAndKeepsBase() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var _ = new WindowCloser(window);
        {
            border.Opacity = 0;

            Assert.True(border.IsAnimating(Visual.OpacityProperty));
            Assert.Equal(0, border.GetBaseValue(Visual.OpacityProperty).Value);
            Assert.Equal(1, border.Opacity);
            Assert.True(clock.PendingFrameCount > 0);
        }
    });

    [Fact]
    public Task LocalValueChange_InterpolatesTowardBaseThenReleasesLayer() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var _ = new WindowCloser(window);
        {
            border.Opacity = 0;
            clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, border.Opacity);

            clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
            Assert.True(border.IsAnimating(Visual.OpacityProperty));
            Assert.InRange(border.Opacity, 0.05, 0.95);
            Assert.Equal(0, border.GetBaseValue(Visual.OpacityProperty).Value);

            StepUntilIdle(border, Visual.OpacityProperty, clock);
            Assert.Equal(0, border.Opacity);
            Assert.Equal(0, border.GetBaseValue(Visual.OpacityProperty).Value);
            Assert.False(border.IsAnimating(Visual.OpacityProperty));
        }
    });

    [Fact]
    public Task Retarget_ContinuesFromDisplayedValue() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var _ = new WindowCloser(window);
        {
            border.Opacity = 0;
            clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
            clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
            var mid = border.Opacity;
            Assert.InRange(mid, 0.05, 0.95);

            border.Opacity = 1;
            Dispatcher.UIThread.RunJobs();
            Assert.True(border.IsAnimating(Visual.OpacityProperty));
            Assert.Equal(1, border.GetBaseValue(Visual.OpacityProperty).Value);
            Assert.Equal(mid, border.Opacity);

            var lowest = mid;
            for (var i = 0; i < 20 && border.IsAnimating(Visual.OpacityProperty); i++)
            {
                clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
                lowest = Math.Min(lowest, border.Opacity);
            }

            Assert.True(lowest >= mid - 0.02, $"Retarget dropped from {mid} to {lowest}.");
            Assert.Equal(1, border.Opacity);
        }
    });

    [Fact]
    public Task SpecChange_DoesNotRestartCurrentRun() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder(LinearTween(TimeSpan.FromMilliseconds(1000)));
        using var _ = new WindowCloser(window);
        {
            var declaration = Animated.GetProperties(border)![0];
            border.Opacity = 0;
            clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();

            ((AnimatedDouble)declaration).Spec = LinearTween(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();

            Assert.True(border.IsAnimating(Visual.OpacityProperty));
            Assert.InRange(border.Opacity, 0.7, 1);
        }
    });

    [Fact]
    public Task Detach_ReleasesAnimationAndJumpsToBase() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var _ = new WindowCloser(window);
        {
            border.Opacity = 0;
            clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
            clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
            Assert.True(border.IsAnimating(Visual.OpacityProperty));
            Assert.InRange(border.Opacity, 0.05, 0.95);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();

            Assert.False(border.IsAnimating(Visual.OpacityProperty));
            Assert.Equal(0, border.Opacity);
        }
    });

    [Fact]
    public Task RemovingDeclaration_ReleasesAnimationLayer() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var _ = new WindowCloser(window);
        {
            border.Opacity = 0;
            clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
            Assert.True(border.IsAnimating(Visual.OpacityProperty));

            Animated.GetProperties(border)!.Clear();
            Assert.False(border.IsAnimating(Visual.OpacityProperty));
            Assert.Equal(0, border.Opacity);
        }
    });

    [Fact]
    public Task NotAttached_DoesNotAnimate() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var border = new Border { Opacity = 1 };

        border.Animate(new AnimatedDouble
        {
            Property = Visual.OpacityProperty,
            Spec = LinearTween()
        });

        border.Opacity = 0;

        Assert.Equal(0, border.Opacity);
        Assert.False(border.IsAnimating(Visual.OpacityProperty));
        Assert.Equal(0, clock.PendingFrameCount);
    });

    [Fact]
    public Task CodeBehind_AnimateHelper_UsesAnimationPriority() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var border = new Border { Width = 80, Height = 80, Opacity = 1 };

        border.Animate(new AnimatedDouble
        {
            Property = Visual.OpacityProperty,
            Spec = LinearTween()
        });
        var window = new AnimationTestWindow(clock) { Width = 200, Height = 200, Content = border };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        using var _ = new WindowCloser(window);
        {
            border.Opacity = 0;
            Assert.Equal(1, border.GetValue(Visual.OpacityProperty));
            Assert.Equal(0, border.GetBaseValue(Visual.OpacityProperty).Value);
            Assert.True(border.IsAnimating(Visual.OpacityProperty));
        }
    });

    [Fact]
    public Task DirectProperty_IsRejected() => OnUi(() =>
    {
        var border = new Border();
        Assert.Throws<InvalidOperationException>(() =>
            Animated.SetProperties(border, new AnimatedPropertyCollection
            {
                new AnimatedDouble { Property = Layoutable.DesiredSizeProperty }
            }));
    });

    [Fact]
    public Task SharedCollection_KeepsIndependentSessions() => OnUi(() =>
    {
        var clockA = new ManualFrameSource();
        var clockB = new ManualFrameSource();
        var collection = new AnimatedPropertyCollection
        {
            new AnimatedDouble { Property = Visual.OpacityProperty, Spec = LinearTween() }
        };
        var a = new Border { Opacity = 1, Width = 40, Height = 40 };
        var b = new Border { Opacity = 1, Width = 40, Height = 40 };


        Animated.SetProperties(a, collection);
        Animated.SetProperties(b, collection);
        var panel = new StackPanel();
        panel.Children.Add(a);
        panel.Children.Add(b);
        var window = new AnimationTestWindow(clockB) { Width = 200, Height = 200, Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        using var _ = new WindowCloser(window);
        {
            a.Opacity = 0;
            Assert.True(a.IsAnimating(Visual.OpacityProperty));
            Assert.Equal(1, a.Opacity);
            Assert.Equal(1, b.Opacity);
            Assert.False(b.IsAnimating(Visual.OpacityProperty));
        }
    });

    [Fact]
    public Task Xaml_DeclarationAnimatesUnderlyingProperty() => OnUi(() =>
    {
        var xaml =
            """
            <Window xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    xmlns:ca='clr-namespace:ComposeAnimation;assembly=ComposeAnimation.Avalonia'
                    Width='200' Height='200'>
              <Border x:Name='Panel' Width='80' Height='80' Opacity='1' Background='Red'>
                <ca:Animated.Properties>
                  <ca:AnimatedPropertyCollection>
                    <ca:AnimatedDouble Property='{x:Static Visual.OpacityProperty}'>
                      <ca:AnimatedDouble.Spec>
                        <ca:DoubleTweenSpec Duration='0:0:0.1'>
                          <ca:DoubleTweenSpec.Easing>
                            <LinearEasing />
                          </ca:DoubleTweenSpec.Easing>
                        </ca:DoubleTweenSpec>
                      </ca:AnimatedDouble.Spec>
                    </ca:AnimatedDouble>
                  </ca:AnimatedPropertyCollection>
                </ca:Animated.Properties>
              </Border>
            </Window>
            """;

        var window = new AnimationTestWindow(new ManualFrameSource());
        AvaloniaRuntimeXamlLoader.Load(xaml, typeof(AnimatedDouble).Assembly, rootInstance: window);
        var border = window.FindControl<Border>("Panel")!;
        var clock = (ManualFrameSource)window.FrameSource!;

        window.Show();
        Dispatcher.UIThread.RunJobs();

        using var _ = new WindowCloser(window);
        {
            Assert.Equal(Visual.OpacityProperty, Animated.GetProperties(border)![0].Property);
            Assert.IsType<DoubleTweenSpec>(((AnimatedDouble)Animated.GetProperties(border)![0]).Spec);

            border.Opacity = 0;
            Assert.True(border.IsAnimating(Visual.OpacityProperty));
            Assert.Equal(0, border.GetBaseValue(Visual.OpacityProperty).Value);
            Assert.Equal(1, border.Opacity);

            StepUntilIdle(border, Visual.OpacityProperty, clock);
            Assert.Equal(0, border.Opacity);
            Assert.False(border.IsAnimating(Visual.OpacityProperty));
        }
    });

    [Fact]
    public Task Xaml_StyleSharedCollection_IsIndependentPerControl() => OnUi(() =>
    {
        var xaml =
            """
            <Window xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    xmlns:ca='clr-namespace:ComposeAnimation;assembly=ComposeAnimation.Avalonia'
                    Width='240' Height='200'>
              <Window.Styles>
                <Style Selector='Border.fade'>
                  <Setter Property='(ca:Animated.Properties)'>
                    <ca:AnimatedPropertyCollection>
                      <ca:AnimatedDouble Property='{x:Static Visual.OpacityProperty}'>
                        <ca:AnimatedDouble.Spec>
                          <ca:DoubleTweenSpec Duration='0:0:0.1'>
                            <ca:DoubleTweenSpec.Easing>
                              <LinearEasing />
                            </ca:DoubleTweenSpec.Easing>
                          </ca:DoubleTweenSpec>
                        </ca:AnimatedDouble.Spec>
                      </ca:AnimatedDouble>
                    </ca:AnimatedPropertyCollection>
                  </Setter>
                </Style>
              </Window.Styles>
              <StackPanel>
                <Border x:Name='A' Classes='fade' Width='40' Height='40' Opacity='1' Background='Red'/>
                <Border x:Name='B' Classes='fade' Width='40' Height='40' Opacity='1' Background='Blue'/>
              </StackPanel>
            </Window>
            """;

        var window = new AnimationTestWindow(new ManualFrameSource());
        AvaloniaRuntimeXamlLoader.Load(xaml, typeof(AnimatedDouble).Assembly, rootInstance: window);
        var a = window.FindControl<Border>("A")!;
        var b = window.FindControl<Border>("B")!;
        var clockA = new ManualFrameSource();
        var clockB = new ManualFrameSource();


        window.Show();
        Dispatcher.UIThread.RunJobs();

        using var _ = new WindowCloser(window);
        {
            Assert.Same(Animated.GetProperties(a), Animated.GetProperties(b));

            a.Opacity = 0;
            Assert.True(a.IsAnimating(Visual.OpacityProperty));
            Assert.Equal(1, a.Opacity);
            Assert.Equal(1, b.Opacity);
            Assert.False(b.IsAnimating(Visual.OpacityProperty));
        }
    });

    [Fact]
    public Task OneWayBinding_IsTheDestination() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var source = new TestOpacitySource { Value = 1 };
        var border = new Border { Width = 80, Height = 80 };
        border.Bind(Visual.OpacityProperty, new Binding(nameof(TestOpacitySource.Value))
        {
            Source = source,
            Mode = BindingMode.OneWay
        });

        Animated.SetProperties(border, new AnimatedPropertyCollection
        {
            new AnimatedDouble { Property = Visual.OpacityProperty, Spec = LinearTween() }
        });
        var window = new AnimationTestWindow(clock) { Width = 200, Height = 200, Content = border };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        using var _ = new WindowCloser(window);
        {
            Assert.Equal(1, border.Opacity);
            source.Value = 0;
            Dispatcher.UIThread.RunJobs();

            Assert.True(border.IsAnimating(Visual.OpacityProperty));
            Assert.Equal(0, border.GetBaseValue(Visual.OpacityProperty).Value);
            Assert.Equal(1, border.Opacity);
        }
    });

    [Fact]
    public Task SpringRetarget_PreservesVelocity() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder(new SpringSpec<double>(1, 100, 0.001));
        using var closer = new WindowCloser(window);
        border.Opacity = 0;
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();

        // Critically damped spring: x(t) = (1 + 10t) exp(-10t).
        var position = 1.5 * Math.Exp(-0.5);
        var velocity = -5 * Math.Exp(-0.5);
        Assert.Equal(position, border.Opacity, 8);
        border.Opacity = 1;
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        var displacement = position - 1;
        var expected = 1 + (displacement + (velocity + 10 * displacement) * 0.016) * Math.Exp(-0.16);
        Assert.Equal(expected, border.Opacity, 8);
        Assert.True(border.Opacity < position, "Momentum should initially continue toward the previous destination.");
        StepUntilIdle(border, Visual.OpacityProperty, clock);
        Assert.Equal(1, border.Opacity);
    });

    [Fact]
    public Task MultipleProperties_ShareOnePendingFrame() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var closer = new WindowCloser(window);
        border.Animate(new AnimatedDouble { Property = Layoutable.WidthProperty, Spec = LinearTween() });
        border.Opacity = 0;
        border.Width = 160;
        Assert.Equal(1, clock.PendingFrameCount);
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.5, border.Opacity, 8);
        Assert.Equal(120, border.Width, 8);
        Assert.Equal(1, clock.PendingFrameCount);
        StepUntilIdle(border, Visual.OpacityProperty, clock);
        Assert.Equal(160, border.Width);
        Assert.False(border.IsAnimating(Layoutable.WidthProperty));
        Assert.Equal(0, clock.PendingFrameCount);
    });

    [Fact]
    public Task AddingDeclaration_KeepsDefaultClockAnimationRunning() => OnUi(() =>
    {
        var (window, border, _) = ShowBorder();
        using var closer = new WindowCloser(window);

        border.Opacity = 0;
        border.Animate(new AnimatedDouble { Property = Layoutable.WidthProperty, Spec = LinearTween() });
        Assert.True(border.IsAnimating(Visual.OpacityProperty));
        Assert.Equal(1, border.Opacity);
        Assert.Equal(0, border.GetBaseValue(Visual.OpacityProperty).Value);
    });

    [Fact]
    public Task StyleRemoval_AnimatesToDefaultWithoutReadingItsOwnSample() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var border = new Border { Width = 80, Height = 80 };
        border.Styles.Add(new Style(x => x.OfType<Border>().Class("dim"))
        {
            Setters = { new Setter(Visual.OpacityProperty, 0.2) }
        });
        border.Classes.Add("dim");

        border.Animate(new AnimatedDouble { Property = Visual.OpacityProperty, Spec = LinearTween() });
        var window = new AnimationTestWindow(clock) { Content = border };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        using var closer = new WindowCloser(window);
        Assert.Equal(0.2, border.Opacity);
        border.Classes.Remove("dim");
        Assert.False(border.GetBaseValue(Visual.OpacityProperty).HasValue);
        Assert.True(border.IsAnimating(Visual.OpacityProperty));
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.6, border.Opacity, 8);
        StepUntilIdle(border, Visual.OpacityProperty, clock);
        Assert.Equal(1, border.Opacity);
        Assert.False(border.GetBaseValue(Visual.OpacityProperty).HasValue);
    });

    [Fact]
    public Task InheritedProperty_IsRejectedBeforeAnimating() => OnUi(() =>
    {
        var text = new TextBlock();
        var error = Assert.Throws<NotSupportedException>(() =>
            text.Animate(new AnimatedDouble { Property = TextBlock.FontSizeProperty }));
        Assert.Contains("inherited", error.Message);
        Assert.False(text.IsAnimating(TextBlock.FontSizeProperty));
    });

    [Fact]
    public Task ReplacingCollection_UnsubscribesUnresolvedDeclarations() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var closer = new WindowCloser(window);
        var unresolved = new AnimatedDouble { Spec = LinearTween() };
        Animated.SetProperties(border, new AnimatedPropertyCollection { unresolved });
        Animated.SetProperties(border, new AnimatedPropertyCollection
        {
            new AnimatedDouble { Property = Layoutable.WidthProperty, Spec = LinearTween() }
        });
        unresolved.Property = Visual.OpacityProperty;
        border.Opacity = 0;
        Assert.Equal(0, border.Opacity);
        Assert.False(border.IsAnimating(Visual.OpacityProperty));
        border.Width = 160;
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
        Assert.Equal(120, border.Width, 8);
    });

    [Fact]
    public Task RemovingDeclarationDuringSample_PreventsLaterPublication() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var closer = new WindowCloser(window);
        var width = border.Animate(new AnimatedDouble { Property = Layoutable.WidthProperty, Spec = LinearTween() });
        border.Opacity = 0;
        border.Width = 160;
        var widthSamples = new List<double>();
        border.PropertyChanged += (_, e) =>
        {
            if (e.Priority != BindingPriority.Animation) return;
            if (e.Property == Layoutable.WidthProperty)
                widthSamples.Add(border.Width);
            if (e.Property == Visual.OpacityProperty && border.Opacity < 1)
                Animated.GetProperties(border)!.Remove(width);
        };
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
        Assert.Empty(widthSamples);
        Assert.Equal(160, border.Width);
        Assert.False(border.IsAnimating(Layoutable.WidthProperty));
        Assert.Equal(0.5, border.Opacity, 8);
    });

    [Fact]
    public Task TargetChangedByFinalSample_RetargetsBeforeReleasingBinding() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var closer = new WindowCloser(window);
        border.Opacity = 0;
        border.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.OpacityProperty && e.Priority == BindingPriority.Animation && border.Opacity == 0)
                border.Opacity = 0.75;
        };
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TweenDuration); Dispatcher.UIThread.RunJobs();
        Assert.True(border.IsAnimating(Visual.OpacityProperty));
        Assert.Equal(0, border.Opacity);
        Assert.Equal(0.75, border.GetBaseValue(Visual.OpacityProperty).Value);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.375, border.Opacity, 8);
        StepUntilIdle(border, Visual.OpacityProperty, clock);
        Assert.Equal(0.75, border.Opacity);
    });

    [Fact]
    public Task SamplingFailure_ReleasesLayerReportsErrorAndAllowsNextTarget() => OnUi(() =>
    {
        var failure = new InvalidOperationException("sample failed");
        var spec = new TweenSpec<double>(TweenDuration, easing: new FailingEasing(failure));
        var (window, border, clock) = ShowBorder(spec);
        using var closer = new WindowCloser(window);
        border.Opacity = 0;
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        Assert.Same(failure, ((AnimationTestWindow)TopLevel.GetTopLevel(border)!).AnimationRoot.Diagnostics.LastError);
        Assert.False(border.IsAnimating(Visual.OpacityProperty));
        Assert.Equal(0, border.Opacity);
        Assert.Equal(0, clock.PendingFrameCount);
        ((AnimatedDouble)Animated.GetProperties(border)![0]).Spec = LinearTween();
        border.Opacity = 1;
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.5, border.Opacity, 8);
        StepUntilIdle(border, Visual.OpacityProperty, clock);
        Assert.Equal(1, border.Opacity);
    });


    [Fact]
    public Task Reattachment_StartsFreshFromCurrentBase() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var closer = new WindowCloser(window);
        border.Opacity = 0;
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
        window.Content = null;
        Assert.Equal(0, border.Opacity);
        border.Opacity = 0.25;
        window.Content = border;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.25, border.Opacity);
        Assert.False(border.IsAnimating(Visual.OpacityProperty));
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs(); // Drain the canceled request from before detachment.
        border.Opacity = 0.75;
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.5, border.Opacity, 8);
        StepUntilIdle(border, Visual.OpacityProperty, clock);
        Assert.Equal(0.75, border.Opacity);
    });

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0.75)]
    public Task DurationScale_IsAppliedToSamples(double scale, double expected) => OnUi(() =>
    {
        MotionDurationScale.ScaleFactor = scale;
        try
        {
            var (window, border, clock) = ShowBorder();
            using var closer = new WindowCloser(window);
            border.Opacity = 0;
            clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
            clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
            Assert.Equal(expected, border.Opacity, 8);
            StepUntilIdle(border, Visual.OpacityProperty, clock);
            Assert.Equal(0, border.Opacity);
        }
        finally { MotionDurationScale.Reset(); }
    });

    [Fact]
    public Task AutoWidth_SnapsAndSubsequentFiniteTargetsAnimate() => OnUi(() =>
    {
        var (window, border, clock) = ShowBorder();
        using var closer = new WindowCloser(window);
        border.Animate(new AnimatedDouble { Property = Layoutable.WidthProperty, Spec = LinearTween() });
        border.Width = double.NaN;
        Assert.True(double.IsNaN(border.Width));
        Assert.False(border.IsAnimating(Layoutable.WidthProperty));
        border.Width = 100;
        Assert.Equal(100, border.Width);
        Assert.False(border.IsAnimating(Layoutable.WidthProperty));
        border.Width = 200;
        clock.AdvanceBy(Frame); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50)); Dispatcher.UIThread.RunJobs();
        Assert.Equal(150, border.Width, 8);
        Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(border)!).AnimationRoot.Diagnostics.LastError);
    });

    private sealed class FailingEasing(Exception failure) : Easing
    {
        public override double Ease(double progress) => throw failure;
    }

    private sealed class FailingClock : IFrameSource
    {
        public Exception Failure { get; } = new InvalidOperationException("clock failed");
        public int Requests { get; private set; }
        public void VerifyAccess() { }
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
        {
            Requests++;
            return ValueTask.FromException<TimeSpan>(Failure);
        }
    }

    private sealed class TestOpacitySource : AvaloniaObject
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<TestOpacitySource, double>(nameof(Value));

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}
