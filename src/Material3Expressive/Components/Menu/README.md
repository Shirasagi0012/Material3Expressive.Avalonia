# Menu

库提供独立 DropdownMenu / DropdownMenuPopup，以及 Avalonia 原生 MenuFlyout / Menu 的 Material 主题。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)并初始化窗口动画作用域。

## 基础用法

独立菜单在视图中声明，以 PlacementTarget 指定锚点。下面的事件处理器放在所属视图代码中。

```xml
<Grid xmlns="https://github.com/avaloniaui"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <Button x:Name="MenuAnchor" Content="菜单" Click="OpenMenu" />
  <m3:DropdownMenu x:Name="Actions" PlacementTarget="{Binding #MenuAnchor}"
                   DismissRequested="DismissMenu">
    <m3:DropdownMenuItem Content="复制" LeadingIcon="{m3:Symbol ContentCopy}" Click="Copy" />
    <Separator />
    <m3:DropdownMenuItem Content="不可用" IsEnabled="False" />
  </m3:DropdownMenu>
</Grid>
```

```csharp
private void OpenMenu(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Actions.IsOpen = true;
private void DismissMenu(object? sender, Material3Expressive.Foundation.Overlays.DismissRequestedEventArgs e) => Actions.IsOpen = false;
private void Copy(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    // 完成业务操作后关闭菜单。
    Actions.IsOpen = false;
}
```

## 变体与属性

| 入口 | 用途及状态归属 |
| --- | --- |
| DropdownMenu | 自带表面、滚动、定位和开闭动画，IsOpen 由应用管理 |
| DropdownMenuPopup + DropdownMenuGroup | 组合多组内容、组间距及滚动容器 |
| DropdownMenuItem | 默认 Baseline，显式 MaterialExpressiveDropdownMenuItemTheme 可选 Expressive 外观 |
| CheckableDropdownMenuItem / SelectableDropdownMenuItem | 默认 Expressive，由应用写回 IsChecked / IsSelected |
| 原生 MenuFlyout / MenuItem | 原生选择、组互斥、键盘及关闭行为 |
| 原生 Menu | 水平菜单栏，沿用原生访问键和子菜单 |

独立菜单的 DismissRequested 发出关闭请求，IsPresented 表示呈现内容是否仍存在。项目支持 Click / Command / CommandParameter。
勾选项目通过 CheckedChangeRequested.Value 请求新值，单选通过 Click / Command 写回选择。尾部快捷键文本用于展示。
普通菜单支持上下键导航，Popup 支持 Tab、Escape / Back 和焦点恢复，相关行为可通过 IsPopupFocusable、DismissOnBack、DismissOnPointerOutside、ConsumePointerInputOutside 配置。

原生 flyout 默认使用 Vertical / Standard。设置 `MenuAppearance.ColorStyle="Vibrant"` 切换配色，设置 FlyoutPresenterTheme 选择 Baseline。

```xml
<Button Content="原生菜单">
  <Button.Flyout>
    <MenuFlyout m3:MenuAppearance.ColorStyle="Vibrant">
      <MenuItem Header="只读" ToggleType="CheckBox" IsChecked="True" StaysOpenOnClick="True"
                m3:MenuAppearance.SupportingText="保护文件内容"
                m3:MenuAppearance.TrailingContent="{m3:Symbol Lock}" />
      <MenuItem Header="排序">
        <MenuItem Header="名称" ToggleType="Radio" GroupName="Sort" IsChecked="True" />
        <MenuItem Header="日期" ToggleType="Radio" GroupName="Sort" />
      </MenuItem>
    </MenuFlyout>
  </Button.Flyout>
</Button>
```

后续片段沿用基础示例命名空间。Baseline 的资源名为 MaterialBaselineMenuTheme，默认 Vertical 为 MaterialVerticalMenuTheme。
原生项目的业务选择使用 IsChecked，IsSelected 表达导航位置。InputGesture、Command、StaysOpenOnClick 和 GroupName 遵循原生语义。

### 共享交互源

独立项目和 DropdownMenuGroup 可设置 InteractionSource，null 使用内部源。调用需在 UI 线程，结束事件引用同一开始对象。

```csharp
using Material3Expressive.Foundation.Interaction;

var source = new MaterialInteractionSource();
item.InteractionSource = source;
var hover = new MaterialInteractionEvent.HoverEnter();
source.Emit(hover);
source.Emit(new MaterialInteractionEvent.HoverExit(hover));
```

源提供交互反馈，业务选择与关闭仍由应用控制。项目替换源后接收新事件，组的悬浮外观保留到新源下一事件重新求值。

## 内容槽与定制

独立项目支持 Content、LeadingIcon、SelectedLeadingIcon、TrailingContent、SupportingText 及对应模板。
仅选中图标存在时使用展开和淡入效果。MenuDefaults.ItemShape / GroupShape 按调用方提供的 index、count 生成首尾形状，组间距由容器设置，默认参考 MenuDefaults.GroupSpacing。
同时提供普通和选中图标时按选择状态直接替换；运行中添加普通图标会结束选中图标的展开/淡入淡出，移除后重新按当前选择状态建立动画分支。内容或模板均适用，Catalog 提供可切换示例。
DropdownMenuPopup 可组合 StackPanel 和多个 DropdownMenuGroup，内容需要滚动时显式放置 ScrollViewer。

MenuAppearance.ColorStyle 可从 Popup 传到组和项目。Padding、CornerRadius、颜色及 `Comp.Menu` Token 可局部覆盖，MenuItemShapes / MenuGroupShapes 的 null 字段继承主题。
Offset 使用 DIP，X 随 RTL 反向，PositionProvider 使用物理像素。AnchorPosition 的 Left / Right 与逻辑 Start / End 行为一致。
DropdownMenu.ScrollOffset 可双向绑定，MaxHeight 限制视口。

原生 Vertical 项支持 `MenuAppearance.SupportingText` / `SupportingTextTemplate` 和
`MenuAppearance.TrailingContent` / `TrailingContentTemplate`。辅助文字使用 BodyMedium，位于标题下方；
尾部内容最小宽度为 20 DIP，小内容居中。只设置模板也会显示对应槽。
尾部内容可与原生 InputGesture 和子菜单箭头并存，依次排列；辅助文字存在时项目外边距为 4×2 DIP。
这些附加属性不改变原生点击、命令、选择和关闭逻辑，Baseline 项不呈现新增槽。

原生菜单保留 ItemsSource、ItemTemplate、显式 ItemContainerTheme 和 MenuItem.Theme。
菜单栏可直接组合原生 MenuItem，标签中的下划线定义访问键。

```xml
<Menu>
  <MenuItem Header="_File">
    <MenuItem Header="_Open" InputGesture="Ctrl+O" />
    <Separator />
    <MenuItem Header="E_xit" />
  </MenuItem>
</Menu>
```

## 使用限制与注意事项

独立菜单使用当前窗口的 PopupOverlayLayer，自定义窗口模板需提供 PART_VisualLayerManager。形状配置使用圆角，平台系统 inset 和预测返回尚未提供。
原生菜单主层和子菜单采用无阴影外观，独立菜单保留 Material 阴影。原生 flyout 的组级形变和开闭动画尚未提供。
菜单栏沿用 Alt 访问键与方向键，当前原生版本的 F10 激活尚未实现。公开菜单栏 keyed 主题仍有待确认，维护状态见内部契约。

[MenuDemo](../../../Material3Expressive.Catalog/MenuDemo.axaml)展示独立和原生菜单，[MenuBarDemo](../../../Material3Expressive.Catalog/MenuBarDemo.axaml)展示菜单栏。
内部机制、已确认适配和验证范围见 [INTERNALS.md](INTERNALS.md)。
