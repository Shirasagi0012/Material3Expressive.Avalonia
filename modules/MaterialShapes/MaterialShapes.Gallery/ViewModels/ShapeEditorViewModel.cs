using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialShapes;
using MaterialShapes.Gallery.Models;

namespace MaterialShapes.Gallery.ViewModels;

public sealed partial class ShapeEditorViewModel : ViewModelBase
{
    private readonly ShapeParameters _parameters;

    public ShapeEditorViewModel(ShapeParameters parameters)
    {
        _parameters = parameters;
        _parameters.PropertyChanged += (_, _) => UpdatePreview();
        UpdatePreview();
    }

    public ShapeParameters Parameters => _parameters;

    [ObservableProperty] public partial RoundedPolygon? PreviewShape { get; set; }
    [ObservableProperty] public partial bool Debug { get; set; }
    [ObservableProperty] public partial bool AutoSize { get; set; } = true;
    [ObservableProperty] public partial string OutputText { get; set; } = string.Empty;

    public event Action? CloseRequested;

    [RelayCommand]
    private void Accept()
    {
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void NextBaseShape()
    {
        Parameters.ShapeIndex = (Parameters.ShapeIndex + 1) % Parameters.Shapes.Count;
        UpdatePreview();
    }

    [RelayCommand]
    private void OutputDetails()
    {
        var shape = Parameters.SelectedShape.ShapeGen();
        OutputText = Parameters.ShapeDetails + "\nSVG:\n" + ShapeUtilities.ToSvgString(shape);
    }

    private void UpdatePreview()
    {
        var shape = Parameters.GenerateShape(autoSize: AutoSize);
        PreviewShape = AutoSize ? shape.Normalized() : shape;
    }

    partial void OnAutoSizeChanged(bool value)
    {
        UpdatePreview();
    }
}
