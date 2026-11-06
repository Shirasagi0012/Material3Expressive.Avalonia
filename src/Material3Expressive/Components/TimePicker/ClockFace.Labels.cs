// Portions translated from AndroidX TimePicker.kt (f3ed195f0c9e).
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.MarkupExtensions;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;
using static Material3Expressive.Tokens.MaterialTokens;
using Spring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

internal sealed partial class ClockFace
{
    internal static readonly StyledProperty<Spring?> EffectsMotionProperty =
        AvaloniaProperty.Register<ClockFace, Spring?>(nameof(EffectsMotion));
    public Spring? EffectsMotion { get => GetValue(EffectsMotionProperty); set => SetValue(EffectsMotionProperty, value); }
    private static readonly object HourScreen = TimeSelection.Hour, MinuteScreen = TimeSelection.Minute;
    private Crossfade? _crossfade;
    private readonly ClockLabels?[] _screens = new ClockLabels?[2];
    private Transition<TimeSelection>? _labelTransition;
    private CancellationTokenSource? _labelLoopCancellation;
    private Task? _labelLoop;

    private void UpdateLabels()
    {
        if (EffectsMotion is not { Stiffness: > 0 } effects) return;
        var selection = _controller.Selection;
        if (_crossfade is null)
        {
            _labelTransition = new Transition<TimeSelection>(selection, "Clock labels");
            _crossfade = new Crossfade
            {
                ClipToBounds = false,
                Content = selection == TimeSelection.Hour ? HourScreen : MinuteScreen,
                Source = ContentSource.From(_labelTransition, screen => screen == TimeSelection.Hour ? HourScreen : MinuteScreen),
                Spec = effects.ToAnimationSpec(),
                ContentTemplate = new FuncDataTemplate<TimeSelection>((screen, _) => Screen(screen)),
            };
            LogicalChildren.Add(_crossfade); VisualChildren.Insert(0, _crossfade);
            StartLabelTransition(false);
        }
        _crossfade.Spec = effects.ToAnimationSpec();
        _labels = Screen(selection).Labels;
        foreach (var screen in _screens.OfType<ClockLabels>()) screen.Refresh();
        _lastSelection = selection; _last24 = _controller.Is24Hour;
        if (_labelTransition!.TargetState != selection)
        {
            _labelLoopCancellation?.Cancel();
            _labelTransition.UpdateTarget(selection);
            StartLabelTransition(true);
        }
        InvalidateMeasure();

        ClockLabels Screen(TimeSelection screen) => _screens[(int)screen] ??= new ClockLabels(this, screen) { Tag = screen };
    }

    private void StartLabelTransition(bool defer)
    {
        if (_scope is not { } scope || _labelTransition is not { } transition) return;
        var previous = _labelLoop;
        _labelLoopCancellation?.Cancel();
        var old = _labelLoopCancellation;
        var cancellation = _labelLoopCancellation = new CancellationTokenSource();
        _labelLoop = scope.Launch(async ct =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellation.Token);
            if (previous is not null) try { await previous; } catch (OperationCanceledException) { }
            old?.Dispose(); if (linked.IsCancellationRequested) return;
            // updateTransition's new segment is committed in recomposition, after the
            // preceding segment's frame. Reuse the library's transition and Crossfade.
            if (defer && !await scope.WaitForNextFrameAsync(linked.Token)) return;
            await transition.RunAsync(scope, linked.Token);
        }, "Clock labels crossfade");
    }

    private void ReleaseLabels()
    {
        _labelLoopCancellation?.Cancel();
        _labelLoopCancellation?.Dispose(); _labelLoopCancellation = null; _labelLoop = null;
        if (_crossfade is { } crossfade) { VisualChildren.Remove(crossfade); LogicalChildren.Remove(crossfade); }
        _crossfade = null; _labelTransition = null;
        Array.Clear(_screens); _labels = []; _lastSelection = null;
    }

    private void UpdateLabelSelection()
    {
        foreach (var screen in _screens.OfType<ClockLabels>())
        foreach (var label in screen.Labels.OfType<ClockLabel>())
            label.SelectorSelected = label.Bounds.Contains(SelectorPosition);
    }

    // Compose's selected semantics follows the moving selector, independently from
    // the discrete value used to choose the keyboard focus target.
    private sealed class ClockLabel : Button
    {
        public static readonly DirectProperty<ClockLabel, bool> SelectorSelectedProperty =
            AvaloniaProperty.RegisterDirect<ClockLabel, bool>(nameof(SelectorSelected), x => x.SelectorSelected);
        private bool _selectorSelected;
        public bool SelectorSelected { get => _selectorSelected; set => SetAndRaise(SelectorSelectedProperty, ref _selectorSelected, value); }
        protected override Type StyleKeyOverride => typeof(Button);
        protected override AutomationPeer OnCreateAutomationPeer() => new ClockLabelPeer(this);
        private sealed class ClockLabelPeer : ButtonAutomationPeer, ISelectionItemProvider
        {
            private readonly ClockLabel _label;
            public ClockLabelPeer(ClockLabel label) : base(label)
            {
                _label = label;
                label.PropertyChanged += (_, change) =>
                {
                    if (change.Property == SelectorSelectedProperty)
                        RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, change.OldValue, change.NewValue);
                };
            }
            public bool IsSelected => _label.SelectorSelected;
            public ISelectionProvider? SelectionContainer => null;
            public void Select() => Invoke();
            public void AddToSelection() => Invoke();
            public void RemoveFromSelection() { }
        }
    }

    // The outgoing screen retains its own numbers, but both screens read the live
    // selection for 24-hour inner-ring presence, exactly as ClockFace's Crossfade body.
    private sealed class ClockLabels(ClockFace owner, TimeSelection screen) : Panel
    {
        internal List<Button> Labels { get; } = [];
        internal void Refresh()
        {
            var active = screen == owner._controller.Selection;
            var inner = owner._controller.Is24Hour && owner._controller.Selection == TimeSelection.Hour;
            var count = inner ? 24 : 12;
            while (Labels.Count > count)
            {
                var label = Labels[^1]; Labels.RemoveAt(Labels.Count - 1); Children.Remove(label);
            }
            while (Labels.Count < count)
            {
                var button = new ClockLabel { Padding = new(0) };
                MaterialInteraction.GetOrCreate(button).RippleRadius = 24;
                TimeEntryController.BindDimension(button, WidthProperty, Comp.TimePicker.ClockDialLabelContainerSize);
                TimeEntryController.BindDimension(button, HeightProperty, Comp.TimePicker.ClockDialLabelContainerSize);
                button.Bind(StyledElement.ThemeProperty, new DynamicResourceExtension("MaterialClockLabelTheme"));
                button.Click += (_, _) =>
                {
                    if (screen != owner._controller.Selection) return;
                    owner.ActivateLabel(button, keyboard: false);
                };
                Labels.Add(button); Children.Add(button);
            }
            for (var i = 0; i < Labels.Count; i++)
            {
                var value = screen == TimeSelection.Minute ? i % 12 * 5 : i % 12 == 0 ? 12 : i % 12;
                if (inner) value = i < 12 ? value % 12 : i;
                Labels[i].Content = value.ToString();
                Labels[i].Focusable = active && owner._dialFocusable;
                Labels[i].IsHitTestVisible = active;
                Labels[i].Classes.Set("selected", owner.IsSelected(value));
                AutomationProperties.SetName(Labels[i], $"{value} {(owner._controller.Selection == TimeSelection.Hour ? "hours" : "minutes")}");
            }
            InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var label in Labels) label.Measure(new(owner.LabelSize, owner.LabelSize));
            return new(owner.DialSize, owner.DialSize);
        }
        protected override Size ArrangeOverride(Size size)
        {
            var scale = owner.Density;
            for (var i = 0; i < Labels.Count; i++)
            {
                var angle = RadiansPerHour * (i % 12) - QuarterCircle;
                var radius = (float)(size.Height * scale) * (i >= 12 ? 69f / 256 : 101f / 256);
                // CircularLayout places its integer-sized children using roundToInt.
                var centerX = (int)(size.Width * scale) / 2 - (int)(owner.LabelSize * scale) / 2;
                var centerY = (int)(size.Height * scale) / 2 - (int)(owner.LabelSize * scale) / 2;
                var x = Math.Floor(centerX + Math.Cos(angle) * radius + .5) / scale;
                var y = Math.Floor(centerY + Math.Sin(angle) * radius + .5) / scale;
                Labels[i].Arrange(new(x, y, owner.LabelSize, owner.LabelSize));
            }
            owner.UpdateLabelSelection();
            return size;
        }
    }
}
