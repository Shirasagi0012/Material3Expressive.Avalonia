# Segmented button

实现 `SingleChoiceSegmentedButtonRow`、`MultiChoiceSegmentedButtonRow` 和共享的
`SegmentedButton : Button`。两种行均为 `ItemsControl`，可直接添加按钮，也可用
`ItemsSource` / `ItemTemplate` 生成按钮。

依据 [固定基线](../../../../docs/upstream-baseline.json) 中 AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与 Compose Multiplatform Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SegmentedButton.kt`。
两份组件文件的 Git blob 相同（`7b7382eeb52153f86368551ef3754bd638dc118a`）。
Token 依据 `tokens/OutlinedSegmentedButtonTokens.kt`；键和默认引用分别位于
[MaterialTokens.Comp.SegmentedButton.cs](../../Tokens/MaterialTokens.Comp.SegmentedButton.cs)
和 [SegmentedButtonTokenSource.cs](SegmentedButtonTokenSource.cs)。桌面输入与动画辅助行为按固定 Core 核验。

## 使用与选择职责

行决定布局、首/中/尾圆角和无障碍角色，应用决定选择。`Click` / `Command` 请求动作，
不会自动修改 `IsSelected`，单选行也不会强制删除其他选择。这与 Compose 的外部
`selected` / `checked` 参数及回调职责一致。

```xml
<m3:SingleChoiceSegmentedButtonRow
    xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:SegmentedButton Content="Day" IsSelected="{Binding IsDay}"
                      Command="{Binding SelectDay}" />
  <m3:SegmentedButton Content="Week" IsSelected="{Binding IsWeek}"
                      Command="{Binding SelectWeek}" />
</m3:SingleChoiceSegmentedButtonRow>
```

上述命令由 ViewModel 设置互斥状态。C# 多选可这样处理：

```csharp
var row = new MultiChoiceSegmentedButtonRow();
var item = new SegmentedButton { Content = "Unread" };
item.Click += (_, _) => item.IsSelected = !item.IsSelected;
row.Items.Add(item);
```

数据项通过模板生成按钮，选择同样由数据和命令维护：

```xml
<m3:MultiChoiceSegmentedButtonRow ItemsSource="{Binding Filters}">
  <m3:MultiChoiceSegmentedButtonRow.ItemTemplate>
    <DataTemplate x:DataType="vm:FilterViewModel">
      <m3:SegmentedButton Content="{Binding Label}" IsSelected="{Binding IsSelected}"
                          Command="{Binding ToggleCommand}" />
    </DataTemplate>
  </m3:MultiChoiceSegmentedButtonRow.ItemTemplate>
</m3:MultiChoiceSegmentedButtonRow>
```

单选按钮提供 RadioButton / SelectionItem 语义，单选行提供 Selection group；多选按钮
提供 CheckBox / Toggle 语义。辅助技术的 Select / Toggle 调用也只请求应用动作。
Space 和 Enter 都在释放时激活，失焦或禁用取消待激活的按键；复用共享交互反馈。

## 外观与布局

默认 `Space=1` 是相邻描边的重叠量。行按最大的 intrinsic item width 分配等权宽度，
默认背景高度 40、交互留白至少 48、按钮最小背景宽度 58 DIP。内容或模板改变后重新测量。
首尾保留 `BaseShape` 的外侧圆角，中间项为矩形，单项使用完整形状，RTL 镜像首尾。
默认 `BaseShape` 来自 Full shape Token；调用方的按钮 `CornerRadius` 优先于行生成值。

```xml
<m3:SingleChoiceSegmentedButtonRow Space="2" BaseShape="8">
  <m3:SegmentedButton Content="List" BorderThickness="2" />
  <m3:SegmentedButton Content="Grid" BorderThickness="2" />
</m3:SingleChoiceSegmentedButtonRow>
```

`Space` 应与描边宽度相同；行不会反向修改按钮描边。原生 `Padding`、`Background`、
`Foreground`、`BorderBrush`、`BorderThickness` 与 `CornerRadius` 的局部值或绑定保留。
默认 Padding 是固定上游的旧 `ButtonDefaults.ContentPadding`（水平 12、垂直 8），
不采用 expressive Small 按钮的垂直 10。

`Content` / `ContentTemplate` 是 label 槽；默认 LabelLarge 排版。`ActiveIcon` 为空时使用
固定上游 Check 向量路径；`InactiveIcon` 为空时不显示未选中图标。两者分别支持
`ActiveIconTemplate` / `InactiveIconTemplate`。`ShowSelectionIcon=false` 关闭整个图标槽，
标签仍遵循上游的图标预留及居中规则。

```xml
<m3:SegmentedButton Content="List" IsSelected="{Binding IsList}">
  <m3:SegmentedButton.InactiveIcon>
    <m3:MaterialSymbol Symbol="List" />
  </m3:SegmentedButton.InactiveIcon>
  <m3:SegmentedButton.Colors>
    <m3:SegmentedButtonColors ActiveContainerColor="#126E60"
                             ActiveContentColor="#FFFFFF" />
  </m3:SegmentedButton.Colors>
</m3:SegmentedButton>
```

`SegmentedButtonColors` 的 12 个可空颜色槽对应 active/inactive、enabled/disabled 的
container/content/border 组合；未指定的槽解析默认 Token。默认禁用 active 背景保留
SecondaryContainer，content 使用 OnSurface × 0.38，border 使用 Outline × 0.12。
后者按上游 colors factory，不能直接用生成 Token 的 DisabledOutlineColor（OnSurface）。
不要在两个图标槽共享同一个 Control 实例；需要复用图标表示时使用数据和模板。

## 内部适配及确认范围

2026-10-04，维护者确认上述行/按钮 API、外部选择职责、12 槽颜色配置、原生属性覆盖，
以及组件内部 intrinsic 测量适配。此方案不增加通用布局或 Shape 框架。

- Avalonia 没有 Compose 的多内容 intrinsic measure policy。内部 ItemsPanel 与 ContentLayout
  重放必要测量：高度由 label 决定，icon 受该高度约束，宽度始终预留 IconSize + 8；
  label offset 使用惰性 `Animatable<int>`、FastSpatial spring 和整数截断转换。
  对数据模板只调用 `ApplyTemplate`，避免每次测量重建非复用模板、丢失应用状态。
- 上游内容使用物理 `place()`；内部槽位固定物理坐标，文字和图标继承调用方的 FlowDirection。
  Surface 的内部 chrome 使用 LTR 防止 Avalonia 再次镜像已经生成的物理圆角。
  上游 CornerBasedShape 按 min(width,height) 归一化每个竖向角对，组件在 Surface 输入端保留此算法。
- 绘制层级保留 selected × 5 + press + focus，hover 不增加层级；ItemsSource 模板包装时，
  层级放在实际 sibling ContentPresenter 上。
- 有 inactive icon 时消费 ComposeAnimation `Crossfade`；否则消费 `AnimatedVisibility`
  的 fade + scale（origin 0,1）及 `Exit.None`，保留空间 spring 的 overshoot。
  组件作用域在模板替换和 detach 时释放动画、帧请求和调用方图标的父级关系。

进入途中反向曾暴露 ComposeAnimation 的两个共享缺口：`Exit.None` 缺少 fade / scale 规格时
使用了 Snap，而上游仍使用默认 spring；缩放中心使用像素 Point 和 Snap，未保留上游比例坐标
spring。维护者于同日批准修正共享中断行为并打包接入，指定版本 `0.2.4-alpha`。复核
Transition 的 `ResetNoSnap` 后保留状态改变时重建通道的原规则，修正淡入/缩放的规格选择
（fallback stiffness 400，scale 阈值 0.002），按比例坐标运行默认 origin spring（阈值 0.01）。
未增加公共 API；组件没有复制或替代依赖的动画机制。
Crossfade 的任意反向段继续使用它自己的 Spec，不采用 AnimatedVisibility 的 fallback。
Surface 的反馈墨色跟随显式 content color，包括仅覆盖颜色配置中 content 槽的情况。

尚未实现、已确认排除在本次范围之外：任意路径 / 切角 Shape，以及外部 hoisted
InteractionSource。`BaseShape` / `CornerRadius` 当前只表达圆角矩形；此限制不代表上游能力已删除。

## 验证入口

[SegmentedButtonTests.cs](../../../../tests/Material3Expressive.Tests/SegmentedButtonTests.cs)
覆盖布局、外部选择、Automation、键盘、Token 更新、原生覆盖、ItemsSource、图标模板、
空间 overshoot、模板替换和 detach。Catalog 展示单选、多选和 inactive icon。

双端测试各自使用真实组件 API：
[SegmentedButtonCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/SegmentedButtonCases.cs) /
[SegmentedButtonCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/SegmentedButtonCases.kt)。
75 个用例涵盖 52 个静态组合与 23 个动态场景：选择/反向、Crossfade、按压/取消/禁用、
真实点击、键盘释放、主题、内容、形状、图标变更、部分 content 颜色覆盖的反馈，
以及单项、多项、窄/矮约束、RTL。
非文字 marker 隔离布局、颜色及图标动效；它们不能证明真实文字排版或屏幕阅读器的桌面体验。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~SegmentedButtonTests
./tests/VisualParity/parity.ps1 self-test --family segmented-button --require-match
```

双端 self-test 重新采集两端并检查重复性与额外被动截图的稳定性，不消费或发布参考缓存。
结果需按 [VisualParity 文档](../../../../tests/VisualParity/README.md) 分别阅读精确事实、raster 容差
和重复性；`withinTolerance` 不表示严格像素相等。实际执行结果和本地制品路径见任务交付。
