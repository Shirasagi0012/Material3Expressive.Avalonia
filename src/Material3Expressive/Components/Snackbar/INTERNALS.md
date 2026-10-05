# Snackbar 实现说明

使用方法见 [README](README.md)。组件分为外观/布局、应用请求状态与 Host 呈现生命周期三层；
不要把 Host 的卸载与应用请求的取消合并。

## 默认配置数据流

单/双行高度由主题 Token 绑定到 SnackbarLayout 配置属性，属性变化触发重新测量；布局不再直接查询 Token 或补 48/68 常量。Host 的缩放/透明度规格同样通过主题配置输入，FIFO、退出保留和目标提交帧不变。

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

**维护者决定（本次移植）：手动基线覆盖 API 留作 backlog。** 不加入
ContentFirstBaseline、ContentLastBaseline、ActionFirstBaseline，也不新增共享基线框架。
复杂自定义容器的基线传播尚未实现；无可读取基线时沿用 Legacy 缺少基线的分支。
该分支在“消息没有基线、动作却有基线”时本身可能产生异常放置，本实现保留其计算。
自定义复杂内容宜使用相应模板或不依赖该对齐关系的布局。

各槽通过 presenter 建立独立逻辑/继承关系，不能让正文直接继承声明处的文字角色。
模板 `PART_Layout` 的三个子项依次为消息、动作、关闭；布局算法依赖这一合约。
默认 `PART_DataAction` / `PART_DataDismiss` 才连接 Data 结果，更换模板先解绑旧按钮。
关闭提示在独立 Popup 树里，必须显式绑定当前 Snackbar 的 DismissActionText。

## 状态、动画和生命周期

ShowSnackbarAsync 公平 FIFO 排队。动作/关闭使任务立即完成，退出动画可能仍在运行；
调用方取消会移除当前项或排队项。没有 Host 时，请求等待明确结果或取消。
Host 分离只结束自身计时和动画，不结束应用持有的请求；重挂显示当前项并重新计时。
应用按所属页面/任务的生命周期取消请求，状态入口与其他 Avalonia 内容状态一样通常在 UI 线程使用。

State 立即变化；Host 在下一动画帧提交新呈现，保留固定 Compose 先采样旧动画、再重组的顺序。
Host 保留退出项，使用 FastEffects/FastSpatial spring 淡入淡出和缩放；透明度应用于完整子树
的合成结果，避免重叠内容改变效果。作用域由 ComposeAnimation 管理。

Avalonia 没有 Compose 的 dismiss 语义入口，当前项 Pane AutomationPeer 通过 Invoke 提供关闭，
设置 Polite live setting；退出项为 Off。实际屏幕阅读器播报尚未验证。

## 确认依据与共享能力

手动基线覆盖留作 backlog 的确认依据来自 [拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Snackbar/README.md)；
其范围是本次 Snackbar，不能据此宣称复杂容器已完整保留 Compose AlignmentLine，
也不能将已观察到的缺基线异常放置升级为“已接受视觉差异”。
排版与 Surface 描边分别复用[共享文字决定](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)
和 [Surface 决定](../../../../docs/DECISIONS.md#surface-圆角描边)。
依赖按[公共接入](../../../../docs/GETTING_STARTED.md)解析，历史 0.2.x 本地包信息不是当前安装指引。

## 一致性边界与验证

尚未实现复杂自定义容器基线传播、手动 ContentFirstBaseline/ContentLastBaseline/ActionFirstBaseline，
也没有通用基线框架。未实现 Compose dismiss 语义入口，以 Invoke 暴露当前项关闭；实际辅助技术
体验未经验证。文字像素、实体桌面、其他操作系统也未经验证。

- [SnackbarHostStateTests](../../../../tests/Material3Expressive.Tests/SnackbarHostStateTests.cs)：默认时长、FIFO、当前/排队取消、自定义数据与幂等结果
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
