# IconButton 实现说明

使用方法见 [README](README.md)。普通四种外观及两个 Vibrant 入口共享 IconButton 类与 ButtonLayout/Surface，Symbol 是默认模板的内容入口。

## 固定上游与本地入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 `1.5.0-alpha27`）为默认逻辑依据，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（`v1.13.0-alpha01`）
为桌面平台依据。组件源为 [IconButton.kt（AndroidX）](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/IconButton.kt) / [Core 固定提交](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/IconButton.kt)，
路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。
不要用上游 HEAD 替代固定提交，也不要把当前实现直接视为一致性或设计授权的证明。

默认值还需核对固定上游 `IconButtonDefaults.kt`；普通与 vibrant helper 不能混用。

| 上游职责 / 关键符号 | 本地职责 |
| --- | --- |
| `IconButton` / `FilledIconButton` / `FilledTonalIconButton` / `OutlinedIconButton` | [IconButton.cs](IconButton.cs) 的 Button 派生类与 [IconButton.axaml](IconButton.axaml) 四种主题 |
| `IconButtonDefaults` 颜色、尺寸/形状与 outline 默认值 | IconButton.cs 的尺寸/四外观 TokenSource、[Token 入口](../../Tokens/MaterialTokens.Comp.IconButton.cs) |
| XS/Small/Medium/Large/XLarge IconButton Token | 尺寸 class、MaterialSymbol 模板部件与 [ButtonLayout](../Button/ButtonLayout.cs) |
| fixed shape / pressed shapes 与 `AnimatedShape` | MaterialButton.PressedCornerRadius → MaterialSurface → 共享 AnimatedCornerRadius |
| clickable/indication | 原生 Button 负责激活，共享 MaterialInteractionController 负责反馈 |

## 配置流与覆盖优先级

尺寸与四外观 TokenSource 保存基线/系统 Token 引用；IconButton.axaml 把它们接成主题默认配置。
共享 [ButtonThemeProperties](../Button/ButtonThemeProperties.cs) 承接尺寸和禁用条件的候选，
公开 Background/Foreground、描边、CornerRadius 及 XS Width/Height 通过主题级绑定消费。
条件选择器不直接把普通用户 Style 覆盖掉；Style、绑定与局部值仍由 Avalonia 形成有效配置。
ButtonLayout 和 MaterialSymbol 只消费模板传入的尺寸，不再读取另一份组件默认值。

XS 的 32 DIP 是主题选出的 Width/Height 默认值；普通 Style 可以改成 44，清除局部值后再按当前
class 和资源恢复。ButtonLayout 的共享 48 DIP 留白来自 `Comp.Button.MinimumInteractiveSize`，
容器宽高仍由 IconButton 自己的尺寸 Token 提供，不能误用普通 Button 的 58 DIP 容器最小宽。
资源、主题或挂载作用域改变时保留实时 Token 绑定；形状动画的当前呈现不写回配置属性。
此组织沿用维护者 2026-10-05 的配置迁移确认；ambient 颜色迁移另见下方 2026-11-03 决定。

## 模板与布局不变量

显式 `MaterialButton.Shape / PressedShape` 和 `IconToggleButton.CheckedShape` 进入同一个 Surface。
固定 IconButton.kt 的 isCornerBasedShape / shapeByInteraction 分支在当前切角族复用原圆角播放器：
按压优先、checked 次之，保留第三目标捕获、反向速度、百分比尺寸和 key(shapes) 重建。普通路径
直接切换，同时按 Core ripple(focusRingShape) 重新建立 indication，不重放旧按压。原圆角 API 保留。
默认未设置显式 Shape 时仍沿原流程；自定义路径不参加任意几何 morph。
新增验证为 `ButtonShapeApiTests` / `IconToggleButtonTests` 以及双端 `material-shape.*.icon-*`：
六种动作主题、四种切换主题、RTL、按压反向、百分比缩放与第三目标。Catalog 主窗口演示两类入口。

Vibrant Standard 持有完整模板和原有颜色 Token 配置，普通 Standard 派生后改用 ambient；
Filled/Tonal 及两种 Outlined 入口以 BasedOn 修改颜色/描边，隐式主题为普通 Standard。
原模板为 ButtonLayout → MaterialSurface#PART_Container → IconButtonSymbol，仅提供 Symbol 便利入口。
2026-11-08 本轮授权补齐上游 content lambda：Surface 子层同时保留 Symbol 与原生 ContentPresenter，
非空 Content/ContentTemplate 选择自定义内容。采用原生逻辑内容管理和自然尺寸测量，不额外缩放自定义图标。
此行为修订源于时间默认模式按钮需要精确上游矢量，旧限制会令其内容消失；原 Symbol 的字体便利性保留。
仓内 VectorSymbol 夹具改为替换 Grid 内的 Symbol，不再丢弃内容层；旧调用若依赖忽略 Content，应清除两项。
`IconButtonContentTests` 验证十种主题的动态内容/模板优先级、恢复 Symbol、尺寸和原生键盘动作。
双端 `icon-button-content-verified2` 使用真实内容填充与剪裁，覆盖 40 静态和 20 按压/变形缩放用例；
60 例全部通过原门禁（4 equal、56 withinTolerance）、零错误且重复稳定；已查看从切角到圆角的按压截图。原夹具创建但漏赋 Content 的失败报告保留，补齐两端同等输入后重跑，
未改参考、采样时刻或容差。
内部 IconButtonSymbol 继承 MaterialSymbol 的绘制及 StyleKey；它局部对应 Compose `size(iconSize)` 的两轴独立约束，
使用 MaterialSymbol.DefaultSize 而不是会溢出父约束的原生 Width/Height，不改变通用 MaterialSymbol 的正方形测量约定。
Symbol=None 保留真实模板与测量，不应通过删除部件模拟空图标。
Surface 通过 TextElement.Foreground 重新接入 owner 内容色，防止自身隐式主题遮蔽图标色。
状态选择器顺序 hover < focus-visible < pressed，只选墨色，透明度/ripple 时序仍归共享控制器。

尺寸采用 32/40/56/96/136 DIP 容器高度、20/24/24/32/40 DIP 图标。
WidthOption 的宽度由图标尺寸加所选两侧间距组成；Uniform 保留固定上游把 LeadingSpace 相加两次的表达。
IconButtonThemeProperties.ContainerWidth 是主题内部候选，ButtonLayout 与 XS 外层 Width 消费同一值。
Small 保留已确认的最小 48 DIP 外层；其非 Uniform 的 Compose 对应调用为
`Modifier.minimumInteractiveComponentSize().size(IconButtonDefaults.smallContainerSize(widthOption))`，
与上游窄按钮样例的布局组合一致。其余尺寸沿用原来的 size helper 约束。
Small 默认容器 40、外层 48；XS 通过 owner Width/Height 的主题默认 32 约束，对应上游 size(32dp)。
ButtonLayout 持有默认容器尺寸而非写 owner MinWidth/MinHeight，不能用公开最小值重建布局。
受限布局要保留正常容器与外层的分别取整；说明与确认范围见
[最小交互布局](../../../../docs/DECISIONS.md#最小交互布局与工具栏对齐读数)。

默认固定形状；显式 PressedCornerRadius 或 opt-in morph class 才启用按压目标。
square / morph 与尺寸 class 组合后分别选择 ContainerShapeSquare、PressedContainerShape 和
SelectedContainerShapeRound/Square，选中形状保留上游 round/square 互换关系。
条件样式只修改内部候选，公开配置通过主题 Setter 绑定消费，所以普通用户 Style、局部值、
可空目标的显式 null 都能覆盖；移除 class 后恢复默认。所有配置仍实时跟随 Token。
共享形状的反向、第三目标和生命周期细节见 [Button 维护说明](../Button/INTERNALS.md#交互与动画职责)，
替换模板时不得留下旧 Surface 交互或动画绑定。

## 已确认的选择与未解决问题

确认依据与历史观测保留自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/IconButton/README.md)。
以下只保留影响维护判断的决定和边界；旧测试数量、依赖版本及临时报告路径不是当前验证结果。

2026-10-03 确认的固定圆角+可空按压目标同时覆盖四种 IconButton 主题，原因、原始确认及边界见
[Button 组件决定](../Button/INTERNALS.md#固定圆角加可空按压目标)。
原生 Enter 按下激活按[共享键盘职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)接受，
焦点/elevation 与 indication 关系按[Core Desktop 焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)。
2026-10-04 的布局修正仅覆盖最小交互分配及其读数，不代表下列问题已接受或修复。

2026-10-11 为 AppBarRow / AppBarColumn 补齐 Standard IconToggleButton，维护者确认采用
Symbol + 受控 IsChecked / CheckedChangeRequested。继承 IconButton 模板与共享输入反馈，
状态请求不自动写回；普通颜色沿用 ambient，选中 Primary，禁用替换 alpha，遵循上游
IconToggleButtonColors.rememberUpdatedState，不添加颜色过渡。独立 peer 暴露 Checkbox / Toggle。
AppBar 项目的一般图标槽使用 Icon / IconTemplate，不扩展独立 IconButton 的 Symbol 合约。
后续普通 IconButton 的 ambient 迁移见下方决定。验证见 AppBarOverflowTests 和 icon-toggle-button suite。

Filled / Tonal toggle 的六槽颜色定义在独立 IconToggleButton TokenSource，避免误用普通 ToggleButton
的禁用内容色（后者是 OnSurfaceVariant）。选择顺序为 disabled → checked → unchecked，匹配
IconToggleButtonColors 的 rememberUpdatedState；颜色没有动画。两者的状态层取实际 Foreground；
Standard 继续取 provider 外的 ambient 内容色。无伪类与有伪类的样式分开，避免普通匹配遮盖触发优先级。
`CheckedCornerRadius` 是可空组件属性；模板向已有 ButtonSurfaceState 提供选中状态和形状，
仍由 Surface 处理 pressed > checked > resting、DefaultEffects、目标中断及模板/挂载清理。
这些新增组件 API 在 2026-11-02 的剩余移植批次授权内实施，不改变已有固定形状默认值。

Vibrant 切换主题对应固定 `IconButtonDefaults.iconToggleButtonVibrantColors` /
`outlinedIconToggleButtonVibrantColors` 与 `outlinedIconToggleButtonVibrantBorder`。
`IconToggleButtonTokenSource` 提供 Standard / Outlined 的六槽默认值，按 disabled → checked → unchecked
即时选择；独立主题继承已有尺寸、形状 collector、受控请求与 Surface 生命周期，不新增动画引擎。
Standard indication 仍在内容色 provider 之外，读取外部 ambient；Outlined 的 Surface indication 使用最终内容色。
选中时描边厚度为零，包括禁用选中态；未选中禁用描边替换 OutlineVariant 的 alpha。
新切换主题最初没有改变旧 Vibrant IconButton 的状态层 Token 合约；现行反馈色决定见下文。
固定 upstream 的 Outlined Vibrant helper 写入普通 Outlined 缓存而没有写回自身缓存；本地无这类全局缓存，
默认返回的六个颜色槽保持对应关系，不将该 helper 的跨入口缓存副作用扩散到组件状态。

**已实现但范围有限：** 静态 MaterialShape、CutCornerShape 同族按压/选中插值；任意路径之间不提供几何插值，按固定上游直接切换。
可选 InsetRing 通过 Surface 自动接入。`icon-button-focus` 覆盖普通四主题、两种旧 Vibrant 主题的
五种尺寸，以及 dark / RTL / wide / square 下的焦点反向；当前未覆盖全部形变中按压与焦点交织组合。
对应原始 Token 可以存在，不意味着模板已经消费或有公开入口。

普通 Standard/Outlined 内容色、禁用色及描边已按下方决定迁移到 ambient；所有尺寸仍使用
Small.OutlinedOutlineWidth，原始 Large/XLarge 2/3 DIP Token 保留供显式配置。

**已观察到、尚未解决：**

- 属性变更当帧的即时更新、更换按压目标后部分状态层像素仍有差异
- 受限图标盒已改为两轴分别约束；文字字体图标绘制仍不由向量用例证明

这些事实未认定为已接受适配。IconButton.cs 中的历史“recorded deviation”注释只能说明实现，
不能代替维护者批准。FloatingToolbar 接入修正也未授权改变颜色、描边或阴影算法。

## 验证范围与历史观测

旧 FloatingToolbar 主线合入后的 4× headless 重采记录仍有不同用例；
与此前分支交付相比，用例/采样事实的分类状态未变，重复和额外被动截图稳定。
可见容器、Small48/XS32 外层分配和固定/显式目标的圆角已对齐，不等于全部视觉一致。
历史计数和临时报告只描述对应代码、依赖与环境；当前状态应通过跟踪用例重新采集确认。

双端用例覆盖四主题×五尺寸、round/square、暗色/禁用、ambient 内容色、RTL、空 Symbol、
约束、非对称圆角、描边、固定/变形按压、取消、中断、运行时更改和键盘。
为隔离字体，仅在夹具将模板字体绘制部件换为继承 MaterialSymbol 的向量箭头，保留测量、StyleKey、
模板关联、原始 Template 优先级尺寸与前景继承。产品尺寸 class 仍控制图标布局，Compose 调用真实组件 API。
矢量不自动镜像；测试不能证明 Material Symbols 字形、字体镜像策略或真实桌面显示效果。

## 验证入口

- [控件测试](../../../../tests/Material3Expressive.Tests/IconButtonThemeTests.cs)：实际主题、四外观、禁用/主题切换、尺寸 class 和交互接入；[ButtonConfigurationTests](../../../../tests/Material3Expressive.Tests/ButtonConfigurationTests.cs) 检查普通 Style 优先、XS 尺寸覆盖和清除恢复；Token 默认值另见下方测试。
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/IconButtonCases.cs) /
  [Compose 双端用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/IconButtonCases.kt)
- [Catalog](../../../Material3Expressive.Catalog/MainWindow.axaml) 提供真实内容组合；其事件处理见
  [MainWindow.axaml.cs](../../../Material3Expressive.Catalog/MainWindow.axaml.cs)

从仓库根目录执行，环境与结果分类见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~IconButtonThemeTests
./tests/VisualParity/parity.ps1 self-test --family icon-button
```

`self-test` 重新采集双端并检查重复捕获及额外被动截图的稳定性；执行完成、稳定、
`withinTolerance` 与严格一致是不同结论。已接受差异仍保留真实参考和精确比较结果，
不得修改门禁来隐藏差异。复核时记录代码与依赖版本、执行环境和新生成的报告，
分别说明执行错误、双端差异、稳定性及未覆盖项。

新增 Vibrant 切换主题的行为测试位于 `IconButtonAmbientTests` / `IconToggleButtonTests`，
覆盖 disabled 优先级、资源和主题切换、用户覆盖、重挂、真实输入与 automation 请求。
双端 `icon-toggle-vibrant` 的 64 例、316 个采样：61 equal、3 withinTolerance、执行错误 0
（`20261105-040507-829fbb88`）。其中 Standard / RTL 的快速反向在 Core repeat / extraImages
各有 5 个样本的 raster 波动，最大通道差 1；Avalonia 两类自检全部稳定，不把该套件声明为稳定性全通过。

新增主题后的原有 `icon-toggle-button` 回归为 226 例、832 个采样：218 equal、8 withinTolerance、
0 different、执行错误 0（`20261105-041341-96424931`）。7 例快速反向在 Core 的 repeat 或 extraImages
检查中不稳定；Avalonia 两类自检全部稳定。该结果不覆盖下一节记载的旧普通 IconButton 颜色差异。

`icon-button-focus` 的 36 例、300 个采样：20 equal、16 withinTolerance、执行错误 0，
双端 repeat / extraImages 全部稳定（`20261105-041102-38f0645c`）。场景显式对齐 Secondary /
OnSecondary 和 Expressive motion；不能把第一轮缺少 OnSecondary 且两端动效模式不同的结果归为组件缺陷。
原有颜色/动画场景的输入及比较门禁未改。

`icon-button.dynamic.vibrant-ink.*` 的 8 例已执行：旧 Standard 四例均不同，Outlined 四例全等，
执行错误 0、双端重复稳定。该证据隔离上文旧 Standard 的状态层 Token 兼容问题。
Windows Catalog 已实际检查新增两主题的点击，以及 Outlined 的空格键反选和描边恢复；
真实字体图标已显示，但不构成双端字体栅格或屏幕阅读器验收。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter 'FullyQualifiedName~IconButtonAmbientTests|FullyQualifiedName~IconToggleButtonTests'
./tests/VisualParity/parity.ps1 self-test --family icon-toggle-vibrant
./tests/VisualParity/parity.ps1 self-test --family icon-button-focus
```

[IconButtonTokenTests.cs](../../../../tests/Material3Expressive.Tests/IconButtonTokenTests.cs) 检查尺寸/颜色/描边 Token；
形状和 ButtonLayout 共享回归见 [Button 维护入口](../Button/INTERNALS.md#一致性边界与验证)。

## 2026-11-03 颜色与缓存决定

维护者在本次对话明确回复“两项都按推荐方案”，批准以下两个范围：

1. Outlined IconToggleButton 按固定 helper 的六槽构造逻辑实现，消除缓存调用顺序依赖。
   固定 AndroidX / Core 都读取 `defaultIconToggleButtonColorsCached`，却写入
   `defaultOutlinedIconToggleButtonColorsCached`；先调用 Standard 时可能得到透明选中容器 / Primary。
   本地明确不复制该缓存问题：未选中和禁用容器透明，未选中内容色为 ambient，禁用内容色替换 alpha，
   选中容器为 InverseSurface，选中内容色由 `contentColorFor(InverseSurface)` 决定。
   使用 `MaterialOutlinedIconToggleButtonTheme`；选中时默认无描边，禁用选中态同样无描边。
   这是已接受的固定上游语义修正，不是升级基线。双端新增用例显式传六槽颜色到真实上游组件，
   注明该修正边界，参考库及既有用例不改写。单元测试覆盖 Standard 先/后创建以及动态资源覆盖。
2. 普通 Standard（含隐式）/ Outlined 改用父级内容色；禁用时替换 alpha，Outlined 描边独立取父级色。
   原颜色 Token 契约迁移到 `MaterialVibrantIconButtonTheme` / `MaterialOutlinedVibrantIconButtonTheme`。
   依赖原 `Comp.StandardIconButton.*` / `Comp.OutlinedIconButton.*` 颜色覆盖的调用方需显式选择 Vibrant。
   普通用户 Foreground / BorderBrush / Background Style、Binding、LocalValue 仍优先。
   普通 Standard 状态层取 provider 外的 ambient；Outlined 与 Filled/Tonal/Outlined toggle 取有效 Foreground。
   颜色迁移不改变尺寸/形状 Token 或共享交互生命周期，也不替用户批准尚存的其他像素差异。

Outlined 选中内容色复用内部 `ContentColorFor` 的角色匹配顺序和 fallback；新增可选 BackgroundColor
绑定输入以直接消费 InverseSurface Token，避免用 disabled/用户覆盖后的实际 Background 推导默认选中内容色。
旧 BackgroundProperty 与 ambient 入口保持不变，未增加公共颜色框架。
内部 CheckedContentColor 候选经主题绑定使用，公开配置仍遵循 Avalonia 覆盖优先级。
ambient 颜色绑定显式观察 ISolidColorBrush.Color，父级替换画刷或修改可变画刷的 Color 都会更新；
转换器保留原有画刷输入支持，并接受颜色值，禁用 alpha 始终替换而非相乘。
仓内 Snackbar 的关闭按钮显式迁移到 Vibrant 基主题，保留原有候选与禁用 Token 契约；AppBar 的显式
Foreground 配置不受默认迁移影响。用法和调用方迁移示例见 [README](README.md#普通与-vibrant-颜色)。

双端普通图标夹具显式统一外层内容色：裸 Compose MaterialTheme/Box 的 LocalContentColor 默认黑色，
Avalonia Window 默认提供 OnSurface；用例在未指定 AmbientColor 时给 Avalonia 宿主黑色，防止比较不同输入。
带形变的 Outlined toggle 参考重载默认 vibrant border，因此显式传普通 outlinedIconToggleButtonBorder，
与本地普通颜色主题加 opt-in 形变的输入一致。未修改参考库、基线或容差。

## 当前验证

前次移植在 2026-11-03 使用固定参考重新运行 self-test（本批交互源补齐后的复验见下文）：

- icon-toggle-button：226 例，218 equal、8 withinTolerance、0 different；其中 5 例快速反向按压
  出现 Compose repeat / extraImages 差异，Avalonia 两类重复检查全部稳定。唯一静态微差为
  Outlined XS Narrow 未选中描边的栅格化，其余微差来自快速反向按压。
  运行身份 `20261102-165137-72b451c3`（产物目录按 UTC 命名）。
- icon-button：279 例，253 equal、6 withinTolerance、20 different；静态用例无 different。
  20 例分别涉及 ambient/主题/禁用/尺寸修改当帧、清除/更换按压目标、原生 Enter 和
  Vibrant Standard 的状态层墨色。ambient 变更后的下一帧及后续采样一致，变更当帧本地已经更新、
  Compose 仍呈现前一值；保留该事实，不推迟本地属性更新来迁就截图。
  `dynamic.reverse` 出现 Compose extraImages 不稳定，Avalonia 两类重复检查全部稳定。
  运行身份 `20261102-165137-eabce136`。

两套均无捕获执行错误；self-test 因重复性不稳定返回 2，不能报告为全通过。容差和参考库未修改。
非文字向量夹具继承实际 IconButtonSymbol 测量、尺寸 class 与前景配置。几何一致不证明字体一致。

行为测试覆盖宽度、约束、样式优先级、画刷对象/Color 更新、重挂载、两种主题入口间迁移、
Outlined 的六槽颜色与描边、contentColorFor 角色优先级、真实指针/automation 请求和形变中断。
Catalog 提供四种切换外观及普通/Vibrant 颜色对照，Windows 桌面已实际检查环境色和 Outlined
鼠标选中/恢复；这不替代跨平台、辅助技术或字体像素验收。
完整解决方案已在 .NET 10.0.114 构建通过。全量测试遇到的 ElevatedButton hover 像素测试未推进动画
时钟问题已改用现有手动时钟与真实输入，保留原像素断言；Headless 清理阶段仍曾出现渲染线程归属异常。
合并远端最新组件改动、双端采集进程退出后，最终单独执行控件全量测试 1587/1587 通过；该次通过不消除前述环境稳定性记录。

验证环境的原始 GitHub Packages restore 被 401/403 阻塞。颜色和 SmoothScroll 使用 SHA512 与锁文件
一致的原 CI nupkg；MaterialSymbols 使用版本所标注的 e577ac071623f74411e19e28e440eaca73a33d81 源码重建，
放在独立临时包缓存，恢复锁文件也放在 obj；仓库包版本、正式 packages.lock.json、参考提交和比较容差未改。
上述运行证据属于该临时环境，原始 MaterialSymbols 锁定制品与字体双端像素验收仍缺失。

## 外部交互源与形变生命周期

2026-11-03 剩余移植批次的非破坏性组件 API 授权下，IconButton/IconToggleButton 增加可选
`InteractionSource`，null 使用每实例内部源。原生 Button 仍拥有激活；源负责观察和反馈，不写 IsChecked。
按固定 `PressInteraction.kt.collectIsPressedAsState` 保存 Press 列表及对象身份，每次事件重新计算 bool。
源更换只重建列表，已记忆的 bool 保留到新事件；detach 取消原生开始事件、停止订阅并清理状态。
模板内部 `ButtonSurfaceState.IsPressed` 仅向 Surface 提供独立 collector 的值，未设置该内部输入的其他
组件仍用原来的 controller 聚合状态。该输入不改变公开 Button.IsPressed 或原生伪类。

新 indication 节点不能重建记忆中的 AnimatedShape。现有内部 ResetSourceIndication 增加可选
preserveSurfaceMotion，仅 IconButton 使用：结束旧 ripple/state-layer 动画和订阅，但保留 Surface
形状作用域、当前插值及速度；其余调用方的默认行为不变。中途换源的连续性有行为测试及双端动画采样。
固定形状与形变分支切换时清理形状 collector，保持上游静态分支不记忆按压的语义；
按住期间才打开形变不会重放旧 Press，波纹与原生按压仍按独立生命周期继续。
固定 AndroidX/Core 的 ripple 工厂都将 focusRingShape 纳入节点身份；key(shapes) 重建形状或
切回静态形状时，Surface 同步重建 indication、清除旧波纹，保留形状 collector 的独立状态。
首次形状配置不属于节点替换，不能清除挂载后刚收到的交互；重挂载会重置该初始化标记。
原生按住时换源与禁用一样，在 Clickable 的组合更新期间取消 Press；复用控制器已有的下一次
组合提交路径，使形状取消保留旧帧姿态和速度，不对普通源事件或外部 Press 换源统一增加延迟。
测试入口为 IconButtonInteractionSourceTests、双端 `icon-button-source`；Catalog TooltipDemo 展示与 Label 共享源。

新增场景保留换源当帧旧 Compose 波纹尚未重绘、本地已清除的差异；随后帧形状和波纹生命周期应分别比较。
**反馈色决定：本轮依据用户扩大授权重新决定。** 固定 AndroidX/Core `IconButton.kt` 相同：
`IconButtonImpl` 在内容色 provider 外创建 ripple，Surface 路径在内容 provider 内创建 indication。
原 Vibrant Standard 保留状态层 Token 是为兼容旧主题；Filled/Tonal/Outlined Vibrant 的相同路径
还导致 Foreground 覆盖及禁用时的活动波纹使用旧色。现统一遵循上述 provider 边界：Standard
（普通/Vibrant）用父级 ambient，其余三种样式（含 Outlined Vibrant）用实际 Foreground。
使用现有 XAML 绑定，不新增颜色缓存、动画或共享调度机制。`button.Foreground = Brushes.Red`
在 Filled/Tonal/Outlined 同时改变内容和反馈；Standard 反馈由父级 TextElement.Foreground 控制。
备选的“默认仍读独立 Token，另加 opt-in 属性”会保留错误默认行为；自动侦测资源覆盖会引入
额外优先级，均不采用。旧 Token 类型和来源保留供自定义主题使用，独立反馈色需迁移到
模板 Surface.StateLayerBrush；容器/内容/描边 Token 与所有布局、动画和交互源 API 不变。
此决定替代旧状态层消费合约，不追认为早期已批准。

本批最终控件测试 1702/1702 通过，其中新交互源及 Label 行为用例 26 项；VisualParity 系统测试
124/124 通过。期间全量测试曾有一次 IconButtonThemeTests 清理阶段 InvalidOperationException，
该单例复跑以及随后两次全量测试通过；保留该稳定性事实，不把它归为已修复缺陷。

2026-11-03 本批双端复验（容差、参考提交及用例均未删除或放宽）：

- `icon-button` 279 例，255 equal、6 withinTolerance、18 different，零执行错误
  （`20261103-115748-afb77612`）。原有 clear-shape / change-shape 两例现为 equal；剩余差异
  涉及属性修改当帧、原生 Enter 和既有墨色边界，静态矩阵没有 different。
  Compose repeat / extraImages 各 1 例不稳定，Avalonia 两类检查稳定。
- `icon-button-source` 48 例，11 equal、13 withinTolerance、24 different，零执行错误
  （`20261103-115919-b7ea4173`）。覆盖四种外观、固定/形变、重叠按压、换源、原生按压换源、
  hover、禁用及 LTR/RTL 静态/形变往返。换源与形状重建的后续形变采样已对齐，变更当帧和
  禁用墨色差异仍保留。Compose repeat 3 例、extraImages 4 例不稳定，Avalonia 两类检查稳定。
- `icon-toggle-button` 226 例，218 equal、8 withinTolerance、0 different、零执行错误
  （`20261103-120453-4bb7985d`）。微差为快速反向动画及 Outlined XS Narrow 未选中描边；
  Compose repeat 3 例、extraImages 6 例不稳定，Avalonia 两类检查稳定。

这些 self-test 完成并不表示双端全匹配；原始报告保留不同值、实际截图与重复性证据。
最终解决方案构建通过，保留现有私有 API、可空性、过时属性及测试分析器警告；依赖验证范围仍受
上文临时包缓存与原始字体制品缺失的限制。

## 2026-11-05 反馈色修复复验

本轮先用当前代码重现 Vibrant Standard hover 持续墨色差异，再按固定两端的 provider 边界修复。
`IconButtonAmbientTests` 的新增行为回归在修复前失败，修复后 IconButton 相关测试 54/54 通过；
覆盖局部前景、父级画刷对象及 Color 更新、hover/press、禁用、主题切换和重挂，Catalog 增加对应示例。

- `icon-ink-current`：icon-button 293 例，263 equal、7 withinTolerance、23 different，全部重复稳定，零执行错误。
  六个新增 content-ink 用例覆盖普通/鲜艳 Standard、Filled、Tonal 和普通/鲜艳 Outlined；
  前景改变、禁用和清除覆盖的修改当帧仍不同，下一采样帧及后续反馈色相等。保留实际事件时序，
  没有延迟属性更新或补帧来掩盖差异。Vibrant Standard 的稳定 hover 墨色已相等；
  其 ambient 修改当帧以及其他既有主题/禁用/尺寸变更当帧的差异仍未解决；原生 Enter 按下激活
  则按 DECISIONS 的共享键盘职责保留，不能将两类情况混为尚待决定。
- `icon-ink-source-current`：icon-button-source 48 例，12 equal、12 withinTolerance、24 different，
  全部重复稳定，零执行错误。保留换源、禁用及原生按压取消的差异，不能表述为交互源全匹配。

原始报告位于 `artifacts/visual-parity/runs/` 下同名目录，包含实际图像、结构化状态、事件与重复性。
固定参考提交、依赖和容差未改；上述结论不替代字体、真实辅助技术或所有历史未验收能力的验证。
