using Avalonia;
using Avalonia.Controls;

namespace MaterialShapes.Gallery.Views;

public partial class MorphsView : UserControl
{
    private TopLevel? _topLevel;
    private TimeSpan? _lastFrameTime = TimeSpan.Zero;

    public MorphsView()
    {
        InitializeComponent();
        DataContextProperty.Changed.AddClassHandler<MorphsView>((x, e) =>
        {
            if (e.OldValue is ViewModels.MorphsViewModel oldVm)
            {
                oldVm.PropertyChanged -= x.Vm_PropertyChanged;
            }

            if (e.NewValue is ViewModels.MorphsViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
            }
        });
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.MorphsViewModel.IsAnimating) &&
            sender is ViewModels.MorphsViewModel { IsAnimating: true } vm)
        {
            _lastFrameTime = null;
            _topLevel?.RequestAnimationFrame(OnFrame);
        }
    }

    void OnFrame(TimeSpan ts)
    {
        if (DataContext is ViewModels.MorphsViewModel vm)
        {
            if (_lastFrameTime is not { } last)
                vm.OnAnimationTick(TimeSpan.Zero);
            else
                vm.OnAnimationTick(ts - last);

            _lastFrameTime = ts;

            if (vm.IsAnimating)
            {
                _topLevel?.RequestAnimationFrame(OnFrame);
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel = null;
    }
}
