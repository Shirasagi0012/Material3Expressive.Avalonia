# BottomSheet

底部面板提供隐藏、部分展开和展开三个状态。`BottomSheet` 在树内呈现，`BottomSheetScaffold` 组合常驻面板与页面，`ModalBottomSheet` 在窗口覆盖层中提供模态面板、遮罩与焦点约束。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)，窗口需要动画作用域。

## 基础用法

```xml
<Grid xmlns="https://github.com/avaloniaui"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
      Height="600" ClipToBounds="True">
  <TextBlock Text="页面内容" Margin="24" />
  <m3:BottomSheet x:Name="Sheet">
    <Border Height="400" Padding="24">
      <TextBlock Text="面板内容" />
    </Border>
  </m3:BottomSheet>
</Grid>
```

在挂载和首次布局后调用 `await Sheet.State.ShowAsync()` 显示面板。

## 变体与属性

模态面板的声明不占布局空间。设置 `IsOpen=True` 后自动显示到部分展开（禁用该状态时完全展开）；
恢复为可见状态时保留该状态。`DismissRequested` 在用户隐藏完成后通知，应用负责将 `IsOpen` 设为 false：

```xml
<m3:ModalBottomSheet xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
    x:Name="Modal" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    IsOpen="{Binding IsSheetOpen, Mode=TwoWay}" DismissRequested="OnModalDismissed">
  <Border xmlns="https://github.com/avaloniaui" Height="400" Padding="24">
    <TextBlock Text="选择一个目的地" />
  </Border>
</m3:ModalBottomSheet>
```

```csharp
private void OnModalDismissed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    => Modal.IsOpen = false;

// 程序化关闭先等待隐藏，然后移除模态呈现。
await Modal.State.HideAsync();
if (!Modal.State.IsVisible) Modal.IsOpen = false;
```

`IsPresented` 表示模态层仍存在；单独 HideAsync 不移除该层，也不触发关闭请求。
直接设置 `IsOpen=False` 会立即移除并取消正在运行的动画。
`ShouldDismissOnBackPress` 和 `ShouldDismissOnClickOutside` 分别控制返回/Escape 与遮罩点击。
Escape 在完全展开时先部分收起，再次按下才隐藏；禁用部分展开时直接隐藏。
`ScrimColor` 定制遮罩颜色，设为 null 同时去除遮罩绘制与点击动作；透明颜色仍保留点击动作。
模态变体默认 `TonalElevation=0`，其余面板外观与内容槽沿用下文用法。

默认每个控件独立持有 `SheetState`，也可绑定应用持有的状态。类型位于 `Material3Expressive.Components`。

```csharp
Sheet.State = new SheetState(SheetValue.Hidden,
    enabledValues: [SheetValue.Hidden, SheetValue.Expanded],
    confirmValueChange: value => value != SheetValue.Hidden || CanClose());
await Sheet.State.ExpandAsync();
await Sheet.State.HideAsync();
```

`ShowAsync` 优先选择部分展开；`ExpandAsync`、`PartialExpandAsync`、`HideAsync` 指定目标。
启用状态必须包含 Expanded 和初始状态；对未启用的 Hidden/PartiallyExpanded 发出对应请求会抛异常。
异步操作接受取消令牌，新操作、拖动和卸载会取消旧动画；调用方可处理 `OperationCanceledException`。
`CurrentValue` 是最后停稳的状态，`TargetValue` 是当前目标；`IsVisible` 可在动画结束前改变。
`CurrentOffset` 和位置阈值使用 DIP，首次布局前 offset 为 NaN，`RequireOffset()` 会报告未初始化。

`GesturesEnabled` 控制直接拖动及嵌套滚动，手柄仍可点击。`BackHandlerEnabled` 控制 Escape：
展开时先部分收起，否则隐藏。`DismissRequested` 报告用户关闭或拖动隐藏，程序化 HideAsync 不触发该事件。
关闭父级手势不会取消内容按钮已经开始的点击；例如面板内的 Done 按钮仍可独立执行。
内容中的原生 Slider / Thumb 开始拖动后由子控件持有该手势，不同时拖动面板。

`BottomSheetScaffold.State` 是 `BottomSheetScaffoldState`，包含 `BottomSheetState` 和 `SnackbarHostState`。
默认只启用 PartiallyExpanded / Expanded，停在 56 DIP 的 `SheetPeekHeight`；展开后点击手柄返回 peek。
需要允许隐藏时传入包含 Hidden 的 SheetState。标准面板不安装 Escape 关闭处理。

```xml
<m3:BottomSheetScaffold xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
    x:Name="Page" Height="600" SheetPeekHeight="56">
  <m3:BottomSheetScaffold.TopBar>
    <Border Height="64"><TextBlock Text="保存的位置" Margin="24,0" /></Border>
  </m3:BottomSheetScaffold.TopBar>
  <m3:BottomSheetScaffold.SheetContent>
    <Border Height="360" Padding="24"><TextBlock Text="面板内容" /></Border>
  </m3:BottomSheetScaffold.SheetContent>
  <Border Padding="{Binding ContentPadding, ElementName=Page}"
          m3:MaterialWindowInsets.ConsumePadding="{Binding ContentPadding, ElementName=Page}">
    <TextBlock Text="页面内容" />
  </Border>
</m3:BottomSheetScaffold>
```

通过 `Page.State.BottomSheetState.ExpandAsync()` 展开；
`Page.State.SnackbarHostState.ShowSnackbarAsync("已保存")` 使用默认消息宿主。
正文已经避让顶部栏，`ContentPadding` 只提供底部 peek 留白，是否应用由正文决定。

## 内容槽与定制

`Content` / `ContentTemplate` 提供面板内容，内容自然高度加上手柄及 Insets 决定面板高度。
默认最大宽度 `SheetMaxWidth` 为 640 DIP，居中放置。`Background`、`Foreground`、`CornerRadius`、
`TonalElevation`、`ShadowElevation` 定制表面。`WindowInsets` 是逻辑 start/top/end/bottom 内容边距，扣除祖先已消费的部分。

`DragHandle="{x:Null}"` 隐藏手柄。自定义手柄使用 `DragHandleTemplate`；直接提供控件时同时设 `DragHandleTemplate="{x:Null}"`。

标准脚手架另有 `SheetContent`、`TopBar`、`SnackbarHost` 及各自 Template 槽。
`SheetBackground` / `SheetForeground` / `SheetCornerRadius`、`SheetTonalElevation` / `SheetShadowElevation`
定制面板，默认色调高度 0、阴影高度 1；`SheetDragHandle` / `SheetDragHandleTemplate` 定制手柄。
`SheetSwipeEnabled` 禁止拖动；Snackbar 在部分展开时放到面板顶部，其余状态放到宿主底部。

## 使用限制与注意事项

放在有明确可用宽高的 Grid 等容器中，不要放进沿垂直方向无限测量的 StackPanel 或 ScrollViewer。
外层容器负责裁剪移动到其边界外的内容。短内容的部分展开位置与完全展开位置相同；内容增长后会保持完全展开。

内含滚动内容时，应用需把嵌套滚动的前置、剩余位移和速度交给 `OnPreScroll`、`OnPostScroll`、
`OnPreFlingAsync`、`OnPostFlingAsync`（DIP 与 DIP/s）；目前没有自动 ScrollViewer 接线。
模态面板要求 Window 模板包含 `PART_VisualLayerManager`，默认 Material 窗口主题已提供；
面板使用当前客户区，不创建独立窗口。系统/IME 安全边距仍需应用通过 `WindowInsets` 提供。
Catalog 用 `--bottom-sheet`、`--bottom-sheet-scaffold` 或 `--modal-bottom-sheet` 启动。

内部机制与验证范围见 [INTERNALS.md](INTERNALS.md)。
