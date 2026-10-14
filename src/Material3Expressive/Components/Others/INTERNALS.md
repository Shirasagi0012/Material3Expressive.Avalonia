# Others 实现说明

使用入口见 [README](README.md)。本组不含 Compose Material3 对应物，因此没有 Material3 上游文件；
它把 Avalonia 通用控件的 Fluent 默认主题搬到 Material 主题下，作为尚未 Material 化的原生控件的
结构基线。

## 来源与固定版本

参照物是 **Avalonia** 仓库，而非 `docs/upstream-baseline.json` 中的 Compose 基线。本仓库固定的
Avalonia 版本见 [Directory.Packages.props](../../../../Directory.Packages.props)（当前 `12.1.2`），
对应上游 tag `12.1.2`，文件位于
`src/Avalonia.Themes.Fluent/Controls/*.xaml`。读取方式与其它源码一致，通过 Avalonia 仓库查看对应
commit，不使用浮动版本。

| Fluent 文件 | 本地文件 | 说明 |
| --- | --- | --- |
| `ItemsControl.xaml` | [Layout.axaml](Layout.axaml) | 原样，无资源 |
| `HeaderedContentControl.xaml` | [Layout.axaml](Layout.axaml) | 去掉 `Padding=3` |
| `Label.xaml` | [Layout.axaml](Layout.axaml) | 去掉 `Padding=3` |
| `TransitioningContentControl.xaml` | [Layout.axaml](Layout.axaml) | 原样 |
| `Carousel.xaml` | [Layout.axaml](Layout.axaml) | 原样 |
| `TabStrip.xaml` | [Layout.axaml](Layout.axaml) | 原样（含默认 `ItemsPanel=WrapPanel`） |
| `TreeView.xaml` | [Layout.axaml](Layout.axaml) | 原样（背景透明） |
| `RefreshContainer.xaml` | [Layout.axaml](Layout.axaml) | 原样 |
| `PopupRoot.xaml` | [Infrastructure.axaml](Infrastructure.axaml) | 画刷改 Token，去掉字体设置 |
| `EmbeddableControlRoot.xaml` | [Infrastructure.axaml](Infrastructure.axaml) | 同上 |
| `ThemeVariantScope.xaml` | [Infrastructure.axaml](Infrastructure.axaml) | 画刷改 Token |
| `Window.xaml` | [Window.axaml](Window.axaml) | 画刷改 Token，去掉字体设置 |
| `SelectableTextBlock.xaml` | [TextSelection.axaml](TextSelection.axaml) | 选择色改 Token，复制菜单改字面量 |
| `GridSplitter.xaml` | [Splitter.axaml](Splitter.axaml) | 画刷改 Token |
| `TableViewCell.xaml` | [TableView.axaml](TableView.axaml) | 保留 6,0 内边距 |

模板逐字保留 Fluent 结构（Border / ScrollViewer / presenter 部件名不变），只做资源与默认值替换，
使控件契约与原生行为一致。

## 资源映射

| Fluent 资源 | Material Token |
| --- | --- |
| `SystemControlForegroundBaseHighBrush` | `Sys.Color.OnSurface` |
| `SystemRegionBrush` | `Sys.Color.Surface` |
| `SystemControlBackgroundAltHighBrush` | `Sys.Color.SurfaceContainer` |
| `SystemAltMediumLowColor`（GridSplitter 底） | `Sys.Color.OutlineVariant` |
| `SystemAccentColor`（GridSplitter 预览） | `Sys.Color.Primary` |
| `TextControlSelectionHighlightColor` | `Comp.FilledTextField.SelectionColor` |

`Sys.Color.SurfaceContainer` 用于替换 Fluent 的 `SystemControlBackgroundAltHighBrush`：Fluent 的
"AltHigh" 在浅色下是 `#FFFFFFFF`、深色下是 `#FF000000`，而 M3 的 `Surface` 本身就是该角色，因此
`TransparencyBackgroundFallback` 取 `Surface`；`SystemBarColor` / 内嵌根背景取相邻容器色以保持可见
层次。这是本地适配，没有 Material3 上游可对照。

## 已确认选择

- **不新增 Comp token**：按维护者 2026-10-15 决定，Fluent 资源直接引用已有 `Sys.Color.*`，
  不为本组新建 `Comp.Others.*` 键或 `ITokenSource`。理由：这些控件没有 Material3 规范取值，
  新增 Component 层会制造没有上游依据的 API。
- **不保留 Fluent 字体设置**：`Window` / `PopupRoot` / `EmbeddableControlRoot` 不再设置
  `FontFamily` / `FontSize`。Material 的 BodyLarge 基线与 Roboto 通过原生继承生效，若在这些宿主上
  设置会覆盖应用排版。已在 README 与 [Typography](../../Foundation/Typography/README.md) 说明。
- **不保留 Fluent 专有默认值**：`HeaderedContentControl.Padding`、`Label.Padding`（均为 3）不搬入。
  `TableViewCell.Padding`（6,0）例外保留，因为 `TableViewRow` 布局依赖该内边距。
- **选择色复用 TextField**：`SelectableTextBlock.SelectionBrush` 取 `Comp.FilledTextField.SelectionColor`
  （`Primary` 40% alpha），与 TextField 一致，而不是新增 sys 选择色。
- **`TopLevel.SystemBarColor` 不设 Token**：该属性类型是 `SolidColorBrush?`，而 Token 解析结果是
  `Color`，`TokenBindingConverter` 只在目标为 `IBrush` 时转换，直接赋值会在样式应用时抛
  `InvalidCastException`。因此本组不设置 `SystemBarColor`，保留 Avalonia 默认。
- **跳过 `WindowNotificationManager`**：其语义与 [Snackbar](../Snackbar/README.md) 重叠，
  归属待维护者决定，本次不搬。
- **不搬 `TabStripItem`**：`TabStripItem` 的 Fluent 主题是一整套 `TabItemHeader*` 视觉，
  不属于"无 Fluent 风格"的纯结构，留待按 Material3 重做。

## 一致性边界与验证

- 模板结构与部件名直接来自 Avalonia 12.1.2 Fluent 源码，可逐文件对照上表。
- 资源替换是本地适配：Fluent 的 `System*` 调色板与 M3 `Sys.Color.*` 不是同一套颜色，
  本组不声称像素级一致，只声称控件契约（部件、行为、布局）不变。
- 未经验证：真实桌面上的窗口外观、系统栏颜色、Popup 阴影、触屏与高 DPI 组合。
- 回归入口：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter "FullyQualifiedName~ProgressBarThemeTests"
dotnet build src/Material3Expressive.Catalog/Material3Expressive.Catalog.csproj
```

`Window` 主题新增后，`MaterialTemplate_ProvidesHiddenIndicatorPartWithoutErrors` 等依赖窗口
`PART_VisualLayerManager` 的既有用例随全量测试一并通过（1447/1447）。
