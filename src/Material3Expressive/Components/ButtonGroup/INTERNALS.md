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
例如间距 12 DIP 经 Comp.ButtonGroup.Spacing 绑定到 ButtonGroup.Spacing，再由布局按物理像素取整。
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

内置 Action / ToggleAction 通过按钮主题的 `TextBlock.TextWrapping=NoWrap` 对齐上游 `softWrap=false`。
Button / ToggleButton 模板读取原生附加属性，详见[共享决定](../../../../docs/DECISIONS.md#按钮标签换行使用原生附加属性)。
没有为此另建标签模板；CustomItem 继续由调用方配置。上游同时设置的 `maxLines=1` 对显式换行的截断尚未对齐，
本次只修正自动换行，不将两者视为同一行为。

## 验证与边界

单元/headless 入口：[ButtonGroupTests](../../../../tests/Material3Expressive.Tests/ButtonGroupTests.cs)。
双端入口：[Avalonia cases](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/ButtonGroupCases.cs) /
[Compose cases](../../../../tests/VisualParity/reference/src/main/kotlin/cases/ButtonGroupCases.kt)。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~ButtonGroupTests
./tests/VisualParity/parity.ps1 self-test --family button-group
```

用例覆盖固有尺寸、有限约束、权重、全部排列、逻辑间距对齐、RTL、connected 状态，以及按压、快速释放、
取消、禁用、重按、零扩宽、压缩上限、单项可见、菜单打开/关闭/反向和动态调整空间。
场景自行调用真实上游组件，没有复制算法生成参考结果。
动态项目两端用普通外层内容宿主观察实际分配，避免把 Compose Button 内部未受限的最小交互节点尺寸
当成外层分配；颜色输入固定为相同 ColorScheme，排除配色生成器差异。

尚未提供通用可注入 MutableInteractionSource；PressSource 使用真实控件的共享输入状态。
任意多指交互源、任意 Compose Shape、文字栅格、OS 屏幕阅读器及实际桌面双端体验未经完整验证。
公开 Action 使用现有原生 Button 键盘入口；原生 Enter 与 Compose 的激活时机边界见 Button 的维护说明，
不能用本套件的指针用例证明键盘完全一致。

`self-test` 的执行、重复性和双端比较是不同结论；本次结果及剩余采样在任务交付中按真实产物报告。

当前保留的观测：动态 `content-0` / `content-2` 有居中取整差异，组和项目的分配几何、回调状态一致。
`connected` 在 release / time-96000、`disable` 在 disabled 至 time-400000 仍有像素差异，
尚未证明全部属于上述舍入范围；不把这些失败归入半像素豁免。
两个方向的 `repress` 在 Compose 额外被动截图检查中出现最大 1 通道值变化，普通重复捕获与 Avalonia 自检稳定。
保留不稳定标记和原始产物，不放宽门禁。显式组高度/MinHeight 的约束传递、任意自定义内容的固有尺寸查询
尚未由当前宽度用例证明；Avalonia 以自然尺寸测量适配 Compose 固有查询，不存在通用 intrinsic 接口。
