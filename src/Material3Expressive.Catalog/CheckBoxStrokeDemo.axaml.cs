using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ComposeAnimation;
using Material3Expressive.Components;
using SkiaSharp;

namespace Material3Expressive.Catalog;

public partial class CheckBoxStrokeDemo : Window
{
    private readonly List<SKPathEffect> _effects = [];
    public CheckBoxStrokeDemo()
    {
        AnimationScope.Initialize(this); InitializeComponent();
        Opened += (_, _) => UpdateEffects();
        PropertyChanged += (_, e) => { if (e.Property.Name == nameof(RenderScaling) && IsVisible) UpdateEffects(); };
        Closed += (_, _) => { foreach (var effect in _effects) effect.Dispose(); _effects.Clear(); };
    }

    private void UpdateEffects()
    {
        var density = (float)RenderScaling;
        var corner = SKPathEffect.CreateCorner(3 * density);
        var dash = SKPathEffect.CreateDash([2 * density, 2 * density], density);
        var composed = SKPathEffect.CreateCompose(dash, corner);
        _effects.AddRange([corner, dash, composed]);
        CheckBoxAppearance.SetCheckmarkStroke(Rounded, new() { Width = 3, Cap = PenLineCap.Round, PathEffect = corner });
        CheckBoxAppearance.SetCheckmarkStroke(Dashed, new() { Width = 2, PathEffect = composed });
        CheckBoxAppearance.SetOutlineStroke(Outlined, new() { Width = 2, Cap = PenLineCap.Flat, PathEffect = dash });
    }
}
