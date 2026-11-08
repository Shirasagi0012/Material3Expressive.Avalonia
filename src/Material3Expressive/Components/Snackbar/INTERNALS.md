# Snackbar 实现说明

使用方法见 [README](README.md)。组件分为外观/布局、应用请求状态与 Host 呈现生命周期三层；
不要把 Host 的卸载与应用请求的取消合并。

## 默认配置数据流

内部 motion 输入以 null 表示尚未绑定；挂载、重挂或资源临时失效时不构造无效弹簧，恢复后提交当前目标。已有动画保留其已捕获规格，不以 Expressive 常量补齐配置。

单/双行高度由主题 Token 绑定到 SnackbarLayout 配置属性，属性变化触发重新测量；布局不再直接查询 Token 或补 48/68 常量。Host 的缩放/透明度规格同样通过主题配置输入，FIFO、退出保留和目标提交帧不变。

默认数据动作/关闭按钮的父级前景输入绑定 ButtonThemeProperties 候选，由按钮主题转发到原生有效 Foreground；禁用默认、用户普通 Style 和局部值仍按正常优先级工作。Snackbar 动作主题的 12/8 Padding 与 LabelLarge 同样作为候选进入现有 size-class 路径，`.medium` 等尺寸配置不会被派生主题截断。Padding 基线保存在 ActionContentPadding Token。

内容宽度上限、留白、文字与基线间距由组件 Token 传入内部布局属性，保持原像素取整和分支约束顺序。DataMargin 同时驱动模板外边距与最小约束扣除；关闭按钮可见容器尺寸与最小交互占位独立。

## 固定上游与本地入口

固定版本见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）与 Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）。下述 Material3 路径均相对于
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

固定两仓库的 Snackbar 组件源码一致。`ComposeMaterial3Flags.isSnackbarStylingFixEnabled`
为 false，当前移植 Legacy 分支，不公开未默认启用的 styling-fix 开关。

| 上游文件 / 关键符号 | 本地职责 |
| --- | --- |
| `Snackbar.kt`：`Snackbar` 的槽与数据重载 | [Snackbar.cs](Snackbar.cs)、[Snackbar.axaml](Snackbar.axaml)：内容优先级、默认按钮、槽环境和 Data 外侧留白 |
| `Snackbar.kt`：`LegacyOneRowSnackbar`、`LegacyNewLineButtonSnackbar` | [SnackbarLayout.cs](SnackbarLayout.cs)：约束、文字基线、测量顺序与 RTL |
| `SnackbarHost.kt`：`SnackbarHostState.showSnackbar`、`SnackbarData`、`SnackbarVisuals` | [SnackbarHostState.cs](SnackbarHostState.cs)：FIFO、结果、取消与过期回调 |
| `SnackbarHost.kt`：`SnackbarHost`、`FadeInFadeOutWithScale` | [SnackbarHost.cs](SnackbarHost.cs)：时长、状态提交帧、退出项和动画作用域 |
| `tokens/SnackbarTokens.kt` | [SnackbarTokenSource.cs](SnackbarTokenSource.cs)、[MaterialTokens.Comp.Snackbar](../../Tokens/MaterialTokens.Comp.Snackbar.cs) |

## 布局与内容不变量

保留 LegacyOneRowSnackbar/LegacyNewLineButtonSnackbar 的约束、测量顺序、整数像素
舍入、基线分支、RTL 相对放置及换行动作行的 Top 对齐。
OneRow 的 600 DIP 上限在 start/end 留白之后应用；NewLine 的上限在 start 留白之前应用。
普通单行默认高 48 DIP，多行文字至少 68 DIP；实际大小仍受上游布局关系和外部约束影响。
阴影 6 DIP、轮廓、InverseSurface/InverseOnSurface/InversePrimary 与字体从 Token 解析。
数据动作复用标准 Button，保留简单 TextButton 重载的 12/8 留白；关闭按钮使用 Core 内部
Filled.Close 路径，不替换为轮廓不同的 Material Symbols 字体图标。

Avalonia 没有 Compose 的 AlignmentLine 传播。局部布局读取实际 TextBlock 的首末文字基线、
ContentPresenter 和单子项 Grid，并读取已有标准 Button 的实际容器/文字度量。
默认字符串复用现有 ButtonContentPresenter 的段落测量适配；消息采用 BodyMedium，
动作槽采用 LabelLarge，各槽保留各自内容颜色环境。

**2026-11-05，本轮依据用户扩大授权重新决定**：使用共享 `MaterialTextBaseline.First/Last`
在自定义槽根发布实际基线，不新增三项 Snackbar 专属覆盖属性。原先因没有共同基线合约而推迟的方案，
现有 Snackbar 首末基线和 RichTooltip 首基线两个真实调用方；共享显式输入解决复杂内容入口，
不引入完整 AlignmentLine 引擎。原生文字/ContentPresenter 读取复用 Foundation；标准 Button 模板适配仍留在 Snackbar。
First 单独设置表示单行；Last 显式覆盖末基线，null 恢复自动读取。变化使消费者重新测量，即使 DesiredSize 不变。
BadgedBox 通过共享读取器转发锚点基线；任意复杂容器的自动传播尚未实现，无可读取基线时沿用 Legacy 缺少基线的分支。
该分支在“消息没有基线、动作却有基线”时本身可能产生异常放置，本实现保留其计算。
自定义复杂内容宜使用相应模板或不依赖该对齐关系的布局。

各槽通过 presenter 建立独立逻辑/继承关系，不能让正文直接继承声明处的文字角色。
IconButton 普通颜色入口于 2026-11-03 迁移到 ambient 后，内部关闭按钮主题改为继承
MaterialVibrantIconButtonTheme，保留既有 DismissActionForeground 候选与禁用颜色 Token 契约。
它仍自行绑定状态层墨色，不把通用图标按钮的主题迁移扩展为 Snackbar 颜色行为变更。
模板 `PART_Layout` 的三个子项依次为消息、动作、关闭；布局算法依赖这一合约。
默认 `PART_DataAction` / `PART_DataDismiss` 才连接 Data 结果，更换模板先解绑旧按钮。
关闭提示在独立 Popup 树里，必须显式绑定当前 Snackbar 的 DismissActionText。

## 状态、动画和生命周期

ShowSnackbarAsync 公平 FIFO 排队。动作/关闭使任务立即完成，退出动画可能仍在运行；
调用方取消会移除当前项或排队项。没有 Host 时，请求等待明确结果或取消。
Host 分离只结束自身计时和动画，不结束应用持有的请求；重挂显示当前项并重新计时。
普通超时经已有 AnimationScope.DelayAsync 使用根 TimeProvider，Short/Long 的 4/10 秒不受动画帧或 DurationScale 影响。
更换当前数据取消旧计时；只替换模板不重置期限。对应固定 SnackbarHost.kt 的 LaunchedEffect(currentSnackbarData) 与 delay。
应用按所属页面/任务的生命周期取消请求，状态入口与其他 Avalonia 内容状态一样通常在 UI 线程使用。

State 立即变化；Host 在下一动画帧提交新呈现，保留固定 Compose 先采样旧动画、再重组的顺序。
Host 保留退出项，使用 FastEffects/FastSpatial spring 淡入淡出和缩放；透明度应用于完整子树
的合成结果，避免重叠内容改变效果。作用域由 ComposeAnimation 管理。

2026-11-05 补齐既有 Animation 优先级契约：Float opacity/scale 通过共享 `BindAnimatedValue` 投影到
double opacity / ScaleTransform，保留源的 Float 算法、时序和整层合成。作用域结束恢复底层本地值/绑定，
不再由逐帧普通赋值覆盖它们。`AnimatedPresenterValuesPreserveLocalValuesUntilDetach` 在修复前失败、修复后通过，
同时验证卸载不取消应用请求和重新挂载创建新呈现。公开 Host API 无需迁移。

Avalonia 没有 Compose 的 dismiss 语义入口，当前项 Pane AutomationPeer 通过 Invoke 提供关闭，
设置 Polite live setting；退出项为 Off。实际屏幕阅读器播报尚未验证。

## 确认依据与共享能力

原先手动基线覆盖留作 backlog 的确认依据来自 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Snackbar/README.md)；
本轮替代决定见 [共享显式基线](../../../../docs/DECISIONS.md#24-自定义内容的显式文字基线)。不能据此宣称复杂容器已完整保留 Compose AlignmentLine，
也不能将已观察到的缺基线异常放置升级为“已接受视觉差异”。
排版与 Surface 描边分别复用[共享文字决定](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)
和 [Surface 决定](../../../../docs/DECISIONS.md#surface-圆角描边)。
依赖按[公共接入](../../../../docs/GETTING_STARTED.md)解析，历史 0.2.x 本地包信息不是当前安装指引。

## 一致性边界与验证

BadgedBox 锚点基线已接入并由双端组合验证；任意复杂自定义容器的自动传播仍未实现，手动覆盖由共享附加属性提供，没有通用基线布局框架。
未实现 Compose dismiss 语义入口，以 Invoke 暴露当前项关闭；实际辅助技术
体验未经验证。文字像素、实体桌面、其他操作系统也未经验证。

- [SnackbarHostStateTests](../../../../tests/Material3Expressive.Tests/SnackbarHostStateTests.cs)：默认时长、FIFO、当前/排队取消、自定义数据与幂等结果
- `SnackbarTimeoutTests` 用现有 ManualTimeProvider 检查精确期限、动画速度独立性、队列、状态/模板替换、卸载和重挂；默认系统时间仍由 SnackbarHostTests 覆盖。
- [SnackbarHostTests](../../../../tests/Material3Expressive.Tests/SnackbarHostTests.cs)：退出保留、状态/模板替换、真实 4/10 秒超时、卸载/重挂与 automation
- [SnackbarButtonThemeTests](../../../../tests/Material3Expressive.Tests/SnackbarButtonThemeTests.cs)：动作度量、字体、Core Close 路径、交互布局与颜色
- [SnackbarTests](../../../../tests/Material3Expressive.Tests/SnackbarTests.cs)：硬/软换行与基线、同行/换行、RTL、Token、槽替换、默认动作/关闭的 Headless 点击
- [SnackbarCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/SnackbarCases.cs) 与 [SnackbarCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/SnackbarCases.kt)：`snackbar` / `snackbar-host` 的独立真实组件、约束、动画中断、队列与合成透明度

双端 Host 卸载必须实际移除 composition 内容，应用 coroutine 不跟着 Host 移除。
同一帧前连续改 mounted 两次可能被重组合并，不能当作已测卸载。非文字槽做像素比较，真实文字
布局由 Headless 测试覆盖；不能把排除文字的 parity 说成文字栅格一致。
旧合并记录中两 suite 结构化事实相等、raster 在既定容差内且重复稳定。旧 Catalog headless smoke
曾用真实主题、字体与窗口点击动作/等待退出，但专用探针与截图未跟踪；当前可复现入口是上面的
正式测试与 [Catalog 源码](../../../Material3Expressive.Catalog/SnackbarDemo.axaml)，不声称探针仍可运行。
历史统计与环境见固定提交，以下为复现入口，实际结果需以新生成报告为准：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter "FullyQualifiedName~Snackbar"
./tests/VisualParity/parity.ps1 self-test --family snackbar --require-match
./tests/VisualParity/parity.ps1 self-test --family snackbar-host --require-match
```

参考环境与报告解释见 [VisualParity](../../../../tests/VisualParity/README.md)。

2026-11-05 显式基线接入：`TextBaselineIntegrationTests` 与两组件既有测试合计 29 项通过；全库当前 2267 项通过。
最终 `shared-baseline-complete` 的 63 例全部零执行错误、重复/额外截图稳定：61 withinTolerance、2 different。
8 个静态首末基线组合、4 个 Last-only 包装槽及 4 个动态序列中，有效首末基线和换行分支的全部几何读数相等。
Last-only 包装槽保留已有 Grid 单子项适配提供的 First，并正确计入顶部 margin；真实多行 TextBlock 测试另覆盖零/非零 margin。
两例 different 仅发生在同行布局清除消息基线、保留动作基线之后：上游用 Int.MinValue 参与动作坐标计算，
最终场景坐标为 -536870848 DIP，本地 double 场景变换为 536870990 DIP；动作在两端都移出屏幕，图像仍在既定容差内。
该极端缺基线分支未修复、未接受为视觉兼容，也没有用清除动作基线来隐藏原输入。新的显式 API 使有效复杂内容可提供真实基线。
真实文字仍只由行为测试检查布局，非文字 marker 的 parity 不证明字形栅格相同。

前轮 `snackbar-host` 的 16 个双端用例执行错误为零、重复和额外截图稳定；15 withinTolerance、1 different。
新增 timeout-* 六例的全部状态、队列计数、alpha/scale 和尺寸读数相等。timeout-queue 在 4096000µs
两层交接时的上下边缘有图像差异，固定容差未通过；这不是已接受差异，也不影响超时状态一致的结论。
两层 scale 分别为 0.900037407875061 / 0.8999626040458679，双端数值相等；尚需核验变换后的层边缘栅格化。
结果：`artifacts/visual-parity/runs/20261104-181018-f2fed506`。真实桌面及文字栅格未由这些几何场景验证。

本轮先在 `snackbar-host-current` 复现该失败，再确认其来自夹具：空主题原生 Button 继承了
TemplatedControl 默认 ClipToBounds=true，Compose Canvas 无裁剪。固定 Avalonia 12.1.5 程序集默认值及
上下边缘像素证明该输入差异；对齐为不裁剪后 `snackbar-host-unclipped` 全部 16 equal。
完成生产属性绑定迁移后的 `snackbar-host-bound` 仍为 16 equal，全部重复/额外截图稳定，零执行错误。
没有放宽 raster 容差、改变动画、增加帧或裁掉内容；原来缺失的抗锯齿边缘像素现与参考一致。

## 显式容器路径（2026-11-08 本轮授权）

固定 AndroidX 与 Core 的槽/Data 两个 Snackbar 重载都接受 shape 并交给 Surface；本地原先只公开
CornerRadius，现补齐 `Snackbar.Shape`，直接 TemplateBinding 到已有 MaterialSurface，null 保留旧圆角。
原先缺口为组件入口未接入已存在的路径能力，无需新建绘制层、改变布局或迁移现有调用。
背景、阴影、内容裁剪使用同一实际轮廓，Host 的缩放/透明度、队列结果与计时语义不变。
Catalog 的自定义消息展示切角；行为测试覆盖 live 角值、清除回退、尺寸不变及重挂。
双端 snackbar 增加切角/百分比/凹路径、深色、RTL、动作换行造成的尺寸变化、换路径与清除。

Host 的 shaped 模板另覆盖 LTR/RTL、退出和入场中断。验证同时修复两处实际问题：
运行时现在以 AnimationEndReason 返回普通取消，Host 必须检查 Finished 才执行退出移除，
否则被取消的入场也会提前移除当前退出层；这遵循共享决定 §32，不修改弹簧或等待时序。
SnackbarLayout 已物理放置槽，内部 ContentPresenter 仍使用 Avalonia RTL 镜像；原 Right 对齐
会在显式宽度引起的宽槽中再次反转，现保留逻辑起点 Left。新增测试检查真实内容位置和中断层保留。

`snackbar-host-paths-final` 20 例：16 equal、2 withinTolerance、2 different，全部重复稳定、零执行错误。
原有 16 例全部 equal（包括早退、重新挂载后超时）；新增两个普通 shaped 退出例在容差内。
两个 shaped interrupt 例只在 224000µs 的阴影栅格超差，结构化几何、层数、alpha/scale 全部相等；
当时仅保留第二层，alpha=0.9813839793205261、scale=0.9979384541511536。
实际图像检查显示差异集中在外部阴影，1x 最大通道差 23，现有阈值 20；尚未解决、未接受为兼容，
没有改动画、关闭阴影、放宽容差或删除用例。固定上游的两例缺基线 Int32 溢出差异也继续保留；
本轮开始前提交 fb5605d 的独立工作树已复现 `snackbar.dynamic.baseline.false.false`。

最终 `snackbar-paths-final` 77 例为 6 equal、69 withinTolerance、上述 2 个缺基线 different，
全部重复稳定、零执行错误；新增 14 个形状用例全部相等或容差内。Snackbar 相关行为测试 52 项通过。
最终整仓构建成功（5 个既有警告），控件库全量测试 2559 项全部通过，结果保留在
`artifacts/test-results/full-complete.trx`；未将之前 Headless 隔离清理的线程错误当作已通过结果。
