# BasicAlertDialog / AlertDialog

`AlertDialog` 用于需要用户确认或取消的模态提示，提供图标、标题、正文和动作槽；
`BasicAlertDialog` 只提供对话框尺寸与宿主，外观由内容自行组合。它们都是库控件，
继承 [Foundation Dialog](../../Foundation/Overlays/README.md)，不占声明处的布局空间。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)，包括 Window 加载内容前的动画初始化。
维护与移植说明见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

下面的 Window 与 code-behind 放在已完成公共接入的应用中，无需 ViewModel。
使用标准 Window 模板；自定义模板须符合 [Dialog 宿主要求](../../Foundation/Overlays/README.md#接入)。

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
        x:Class="ComponentExamples.DialogExample" Width="600" Height="400">
  <Grid>
    <Button Content="显示提示" Click="OnOpen"
            HorizontalAlignment="Center" VerticalAlignment="Center" />
    <m3:AlertDialog x:Name="Notice" Title="更改已保存"
                    DismissRequested="OnDismissRequested">
      <TextBlock Text="可以继续编辑这份记录。" TextWrapping="Wrap" />
      <m3:AlertDialog.ConfirmButton>
        <Button Content="知道了" Click="OnConfirm"
                Theme="{StaticResource MaterialTextButtonTheme}" />
      </m3:AlertDialog.ConfirmButton>
    </m3:AlertDialog>
  </Grid>
</Window>
```

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;
using Material3Expressive.Foundation.Overlays;

namespace ComponentExamples;

public partial class DialogExample : Window
{
    public DialogExample()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
    }

    private void OnOpen(object? sender, RoutedEventArgs e) => Notice.IsOpen = true;
    private void OnConfirm(object? sender, RoutedEventArgs e) => Notice.IsOpen = false;
    private void OnDismissRequested(object? sender, DismissRequestedEventArgs e)
        => Notice.IsOpen = false;
}
```

应用入口需打开该窗口。也可绑定 ViewModel 的布尔属性到 `IsOpen`，在
`ConfirmButton` / `DismissButton` 中绑定业务命令；启用编译绑定时为视图声明对应 `x:DataType`。

## 打开、关闭与内容槽

必须提供 `ConfirmButton` 内容或模板。控件不创建按钮、不连接自定义按钮事件，也不会因
点击确认或取消按钮自动关闭。`DismissRequested` 同样只是请求，由应用修改 `IsOpen`。
未处理的 Escape KeyDown、Back 和外部点击可发出请求，分别由 `DismissOnEscape`、
`DismissOnBack`、`DismissOnPointerOutside` 控制，默认均启用。
`IsPresented` 在退出动画中仍可为 true；需要等待视觉层消失时不要只检查 `IsOpen`。

- `Content` / `ContentTemplate` 是正文；`BasicAlertDialog` 的同名槽是全部自定义内容
- `Icon`、`Title`、`ConfirmButton`、`DismissButton` 均有相应的 `…Template`
- 非 null 模板表示槽存在，即使模板输出为空；组合控件作为一个槽布局项
- 按钮使用原生 `Click` / `Command` / `CommandParameter`，其主题与颜色由按钮本身处理

后续 XAML 片段沿用上面的命名空间。自定义布局可用 Basic 入口：

```xml
<m3:BasicAlertDialog Width="380" UsePlatformDefaultWidth="False">
  <m3:MaterialSurface CornerRadius="28" Padding="24">
    <TextBox PlaceholderText="自定义内容" />
  </m3:MaterialSurface>
</m3:BasicAlertDialog>
```

片段省略开关和关闭逻辑；为它设置 `IsOpen` 后才会显示。Basic 默认不添加 Surface、
padding、阴影或内容颜色。不提供已废弃的任意内容 AlertDialog 重载。

## 尺寸与外观

Basic 默认宽度为 280–560 DIP，无默认高度；显式 `Width` / `MinWidth` / `MaxWidth` 可替换
相应 Material 默认约束，例如 `Width="100"` 可以小于 280。默认还受 Foundation 平台宽度上限
约束：按客户区短边选择 320 / 440 / 580 DIP；`UsePlatformDefaultWidth="False"` 只关闭该平台上限。

AlertDialog 自带 24 DIP 容器留白。图标居中；有图标时标题居中，否则靠 start。
正文使用剩余高度但不主动撑满，不自动滚动；较长正文应自行使用 `ScrollViewer`。
按钮横排时 LTR 下取消在左、确认在右，空间不足则确认在上；RTL 调整水平位置。

用 `Background` / `CornerRadius` 定制容器，`IconForeground` / `TitleForeground` /
`TextForeground` 定制对应槽。默认提供 HeadlineSmall / BodyMedium / LabelLarge 文字环境，
自定义子控件的局部字体与颜色仍可覆盖。图标尺寸由内容设置，默认 IconSize Token 为 24 DIP。
`TonalElevation` 默认 0；标准 AlertDialog 不自动添加阴影。
统一外观优先使用[主题与 Token 接入](../../../../docs/GETTING_STARTED.md#主题与定制)。

## 使用边界

系统/IME inset、平台安全窗口选项和自定义最小交互尺寸环境尚未实现；正文不会自动避让软键盘。
真实输入法、屏幕阅读器模态隔离、触屏、多显示器 DPI 切换与跨平台冻结行为尚未完成验收。
Catalog 的 [AlertDialogDemo](../../../Material3Expressive.Catalog/AlertDialogDemo.axaml) 提供带完整
打开、确认、取消和关闭请求处理的例子；测试范围见 [INTERNALS](INTERNALS.md#一致性边界与验证)。
