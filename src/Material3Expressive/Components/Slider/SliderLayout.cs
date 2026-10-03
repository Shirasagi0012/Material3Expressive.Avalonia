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
using Avalonia.Controls.Presenters;
using Avalonia.Media;

namespace Material3Expressive.Components;

// RangeSliderImpl: measure both thumbs before the track, preserving the physical
// integer placement and unequal-thumb correction of the fixed Compose source.
internal sealed class SliderLayout : Panel
{
    protected override bool BypassFlowDirectionPolicies => true;
    private float Scale => (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
    private int Pixels(double dip) => SliderMath.Round((float)dip * Scale);
    protected override Size MeasureOverride(Size available)
    {
        if (Children.Count != 3) return default;
        var contentSpace = new Size(Math.Max(available.Width, 4), Math.Max(available.Height, 16));
        var track = Children[0]; var start = Children[1]; var end = Children[2];
        start.Measure(contentSpace); end.Measure(contentSpace);
        var offset = (Pixels(start.DesiredSize.Width) + Pixels(end.DesiredSize.Width)) / 2;
        track.Measure(new Size(Math.Max(0, contentSpace.Width - offset / Scale), contentSpace.Height));
        var width = track.DesiredSize.Width + offset / Scale;
        var height = Math.Max(track.DesiredSize.Height, Math.Max(start.DesiredSize.Height, end.DesiredSize.Height));
        return new(Math.Min(available.Width, Math.Max(48, width)), Math.Min(available.Height, Math.Max(48, height)));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 3 || TemplatedParent is not RangeSlider range) return finalSize;
        var scale = Scale; var track = Children[0]; var start = Children[1]; var end = Children[2];
        var width = Pixels(finalSize.Width); var height = Pixels(finalSize.Height);
        var startWidth = Pixels(start.DesiredSize.Width); var startHeight = Pixels(start.DesiredSize.Height);
        var endWidth = Pixels(end.DesiredSize.Width); var endHeight = Pixels(end.DesiredSize.Height);
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        void Place(Control control, int x, int y, int w, int h) => control.Arrange(new Rect((rtl ? width - x - w : x) / scale, y / scale, w / scale, h / scale));
        var corner = (track as ContentPresenter)?.Child is SliderTrack ? Pixels(track.DesiredSize.Height) / 2 : 0;
        int Offset(int length, float fraction, float[] ticks, int correction = 0) => ticks.Length > 0 && fraction != ticks[0] && fraction != ticks[^1]
            ? SliderMath.Round((length - corner * 2) * fraction + correction) + corner : SliderMath.Round(length * fraction + correction);
        var innerWidth = Math.Max(width, (startWidth + endWidth) / 2); var outerX = (width - innerWidth) / 2;
        var trackWidth = Math.Max(0, innerWidth - (startWidth + endWidth) / 2); var trackHeight = Pixels(track.DesiredSize.Height);
        var innerHeight = Math.Max(trackHeight, Math.Max(startHeight, endHeight)); var outerY = (height - innerHeight) / 2;
        var state = range.ActualState;
        state.TotalWidth = innerWidth; state.StartThumbWidth = startWidth; state.StartThumbHeight = startHeight;
        state.EndThumbWidth = endWidth; state.EndThumbHeight = endHeight; state.UpdateMinMaxPx();
        Place(track, outerX + startWidth / 2, outerY + (innerHeight - trackHeight) / 2, trackWidth, trackHeight);
        Place(start, outerX + Offset(trackWidth, state.StartFraction, state.TickFractions), outerY + (innerHeight - startHeight) / 2, startWidth, startHeight);
        Place(end, outerX + Offset(trackWidth, state.EndFraction, state.TickFractions, (startWidth - endWidth) / 2), outerY + (innerHeight - endHeight) / 2, endWidth, endHeight);
        return finalSize;
    }
}
