# TimePickerDialog 实现说明

使用入口见 [README](README.md)。固定基线来自 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。

## 源码与布局

`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/TimePickerDialog.kt`
中的 TimePickerDialog、TimePickerDialogLayout、TimePickerCustomLayout 是主依据；
两端固定源码的这些实现一致。TimePicker.kt 中 ClockDialMinContainerSize=200 DIP 决定横向分支阈值。

TimePickerDialogLayout 保留内容、动作、标题的测量顺序；自然宽度是内容宽加 48 DIP。
内容宽大于高且高至少 200 时横排；有限高度填满上限，无限高度为内容高+动作高+28。
横向使用 16 顶部、4 内容动作间距、8 底部，将剩余空间平分；高度至少 384 时动作再上移 16。
纵向为顶部 24 + 标题 + 内容 + 动作 + 底部 24。
上述边距、横向附加空间与动作偏移由 TimePickerDialog 专属 Token 接入内部布局属性，测量和排列共用输入并响应动态资源更新；200/384 DIP 分支阈值保持固定。
place() 使用物理坐标；RTL 只影响动作 Row 和槽内内容，不二次镜像整体。
Surface 传递调用方最小宽度；Avalonia 的 Measure 参数仅表示上限，所以模板内容容器保留该最小值，
其子内容继续按零最小宽度测量。自定义布局报告的自然尺寸被外部约束收紧时，保留 Compose 的居中偏移。
TimePickerDialogActions 保留按声明顺序测无权重子项、弹性 Spacer、8 DIP 间距，不换行。

Surface 使用 Dialog 的颜色、圆角与 ContainerElevation，tonal elevation 不转成投影。
标题是调用方内容：核心入口没有排版或底部 padding，不把 Defaults.Title 的组合强塞进槽。
字体/内容颜色通过现有 Typography 与 ContentColorFor 能力传递。

## 已确认决定

2026-10-11，维护者确认 TimePickerDialog : Dialog；Content、ConfirmButton、DismissButton、
Title、ModeToggleButton 及对应 Template；沿用 IsOpen/DismissRequested，按钮和模式状态属于调用方。
不额外公开 TonalElevation，保持上游固定传 DialogTokens.ContainerElevation。
当时范围不包括 Picker/TimeInput 本体、VibrantTimePickerDialog 或 Defaults 中独立的标题/带 Tooltip 切换按钮。

复用 [Foundation Dialog](../../Foundation/Overlays/README.md) 的 PopupOverlayLayer、模态、焦点与动画；
模板保持 PART_Root / PART_Modal / PART_Scrim / PART_Surface / PART_DialogContent。
内容由实际 presenter 持有逻辑关系，继承 Surface 环境；不新增共享宿主或动画机制。

## Vibrant 入口

剩余移植任务对新增非破坏性组件 API 的授权包括 `VibrantTimePickerDialog : Dialog`。
它与普通入口保持独立，具有 Content、ConfirmButton、DismissButton、ModeToggleButton 及对应 Template，
不继承无意义的 Title API。固定 AndroidX / Core 的 TimePickerDialog.kt 全文一致。
VibrantTimePickerDialogLayout / VibrantTimePickerCustomLayout 使用 SurfaceContainer、ExtraLarge 和固定 tonal Level3。
Vibrant 圆角直接读取 Sys.Shape.ExtraLarge，不依赖普通 Dialog.ContainerShape 的局部覆盖。

两个模板复用内部 TimePickerDialogLayout：普通模板有 title/content/actions 三项，Vibrant 只有 content/actions。
内容先测量、动作随后、普通标题最后；两者均保留 200 DIP 横向判定、物理坐标排列、传入最小宽度和受约束后的居中偏移。
Vibrant 参数为横向留白 12、纵向上下各 12、纵向内容/动作间距 12；横向顶部 12、内容/动作间距 8、底部 12，
额外高度合计 32，没有普通入口的 384 DIP 上移分支。默认数值由专用 Vibrant Token 绑定。
动作继续复用 TimePickerDialogActions 的顺序测量、弹性 Spacer 和 8 DIP 间距，不新建共享布局或宿主。
新组件复用 Dialog 的生命周期、输入和模态机制，模板合约与普通入口相同。

## 验证入口与边界

- [PickerDialogTests](../../../../tests/Material3Expressive.Tests/PickerDialogTests.cs)：200 DIP 阈值、槽模板、
  实际按钮输入、关闭请求、重开和卸载。
- [TimePickerDialogCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/TimePickerDialogCases.cs)：配置展开。
- [PickerDialogScene.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/PickerDialogScene.cs) 和
  [PickerDialogCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/PickerDialogCases.kt)：两端独立调用真实 API。

非文字几何和实际继承色覆盖可选槽组合、主题、RTL、宽高约束、横向 200/384 边界；动态覆盖主题、
槽、内容高度、动作宽度、点击/关闭请求及进出场中断。日期、普通时间与 Vibrant 时间容器复用槽/输入夹具，双端各自调用真实 API。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~PickerDialogTests
./tests/VisualParity/parity.ps1 self-test --family time-picker-dialog --require-match
./tests/VisualParity/parity.ps1 self-test --family vibrant-time-picker-dialog --require-match
```

self-test 实际采集双端并验证重复性及额外被动截图；结果按输出路径读取。
withinTolerance 不等于严格像素相等，未测文字不视为匹配。
系统/IME inset、平台安全窗口能力尚未实现；文字像素、真实输入法、屏幕阅读器隔离、触屏、
多显示器 DPI 与跨平台桌面效果未经验证。

自定义/透明背景且未显式设置环境内容色的新增 Vibrant 场景保留双端差异：Core 的默认 LocalContentColor 为黑色，
本地宿主默认继承 OnSurface。ContentColorFor 的角色匹配/ambient 回退符合现有共享契约，不能在组件内强制黑色。
额外场景分别显式设置 `CompositionLocalProvider(LocalContentColor provides Color(0xFF448855))` 与
外层 `Grid TextElement.Foreground="#448855"`，独立检查同一环境下的回退；原失败用例未删除或替换。
推荐应用显式提供环境色；是否统一两个平台的根环境默认值影响整个主题，需维护者决定，未获批准、未实施。
