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

// SliderImpl / RangeSliderImpl: measure thumbs first, offset track constraints, then
// place in physical integer pixels. Minimum interactive space encloses the visual layout.
internal sealed class SliderLayout : Panel
{
    protected override bool BypassFlowDirectionPolicies => true;
    private float Scale => (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
    private bool Vertical => TemplatedParent is VerticalSlider;
    private int Pixels(double dip) => SliderMath.Round((float)dip * Scale);
    protected override Size MeasureOverride(Size available)
    {
        if (Children.Count < 2) return default;
        // requiredSizeIn (4 x 16, transposed vertically) sits inside the caller's
        // constrained size. Its visual layout may overflow a still smaller outer slot.
        var contentSpace = new Size(Math.Max(available.Width, Vertical ? 16 : 4), Math.Max(available.Height, Vertical ? 4 : 16));
        var track = Children[0]; var thumb = Children[1];
        thumb.Measure(contentSpace);
        if (Children.Count == 3) Children[2].Measure(contentSpace);
        var offset = Children.Count == 3 ? (Pixels(thumb.DesiredSize.Width) + Pixels(Children[2].DesiredSize.Width)) / 2
            : Pixels(Vertical ? thumb.DesiredSize.Height : thumb.DesiredSize.Width);
        var trackSpace = Vertical ? new Size(contentSpace.Width, Math.Max(0, contentSpace.Height - offset / Scale))
            : new Size(Math.Max(0, contentSpace.Width - offset / Scale), contentSpace.Height);
        track.Measure(trackSpace);
        var width = Vertical ? Math.Max(thumb.DesiredSize.Width, track.DesiredSize.Width) : track.DesiredSize.Width + offset / Scale;
        var height = Vertical ? track.DesiredSize.Height + offset / Scale : Math.Max(thumb.DesiredSize.Height, track.DesiredSize.Height);
        if (Children.Count == 3) height = Math.Max(height, Children[2].DesiredSize.Height);
        return new(Math.Min(available.Width, Math.Max(48, width)), Math.Min(available.Height, Math.Max(48, height)));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 2) return finalSize;
        var scale = Scale; var track = Children[0]; var thumb = Children[1];
        var width = Pixels(finalSize.Width); var height = Pixels(finalSize.Height);
        var thumbWidth = Pixels(thumb.DesiredSize.Width); var thumbHeight = Pixels(thumb.DesiredSize.Height);
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        void Place(Control control, int x, int y, int w, int h) => control.Arrange(new Rect((rtl ? width - x - w : x) / scale, y / scale, w / scale, h / scale));
        var trackControl = (track as ContentPresenter)?.Child as SliderTrack;
        var corner = trackControl is null ? 0
            : TemplatedParent is RangeSlider || double.IsNaN(trackControl.TrackCornerSize)
                ? Pixels(Vertical ? track.DesiredSize.Width : track.DesiredSize.Height) / 2
                : Pixels(trackControl.TrackCornerSize);
        int Offset(int length, float fraction, float[] ticks, int correction = 0) => ticks.Length > 0 && fraction != ticks[0] && fraction != ticks[^1]
            ? SliderMath.Round((length - corner * 2) * fraction + correction) + corner : SliderMath.Round(length * fraction + correction);
        if (TemplatedParent is Slider slider)
        {
            var state = slider.ActualState;
            var trackWidth = Vertical ? Pixels(track.DesiredSize.Width) : Math.Max(0, width - thumbWidth);
            var trackHeight = Vertical ? Math.Max(0, height - thumbHeight) : Pixels(track.DesiredSize.Height);
            var innerWidth = Vertical ? Math.Max(trackWidth, thumbWidth) : trackWidth + thumbWidth;
            var innerHeight = Vertical ? trackHeight + thumbHeight : Math.Max(trackHeight, thumbHeight);
            var outerX = (width - innerWidth) / 2; var outerY = (height - innerHeight) / 2;
            var trackX = Vertical ? (innerWidth - trackWidth) / 2 : thumbWidth / 2;
            var trackY = Vertical ? thumbHeight / 2 : (innerHeight - trackHeight) / 2;
            var offset = Offset(Vertical ? trackHeight : trackWidth, state.CoercedValueAsFraction, state.TickFractions);
            var thumbX = Vertical ? (innerWidth - thumbWidth) / 2 : offset + trackX - thumbWidth / 2;
            var thumbY = Vertical ? (state.ReverseVerticalDirection ? trackHeight - offset : offset) + trackY - thumbHeight / 2 : (innerHeight - thumbHeight) / 2;
            state.ThumbWidth = thumbWidth; state.ThumbHeight = thumbHeight; state.TotalWidth = innerWidth; state.TotalHeight = innerHeight;
            Place(track, outerX + trackX, outerY + trackY, trackWidth, trackHeight);
            Place(thumb, outerX + thumbX, outerY + thumbY, thumbWidth, thumbHeight);
        }
        else if (TemplatedParent is RangeSlider range)
        {
            var end = Children[2]; var endWidth = Pixels(end.DesiredSize.Width); var endHeight = Pixels(end.DesiredSize.Height);
            var innerWidth = Math.Max(width, (thumbWidth + endWidth) / 2); var outerX = (width - innerWidth) / 2;
            var trackWidth = Math.Max(0, innerWidth - (thumbWidth + endWidth) / 2); var trackHeight = Pixels(track.DesiredSize.Height);
            var innerHeight = Math.Max(trackHeight, Math.Max(thumbHeight, endHeight)); var outerY = (height - innerHeight) / 2;
            var state = range.ActualState;
            state.TotalWidth = innerWidth; state.StartThumbWidth = thumbWidth; state.StartThumbHeight = thumbHeight; state.EndThumbWidth = endWidth; state.EndThumbHeight = endHeight; state.UpdateMinMaxPx();
            Place(track, outerX + thumbWidth / 2, outerY + (innerHeight - trackHeight) / 2, trackWidth, trackHeight);
            Place(thumb, outerX + Offset(trackWidth, state.StartFraction, state.TickFractions), outerY + (innerHeight - thumbHeight) / 2, thumbWidth, thumbHeight);
            Place(end, outerX + Offset(trackWidth, state.EndFraction, state.TickFractions, (thumbWidth - endWidth) / 2), outerY + (innerHeight - endHeight) / 2, endWidth, endHeight);
        }
        return finalSize;
    }
}
