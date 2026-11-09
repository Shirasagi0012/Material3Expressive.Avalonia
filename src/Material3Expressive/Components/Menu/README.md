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
Vertical 项的 CheckBox / Radio 在未提供 Icon 时会展开、淡入选中勾，取消选择时反向退出；快速切换保留正在运行的过渡。
提供 Icon 后按 IsChecked 直接替换普通图标与勾，运行中增删 Icon 也会切换对应分支。Baseline 保持静态勾选外观。

### 共享交互源

独立项目和 DropdownMenuGroup 可设置 InteractionSource，null 使用内部源。调用需在 UI 线程，结束事件引用同一开始对象。
原生 Baseline / Vertical MenuItem 使用 `MenuAppearance.InteractionSource` 附加属性，
例如 `MenuAppearance.SetInteractionSource(nativeItem, source)`；也可在 XAML 中绑定同一源：

```xml
<MenuItem Header="Copy" m3:MenuAppearance.InteractionSource="{Binding MenuInteractions}" />
```

```csharp
using Material3Expressive.Foundation.Interaction;

var source = new MaterialInteractionSource();
item.InteractionSource = source;
var hover = new MaterialInteractionEvent.HoverEnter();
source.Emit(hover);
source.Emit(new MaterialInteractionEvent.HoverExit(hover));
```

源提供交互反馈，业务选择与关闭仍由应用控制。项目替换源后接收新事件，组的悬浮外观保留到新源下一事件重新求值。
原生项目仍由 MenuItem 执行点击、命令、选择和关闭。模板替换、关闭和卸载会取消原生按压，
重建或重开只接收后续外部事件，不重放源中先前发出的按压。Catalog 的原生项目提供共享悬浮预览。

## 内容槽与定制

分组标题使用 `DropdownMenuGroupLabel`，尾部提示使用 `DropdownMenuItemTrailingLabel`：

```xml
<m3:DropdownMenuGroup>
  <m3:DropdownMenuGroupLabel Content="编辑" />
  <m3:DropdownMenuItem Content="复制">
    <m3:DropdownMenuItem.TrailingContent>
      <m3:DropdownMenuItemTrailingLabel Content="Ctrl+C" />
    </m3:DropdownMenuItem.TrailingContent>
  </m3:DropdownMenuItem>
</m3:DropdownMenuGroup>
```

两者支持 `ContentTemplate`、`Padding` 和内容对齐，默认使用 LabelLarge 并继承所在位置的前景色。
分组标题默认宽度范围 112–280 DIP、最小高度 32 DIP；父约束和显式尺寸优先，默认起始/末尾留白为 12/4 DIP。
尾部标签在精确指针环境下增加末尾 6 DIP 留白，运行时切换有效；显式 Padding 或样式优先。
标题不会参与菜单项激活或键盘焦点。Catalog 的 Grouped / selectable 菜单展示这两个入口。

独立 Expressive / Checkable / Selectable 项与原生 Vertical 菜单项可继承 `MaterialPrecisionPointer.IsEnabled`：
例如在窗口、锚点容器或 `<m3:DropdownMenuPopup m3:MaterialPrecisionPointer.IsEnabled="True" ... />` 上开启。
启用后左右留白为 start 16 / end 10 DIP、上下 12 DIP，首尾内容间距为 12 DIP；项目高度规则保持原值。
支持打开时切换、局部关闭及显式 `Padding` / `LeadingSpacing` / `TrailingSpacing` 覆盖。
后两个间距属性用于独立项；原生项可用 `HorizontalArrangement` 覆盖排列。
未显式指定尺寸的尾部 MaterialSymbol 和原生子菜单箭头由 20 改为 24 DIP，任意尾部内容保持自身尺寸，
尾部槽的最小宽度仍为 20 DIP。默认值可用 `Comp.Menu.PrecisionItemPadding`、`PrecisionContentSpacing`
和 `PrecisionTrailingIconSize` Token 配置。Catalog 的 Native menus 与 grouped / selectable 示例提供开关。
Baseline 与菜单栏顶层继续使用原有尺寸。
若旧代码曾通过 `Comp.Menu.LeadingIconSize` 同时修改独立项的首尾槽，请为尾槽补上
`Comp.Menu.TrailingIconSize`；精确模式的推荐尾图标大小使用 `PrecisionTrailingIconSize`。

独立 Expressive、Checkable 和 Selectable 项支持 `HorizontalArrangement`，可选择六种标准分布、
`Absolute.Left/Right/Center/SpaceBetween/SpaceAround/SpaceEvenly`、固定正负间距或自定义排列对象：

```xml
<m3:SelectableDropdownMenuItem Content="收藏" SelectedLeadingIcon="{m3:Symbol Check}">
  <m3:SelectableDropdownMenuItem.HorizontalArrangement>
    <m3:SpacedHorizontalArrangement Space="8" Alignment="Center" />
  </m3:SelectableDropdownMenuItem.HorizontalArrangement>
</m3:SelectableDropdownMenuItem>
```

非 null 排列替代 LeadingSpacing / TrailingSpacing；清除后恢复最新默认或显式间距。
只排列存在的槽；仅有选中图标时，其零宽退场槽仍参与排列，直到移除该图标入口。
`Space` 允许负值，Left/Right 对齐表示逻辑起点/终点；Mirror=false 固定子项物理顺序。
C# 使用 `Material3Expressive.Foundation.MaterialHorizontalArrangement.SpacedBy(...)`，自定义算法派生同一类型。
对象不可变，运行时替换属性即可重排，不取消正在进行的图标动画。
Baseline 和原生 MenuItem 不消费此入口。Catalog 的 Content arrangements 可直接切换选中并观察居中展开。

独立项目支持 Content、LeadingIcon、SelectedLeadingIcon、TrailingContent、SupportingText 及对应模板。
仅选中图标存在时使用展开和淡入效果。MenuDefaults.ItemShape / GroupShape 按调用方提供的 index、count 生成首尾形状，组间距由容器设置，默认参考 MenuDefaults.GroupSpacing。
同时提供普通和选中图标时按选择状态直接替换；运行中添加普通图标会结束选中图标的展开/淡入淡出，移除后重新按当前选择状态建立动画分支。内容或模板均适用，Catalog 提供可切换示例。
DropdownMenuPopup 可组合 StackPanel 和多个 DropdownMenuGroup，内容需要滚动时显式放置 ScrollViewer。

MenuAppearance.ColorStyle 可从 Popup 传到组和项目。Padding、CornerRadius、颜色及 `Comp.Menu` Token 可局部覆盖，MenuItemShapes / MenuGroupShapes 的 null 字段继承主题。

`DropdownMenuGroup.Shape` 与 `InactiveShape` 可分别覆盖主轮廓和离开悬浮后的轮廓；null 继续使用对应圆角配置。
首次悬浮前使用主轮廓，离开后切换到 inactive，再次进入恢复主轮廓。两端为圆角或切角时保留形变；
两种角形混用时按上游在动画中点切换类型，任一端为自定义路径时直接切换。

```xml
<m3:DropdownMenuGroup Width="280">
    <m3:DropdownMenuGroup.Shape>
        <m3:CutCornerShape TopStart="24" TopEnd="4" BottomEnd="24" BottomStart="4" />
    </m3:DropdownMenuGroup.Shape>
    <m3:DropdownMenuGroup.InactiveShape>
        <m3:CutCornerShape TopStart="8%" TopEnd="8%" BottomEnd="8%" BottomStart="8%" />
    </m3:DropdownMenuGroup.InactiveShape>
    <m3:DropdownMenuItem Content="Open" />
</m3:DropdownMenuGroup>
```

独立 `DropdownMenu.Shape`（包括继承它的 `ExposedDropdownMenu`）用于整个容器；形状改变不影响
布局和开闭动画。`DropdownMenuItem.Shape` / `SelectedShape` 用于专用菜单项及其 Checkable / Selectable
子类，null 分别回退到原 `CornerRadius` / `Shapes.SelectedShape`。选中形变采用与分组相同的角形规则；
路径形状不会自动写回选择状态。Baseline 普通项只用 `Shape`。

```xml
<m3:SelectableDropdownMenuItem Content="Sort by name" IsSelected="{Binding SortByName}">
    <m3:SelectableDropdownMenuItem.Shape><m3:CutCornerShape TopStart="16" TopEnd="4" BottomEnd="16" BottomStart="4" /></m3:SelectableDropdownMenuItem.Shape>
    <m3:SelectableDropdownMenuItem.SelectedShape><m3:CutCornerShape TopStart="25%" TopEnd="25%" BottomEnd="25%" BottomStart="25%" /></m3:SelectableDropdownMenuItem.SelectedShape>
</m3:SelectableDropdownMenuItem>
```
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

独立菜单使用当前窗口的 PopupOverlayLayer，自定义窗口模板需提供 PART_VisualLayerManager。原生 MenuItem 主题路径尚未接入显式 Shape；平台系统 inset 和预测返回尚未提供。
原生菜单主层和子菜单采用无阴影外观，独立菜单保留 Material 阴影。原生 flyout 的组级形变和开闭动画尚未提供。
菜单栏沿用 Alt 访问键与方向键，当前原生版本的 F10 激活尚未实现。公开菜单栏 keyed 主题仍有待确认，维护状态见内部契约。

[MenuDemo](../../../Material3Expressive.Catalog/MenuDemo.axaml)展示独立和原生菜单，[MenuBarDemo](../../../Material3Expressive.Catalog/MenuBarDemo.axaml)展示菜单栏。
内部机制、已确认适配和验证范围见 [INTERNALS.md](INTERNALS.md)。

形状配置也接受 `RoundedCornerShape`：逻辑四角可独立设置 DIP 或百分比（如 `TopStart="50%"`）。
它可与 `CutCornerShape` 组合为不同状态；同族连续插值，圆/切角混合时按上游在动画中点切换。
清除相应 Shape 属性后恢复原圆角配置。完整形状用法见 [Surface](../Surface/README.md)。

## 原生菜单项的选中形状

使用 MaterialVerticalMenuItemTheme 的原生 MenuItem，可通过附加属性设置普通/选中形状：

```xml
<MenuItem Header="Read only" ToggleType="CheckBox" StaysOpenOnClick="True">
  <m3:MenuAppearance.Shape><m3:CutCornerShape TopStart="16" BottomEnd="8" /></m3:MenuAppearance.Shape>
  <m3:MenuAppearance.SelectedShape><m3:RoundedCornerShape TopStart="35%" TopEnd="35%" BottomStart="35%" BottomEnd="35%" /></m3:MenuAppearance.SelectedShape>
</MenuItem>
```

两端均为角形时按固定上游弹簧形变，任意路径直接切换；支持逻辑 RTL 和可变形状对象。
普通 Shape 的 null 使用 MenuItem.CornerRadius，SelectedShape 的 null 使用选中形状 Token。
原生 IsChecked、ToggleType、GroupName、Click 和菜单关闭行为不变。Baseline/MenuBar 主题不使用这两个属性。
Catalog Standard 的 Read only 项展示切角选中动画。


原生 Expressive 项还支持 `m3:MenuAppearance.HorizontalArrangement="SpaceBetween"`，
或与独立项相同的 SpacedHorizontalArrangement 对象。它排列真实存在的 leading、正文和尾部组合槽，
不拆分尾部快捷键与子菜单箭头。null 恢复默认首尾布局，Baseline/MenuBar 忽略此属性。
