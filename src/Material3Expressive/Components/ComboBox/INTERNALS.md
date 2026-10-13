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
- 聚焦外观使用 `:focus-within`，因为可编辑时键盘焦点在内部 TextBox。颜色优先级与 TextField 相同：禁用、错误、聚焦、普通
- 弹出层 `ShouldUseOverlayLayer=True`、`WindowManagerAddShadowHint=False`，并在打开时给 `OverlayPopupHost` 和其 `LayoutTransformControl` 取消 `ClipToBounds`，否则 Material 阴影会被裁掉。这与原生 MenuFlyout 不加 Material 阴影的决定无关
- 弹出层 `MinWidth` 绑定整个控件宽度，包含支持文字。`MaxHeight` 绑定原生 `MaxDropDownHeight`

## 菜单项

`ComboBoxPopupChrome` 在容器准备、序号变化、项数变化和菜单打开时调用 `MenuDefaults.ItemShape`。
虚拟化面板在 `ContainerPrepared` 期间还不能通过 `ContainerFromIndex` 找到当前容器，因此事件参数上的容器要直接写入。
单项保持 null，圆角回退到 `Menu.ItemShape` / `Menu.SelectedItemShape`。多项的首尾圆角与独立菜单相同。
只有一组：没有组间距、组 hover 形变，也没有把分隔线解释成组。

项主题对应 `SelectableDropdownMenuItem`：BodyLarge、外沿水平 4、内容水平 16、选中勾。
勾的宽度和透明度使用与 `DropdownMenuLeadingIcon` 相同的 Transition；未选中宽度为 0 时，文本仍留出 8 DIP，对应 `MenuArrangement` 的 leading spacing。
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

Headless 覆盖隐式 Filled、Outlined 主题、overlay 与裁剪样式、项圆角、可编辑文本框主题、箭头角度，以及未选择时不实例化 `ItemTemplate`。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ComboBoxTests
```

本轮 5 项通过。其中 `SelectingWhileOpenDoesNotDropTheLabelToTheCenter` 在项拿走焦点、随后选中并关闭时，标签进度保持 1。

视觉用例在 `ComboBoxCases.cs` / `ComboBoxCases.kt`，suite 为 `combo-box`。关闭态沿用 TextField 的矩形槽位；菜单项也是矩形，不是文字。箭头和选中勾仍是模板里的真实字形，字形像素不在这套用例的覆盖里。参考端字段颜色用 TextField / OutlinedTextField 默认色，对应已接受的 autocomplete 调色板差异。菜单场景把字段高度固定为 96，避免空行高度差进到项的位置里；`static.empty.*` 仍保留自动高度。

本轮执行的是 self-test，两端都重新采集，没有发布参考快照。重复性 stable，执行错误 0。结果目录：

`artifacts/visual-parity/runs/20261011-194111-75720319`

```powershell
./tests/VisualParity/parity.ps1 self-test --family combo-box
```

self-test 退出码 0 只表示比较完成。固定高度的关闭态里，字段、标签和槽位几何一致；剩余光栅集中在尾部箭头，约 81 像素。自动高度空字段是 Compose 58.5 DIP、Avalonia 56 DIP，高差 2.5，与 TextField 的空行度量同一类，没有用填充高度去抹平。

打开的菜单里，三项矩形的位置和像素一致。描边未选择菜单在修正后重跑，标签进度两端都是 1，标签位置一致：`artifacts/visual-parity/runs/20261012-075528-bb3929c9`。这次只重跑了 `combo-box.static.menu.outlined.items-3`，其余用例仍是上一轮 self-test。选中项在 Avalonia 字段里有选择框矩形，Compose 参考没有对应探针，该点为 `notCompared`。

`dynamic.focus.filled` 只在 160000 µs 有标签纵坐标差 0.25 DIP，前后采样一致。`dynamic.reverse.filled` 在 160000 / 240000 µs 的标签进度差约 `7.45e-9`，与 TextField 记录的弹簧尾数相同，400000 µs 起一致。`dynamic.disable.*` 在禁用当帧 Compose 标签进度已是 0，Avalonia 约为 0.51；`ComboBoxField` 与 `TextFieldLayout` 一样不在禁用时 snap 标签进度，400000 µs 起两端回到同一位置。这些采样没有改弹簧或容差。错误与主题采样的剩余光栅仍是箭头字形。

**尚未实现：** 关闭、中断和反向的菜单动画，以及 Compose 按剩余空间计算的最大高度、位置和缩放原点。本轮菜单项没有额外的放置偏差，但不能据此当作这两项已经实现。

**未经验证：** 桌面点击、输入法、无障碍树。没有跑 `prepare-reference` 或消费已发布参考的 `run`。

**已接受的差异：** autocomplete 调色板、只读菜单打开时的字段焦点、文本与选择耦合、原生键盘/滚轮/关闭。见上文。
