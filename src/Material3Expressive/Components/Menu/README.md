# Menu

## 独立 DropdownMenu

`DropdownMenu` 提供普通 Material 菜单的 Surface、滚动、定位和进入/退出动画。
`DropdownMenuPopup` 只提供弹出宿主、Column 和动画，可组合多个 `DropdownMenuGroup`。
加载主题和初始化窗口动画环境见[公共接入](../../../../docs/GETTING_STARTED.md)。
这两个控件在可视树中声明，不占父布局空间；`PlacementTarget` 指定锚点。

以下事件处理器位于宿主 code-behind，`m3` 命名空间与下方原生示例相同：

```xml
<Grid>
  <Button x:Name="MenuAnchor" Content="菜单" Click="OpenMenu" />
  <m3:DropdownMenu x:Name="Actions" PlacementTarget="{Binding #MenuAnchor}"
                   DismissRequested="DismissMenu">
    <m3:DropdownMenuItem Content="复制" LeadingIcon="{m3:Symbol ContentCopy}"
                         TrailingContent="Ctrl+C" Click="Copy" />
    <Separator />
    <m3:DropdownMenuItem Content="不可用" IsEnabled="False" />
  </m3:DropdownMenu>
</Grid>
```

```csharp
private void OpenMenu(object? sender, RoutedEventArgs e) => Actions.IsOpen = true;
private void DismissMenu(object? sender, DismissRequestedEventArgs e) => Actions.IsOpen = false;
private void Copy(object? sender, RoutedEventArgs e)
{
    // 执行业务操作后，由调用方决定是否关闭。
    Actions.IsOpen = false;
}
```

`IsOpen` 可以绑定 ViewModel，目标变更在下一动画帧合并。`DismissRequested` 只发出请求，不自动改写状态；
`IsPresented` 表示当前呈现内容是否存在，正常退出会保留到动画结束；进入中断的状态顺序遵循固定上游，
详见 [INTERNALS](INTERNALS.md)。菜单项支持 `Click`、`Command` 和 `CommandParameter`，
但不会自动关闭、互斥或切换业务状态。尾部的快捷键文字只负责呈现，不注册快捷键。

普通 `DropdownMenuItem` 默认使用 Baseline 布局。需要 Expressive 动作项时设置
`Theme="{StaticResource MaterialExpressiveDropdownMenuItemTheme}"`。
`CheckableDropdownMenuItem` 和 `SelectableDropdownMenuItem` 默认使用 Expressive 布局：

```xml
<m3:DropdownMenuPopup IsOpen="{Binding IsMenuOpen}" PlacementTarget="{Binding #MenuAnchor}"
                      DismissRequested="DismissGroupedMenu" m3:MenuAppearance.ColorStyle="Vibrant">
  <m3:DropdownMenuPopup.ItemsPanel>
    <ItemsPanelTemplate><StackPanel Spacing="2" /></ItemsPanelTemplate>
  </m3:DropdownMenuPopup.ItemsPanel>
  <m3:DropdownMenuGroup>
    <m3:CheckableDropdownMenuItem Content="只读" SupportingText="保留文档内容"
        IsChecked="{Binding IsReadOnly}" SelectedLeadingIcon="{m3:Symbol Check}"
        CheckedChangeRequested="ChangeReadOnly" />
  </m3:DropdownMenuGroup>
</m3:DropdownMenuPopup>
```

此片段的 `ChangeReadOnly` 将 `MenuCheckedChangeEventArgs.Value` 写回 ViewModel；
`DismissGroupedMenu` 将 `IsMenuOpen` 设为 false。单选项由 Click/Command 写回 `IsSelected`，
同组其他项目也由业务方更新。没有原生 MenuItem 的 `GroupName` 或自动勾选行为。

- `Shapes` 使用 `MenuDefaults.ItemShape(index, count)` / `GroupShape(index, count)`；位置由调用方给出，
  分隔线不会隐式成为组边界。`MenuDefaults.GroupSpacing` 为 2 DIP。
- `LeadingIcon`、`SelectedLeadingIcon`、`TrailingContent`、`SupportingText` 都有对应模板槽；
  正文使用 `Content` / `ContentTemplate`。只有选中图标、没有普通 leading icon 时，选中会动画展开/淡入；
  两种图标都存在时按上游直接替换。辅助文字使用 BodyMedium，正文使用 LabelLarge。
- `MenuAppearance.ColorStyle` 的 Standard / Vibrant 可从独立 Popup 继承到分组和菜单项。
  颜色、Padding、CornerRadius 及 Token 仍可覆盖。`MenuItemShapes` / `MenuGroupShapes` 的 null 字段保留主题默认值。
- `Offset` 使用 DIP，X 在 RTL 中反向。`AnchorPosition` 和 `PositionProvider` 可定制候选位置；
  定位器使用物理像素。固定上游的 Left/Right 实际使用逻辑 Start/End 算法，本实现保留这一行为。
- `DropdownMenu.ScrollOffset` 可双向绑定；`MaxHeight` 限制滚动视口。可组合 Popup 本身不自动滚动，
  按上游由内容显式放置 ScrollViewer。子菜单同样组合另一个 Popup，以菜单项为锚点。
- 普通 DropdownMenu 用上下键移动焦点；所有可聚焦 Popup 支持 Tab、Escape/Back、焦点恢复。
  `IsPopupFocusable`、`DismissOnBack`、`DismissOnPointerOutside`、`ConsumePointerInputOutside` 控制相关行为。
  `ClippingEnabled` 控制定位的最终窗口夹取，`UsePlatformDefaultWidth` 默认 false。

宿主限定当前窗口 PopupOverlayLayer。自定义 Window 模板需提供 `PART_VisualLayerManager`。
形状接口目前使用现有 CornerRadius 能力；任意 Compose Shape / CutCornerShape、平台系统 inset 和预测返回
尚未实现。独立菜单已加入 Linux 双端视觉测试，覆盖范围及共享绘制差异见 [INTERNALS](INTERNALS.md)；
真实桌面输入、屏幕阅读器和文字栅格仍需验证。
[MenuDemo](../../../Material3Expressive.Catalog/MenuDemo.axaml) 同时提供原生和独立菜单示例。

## 原生 MenuFlyout

原生 Avalonia `MenuFlyout` 的 Material 3 模板。加载 `MaterialTheme.axaml` 后，默认使用
Vertical / Standard；菜单项、原生生成的容器和子菜单自动使用配套外观。
窗口的动画环境初始化遵循[应用接入](../../../../docs/ARCHITECTURE.md)。

## 默认与 Vibrant

```xml
<Button Content="菜单">
  <Button.Flyout>
    <MenuFlyout>
      <MenuItem Header="复制" Icon="{m3:Symbol ContentCopy}"
                InputGesture="Ctrl+C" Command="{Binding CopyCommand}" />
      <MenuItem Header="只读" ToggleType="CheckBox"
                IsChecked="{Binding IsReadOnly}" StaysOpenOnClick="True" />
      <Separator />
      <MenuItem Header="排序">
        <MenuItem Header="名称" ToggleType="Radio" GroupName="Sort"
                  IsChecked="True" />
        <MenuItem Header="日期" ToggleType="Radio" GroupName="Sort" />
      </MenuItem>
    </MenuFlyout>
  </Button.Flyout>
</Button>
```

`m3` 是 `https://github.com/Shirasagi0012/Material3Expressive.Avalonia`。
使用 Vibrant 时只需给 flyout 加一项设置：

```xml
<MenuFlyout m3:MenuAppearance.ColorStyle="Vibrant">
  <MenuItem Header="选中的项目" ToggleType="CheckBox" IsChecked="True" />
</MenuFlyout>
```

Standard 的选中背景是 TertiaryContainer，Vibrant 的选中背景是 Tertiary。
配色可在运行时更改，并传递到子菜单。单个 `MenuItem` 也可覆盖该附加属性。

## 切换到 Baseline

使用同一个 `MenuFlyout`，选择另一套主题即可：

```xml
<MenuFlyout FlyoutPresenterTheme="{StaticResource MaterialBaselineMenuTheme}">
  <MenuItem Header="复制" Command="{Binding CopyCommand}" />
  <MenuItem Header="设置" />
</MenuFlyout>
```

| 主题资源 | 作用 |
| --- | --- |
| `MaterialVerticalMenuTheme` | Vertical 容器，默认主题 |
| `MaterialBaselineMenuTheme` | Baseline 容器及其配套菜单项 |
| `MaterialVerticalMenuItemTheme` | 需要显式指定时可用于菜单项 |
| `MaterialBaselineMenuItemTheme` | 需要显式指定时可用于菜单项 |

Baseline 不消费 `MenuAppearance.ColorStyle`。两种布局使用不同 `ControlTheme`，
不通过 classes 把两套模板拼在一起。显式指定 `MenuItem.Theme` 或原生
`MenuFlyout.ItemContainerTheme` 时，使用调用方提供的主题。

对应的 C#：

```csharp
using Avalonia.Controls;
using Avalonia.Styling;
using Material3Expressive.Components;

var flyout = new MenuFlyout();
flyout.Items.Add(new MenuItem { Header = "复制", Command = viewModel.CopyCommand });
MenuAppearance.SetColorStyle(flyout, MenuColorStyle.Vibrant);
flyout.ShowAt(button);

// 再次打开时选择 Baseline。资源从已经加载 MaterialTheme 的控件查找。
flyout.Hide();
flyout.FlyoutPresenterTheme =
    (ControlTheme)button.FindResource("MaterialBaselineMenuTheme")!;
flyout.ShowAt(button);
```

## 状态、配置与边界

- Vertical 的业务选中状态使用原生 `IsChecked`。`IsSelected` 是原生菜单的导航状态，
  不用它表示业务选中。`ToggleType` 控制复选/单选行为；原生 `GroupName` 处理单选互斥。
- 点击、`Command` / `CommandParameter` / `CanExecute`、键盘导航、快捷键文本、
  `StaysOpenOnClick`、关闭和定位沿用 Avalonia。`InputGesture` 的执行规则也沿用原生 API。
- `ItemsSource`、`ItemTemplate` 和多级子菜单可继续使用。`Separator` 及
  `MenuItem Header="-"` 都是分隔线，不表示新的 Material 菜单组。
- 默认值来自 `MenuTokenSource` 和 `MaterialTokens.Comp.Menu`，通过现有 Token 系统覆盖。
  调整单个项目时也可使用原生 `Background`、`Foreground`、`Padding`、`CornerRadius`。
- 菜单项支持状态层、ripple、Vertical 选中背景和圆角动画。原生 flyout 的打开/关闭动画、
  独立分组/组间空隙、组悬停形变、Compose 的 supportingText / trailingContent 槽及
  选中图标展开/淡入淡出在原生主题路径中暂未提供；独立控件见上文。
- 弹层宿主选择沿用原生 `MenuFlyout`。主菜单和子菜单均不绘制阴影：关闭 Material 自绘阴影和平台阴影提示。
  独立 DropdownMenu 仍保留 Material 阴影。
  此适配及验证边界见 [INTERNALS.md](INTERNALS.md)。

[MenuDemo](../../../Material3Expressive.Catalog/MenuDemo.axaml) 展示三种外观、嵌套子菜单、
单选/复选、暗色及 RTL。源码依据、适配和验证入口见 [INTERNALS.md](INTERNALS.md)。

## 菜单栏（Menu）

M3 没有菜单栏。`Menu` 的默认主题（[MenuBar.axaml](MenuBar.axaml)）由现有菜单部件组合而成：

```xml
<Menu>
  <MenuItem Header="_File">
    <MenuItem Header="_Open…" InputGesture="Ctrl+O" />
    <MenuItem Header="Open _recent">
      <MenuItem Header="Report.docx" />
    </MenuItem>
    <Separator />
    <MenuItem Header="E_xit" />
  </MenuItem>
  <MenuItem Header="_View">
    <MenuItem Header="_Word wrap" ToggleType="CheckBox" IsChecked="True" />
  </MenuItem>
</Menu>
```

- 栏本身是透明的水平行，没有容器色和阴影；需要底色时设置 `Menu.Background`（见下方提议）。
- 顶层项：`LabelLarge`、`OnSurface`，水平内边距 12（Baseline 菜单项内边距），40 DIP
  容器（Small 按钮高度）位于 48 DIP 最小触控区内，状态层圆角为菜单项的 `ItemShape`；
  悬停、按下和焦点经 `MaterialInteraction`。顶层不显示快捷键文字和子菜单箭头，`Icon` 可选。
- `:open` 及没有自身状态层时的 `:selected`（键盘导航、子菜单已打开且焦点在子菜单内）
  显示持久高亮：内容色叠加焦点状态层不透明度。没有复用 Vertical 的“选中”色
  （TertiaryContainer），因为它在 M3 菜单里表示已勾选/选中的项，用于打开状态会与子菜单内的
  勾选项混淆。Vertical 菜单项新增同样的 `:selected` 高亮（叠加在容器色上），MenuFlyout 同样生效。
- 子菜单：弹层与 Vertical 菜单项的子菜单相同（`MaterialMenuBarItemTheme` 基于
  `MaterialVerticalMenuItemTheme`），任意层级的子项使用 Vertical 项主题，包括勾选/单选、
  分隔线、快捷键文字和禁用。显式设置 `ItemContainerTheme` 的父项仍以其为准。
- 键盘和访问键沿用 Avalonia：Alt 激活菜单栏、Alt+字母打开对应菜单、左右箭头在顶层间移动、
  下箭头/Enter 打开、Esc 关闭；`_File` 由 `AccessText` 渲染下划线。Avalonia 12.1.2 并未处理
  F10（`IMainMenu` 的注释提到但没有实现），主题不额外补充。
- RTL 下项目从右向左排列，子菜单弹层随 Avalonia 的弹层定位镜像（向左展开）。
- 不影响 `ContextMenu`、`MenuFlyout` 路径和 `FlyoutPresenter`。

待维护者确认：

- `MaterialMenuBarItemTheme` 是新的公开 keyed 主题名。
- 提议（未实现）：可选的容器式菜单栏，例如 `SurfaceContainer` 底色或滚动时的
  `on-scroll` 容器色，与 TopAppBar 一致。

[MenuBarDemo](../../../Material3Expressive.Catalog/MenuBarDemo.axaml) 展示 File/Edit/View
菜单栏、嵌套子菜单、勾选/单选、禁用、暗色及 RTL。
