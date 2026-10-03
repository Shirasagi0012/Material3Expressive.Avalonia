/*
 * Portions translated from AndroidX Slider.kt.
 * Copyright 2022 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

// Only the template's geometry is adapted. Slider retains its PART_Track, native
// Thumb and button handlers; this code does not capture pointers or write Value.
internal sealed class MaterialSliderLayout : Panel
{
    private bool Vertical => TemplatedParent is Slider { Orientation: Orientation.Vertical };
    private float Scale => (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
    private int Pixels(double dip) => SliderMath.Round((float)dip * Scale);
    protected override Size MeasureOverride(Size available)
    {
        if (Children.Count != 2) return default;
        var painter = Children[0]; var track = Children[1];
        var space = new Size(Math.Max(available.Width, Vertical ? 16 : 4), Math.Max(available.Height, Vertical ? 4 : 16));
        track.Measure(space);
        var offset = Pixels(Vertical ? track.DesiredSize.Height : track.DesiredSize.Width) / Scale;
        painter.Measure(Vertical ? new Size(space.Width, Math.Max(0, space.Height - offset))
            : new Size(Math.Max(0, space.Width - offset), space.Height));
        var width = Vertical ? Math.Max(track.DesiredSize.Width, painter.DesiredSize.Width) : painter.DesiredSize.Width + offset;
        var height = Vertical ? painter.DesiredSize.Height + offset : Math.Max(track.DesiredSize.Height, painter.DesiredSize.Height);
        return new(Math.Min(available.Width, Math.Max(48, width)), Math.Min(available.Height, Math.Max(48, height)));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2 || Children[0] is not SliderTrack painter || Children[1] is not MaterialSliderTrack track) return finalSize;
        var scale = Scale;
        var width = Pixels(finalSize.Width); var height = Pixels(finalSize.Height);
        var thumbWidth = Pixels(track.DesiredSize.Width); var thumbHeight = Pixels(track.DesiredSize.Height);
        var trackWidth = Vertical ? Pixels(painter.DesiredSize.Width) : Math.Max(0, width - thumbWidth);
        var trackHeight = Vertical ? Math.Max(0, height - thumbHeight) : Pixels(painter.DesiredSize.Height);
        var innerWidth = Math.Max(width, Vertical ? Math.Max(trackWidth, thumbWidth) : trackWidth + thumbWidth);
        var innerHeight = Math.Max(height, Vertical ? trackHeight + thumbHeight : Math.Max(trackHeight, thumbHeight));
        var x = (width - innerWidth) / 2; var y = (height - innerHeight) / 2;
        var px = Vertical ? (innerWidth - trackWidth) / 2 : thumbWidth / 2;
        var py = Vertical ? thumbHeight / 2 : (innerHeight - trackHeight) / 2;
        painter.Arrange(new Rect((x + px) / scale, (y + py) / scale, trackWidth / scale, trackHeight / scale));
        track.Painter = painter;
        track.Arrange(new Rect(x / scale, y / scale, innerWidth / scale, innerHeight / scale));
        return finalSize;
    }
}

internal sealed class MaterialSliderTrack : Track
{
    internal SliderTrack? Painter;
    protected override Size ArrangeOverride(Size finalSize)
    {
        // Initialize native Track's value density and drag bookkeeping before adapting
        // final piece placement. Slider handles input using the track's full bounds.
        base.ArrangeOverride(finalSize);
        if (Thumb is not { } thumb || Painter is not { } painter) return finalSize;
        var scale = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        int Pixels(double dip) => SliderMath.Round((float)dip * scale);
        var vertical = Orientation == Orientation.Vertical;
        var width = Pixels(finalSize.Width); var height = Pixels(finalSize.Height);
        var tw = Pixels(thumb.DesiredSize.Width); var th = Pixels(thumb.DesiredSize.Height);
        var length = Math.Max(0, (vertical ? height - th : width - tw));
        var fraction = Maximum == Minimum ? 0 : (float)Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1);
        var corner = double.IsNaN(painter.TrackCornerSize) ? Pixels(vertical ? painter.Bounds.Width : painter.Bounds.Height) / 2
            : Pixels(painter.TrackCornerSize);
        var ticks = painter.TickFractions;
        var offset = ticks.Length > 0 && fraction != 0 && fraction != 1
            ? SliderMath.Round((length - corner * 2) * fraction) + corner : SliderMath.Round(length * fraction);
        var reverse = vertical ? !IsDirectionReversed : IsDirectionReversed;
        var physical = reverse ? length - offset : offset;
        thumb.Arrange(vertical ? new Rect((width - tw) / 2 / scale, physical / scale, tw / scale, th / scale)
            : new Rect(physical / scale, (height - th) / 2 / scale, tw / scale, th / scale));
        var extent = vertical ? th : tw;
        void Button(Control? button, int start, int end)
        {
            var span = Math.Max(0, end - start);
            button?.Arrange(vertical ? new Rect(0, start / scale, width / scale, span / scale)
                : new Rect(start / scale, 0, span / scale, height / scale));
        }
        Button(reverse ? IncreaseButton : DecreaseButton, 0, physical);
        Button(reverse ? DecreaseButton : IncreaseButton, physical + extent, length + extent);
        painter.InvalidateVisual();
        return finalSize;
    }
}
