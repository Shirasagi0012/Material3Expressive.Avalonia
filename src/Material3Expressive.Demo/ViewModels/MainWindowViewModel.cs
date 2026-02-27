using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Material3Expressive.Themes.Tokens;

namespace Material3Expressive.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public IReadOnlyList<TypographyRoleToken> TextStyles { get; } = Enum.GetValues<TypographyRoleToken>();

    [ObservableProperty]
    private TypographyRoleToken selectedTextStyle = TypographyRoleToken.DisplayLarge;

    public string Greeting { get; } = "Welcome to Avalonia!";
}
