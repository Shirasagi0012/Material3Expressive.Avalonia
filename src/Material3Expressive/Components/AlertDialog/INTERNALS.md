# AlertDialog 实现说明

本文面向维护者；使用入口见 [README](README.md)。范围包括 BasicAlertDialog 的默认尺寸、
AlertDialog 内容与按钮布局，以及 Foundation Dialog 模板合约。

## 默认配置数据流

Material 软最小/最大宽度由 Dialog Token 经主题传入 BasicAlertDialogLayout 的内部配置属性，变化触发测量。显式 Width/MinWidth/MaxWidth 继续位于这些软默认值之外，未改为公开硬约束，也不改变 Foundation 平台宽度规则。

内容留白和各槽底部间距由 Dialog Token 传入模板；动作横向/换行间距传入共享 Buttons 布局，变化重新测量。默认布局数值及槽存在性规则不变。

## 固定上游与本地入口

固定版本以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）为桌面分支依据。
下述 Material3 commonMain 文件路径相对于 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

| 上游文件 / 关键符号 | 本地职责 |
| --- | --- |
| `AlertDialog.kt`：`BasicAlertDialog`、`AlertDialogContent`、`AlertDialogFlowRow` | [BasicAlertDialog.cs](BasicAlertDialog.cs)、[AlertDialog.cs](AlertDialog.cs)、[AlertDialogLayout.cs](AlertDialogLayout.cs) 的尺寸、槽存在性和布局 |
| `tokens/DialogTokens.kt` | [DialogTokenSource.cs](DialogTokenSource.cs)、[AlertDialog.axaml](AlertDialog.axaml) 的 Surface、颜色及文字环境 |
| Core [AlertDialog.skiko.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/skikoMain/kotlin/androidx/compose/material3/AlertDialog.skiko.kt) 的 Material 入口 | 保留与 AndroidX 相同的 Material 内容逻辑，宿主复用 Foundation |
| Core UI [Dialog.skiko.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/ui/ui/src/skikoMain/kotlin/androidx/compose/ui/window/Dialog.skiko.kt)、[ComposeSceneLayerMeasurePolicy.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/ui/ui/src/skikoMain/kotlin/androidx/compose/ui/window/ComposeSceneLayerMeasurePolicy.kt)，桌面 `BackNavigationEventInput` | [Foundation Dialog](../../Foundation/Overlays/Dialog.cs)：Popup、平台宽度、模态、关闭请求及进出场 |

不添加上游已废弃的任意内容 AlertDialog 重载；其用途由 BasicAlertDialog 表达。
定位固定上游的方式见 [AGENTS](../../../../AGENTS.md#上游源码)，不要用浮动 HEAD 替代基线。

## 尺寸、槽与模板合约

BasicAlertDialogLayout 先消费 Foundation 平台宽度，再在调用方显式尺寸内应用 Material 的
280–560 DIP 默认宽度。没有默认高度，也不隐式添加 Surface 或留白。
显式 Width/MinWidth/MaxWidth 替代相应默认，而不是把 Width=100 重新抬高到 280。

AlertDialog 的模板以 `PART_Root`、`PART_Modal`、`PART_Scrim`、`PART_Surface` 连接 Dialog。
`PART_DialogContent` 标记退出冻结的实际内容，内含 `MaterialSurface#PART_Container`；
不能只冻结正文而丢失背景、图标和按钮。冻结、首帧和重开归 Foundation，组件不另建动画状态。

Icon / Title / Content / DismissButton 的内容或模板非 null 即表示槽存在。
ConfirmButton 由调用方提供，组件不会创建按钮或接管其点击。正文 presenter 持有内容的逻辑/继承关系，
使正文消费自身 BodyMedium 环境，而非 Dialog 声明处的角色；局部原生文字属性仍优先。
聚合文字的共享规则见[排版决定](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)。

Surface 默认 SurfaceContainerHigh、ExtraLarge 圆角；图标、标题、正文独立取内容色。
标题/正文/按钮提供 HeadlineSmall、BodyMedium、LabelLarge。Dialog IconSize Token 24 DIP 传给图标槽的
MaterialSymbol.DefaultSize，作为无显式尺寸 MaterialSymbol 的默认值；显式图标尺寸和自定义内容不被槽强制缩放。
DialogTokens.ContainerElevation 保留 Level3，但固定 AlertDialogContent
没有给 Surface 传 shadowElevation；默认 tonalElevation=0，不应从 raw Token 推出额外阴影。

## 布局不变量

容器留白为 24 DIP，图标/标题底部各 16，正文底部 24。图标居中；标题有图标时居中，
否则 start；正文 start、动作 end。AlertDialogColumn 先测量非正文内容，再让正文使用剩余高度，
对应 `weight(1, fill=false)`，既不拉伸填满也不自动加 ScrollViewer。

按钮按 confirm、dismiss 测量，LTR 横排的 confirm 在右；空间不足时 confirm 在上一行。
水平间距 8 DIP；竖向 8 DIP 被默认最小交互尺寸 48 与 ButtonDefaults.MinHeight=40 的差抵消。
槽中的组合算一个布局项。内部 Panel 已将 start/end 解析为物理位置，因此绕过整层 RTL 镜像；
不能再次翻转布局，槽内文字仍保持自身 FlowDirection。

## 已确认选择

以下组件选择的确认依据来自 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/AlertDialog/README.md)，不从现有代码推定授权：
2026-10-04 维护者批准 `BasicAlertDialog : Dialog`、`AlertDialog : BasicAlertDialog`、正文 Content
和上述槽 API，以原生尺寸与模板表达接入；同时确认按固定 Core precision-pointer no-op 的默认 false 分支。
这不代表 Android 紧凑分支已完成；可自定义最小交互尺寸环境的后续接入见下方决定。

跨组件宿主、平台宽度/Escape 和退出冻结分别由以下共享决定维护，本文不重复扩大其范围：

- [Dialog / Popup 的窗口内宿主与模态职责](../../../../docs/DECISIONS.md#dialog--popup-的窗口内宿主与模态职责)
- [Dialog 的平台宽度和 Escape 时机](../../../../docs/DECISIONS.md#dialog-的平台宽度和-escape-时机)
- [Dialog 的首帧与退出中重开](../../../../docs/DECISIONS.md#dialog-的首帧与退出中重开)

## 一致性边界与验证

尚未实现系统/IME inset、平台安全窗口选项；Foundation 的其他缺口见
[Overlays 当前边界](../../Foundation/Overlays/README.md#当前边界)。
未经验证的范围包括文字像素、真实 OS IME、屏幕阅读器隔离、触屏、多显示器 DPI 切换和跨平台冻结。
Headless 和离屏双端结果不构成真实桌面验收。

**精确指针尺寸（2026-11-08 本轮授权）**：原先 precision 分支延后，原因是缺少共享输入尺寸环境，
且固定 Core 的桌面监听为空，不能自行按鼠标检测启用。共享决定 §34 现在提供显式继承环境；
模板直接选择固定 AndroidX/Core `AlertDialog.kt` 的 20 DIP 容器留白、16 DIP 正文下间距和
20/26 标题字号/行高。标题使用标准 HeadlineFont 合并局部覆盖，保留字体、字重等其余属性。
BasicAlertDialog 没有新增内置留白。无公共破坏性迁移，不改变弹窗宿主、焦点或进出场动画。
`PrecisionPointerTests` 验证跨 overlay 继承、局部关闭、运行时恢复、真实动作按钮和 Token 覆盖；
`precision-alert-dialog` 双端用例观察 LTR/RTL、图标有无、按钮换行和动态切换的实际布局与标题指标。

本次 `precision-alert-dialog-delivery` 16 例 / 40 个采样通过原门禁，12 equal、4 withinTolerance，
结构化事实全等、执行错误为零、重复/额外截图稳定；已查看堆叠动作的双端截图。
相关 AlertDialog/PrecisionPointer 行为测试 22 项通过。文字采用继承字号/行高读数，仍未验证文字栅格。

**自定义交互尺寸接入决定**：本轮依据用户扩大授权重新决定，采用原建议的环境派生默认值，
保留 `ActionVerticalSpacing` 有限覆盖值的最终间距语义。Token 默认改为 NaN（自动），
模板 MultiBinding 同时观察 Token 和继承的 `MaterialInteractiveSize.Minimum`，按固定两端
`AlertDialog.kt` 的 `clamp(8 - (minimumInteractiveSize - 40), 0, 8)` 计算默认间距。
例如 `<m3:AlertDialog m3:MaterialInteractiveSize.Minimum="40" ... />` 为 8 DIP。
共享最小尺寸 API 的 null/NaN 仍表示组件默认 48；使用 0 关闭最小留白，不重新解释既有公共环境。
若把所有 Token 值都减去交互留白，会破坏显式 0/13 等覆盖；若仅检查值是否为 0，无法区分默认与显式覆盖。
因此选用 NaN 自动值，不新增 Token 解析协议、资源查找旁路或第二个面板。
直接读取默认 Token 的外部自定义模板需处理 NaN；有限覆盖不需迁移。DatePickerDialog 继续直接消费自己的间距，
共享 `AlertDialogButtons` 不负责环境求值。行为测试覆盖真实按钮、继承/局部环境、动态更新、覆盖清除及 DatePickerDialog 回归。

已跟踪入口：

- [AlertDialogTests](../../../../tests/Material3Expressive.Tests/AlertDialogTests.cs)：XAML、模板、文字角色与局部覆盖、槽所有权、尺寸和平台宽度
- [DialogTests](../../../../tests/Material3Expressive.Tests/DialogTests.cs)：Foundation 焦点、退出冻结、重开和卸载；含多倍率快照
- [AlertDialogCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/AlertDialogCases.cs) 与 [AlertDialogCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/AlertDialogCases.kt)：`alert-dialog` / `basic-alert-dialog`，可选槽、主题、约束、RTL、输入和进出场中断

双端用例采用非文字几何，槽颜色读实际内容环境，变更后显式提交重组帧；桌面键盘通过固定 Core
真实 BackNavigationEventInput，保持输入挂载/清理。旧记录中的合并验收显示 Basic 严格匹配，
Alert 内容结构化事实相等、部分 raster 在既定容差内，各端重复稳定；这是历史记录，不是本次重跑。
旧产物未跟踪，历史数字及环境可查上面的固定提交，当前验证应从跟踪用例重新生成：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter "FullyQualifiedName~AlertDialogTests|FullyQualifiedName~DialogTests"
./tests/VisualParity/parity.ps1 self-test --family basic-alert-dialog --require-match
./tests/VisualParity/parity.ps1 self-test --family alert-dialog --require-match
```

运行前按 [VisualParity](../../../../tests/VisualParity/README.md) 准备其参考环境，按
[公共接入](../../../../docs/GETTING_STARTED.md) 解析当前依赖。这些命令是复现入口，实际结果需以新生成报告为准。

2026-11-05 当前实现的 `dialog-interactive-spacing` 完整 self-test：71 例，63 equal、8 withinTolerance，
零执行错误，全部重复/额外截图稳定。新增 0/40/44/48/64 DIP × LTR/RTL 和动态 40→44→64→清除场景均 equal；
真实按钮与继承/局部环境行为测试也通过。已检查双端 40 DIP 堆叠按钮图像及对应间距读数。
