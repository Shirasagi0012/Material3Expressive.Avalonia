using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Material3Expressive.Foundation;

namespace Material3Expressive.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public IReadOnlyList<TypographyRole> TextStyles { get; } = Enum.GetValues<TypographyRole>();

    [ObservableProperty]
    private TypographyRole selectedTextStyle = TypographyRole.DisplayLarge;

    public string Greeting { get; } = "Welcome to Avalonia!";
}
