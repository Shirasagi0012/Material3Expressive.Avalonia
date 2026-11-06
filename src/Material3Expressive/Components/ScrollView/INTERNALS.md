# ScrollView 内部机制与维护说明

## 1. 上游基线与职责映射

- **上游依据**：本组件不是 Compose Material3 控件移植，而是为 `Shirasagi0012.SmoothScroll.Avalonia` 的 `ScrollView` 提供 Material 3 主题集成；滚动条外观遵循 [ScrollBar 维护说明](../ScrollBar/INTERNALS.md)
- **职责映射**：

| 职责 | 本地类型 / 资源 | 对应说明 |
| --- | --- | --- |
| 滚动与缩放宿主 | `SmoothScroll.Avalonia.Controls.ScrollView` | 由依赖包直接提供，不在本库包壳或转发类型 |
| 合成视口承载器 | `SmoothScroll.Avalonia.Controls.ScrollPresenter` (`PART_ContentPresenter`) | 处理双轴物理平移、焦点锚定缩放、惯性与边界弹性 |
| 默认主题与模板 | `MaterialScrollViewTheme` / `MaterialScrollViewTemplate` ([ScrollView.axaml](ScrollView.axaml)) | 在 `MaterialTheme.axaml` 中注册为 `{x:Type smooth:ScrollView}` 的隐式主题 |

## 2. 关键机制与架构契约

- **Overlay 悬浮滚动条几何契约**：
  - 模板外层为 `<Grid ColumnDefinitions="*,Auto" RowDefinitions="*,Auto">`，`PART_ContentPresenter` 设置 `Grid.RowSpan="2" Grid.ColumnSpan="2"` 铺满整个控件区域
  - 与 `MaterialScrollViewerTemplate`（独立 `Auto` 行列预留空间）不同，`ScrollView` 面向照片/画布缩放场景：当 `ZoomFactor` 从 `1.0` 放大跨过溢出阈值使 `Auto` 滚动条显现时，悬浮式布局保证 `Viewport` 宽高恒定等于控件尺寸（扣除 `Padding`），避免视口突然收缩 8 DIP 引发缩放中心抖动
  - `PART_HorizontalScrollBar` 位于 `(Row=1, Column=0)`，`PART_VerticalScrollBar` 位于 `(Row=0, Column=1)`；当双轴滚动条同时可见时，右下角 `(Row=1, Column=1)` 的 `Auto × Auto` 单元格自然留空，防止两条滚动条的滑块在拉到末端时重叠
- **属性转发与部件命名**：
  - 模板必须保留 `PART_ContentPresenter`（`ScrollPresenter`）、`PART_HorizontalScrollBar` 与 `PART_VerticalScrollBar`（`ScrollBar`），供 `ScrollView.OnApplyTemplate` 挂载 `AttachToScrollView` 及滚动条事件
  - `ScrollView` 的 27 项内容、对齐、缩放、手势绑定、锚点比例、吸附与物理参数通过 `TemplateBinding` 完整转发给 `PART_ContentPresenter`
  - 移除 `ScrollViewDefaultTheme` 中依赖 Fluent 资源的 `PART_ScrollBarsSeparator`；`MaterialScrollBarTheme` 使用全透明轨道，无需右下角遮挡块

## 3. 已确认决定与已知差异

- **已确认决定**：
  - 控件本体保留在 `SmoothScroll.Avalonia.Controls` 命名空间，本库仅在 `MaterialTheme.axaml` 中提供 `MaterialScrollViewTheme` 及 `{x:Type smooth:ScrollView}` 隐式主题，不做类型转发或包装控件
  - 采用 Overlay 悬浮滚动条布局，不占用 `Viewport` 布局空间，滚动条直接复用现有 `MaterialScrollBarTheme`（固定 8 DIP 厚度、透明轨道、常态 12% / 悬停拖拽 50% 不透明度，不依赖 `IsExpanded` 扩宽或隐藏）
  - **不接入页面级滚动联动**：`Scaffold`（`AppBarScrollScope` / `AppBarSmoothScrollConnection`）、`PullToRefreshBox` 与 `FloatingToolbar` 仅面向页面流式滚动容器 `ScrollViewer`，不连接 `ScrollView`（避免画布焦点缩放与可变视口顶栏折叠在协议与几何上冲突）
- **尚未实现**：无
- **未经验证**：真实桌面触屏双指捏合缩放、高刷显示器惯性帧率及触控板手势仍需在目标硬件上人工验收
- **已接受差异**：`ScrollView` 始终工作在物理像素模式（`IsLogicalScrollActive == false`），且上游未内置 `AutomationPeer`

## 4. 验证入口

- **自动化测试**：`tests/Material3Expressive.Tests/ScrollViewTests.cs`
  ```powershell
  dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --no-restore --filter FullyQualifiedName~ScrollViewTests
  ```
- **VisualParity**：本组件为 SmoothScroll 控件的主题集成，无对应 Compose Material3 视觉一致性测试族
