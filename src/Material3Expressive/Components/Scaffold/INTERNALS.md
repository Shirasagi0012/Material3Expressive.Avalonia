# Scaffold 实现说明

本文面向维护者；使用入口见 [README.md](README.md)。固定基线以
[upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 1.5.0-alpha27），
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（v1.13.0-alpha01）。
默认逻辑对照 AndroidX，桌面输入与平台实现对照 Core，不用浮动 HEAD 替代。

## 上游与本地职责

主要入口是固定提交的 [AndroidX Scaffold.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Scaffold.kt)
及 [Core Scaffold.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Scaffold.kt)。
下文 Material3 路径均以 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/` 为前缀。

固定两端布局逻辑一致。`Scaffold` / `ScaffoldLayout` 对应 [Scaffold.cs](Scaffold.cs)、
[ScaffoldLayout.cs](ScaffoldLayout.cs) 与 [Scaffold.axaml](Scaffold.axaml)；`FabPosition` 是本地同名枚举，
`ScaffoldDefaults.contentWindowInsets` 映射为宿主提供的物理 ContentWindowInsets。
Scaffold 没有专属组件 Token 表，背景使用系统 Background，内容色使用共享 ContentColorFor。

## 测量、排列与模板合约

- 在有限的宽高约束下填满可用尺寸。和上游 SubcomposeLayout 一样，不支持 intrinsic/wrap-content
  测量；放在垂直 ScrollViewer/StackPanel 中时显式设置 Height，水平无限约束同理设置 Width。
- 测量顺序为 TopBar → Snackbar → FAB → BottomBar → 正文。所有槽使用零最小约束，只有
  Snackbar/FAB 的最大约束扣除剩余水平和底部 Insets；正文保留整个 Scaffold 的可用尺寸。
- 在测量正文前发布只读 ContentPadding。正文默认位于原点，不自动套 Padding。
  顶/底槽存在时使用实际槽高度，零高度槽也替换对应 Insets；空槽使用剩余 Insets。
- FAB 的 Start / Center / End / EndOverlay、16 DIP 留白、RTL 物理坐标以及 Snackbar 避让直接对应上游。
  FAB 任一测量维度为零时不参与避让；EndOverlay 的底部距离使用 Insets，不使用 BottomBar 高度。
- 绘制顺序为正文 → TopBar → Snackbar → BottomBar → FAB；模板树顺序保留
  TopBar → 正文 → Snackbar → FAB → BottomBar 的键盘遍历。Scaffold 是不合并子内容的无障碍 Group。
- MaterialSurface 承担实际绘制与裁剪；Background 默认引用 Sys.Color.Background，Foreground
  动态使用共享 contentColorFor 匹配或环境内容色。显式 Foreground 和资源/主题更新保持原生属性优先级。
- 布局计算在物理整数像素中执行，DIP 换算、居中整除和 RTL 转换保留固定上游规则。
  Scaffold 自身没有动画，内容槽中的组件继续拥有自己的动画和生命周期。

模板的 `PART_Layout` 必须能识别 `PART_TopBar`、`PART_ContentPresenter`、`PART_SnackbarHost`、
`PART_FloatingActionButton`、`PART_BottomBar`。清除了 ContentControl 的单 presenter 回退，
不能将其当作只换背景的 ContentControl 主题。槽的控件对象加入逻辑子树，模板替换与内容变更仍须保留父子归属。

ContentPadding 是稳定只读属性，不是每次布局创建新的正文对象。缺少槽与“存在但测量为零”的槽不同；
FAB 有任一维度为零时不参与 Snackbar 避让。有限约束、零最小约束和物理像素整数运算不可被 Avalonia
默认 MinWidth/MinHeight、布局舍入或补偿偏移悄悄替代。

## 已确认适配与共享边界

以下确认与历史观测依据来自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Scaffold/README.md)；不以当前代码反推维护者授权。
2026-10-03 维护者确认“采用推荐 API 和共享 ConsumePadding”：lambda 映射为五个内容槽及 DataTemplate，
ContentPadding 在测量正文前发布，正文由调用方应用 padding；原因是保留 Compose 的内容组合与消费顺序，
同时使用 Avalonia 原生内容、绑定和布局属性。Scaffold 不实现 Snackbar 队列、FAB 控件或 AppBar 的滚动算法。

Insets 的原因、逐层累加与适用范围统一见
[Padding 消费决定](../../../../docs/DECISIONS.md#padding-消费向嵌套-scaffold-传播)。
共享入口只传播已应用 Padding，不自动处理系统栏、IME 或全部 WindowInsets 操作，也不改变旧 Rail/Bar。
Surface 绘制与 ContentColorFor 复用现有 Foundation，不能据此扩展新的容器色匹配规则。

2026-10-06 维护者确认：标准模板内置 PART_ScrollScope。Scaffold 提供宿主及顶/底 ContentPresenter，
Scope 从实际 presenter.Child 取得根 AppBar 和 behavior，包含数据模板生成的栏；用户无需重复声明 Scope。
重应用模板释放旧 Scope 的宿主与 behavior 引用。五槽测量、遍历和 Padding 发布顺序不变。

AppBar 的合成连接读取本组件真实栏位置、ContentPadding 及 FAB/Snackbar 位置；
它属于 [AppBar 接入](../AppBar/INTERNALS.md)，不是 Scaffold 自己的动画。
更改正文结构、测量发布时机或槽位移时应同时运行 AppBarCompositionTests / AppBarSmoothScrollTests。

## 验证范围与限制

叶子 parity 夹具的 desired size 对应 Compose Modifier.size；不能使用 Arrange 时重新施加的
原生 Width/Height 冒充同样约束，也不能补偿 Scaffold 的 padding 或位置。
用例覆盖四种 FAB 位置、BottomBar/RTL 组合、槽存在/零尺寸、窄约束、小数取整、叠放、嵌套消费、颜色，
以及槽/Insets/方向/主题/尺寸动态变更，参考端调用真实 Scaffold。

历史主线回归对整个 Scaffold 矩阵重新采集双端、普通重复与额外被动截图，结构化事实和 raster 严格相等且稳定。
该记录不证明当前版本已复测，也不证明文字、屏幕阅读器、真实系统 Insets 或 OS/硬件触摸。
AppBar 的 RawTouch 集成探针只覆盖对应输入接入，不能当作真实硬件验收。

## 验证入口

从仓库根目录执行，环境与结果含义见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ScaffoldTests
./tests/VisualParity/parity.ps1 prepare-reference --family scaffold
./tests/VisualParity/parity.ps1 run --family scaffold
./tests/VisualParity/parity.ps1 self-test --family scaffold
```

固定参考源码与配置不变时，无需每次 prepare-reference。
[ScaffoldTests.cs](../../../../tests/Material3Expressive.Tests/ScaffoldTests.cs)、
[Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ScaffoldCases.cs)和
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ScaffoldCases.kt)是已跟踪的复现入口。
Catalog 入口为 [MainWindow.axaml](../../../Material3Expressive.Catalog/MainWindow.axaml)。

控件测试还覆盖 Padding 发布时机、模板/槽替换、嵌套重挂载、实际命中层级、键盘遍历与自动化树。

上述命令与覆盖说明不代表当前修订已执行全量验证。旧测试数量与未跟踪 artifacts 不作为当前验证结果；
详细历史结果见[固定修订的原记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/Scaffold/README.md)，复核以跟踪测试和新生成报告为准。
`run` 执行完成、`self-test` 重复稳定与两端相等是不同结论，必须分别报告。
