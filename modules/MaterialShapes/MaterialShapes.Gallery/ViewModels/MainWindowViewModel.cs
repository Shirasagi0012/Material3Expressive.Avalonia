using CommunityToolkit.Mvvm.ComponentModel;
using MaterialShapes.Gallery.Models;

namespace MaterialShapes.Gallery.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        Morphs = new MorphsViewModel();
        Morphs.EditRequested += OpenEditor;
        CurrentViewModel = Morphs;
    }

    public MorphsViewModel Morphs { get; }

    [ObservableProperty] public partial ViewModelBase CurrentViewModel { get; set; }

    private void OpenEditor(ShapeParameters parameters)
    {
        var editor = new ShapeEditorViewModel(parameters);
        editor.CloseRequested += () => CurrentViewModel = Morphs;
        CurrentViewModel = editor;
    }
}
