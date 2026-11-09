# ButtonGroup 实现说明

使用方法见 [README](README.md)。默认逻辑依据为 AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，桌面参考为 Compose Multiplatform Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`；两端固定 `ButtonGroup.kt` 内容相同。
固定版本身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)。

## 结构与源码依据

Material3 路径前缀为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`。

| 上游符号 | 本地职责 |
| --- | --- |
| `ButtonGroup`、`ButtonGroupMenuState` | ButtonGroup.cs：ItemsControl、配置属性、菜单状态及指示器接入 |
| `ButtonGroupScope.clickableItem/toggleableItem/customItem` | ButtonGroupAction、ButtonGroupToggleAction、ButtonGroupCustomItem：两种呈现及操作职责 |
| `ButtonGroupMeasurePolicy` | ButtonGroupLayout.cs：固有测量、权重、可见前缀、邻项压缩与排列 |
| `EnlargeOnPressNode`、`waitUntil` | ButtonGroupWidthMotion.cs：共享交互观察、Float Animatable、短按等待及中断 |
| `ButtonGroupDefaults` 的可配置数据、`tokens/ButtonGroupSmallTokens.kt`、`tokens/ConnectedButtonGroupSmallTokens.kt` | ButtonGroupTokenSource、组件 Token 和 ButtonGroup.axaml |
| `ButtonGroupDefaults.OverflowIndicator` | XAML 组合 TooltipBox、PlainTooltip、Filled IconButton |
| connected shapes | 基于现有 ToggleButton 的首、中、尾主题，继续消费 MaterialButton / ToggleButtonAppearance 形状属性 |

`ButtonGroup` 的模板包含项目 ItemsPresenter、指示器槽和 DropdownMenu。每个项目有可隐藏的外层槽，
槽内是实际 descriptor 与 Button / ToggleButton / 自定义内容。只隐藏槽，保留内容用于下一次固有测量。
菜单消费后缀的独立内容，不自动把自定义菜单动作解释为关闭请求。

配置路径只有 `TokenSource → XAML 主题绑定 → Avalonia 属性 → 布局/动画/绘制`。
例如默认 SpacedBy(12) 经 Comp.ButtonGroup.HorizontalArrangement 绑定到同名属性，布局将其 Spacing 按物理像素取整。
运行中的扩宽进度是 Float Animatable 状态，不回写 ExpandedRatio、CompressionLimit 或 Width。
1 秒等待上限是上游算法常量，不作为主题默认值另建配置入口。

## 布局与动画不变量

无权重项目先查询固有宽度，权重项目分享剩余空间并分配舍入余数；只有放不下完整内容时才预留指示器。
保留最前面的可见前缀，后续项目进入菜单。上游测量期间固定的三个行为均保留：

- 行高只取已放置项目，不把指示器高度算进行高；全部溢出时可以为零。
- 边界处预留间距可能使返回的真实行宽大于约束。Avalonia 会截断 DesiredSize，因此布局保存真实行尺寸，
  以物理像素整数除法重现 Compose apparentToRealOffset，并在受限外框内居中排列真实行。
- 只有一项可见、但还有隐藏项目时，首项仍可从隐藏的下一项取得压缩额度。

这些是固定源码行为，不是本地修正后的新规则。RTL 使用 Avalonia 子树镜像，对应 Compose 的逻辑排列和 placeRelative。
邻项压缩按项目顺序操作同一个宽度数组，不能并行按初始宽度分别计算。

扩宽用 FastSpatial Float spring。按下启动到 1 的动画，松开保留仍在运行的扩宽，先等待一帧，
再等待进度严格大于 0.75 或帧时间严格超过 1000 ms，之后收回到 0。重按取消等待/收回，复用当前值与速度。
更换 PressSource 取消旧观察和任务，保留 Animatable；移除项目、模板或窗口时释放自己的子作用域。

## 本次确认的适配

2026-10-16，维护者确认项目式 API：Action、受控 ToggleAction 和 CustomItem；自定义项目显式指定 PressSource，
复用本库已有共享交互状态，不建立第二套 InteractionSource。受控切换只发布 CheckedChangeRequested，
按钮与普通菜单项都调用相同操作，再由调用方更新状态。

同日维护者要求默认值沿用 TokenSource，明确不采用 ButtonGroupDefaults；圆角和间距经主题提供，
普通实例属性与 Style 可以覆盖。

同日维护者确认共享图标通过 Icon 数据和 IconTemplate 创建两份实例，直接 Control 组合使用 CustomItem。
因此 Action.Icon 拒绝 Control。上游的 composable icon 会被调用两次；本地不通过搬移单个视觉实例或缓存错误固有宽度近似它。

同日维护者明确：按钮内容居中在半像素处的舍入差异，本次不强制解决，不为该行为硬编码专门补偿。
本次保留原生按钮内容布局；共享 ButtonContentPresenter / ButtonContentRow 未包含本任务改动。
该范围不接受其他未解释的状态、布局或绘制差异，也不把精确比较失败改记为通过。

菜单状态本身、命令执行、布局算法、动画持有者不放进 TokenSource。现有 Button、ToggleButton、IconButton、
Surface、Tooltip 和 DropdownMenu 的基础实现继续按各自维护说明与共享决定执行。

内置 Action / ToggleAction 对齐固定上游 `Text(maxLines=1, softWrap=false, overflow=Visible)`。
两者共用组件内 ButtonGroupLabelTemplate：原生 TextBlock 的 MaxLines=1 / NoWrap / 无省略，
且不在文字节点裁剪；保留父 Button Surface 的裁剪。不能截断 Label 数据，否则菜单会丢失原文。
这次模板补齐单行与 Visible 语义，不修改通用 ButtonContentPresenter，也不改变
[共享换行配置](../../../../docs/DECISIONS.md#按钮标签换行使用原生附加属性)；CustomItem 仍由调用方配置。

## 验证与边界

单元/headless 入口：[ButtonGroupTests](../../../../tests/Material3Expressive.Tests/ButtonGroupTests.cs)。
其中显式换行覆盖 LF、CRLF、Unicode 行分隔符、动态 Label 和菜单原文；文字像素按当前
VisualParity AUTHORING 的范围暂不纳入，不能从这些行为测试推断双端文字绘制一致。
本批次另启动 Windows Catalog，检查三项标签在组内仅显示首行，并在收窄后实际打开菜单，
确认 Share / Archive 的完整换行文字仍显示；这只是本地桌面冒烟检查。
双端入口：[Avalonia cases](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ButtonGroupCases.cs) /
[Compose cases](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ButtonGroupCases.kt)。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter 'FullyQualifiedName~ButtonGroupTests|FullyQualifiedName~ButtonGroupInteractionSourceTests'
./tests/VisualParity/parity.ps1 self-test --family button-group
./tests/VisualParity/parity.ps1 self-test --family button-group-source
```

用例覆盖固有尺寸、有限约束、权重、全部排列、逻辑间距对齐、RTL、connected 状态，以及按压、快速释放、
取消、禁用、重按、零扩宽、压缩上限、单项可见、菜单打开/关闭/反向和动态调整空间。
场景自行调用真实上游组件，没有复制算法生成参考结果。
动态项目两端用普通外层内容宿主观察实际分配，避免把 Compose Button 内部未受限的最小交互节点尺寸
当成外层分配；颜色输入固定为相同 ColorScheme，排除配色生成器差异。

`ButtonGroupCustomItem.InteractionSource` 对应 `ButtonGroupScope.animateWidth(InteractionSource)`，
复用 Foundation 的 MaterialInteractionSource。显式源优先于 PressSource，后者保留真实控件状态的既有入口。
每次订阅持有独立 Press 列表，按对象身份移除一个匹配开始；每个事件都映射到列表是否非空，再去重。
因此换源后的第一个 Hover / 未匹配结束也会提交 false，遵循上游，而非只处理 Press 事件。
外部源替换复用现有 NextFrameAsync + 组合阶段调度：旧动画先采样提交帧，再取消运行与等待、
更换订阅并保留 Animatable 值。连续替换只提交最后一个源，待提交时卸载会取消任务；
首次接入与原有 PressSource 控件桥仍直接连接。该非破坏性组件 API 在剩余移植任务授权范围内，未新增共享机制。

行为入口 `ButtonGroupInteractionSourceTests` 覆盖多按压、快速释放/重按、换源、禁用、PressSource 回退与重挂。
双端 `button-group-source` 的最终运行 `20261104-082324-0e8ea2b7`：12 例全部完全相等、无执行错误、
重复稳定。首轮发现的 65ms 换源旧动画停在 64ms 的问题已按 EnlargeOnPressElement.update 的提交
顺序修复，没有加入固定时间补偿；该修复仅涉及本组件的新外部源路径，不扩大为其他源消费者的结论。

任意多指交互源、任意 Compose Shape、文字栅格、OS 屏幕阅读器及实际桌面双端体验未经完整验证。
公开 Action 使用现有原生 Button 键盘入口；原生 Enter 与 Compose 的激活时机边界见 Button 的维护说明，
不能用本套件的指针用例证明键盘完全一致。

`self-test` 的执行、重复性和双端比较是不同结论；本次结果及剩余采样在任务交付中按真实产物报告。
原有 80 例最终回归 `20261104-082716-feb94049`：54 例完全相等、12 例容差内、14 例不同，
执行错误 0；本轮仅 RTL repress 的 Core 额外被动截图不稳定，其余 79 例稳定。

当前保留的观测：动态 `content-0` / `content-2` 有居中取整差异，组和项目的分配几何、回调状态一致。
历史 `connected` 在 release / time-96000、`disable` 在 disabled 至 time-400000 有像素差异，
不能归入半像素豁免。当前 `ripple-button-group-current` 的 disable 已没有该图像差异；
connected 的 96ms 内容块过期来自夹具缺少 Foreground 绘制失效依赖，独立像素测试先失败后通过。
`button-group-marker-current` 修复后该帧逐像素相等；release 的 81ms 原生选中色即时更新与 Core
组合缓存差异仍保留，未修改组件或补帧。夹具证据见 VisualParity DECISIONS。
本轮已重跑全部 110 个场景；固定最小高度传播、居中取整及非帧状态变化仍按实际失败报告，
没有因为修复内容颜色而缩减覆盖。原 fixture 的失败证据保留在 `ripple-button-group-current`。
两个方向的 `repress` 在 Compose 额外被动截图检查中出现最大 1 通道值变化，普通重复捕获与 Avalonia 自检稳定。
保留不稳定标记和原始产物，不放宽门禁。任意自定义内容的固有尺寸查询
尚未由当前宽度用例证明；Avalonia 以自然尺寸测量适配 Compose 固有查询，不存在通用 intrinsic 接口。

2026-11-05 补充运行时最小尺寸变更：`ButtonGroupMeasurePolicy` 在每次 measure 读取 constraints.minWidth / minHeight，
本地 Panel 读取 owner 对应属性，因此必须显式使内部测量失效。仅让 ItemsControl 自身重新测量时，
传给模板的最大 availableSize 未变，内部 Panel 会继续复用旧行宽/高。修复只增加两项失效依赖，
没有改变测量算法、子项配置或公共 API。`ButtonGroupTests` 新增有界无权重、无界权重和空组高度回归，
ButtonGroup 相关测试 **32/32** 通过。Catalog 新增 Minimum width。

双端动态 minimum 的 LTR/RTL × 权重/无权重四例，依次增大、减小、清除 MinWidth，
所有实际几何、状态与图像完全相等，执行错误 0 且重复稳定。运行 ID 分别为
`20261105-064526-05852dfa`、`20261105-064534-bc9f2d42`、`20261105-064540-a0a2ddfa`、`20261105-064545-66e9e027`。

**已获本轮技术决策授权、尚未实现：最小高度向内容传播。** 补充 26 个双端高度场景（LTR/RTL，Height / MinHeight 为 16 / 64，
全可见 / 部分溢出 / 全溢出，MaxHeight=16）。`20261105-064104-c320a803` 中 18 例完全相等、8 例不同，
全部重复稳定、无执行错误。Height / MinHeight=64 且存在可见项目时，上游把最小高度传给内容，
本地只扩大外层槽：例如 `new ButtonGroup { Height = 64, MaxWidth = 100 }` 内首项自然高 24，
两端行均为 84×64，但首项 Core 为 40×64、本地为 40×24。截图保留实际蓝色内容面积差异。

推荐在现有布局能力中明确“父级最小约束”与原生控件自身 MinHeight 的传播契约，再由 ButtonGroup 传入。
调用方仍使用现有 `Height` / `MinHeight`，不需要新组件 API；实现将影响 ButtonLayout、模板中间层及任意 CustomItem，
必须说明局部 Height、对齐和嵌套自定义布局如何响应。直接改写子控件 MinHeight 会覆盖调用方配置且不等价于 Compose constraints，
单纯拉伸槽也不能改变内容测量，因此未采用这些近似。本轮用户扩大授权已覆盖该技术取舍，
无需再次逐项确认；当前缺口仍是任意自定义内容的最小约束传递，尚未实施，没有把失败用例删除或改为成功。
同一次 106 例运行也复核原有 80 例；原有反复按压两方向的 Core 额外截图不稳定仍保留，
不能把新增静态场景稳定推广到整个组件。

### 2026-11-09 任意水平 Arrangement

固定 ButtonGroupMeasurePolicy 使用 Arrangement.Horizontal.spacing 分配权重/溢出，再把按压调整后的可见宽度
交给同一 Arrangement.arrange；Core common 同义。旧枚举与 Spacing/SpacingAlignment 拆分只表达了常用排列，
不能提供绝对方向或用户定义算法。本轮按用户明确授权改用共享 MaterialHorizontalArrangement，删除局部重复算法。
保留权重、溢出、邻项压缩和指示器 placeRelative(shownItemSpace) 的独立位置：不把指示器塞入 Arrangement 数组。
共享排列输出物理坐标，在 Avalonia RTL 子树镜像前转回局部坐标。默认仍为主题提供的 SpacedBy(12)。

破坏性迁移：枚举常量改同名共享静态值；Spacing/SpacingAlignment 改 SpacedBy 参数；Spacing Token 合并进
HorizontalArrangement Token。仓内 Token 优先级测试、Catalog 和双端用例均已迁移，完整调用方式见 README。
保留既有最小高度传播缺口，不以本次排列接入宣称解决。

### 本轮双端验证记录

`button-group-arrangements` 原始完整运行保留：8 equal / 10 withinTolerance / 23 different / 115 error。
第一处异常为新增 `static.arrangement.absolute-spaced.false.100.none`，固定 Core 的 ButtonGroup.kt:712
在固定宽度发生 overflow 时以 minWidth=400、maxWidth=211 构造 Constraints 并抛错；参考进程退出使后续捕获失败。
这不是 Avalonia 对齐结果，后续 114 error 也不是独立组件缺陷。原用例和失败产物没有删除。

另外增加 `button-group-arrangement-sizing`，采用自然宽度但相同最大宽度、排列、权重、LTR/RTL 的 40 例，
全部 equal、执行错误 0、重复稳定，实际重叠 RTL 帧图已检查。新的六个按压组合有既有原生按钮内容居中差异，
例如 absolute-spaced LTR 的 32ms content-2 为 Core x=292 / Avalonia x=291.75 DIP；组/项目分配几何一致。
不能由此宣称固定宽度溢出、任意内容最小约束或所有旧动态场景已通过。
产物路径为 `artifacts/visual-parity/runs/<运行名>/summary.md`。


为恢复原参考进程异常后未执行的静态回归，`button-group-layout-verified` 另行完整运行原有 76 例和新增自然宽度 40 例，
合计 116 例：108 equal / 0 withinTolerance / 8 different，执行错误 0、重复稳定。
其中 8 个 different 为上文保留的 Height/MinHeight=64 最小约束传播缺口；新增排列 40 例全部 equal。
固定宽度溢出引发 Core 异常的原始用例仍在 button-group family 中，未替换或删除。
