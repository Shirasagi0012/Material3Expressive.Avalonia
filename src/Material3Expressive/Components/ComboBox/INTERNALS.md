# ComboBox 维护说明

使用方法见 [README.md](README.md)。本文记录上游对应、模板机制、已确认边界和尚未实现的部分。
通用移植要求见 [PORTING.md](../../../../docs/PORTING.md)。

## 上游与本地入口

固定版本以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
Compose Material3 没有 ComboBox。本主题对应的是 `ExposedDropdownMenuBox` 与官方只读样例，不是可组合调用的菜单 API。

| 上游 | 本地 |
| --- | --- |
| `ExposedDropdownMenuSamples.kt` / `ExposedDropdownMenuSample` | 关闭态字段加弹出层的唯一菜单外观 |
| `ExposedDropdownMenu.kt` / `ExposedDropdownMenu`、`ExposedDropdownMenuDefaults.TrailingIcon` | 模板内 `PART_Popup`；箭头立即旋转 180° |
| `ExposedDropdownMenu.kt` / `ItemContentPadding` | `ComboBoxItem.Padding` 为 16,0 |
| `Menu.kt` / `DropdownMenuContent`、`SelectableDropdownMenuItem` | `MaterialSurface` 与 `ComboBoxItem` 主题 |
| `MenuDefaults.groupStandardContainerColor`、`standaloneGroupShape`、`itemShape` | `Menu.StandardContainer`、`Menu.VerticalShape`、弹出层写回 `ItemShapes` |
| `TextField` / `OutlinedTextField` 的单行装饰 | `ComboBoxField`，标签插值调用 `TextFieldLayout.InterpolateLabelStyle` |

只读样例把 `ExposedDropdownMenu` 的容器色和形状改成单组 Standard，内容仍进入 `DropdownMenuContent`。
因此表面内边距是 `Menu.BaselinePadding`（垂直 8），不是 `DropdownMenuGroup` 的 `Menu.VerticalPadding`（垂直 2）。
样例没有调用 `heightIn`。可编辑样例的 baseline 普通行和 `heightIn(max = 280.dp)` 不进入这套主题。

`ExposedDropdownMenuDefaults.textFieldColors()` 使用 autocomplete token。本轮关闭态颜色跟随现有 TextField token，
不另建一套 autocomplete 调色板。

## 模板合约

- 隐式 `{x:Type ComboBox}` 基于 `MaterialFilledComboBoxTheme`。Outlined 必须使用 `MaterialOutlinedComboBoxTheme`，它同时设置 `Outlined`、`LabelPosition=Cutout` 和描边色
- 必须保留 `PART_Popup` 与 `PART_ItemsPresenter`。容器类型固定为 `ComboBoxItem`，不能把 `DropdownMenu` 或 `MenuFlyout` 嵌进模板
- `PART_EditableTextBox` 使用 `MaterialComboBoxInputTheme`，输入文字与选择框一样是 BodyLarge。不能让它落入隐式 TextBox 主题，否则字段里会再套一层 TextField，字号也会退回平台默认值
- 占位符由外层 `ComboBoxField` 绘制。可编辑时同时看 `Text` 和内部 `PART_TextPresenter.PreeditText`
- 未选择时选择框不套用 `ItemTemplate`。`FuncDataTemplate<T>` 对引用类型匹配 null，而 `ContentPresenter` 会在 `Content` 为空时仍构建已匹配的模板，字段里会多出一行空数据
- 前导图标由 `ComboBoxField` 写入；尾部箭头不是 `TrailingIcon` 槽
- `ComboBoxThemeDefaults` 承接状态色，再绑定到原生 `Foreground` / `Background` / `BorderBrush` / `PlaceholderForeground`，应用的本地值可以覆盖
- 聚焦外观使用 `:focus-within` 与 `:dropdownopen`，覆盖内部 TextBox 和展开菜单；颜色优先级与 TextField 相同：禁用、错误、聚焦/展开、普通
- 弹出层 `ShouldUseOverlayLayer=True`、`WindowManagerAddShadowHint=False`，并在打开时给 `OverlayPopupHost` 和其 `LayoutTransformControl` 取消 `ClipToBounds`，否则 Material 阴影会被裁掉。这与原生 MenuFlyout 不加 Material 阴影的决定无关
- 弹出层 `MinWidth` 绑定整个控件宽度，包含支持文字。`MaxHeight` 绑定原生 `MaxDropDownHeight`

## 菜单项

`ComboBoxPopupChrome` 在容器准备、序号变化、项数变化和菜单打开时调用 `MenuDefaults.ItemShape`。
虚拟化面板在 `ContainerPrepared` 期间还不能通过 `ContainerFromIndex` 找到当前容器，因此事件参数上的容器要直接写入。
单项保持 null，圆角回退到 `Menu.ItemShape` / `Menu.SelectedItemShape`。多项的首尾圆角与独立菜单相同。
只有一组：没有组间距、组 hover 形变，也没有把分隔线解释成组。

项主题对应 `SelectableDropdownMenuItem`：BodyLarge、外沿水平 4、内容水平 16、选中勾。
勾的宽度和透明度使用与 `DropdownMenuLeadingIcon` 相同的 Transition；未选中宽度为 0 时，文本仍留出 8 DIP，对应 `MenuArrangement` 的 leading spacing。
选中勾和下拉箭头使用固定 Core `internal/Icons.kt` 的 Check / ArrowDropDown 向量，24×24 画布按对应 Token 缩放。
勾的普通、选中和禁用颜色分别使用 LeadingIcon、StandardSelectedContent 和 DisabledIcon；退出期间颜色立即切换，宽度和 alpha 继续过渡。
RTL 的展开仍锚在物理右端，遵循固定 `EnterExitTransition` 内部的 LayoutDirection.Ltr。
透明度使用 Animation 优先级贡献，卸载时恢复基础值。Popup 重开时 ScopeChanged 续跑原 Transition，避免祖先作用域更换后动画停滞。
项背景普通及禁用时为 StandardContainer，选中且启用时为 StandardSelectedContainer；内容按逻辑起点放置，不拉伸固定尺寸子内容。
项不设置 `MaximumIntrinsicWidth`，因此可以跟着宽于 280 的字段伸展。`MenuItemChrome` 的键盘钩子只在 owner 是 `MenuItem` 时接入，这里只复用它的尺寸和颜色动画。

## 已接受的差异

维护者在落地前确认：主题可以包含布局、动画和附加属性，选择、焦点、键盘和关闭仍交给 Avalonia 12.1.2 的 `ComboBox`。

- 文本和选择保持耦合。可编辑输入按完整文本匹配选择；Compose 里输入、选择和查询可以分开，主题改不了这个关系
- 打开菜单会聚焦当前项。可编辑关闭后回到文本框并全选。非聚焦 Popup 不能取消这个行为
- 菜单打开期间字段保持聚焦阶段。项在按下时就拿走焦点，选择要等松开；若这时按空字段回落，标签会先缩回中间再回到顶部。Compose 在菜单展开时把焦点留在锚点上，标签同样停在最小化位置
- 选择调用非虚拟的 `Popup.Close()`，菜单立即关闭
- 折叠态的上下键、聚焦时的滚轮、ComboBox 自动化语义保持原生
- 位置使用 Avalonia 的默认放置和翻转。菜单不能伸出窗口
- 箭头旋转是瞬间的，与上游 `Modifier.rotate` 一致，不是运动过渡
- 标签对齐固定为 Start。没有 Prefix / Suffix，也没有第二种 Baseline 菜单主题

## 尚未实现

- 关闭、中断和反向的 scale/alpha 动画。`Popup.Close()` 不是可取消的公共生命周期，`:dropdownopen` 也不能在退出期间留住内容
- Compose 按上下剩余空间计算的最大高度、位置和缩放原点

## 验证

`ComboBoxTests` 的 9 项 Headless 测试覆盖隐式 Filled / Outlined、overlay 裁剪、项圆角、可编辑模板、
箭头开闭角度、未选择时不实例化 ItemTemplate、选择反向、重开后的宽度/alpha/背景色恢复、
调用方透明度保留、任务释放，以及菜单展开时的聚焦配色和错误/禁用优先级。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ComboBoxTests
./tests/VisualParity/parity.ps1 self-test --family combo-box-selection --require-match
./tests/VisualParity/parity.ps1 self-test --family combo-box
```

双端 `combo-box-selection` 隔离真实 ComboBoxItem，覆盖选中/普通、启用/禁用、明暗、LTR/RTL、
进入及中途反向。使用真实上游 Check 向量，内容为矩形标记，验证不包含文字栅格。
20 例在现有门禁内，结构读数相等，重复及额外截图稳定、捕获错误为零（`20261105-034157-2b4e3b9c`）；原始向量边缘差异保留在报告。

整项 `combo-box` 的最近结果为 `20261105-033650-45d3a5c3`：39 例执行错误为零、重复稳定，
30 例 equal / withinTolerance，9 例 different。下拉箭头、六个未选择菜单配置及错误/主题变更已进入现有门禁。
参考端字段颜色仍用 TextField / OutlinedTextField 默认色，沿用已确认的 autocomplete 调色板边界。
菜单场景保留既有固定 96 DIP 字段高度；`static.empty.*` 继续自动测量，不借固定高度隐藏差异。

剩余差异分别为：

- 两个自动高度空字段：Compose 58.5 DIP、本地 56 DIP，属于原生编辑行度量边界。
- 两个 focus 时间线在 160ms 的标签位置差 0.25 DIP；两个 reverse 时间线在 160/240ms 的进度差约 `7.45e-9`，后续一致。
- 两个 disable 时间线在禁用当帧，Core 装饰重建将标签进度复位，本地保留连续回落；400ms 起一致。未改变共享提交或禁用语义。
- 已选择菜单配置中，本地字段呈现选中内容，参考夹具的空 TextFieldState 没有对应内容探针，保留 `notCompared` 与整体图像差异；菜单项独立覆盖见上述选择族。

这些结果不代表完整 ComboBox 一致，也不把既有差异改记为已接受。
菜单退出动画和 Compose 位置/高度算法仍未实现，边界见上文。
真实桌面点击、输入法、无障碍树、文字栅格及跨 DPI 切换未经本次验收；没有发布或刷新参考快照。
