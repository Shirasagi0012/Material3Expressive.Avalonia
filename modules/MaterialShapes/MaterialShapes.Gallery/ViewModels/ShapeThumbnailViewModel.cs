using CommunityToolkit.Mvvm.ComponentModel;
using MaterialShapes;

namespace MaterialShapes.Gallery.ViewModels;

public sealed partial class ShapeThumbnailViewModel : ObservableObject
{
    public ShapeThumbnailViewModel(int index, RoundedPolygon shape)
    {
        Index = index;
        Shape = shape;
    }

    public int Index { get; }

    [ObservableProperty] public partial RoundedPolygon Shape { get; set; }
}
