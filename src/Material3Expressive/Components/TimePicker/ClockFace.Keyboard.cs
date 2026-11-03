// Portions translated from AndroidX TimePicker.kt / ClockText / ClockFace.
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
using Avalonia.Controls;
using Avalonia.Input;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

internal sealed partial class ClockFace
{
    private bool _dialFocusable;
    private bool _keyboardSelecting;

    internal void SetDialFocusable(bool value)
    {
        _dialFocusable = value;
        foreach (var label in _labels) label.Focusable = value;
    }

    internal void Activate()
    {
        SetDialFocusable(true);
        for (var i = 0; i < _labels.Count; i++)
            if (IsSelected(ClockValue(i)))
            {
                _labels[i].Focus(NavigationMethod.Directional);
                break;
            }
    }

    private void DialKeyDown(object? sender, KeyEventArgs e)
    {
        var index = _labels.FindIndex(label => label.IsFocused);
        if (index < 0) return;
        var delta = e.Key is Key.Right or Key.Down ? 1 : e.Key is Key.Left or Key.Up ? -1 : 0;
        if (delta != 0)
        {
            // The ordered children are the outer circle followed by the inner circle. Both
            // boundaries loop through that flattened order, independently of layout direction.
            _labels[(index + delta + _labels.Count) % _labels.Count].Focus(NavigationMethod.Directional);
            e.Handled = true;
        }
        else if (e.Key == Key.Tab)
        {
            _controller.LeaveDial(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
            e.Handled = true;
        }
        else if (e.Key is Key.Enter or Key.Space)
            e.Handled = true; // ClockText activates on KeyUp and does not start a click ripple.
    }

    private void DialKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space)) return;
        var index = _labels.FindIndex(label => label.IsFocused);
        if (index < 0) return;
        if (_scope is { } scope && _angle is { } angle)
        {
            _ = scope.Launch(ct => _angleMutex.MutateAsync(async token =>
            {
                _keyboardSelecting = true;
                try
                {
                    if (_controller.Selection == TimeSelection.Minute) _controller.SetMinute(index * 5);
                    else _controller.SetHour(_controller.Is24Hour ? index : index % 12 + (_controller.IsPm ? 12 : 0));
                }
                finally { _keyboardSelecting = false; }
                var target = TargetAngle;
                while (_position - target > MathF.PI) target += MathF.Tau;
                while (_position - target <= -MathF.PI) target -= MathF.Tau;
                await angle.SnapToAsync(target, cancellationToken: token);
                return true;
            }, MutatePriority.UserInput, ct), "Clock keyboard selection");
        }
        // Unlike pointer selection, keyboard activation never advances to minutes.
        e.Handled = true;
    }
}
