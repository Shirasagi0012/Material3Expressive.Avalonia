# 接入与运行

本文面向使用当前源码的 Avalonia 应用。组件示例共用这里的主题、字体和动画初始化，
不需要在每个组件处重复配置。先运行 Catalog 是检查本机依赖是否齐备的最短路径。

## 源码与依赖

需要 .NET 10 SDK；VisualParity 脚本另需 PowerShell。
Avalonia、SkiaSharp 与外部依赖的版本见根目录 [Directory.Packages.props](../Directory.Packages.props)，
包源及映射见 [nuget.config](../nuget.config)。仓内模块保留各自的项目及中央包配置，
不要只升级其中一个包而忽略原生渲染依赖。

```powershell
git clone https://github.com/Shirasagi0012/Material3Expressive.Avalonia.git
cd Material3Expressive.Avalonia
```

当前仓库不使用 Git 子模块。ComposeAnimation、MaterialShapes、MaterialSkiaShadow 和
MaterialTypography.Roboto 直接随仓库提供，主库通过仓内 ProjectReference 消费它们。
MaterialColorUtilities、MaterialSymbols Rounded 与 SmoothScroll 使用 `Shirasagi0012.*` NuGet 包；
不需要相邻依赖仓库、旧 submodule 目录或预先执行本地打包脚本。

首次使用需让自己的 NuGet 环境能读取 GitHub Packages 源
`https://nuget.pkg.github.com/Shirasagi0012/index.json`。身份验证方式见
[GitHub Packages 的 NuGet 说明](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)；
凭证放在用户配置或受支持的凭证管理器中，不写入仓库。若 restore 返回 401/403，先检查账户的包读取权限。

```powershell
dotnet restore src/Material3Expressive.Catalog/Material3Expressive.Catalog.csproj
dotnet build src/Material3Expressive.Catalog/Material3Expressive.Catalog.csproj --no-restore
dotnet run --project src/Material3Expressive.Catalog --no-restore
# 直接打开 TextField 示例
dotnet run --project src/Material3Expressive.Catalog --no-restore -- --text-fields
```

### 源码接入与发布包

本指南通过项目引用使用当前源码 API。主库包 ID 为 `Shirasagi0012.Material3Expressive.Avalonia`，
目标包源与上文相同；Roboto 字体模块使用独立包 `Shirasagi0012.MaterialTypography.Roboto`。
安装发布包时，以包源实际可用版本及对应版本文档为准，不把项目文件中的默认 Version 当作已经发布的证明。

仓库的 [发布工作流](../.github/workflows/publish-nuget.yml)通过手动运行选择 ref，
使用各项目 csproj 声明的版本，打包 ComposeAnimation Runtime/Avalonia/TestSupport、
MaterialShapes、Shadow、Roboto 与主库，再按依赖顺序推送至 GitHub Packages。
工作流不会从 tag 名推导版本。
新增工作流、源码构建成功或[已有 Release](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/releases)
都不单独证明某次包发布已完成。不要从配套模块的历史版本推断主库可用版本；旧版包不保证包含当前源码的 API。

## 最小桌面应用

以下示例放在本仓库 `examples/HelloMaterial/`，因此继承根目录的中央包版本与包源配置。
复制到仓库外时，需要按实际位置调整项目路径，并为应用配置同一组中央版本与 NuGet 包源；
不要把省略 `Version` 的 PackageReference 单独复制到没有中央版本管理的项目。

### HelloMaterial.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Avalonia" />
        <PackageReference Include="Avalonia.Desktop" />
        <PackageReference Include="Avalonia.Themes.Fluent" />
        <ProjectReference Include="../../src/Material3Expressive/Material3Expressive.csproj" />
        <ProjectReference Include="../../modules/MaterialTypography.Roboto/MaterialTypography.Roboto.csproj" />
    </ItemGroup>
</Project>
```

### App.axaml

先加载基础主题，再加载 Material 主题。这里与 Catalog 一致使用 Fluent，为尚未由 Material
覆盖的控件（例如编辑菜单）提供模板；Material 同时提供自己的 ScrollViewer/ScrollBar 主题。

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:styles="clr-namespace:Material3Expressive.Styles;assembly=Material3Expressive"
             x:Class="HelloMaterial.App"
             RequestedThemeVariant="Default">
    <Application.Styles>
        <FluentTheme />
        <styles:MaterialTheme />
    </Application.Styles>
</Application>
```

### App.axaml.cs

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace HelloMaterial;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
```

### Program.cs

`WithRobotoFont()` 注册随仓库提供的 Roboto；`UseRoundedSymbols()` 注册 Material Symbols
Rounded 图标。两者与 [Catalog 启动代码](../src/Material3Expressive.Catalog/Program.cs)一致。
使用自己的字体时，可不引用 Roboto 模块，但应配置相应排版资源，而不是依赖每台机器拥有同名字体。

```csharp
using System;
using Avalonia;
using MaterialSymbols.Avalonia;
using MaterialTypography.Roboto;

namespace HelloMaterial;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithRobotoFont()
        .UseRoundedSymbols()
        .LogToTrace();
}
```

### MainWindow.axaml

`m3` 是主题库的 XAML 命名空间；普通 Button / TextBox 仍使用 Avalonia 类型。
这里无需 ViewModel，即可检查主题资源与输入效果。

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
        x:Class="HelloMaterial.MainWindow"
        Title="Hello Material" Width="420" Height="300">
    <StackPanel Margin="24" Spacing="16">
        <TextBox m3:MaterialTextField.Label="姓名" PlaceholderText="请输入姓名" />
        <Button Content="保存" />
    </StackPanel>
</Window>
```

### MainWindow.axaml.cs

每个窗口（TopLevel）在加载 XAML/内容前初始化动画作用域。不要把下面的调用遗漏为纯主题配置，
也不要为每个 Button / TextBox 各建根作用域。

```csharp
using Avalonia.Controls;
using ComposeAnimation;

namespace HelloMaterial;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
    }
}
```

保存这六个文件后，从仓库根目录运行：

```powershell
dotnet run --project examples/HelloMaterial/HelloMaterial.csproj
```

## 主题与定制

普通图标可用 markup extension 构建，组件会提供合适的默认尺寸：

```xml
<Button Content="保存" m3:MaterialButton.Icon="{m3:Symbol Save}" />
<TextBox m3:MaterialTextField.LeadingIcon="{m3:Symbol Search}" />
<m3:NavigationRailItem Icon="{m3:Symbol Home, Fill=1}" Label="首页" />
```

需要显式大小时用 `{m3:Symbol Home, Size=32}`，或给 MaterialSymbol 设置 Width / Height。
自定义图标仍可直接传 Control；数据图标使用对应的 IconTemplate：

```xml
<m3:NavigationRailItem Icon="{Binding Destination}" Label="{Binding Title}" x:CompileBindings="False">
    <m3:NavigationRailItem.IconTemplate>
        <DataTemplate>
            <m3:MaterialSymbol Symbol="{Binding Symbol}" />
        </DataTemplate>
    </m3:NavigationRailItem.IconTemplate>
</m3:NavigationRailItem>
```

模板内的 DataContext 是 Icon 数据；Icon 为 null 时显式模板仍会创建内容。
模板可以共享，创建出来的图标控件实例不能同时用于多个槽。
IconButton 保留 `Symbol="Save"` 的专用入口。默认尺寸的继承与布局边界见[架构说明](ARCHITECTURE.md#颜色图标与阴影)。

- `RequestedThemeVariant="Default"` 跟随系统；`Light` / `Dark` 可固定外观，明暗资源变化无需替换组件 Theme
- 单个控件优先使用其原生属性、附加属性或命名主题；需要统一应用外观时，再使用系统/组件 Token 覆盖
- 排版及普通 TextBlock 的用法见 [Typography](../src/Material3Expressive/Foundation/Typography/README.md)
- Token 解析、颜色、资源作用域及动画职责见 [架构说明](ARCHITECTURE.md)；组件专属默认值见各组件 README
- 外观依赖真实字体与平台渲染，构建或 headless 测试通过不等于各平台文字、IME 和视觉一致性已验收

继续阅读 [Button](../src/Material3Expressive/Components/Button/README.md) 或
[TextField](../src/Material3Expressive/Components/TextField/README.md)，或从[全部组件](COMPONENTS.md)查找其他入口。
