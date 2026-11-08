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

## Defaults 辅助组件（2026-11-08 本轮授权）

早期授权只包含 Dialog 容器，故曾延后 Defaults 的标题与带提示模式按钮；本轮明确授权剩余组件后补齐。
固定 AndroidX/Core TimePickerDialog.kt 全文核对一致。TimePickerDialogTitle 读取仓内固定 Core 字符串，
LabelMedium/20 DIP bottom padding 对应 Title；TimePickerDisplayModeToggle 与 TimeScrollDisplayModeToggle
分别对应 DisplayModeToggle/ScrollDisplayModeToggle。分支保持原等值比较，额外模式仍走上游 else，
不推断目标、不自动写回应用 DisplayMode。公开 Click 别名直接监听内部 IconButton 的原生冒泡事件，
Command 绑定至实际按钮，不复制执行或 CanExecute 逻辑，自动化暴露实际按钮。

原模板只接受 Symbol 无法承载固定图标，故配套新增 IconButton Content 入口（决定 §35）。
Keyboard/Schedule/SwipeVertical 从固定 internal/Icons.kt Filled 路径逐命令转换，保留 24×24 viewport、
非零填充和不自动镜像；不是字体图标近似。TooltipBox/PlainTooltip 保留 Above 定位、非持续状态、
独立生命周期和本地化描述。Culture 显式更新即时刷新；不新增全局语言服务或每组件计时器。

TimePickerDialogDefaultsTests 覆盖本地化、Token/显式文字、native Click/Command、CanExecute、
状态不写回、焦点提示、卸载重挂；time-dialog-defaults 双端调用真实 Defaults，对比三模式、两方向、
明暗主题、英法中文、状态变化、按压、悬停、焦点/Escape 和提示反向，包含真实文字和固定矢量。
平台根内容色不同的原始诊断用例单独保留，匹配环境用例显式提供相同黑色内容环境和 Tooltip 配色。
文字像素仍以报告为准，不能由标签/排版读数相等推定完全一致。

`time-dialog-defaults-verified2` 的 41 例完成且全部重复/额外截图稳定、执行错误为零。
图标/描述/点击及模式变化已实测；标题几何与实际排版参数也直接读取。标题和提示的文字栅格仍有差异，
平台默认根内容色诊断同样保留 different，不放宽 raster 门禁。
复核 clock.press 的 160ms 实际截图，差异集中于提示文字；按钮区域仅 22 个 4x 像素有不超过 5 的通道差，
未把整幅图像的失败简单归为模式图标或波纹。初次夹具漏设 inverse 调色盘与反向样本
重名造成的失败产物保留；随后修正等价输入和唯一采样名，时刻、截图和参考输出不改。

## 显式 Shape 接入（2026-11-08）

固定 AndroidX 对应 Dialog 源码公开 shape；本地普通样式属性直接传给现有 Surface，
共用其圆角、路径、内容 clip 与轮廓生命周期。未新增弹窗动画、布局或形状缓存。
日期弹窗继续按固定 Core DatePickerDialog.skiko 的既有动作布局决定执行。
默认 null 兼容 CornerRadius，不需要迁移；BasicAlertDialog 的自定义外观职责不变。

`DialogShapeTests` 覆盖四类弹窗的原生动作、配置观察、退出中重开和关闭后重开。
双端 `dialog-shape` 51 项覆盖切角/圆角/百分比、RTL、约束、进退/中断、主题、槽和模式替换。
`dialog-shape-verified` 的 51 例全部 equal/withinTolerance，零执行错误、重复截图全部稳定；
已查看日期弹窗非对称圆角的实际双端图像。未据此扩展真实桌面验收范围。
