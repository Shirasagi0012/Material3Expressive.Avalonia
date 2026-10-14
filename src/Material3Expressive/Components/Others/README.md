# Others

其余原生控件的默认主题。它们不来自 Compose Material3，而是把 Avalonia 通用控件接入 Material
主题所需的最小结构：这些控件的 Fluent 主题要么只描述布局/部件契约，要么只差少量画刷映射。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)。维护与取舍见 [INTERNALS.md](INTERNALS.md)。

## 覆盖范围

| 控件 | 说明 |
| --- | --- |
| `ItemsControl` | Border + `PART_ItemsPresenter`，无默认画刷 |
| `HeaderedContentControl` | Header/Content 两段，无默认画刷 |
| `Label` | 单 ContentPresenter，`RecognizesAccessKey` |
| `TransitioningContentControl` | 双 ContentPresenter，供原生过渡 |
| `Carousel` | ScrollViewer + ItemsPresenter，隐藏滚动条 |
| `TabStrip` | Border + ItemsPresenter，默认 WrapPanel |
| `TreeView` | Border + ScrollViewer + ItemsPresenter，透明背景 |
| `RefreshContainer` | 内容 + `PART_RefreshVisualizerPresenter` |
| `Window` | 标准窗口模板，含 `PART_VisualLayerManager` |
| `PopupRoot` | Popup 根模板 |
| `EmbeddableControlRoot` | 内嵌根模板 |
| `ThemeVariantScope` | 继承内容色 `OnSurface` |
| `SelectableTextBlock` | 选择色 + 复制上下文菜单 |
| `GridSplitter` | 分隔条，`OutlineVariant` 底、`Primary` 预览 |
| `TableViewCell` | 单元格水平内边距 |

`TabStripItem` 只有 Fluent 的标签页视觉（`TabItemHeader*` 一整套），因此**没有**在这里提供默认
主题；`TabStrip` 本体是纯结构，已提供。

## 最小示例

以下视图无需 ViewModel，`m3` 是主题库命名空间：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <Grid RowDefinitions="Auto,*">
    <ItemsControl Grid.Row="0">
      <TextBlock Text="普通 ItemsControl" />
    </ItemsControl>
    <Grid Grid.Row="1" ColumnDefinitions="*,Auto,*">
      <TextBlock Grid.Column="0" Text="左" />
      <GridSplitter Grid.Column="1" Width="6" ResizeDirection="Columns" />
      <TextBlock Grid.Column="2" Text="右" />
    </Grid>
  </Grid>
</UserControl>
```

以上控件均使用隐式主题，直接写控件类型即可；`Window` / `PopupRoot` 等由应用根自动套用。

## 窗口模板与弹层宿主

`Window` 默认主题包含名为 `PART_VisualLayerManager` 的 `VisualLayerManager`，Window 会因此启用内部
`PopupOverlayLayer`。这是 [Dialog 与模态弹层](../../Foundation/Overlays/README.md) 的接入前提：
自定义 Window 模板必须保留同名部件，否则 `Dialog` 与 `ModalWideNavigationRail` 无法承载。

## 定制

- 颜色走 Token：例如覆盖 `MaterialTokens.Sys.Color.Surface` 会同时改变 `Window` 与 `PopupRoot` 背景。
- 单个控件优先设置其原生属性（如 `GridSplitter.Background`）；需要统一外观时再覆盖对应 sys 颜色。
- `SelectableTextBlock` 的选择色复用 `Comp.FilledTextField.SelectionColor`（`Primary` 40% alpha），
  与 TextField 的选择语义一致；需要单独颜色时设置 `SelectionBrush`。

## 边界

- `Window` 不设置 `FontFamily` / `FontSize`，由 [Typography](../../Foundation/Typography/README.md)
  的 BodyLarge 基线负责，避免覆盖应用排版。
- `HeaderedContentControl` / `Label` 不保留 Fluent 的 `Padding="3"`，使用 Avalonia 默认值。
- `WindowNotificationManager` 未纳入：其语义与 [Snackbar](../Snackbar/README.md) 重叠，尚未决定归属。
- 其余 Fluent 视觉型控件（MenuItem、TabItem、Calendar、FlyoutPresenter、TableView 等）
  仍需按 Material3 重做，不在本组。
