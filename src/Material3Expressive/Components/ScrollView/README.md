# ScrollView

使用 `SmoothScroll.Avalonia.Controls.ScrollView`，由 `MaterialTheme` 提供默认主题与悬浮式（Overlay）Material 滚动条样式。
控件面向照片查看器、2D 自由缩放画布与需要视图锚点或操作完成通知的独立物理滚动视口；常规页面与列表流式滚动请使用 [ScrollViewer](../ScrollViewer/README.md)。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)；内部机制与边界见 [INTERNALS.md](INTERNALS.md)。

## 基础用法

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:smooth="using:SmoothScroll.Avalonia.Controls">
  <smooth:ScrollView Height="280"
                     IsZoomEnabled="True"
                     MinZoomFactor="0.5"
                     MaxZoomFactor="4"
                     HorizontalContentAlignment="Center"
                     VerticalContentAlignment="Center">
    <Border Width="320" Height="220" CornerRadius="16" Background="#EADDFF" Padding="24">
      <TextBlock Text="Ctrl + 滚轮或双指捏合缩放，拖拽漫游画布" VerticalAlignment="Center" HorizontalAlignment="Center" />
    </Border>
  </smooth:ScrollView>
</UserControl>
```

## 变体与属性

- **滚动与测量解耦**：`HorizontalScrollMode` / `VerticalScrollMode`（`Auto`、`Enabled`、`Disabled`）控制轴向手势与滚动能力；`IsHorizontalMeasureInfinite` / `IsVerticalMeasureInfinite` 独立控制子内容是否按无限约束测量
- **滚动条显隐**：`HorizontalScrollBarVisibility` / `VerticalScrollBarVisibility` 使用 `ScrollBarVisibilityMode`（`Auto`、`Visible`、`Hidden`），与滚动开关解耦
- **缩放与手势（Zoom & Gestures）**：`IsZoomEnabled` 开启缩放手势（默认支持双指捏合与 `Ctrl + 鼠标滚轮`），`MinZoomFactor` / `MaxZoomFactor` 限制倍率范围，`ZoomFactor` 读取或设置当前倍率；默认 `GestureBindings` 将触屏单指拖拽（`TouchDrag`）和鼠标中键拖拽（`MouseMiddleDrag`）映射为 `Pan`，桌面画布若需鼠标左键拖拽漫游可配置 `GestureBindings[new(ScrollInputGesture.MouseLeftDrag)] = ScrollGestureAction.Pan`
- **内容对齐与锚点**：当画布小于视口时可通过 `HorizontalContentAlignment` / `VerticalContentAlignment` 居中或靠边展示，此时 `Offset` 仍保持从 `(0, 0)` 起算；`HorizontalAnchorRatio` / `VerticalAnchorRatio` 控制尺寸变化时的视口锚定比例
- **编程操作与事件**：支持 `ScrollTo`、`ScrollBy`、`ZoomTo`、`ZoomBy` 及完成回调事件 `ScrollCompleted`、`ZoomCompleted`；`ScrollChanged` 与 `ZoomChanged` 事件携带 `ScrollChangeSource`（`User`、`Programmatic`、`Layout`）

## 内容槽与定制

- `Content` / `ContentTemplate` 承载单个画布或图像内容
- 默认加载 `MaterialScrollViewTheme` 与 `MaterialScrollViewTemplate`，可通过 `BasedOn="{StaticResource MaterialScrollViewTheme}"` 派生自定义主题
- 内部 `PART_HorizontalScrollBar` 与 `PART_VerticalScrollBar` 采用悬浮式布局（`PART_ContentPresenter` 跨 `RowSpan="2" ColumnSpan="2"`），放大缩小跨过溢出阈值时不会挤压或改变 `Viewport` 尺寸；滚动条外观由 [ScrollBar](../ScrollBar/README.md) 主题统一驱动
- 若希望完全隐藏交互式滚动条并改用滚动时浮现、停止后淡出的只读指示器，可将双轴滚动条设为 `Hidden` 并叠加 `NonInteractiveScrollBar`

## 使用限制与注意事项

- `ScrollView` 是纯物理像素滚动与缩放视口，不激活 `ILogicalScrollable` 逻辑虚拟化；承载带虚拟化的原生列表控件时请使用 `ScrollViewer`
- `ScrollView` 不参与 `Scaffold`（`AppBarScrollScope`）顶底栏折叠联动、`PullToRefreshBox` 或 `FloatingToolbar` 滚动收起；需要页面级滚动联动的场景请使用 `ScrollViewer`
- Catalog 演示入口见 [ScrollBarDemo](../../../Material3Expressive.Catalog/ScrollBarDemo.axaml)
