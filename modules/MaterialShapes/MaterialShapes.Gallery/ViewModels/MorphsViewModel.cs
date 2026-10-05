using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialShapes;
using MaterialShapes.Gallery.Models;

namespace MaterialShapes.Gallery.ViewModels;

public sealed partial class MorphsViewModel : ViewModelBase
{
    private readonly List<ShapeParameters> _shapeParameters;
    private readonly List<RoundedPolygon> _shapes;
    private readonly ObservableCollection<ShapeThumbnailViewModel> _shapeThumbnails;

    private readonly TimeSpan _animationDuration = TimeSpan.FromMilliseconds(600);
    private double _t = 0;
    [ObservableProperty] private bool _isAnimating = false;

    public MorphsViewModel()
    {
        _shapeParameters = BuildDefaultShapes();
        _shapes = _shapeParameters.Select(sp => sp.GenerateShape().Normalized()).ToList();

        _shapeThumbnails = new ObservableCollection<ShapeThumbnailViewModel>(
            _shapes.Select((shape, index) => new ShapeThumbnailViewModel(index, shape)));
        ShapeThumbnails = new ReadOnlyObservableCollection<ShapeThumbnailViewModel>(_shapeThumbnails);

        foreach (var shape in _shapeParameters)
            shape.PropertyChanged += OnShapeParametersChanged;

        //_animationTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, OnAnimationTick);

        SelectedShapeIndex = 0;
        CurrentShapeIndex = 0;
        UpdateMorph();
    }

    public ReadOnlyObservableCollection<ShapeThumbnailViewModel> ShapeThumbnails { get; }

    //public IReadOnlyList<ShapeParameters> ShapeParameters => _shapeParameters;

    [ObservableProperty] public partial RoundedPolygon? From { get; set; }
    [ObservableProperty] public partial RoundedPolygon? To { get; set; }
    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial bool Debug { get; set; }
    [ObservableProperty] public partial int SelectedShapeIndex { get; set; }
    [ObservableProperty] public partial int CurrentShapeIndex { get; set; }

    public event Action<ShapeParameters>? EditRequested;

    [RelayCommand]
    private void SelectShape(int index)
    {
        if (index < 0 || index >= _shapes.Count)
            return;

        CurrentShapeIndex = SelectedShapeIndex;
        SelectedShapeIndex = index;
        UpdateMorph();
        StartAnimation();
    }

    [RelayCommand]
    private void Edit()
    {
        EditRequested?.Invoke(_shapeParameters[SelectedShapeIndex]);
    }

    [RelayCommand]
    private void ToggleDebug()
    {
        Debug = !Debug;
    }

    [RelayCommand]
    private void Replay()
    {
        StartAnimation();
    }

    partial void OnSelectedShapeIndexChanged(int value)
    {
        UpdateMorph();
    }

    private void UpdateMorph()
    {
        if (_shapes.Count == 0)
            return;

        From = _shapes[Math.Clamp(CurrentShapeIndex, 0, _shapes.Count - 1)];
        To = _shapes[Math.Clamp(SelectedShapeIndex, 0, _shapes.Count - 1)];
        Progress = 0;
    }

    private void StartAnimation()
    {
        IsAnimating = true;
        _t = 0;
    }

    internal void OnAnimationTick(TimeSpan ts)
    {
        var dt = ts.TotalMilliseconds / _animationDuration.TotalMilliseconds;
        _t += dt;
        if (_t >= 1)
        {
            Progress = 1;
            IsAnimating = false;
            _t = 0;
            return;
        }

        var eased = 1 - Math.Pow(1 - _t, 3);
        Progress = eased;
    }

    private void OnShapeParametersChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not ShapeParameters parameters)
            return;

        var index = _shapeParameters.IndexOf(parameters);
        if (index < 0)
            return;

        _shapes[index] = parameters.GenerateShape().Normalized();
        _shapeThumbnails[index].Shape = _shapes[index];

        if (index == SelectedShapeIndex || index == CurrentShapeIndex)
            UpdateMorph();
    }

    private static List<ShapeParameters> BuildDefaultShapes()
    {
        return
        [
            new ShapeParameters(sides: 4, roundness: 1, shapeId: ShapeParameters.ShapeId.Polygon),
            new ShapeParameters(sides: 12, innerRadius: .928, roundness: .1, shapeId: ShapeParameters.ShapeId.Star),
            new ShapeParameters(sides: 4, innerRadius: .352, roundness: .32, rotation: 45, shapeId: ShapeParameters.ShapeId.Star),
            new ShapeParameters(innerRadius: 0.1, roundness: 0.22, shapeId: ShapeParameters.ShapeId.Triangle),
            new ShapeParameters(sides: 8, innerRadius: .784, roundness: .16, shapeId: ShapeParameters.ShapeId.Star),

            new ShapeParameters(sides: 15, innerRadius: .892, roundness: 1, shapeId: ShapeParameters.ShapeId.Star),
            new ShapeParameters(innerRadius: .19, roundness: 0.86, rotation: -45, shapeId: ShapeParameters.ShapeId.Blob),
            new ShapeParameters(innerRadius: .19, roundness: 0.86, rotation: 45, shapeId: ShapeParameters.ShapeId.Blob),
            new ShapeParameters(sides: 12, innerRadius: .928, roundness: .928, shapeId: ShapeParameters.ShapeId.Star),
            new ShapeParameters(sides: 3, roundness: .2, rotation: 30, shapeId: ShapeParameters.ShapeId.Polygon),

            new ShapeParameters(roundness: .4, shapeId: ShapeParameters.ShapeId.CornerSE),
            new ShapeParameters(sides: 4, rotation: 45, shapeId: ShapeParameters.ShapeId.Polygon),
            new ShapeParameters(sides: 5, rotation: -360.0 / 20.0, shapeId: ShapeParameters.ShapeId.Polygon),
            new ShapeParameters(sides: 5, rotation: -360.0 / 20.0, innerRadius: .3, shapeId: ShapeParameters.ShapeId.Star),
            new ShapeParameters(sides: 8, innerRadius: .6, shapeId: ShapeParameters.ShapeId.Star)
        ];
    }
}
