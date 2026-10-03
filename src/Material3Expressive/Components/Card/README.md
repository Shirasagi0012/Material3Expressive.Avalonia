# Card

依据 `docs/upstream-baseline.json` 的固定源码：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与 Compose Multiplatform Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Card.kt`。
两个版本的 Card.kt 相同；桌面 indication 的鼠标焦点过滤消费现有共享控制器，依据 Core。

`Card`、`ElevatedCard`、`OutlinedCard` 分别对应三种外观，每个控件以 `IsClickable`
区分上游普通 / onClick 重载，默认普通模式。颜色、圆角、描边、内容色传播、阴影高度、
交互顺序和 elevation 动画已接入；模板与 TokenSource 由 MaterialTheme 注册。

## 使用

```xml
<m3:Card xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:CardColumn Margin="16">
    <TextBlock Text="Title" m3:MaterialText.Style="{m3:Typography TitleMedium}" />
    <TextBlock Text="Supporting text" />
  </m3:CardColumn>
</m3:Card>

<m3:ElevatedCard IsClickable="True" Command="{Binding OpenCommand}">
  <m3:CardColumn Margin="16">
    <TextBlock Text="Open item" />
  </m3:CardColumn>
</m3:ElevatedCard>

<m3:OutlinedCard IsClickable="True" IsEnabled="False">
  <m3:OutlinedCard.Colors>
    <m3:CardColors ContainerColor="#E8DEF8" ContentColor="#1D192B" />
  </m3:OutlinedCard.Colors>
  <m3:OutlinedCard.Elevation>
    <m3:CardElevation Default="0" Hovered="2" Pressed="0" Disabled="0" />
  </m3:OutlinedCard.Elevation>
  <TextBlock Text="Unavailable" Margin="16" />
</m3:OutlinedCard>
```

```csharp
var card = new OutlinedCard
{
    IsClickable = true,
    Command = openCommand,
    Colors = new CardColors { ContainerColor = Color.Parse("#E8DEF8") },
    Elevation = new CardElevation { Hovered = 2 },
    Content = new CardColumn
    {
        Margin = new Thickness(16),
        Children = { new TextBlock { Text = "Open item" } },
    },
};
```

普通模式不激活自身、不提供 Invoke automation，默认不接受焦点；内容中的原生按钮仍可交互。
可点击模式继承 Button 的 Click / Command / CommandParameter 和自动化，默认最小交互分配为
48 DIP，但视觉 Surface 可以更小。普通模式下 Command.CanExecute 不参与启用判断；
`IsEnabled=false` 仍按 Avalonia 禁用整个内容树。上游普通重载没有 enabled 参数，因此普通模式
不选择 disabled 颜色和高度。

`CardColumn` 对应 Card 内默认的无权重 Column：从上到下、Start 对齐、零间距，子项按顺序
使用剩余可用高度，包括高度耗尽后的零约束。默认 Stretch 在这里按 Start 放置，显式
HorizontalAlignment 可指定对齐；RTL 消费 Avalonia 的布局方向。它不提供完整 ColumnScope 的
weight / alignBy DSL。也可把任意原生控件作为 Content，其布局语义由该内容控件决定。
卡片默认无 padding；使用内容 Margin 或卡片 Padding 显式设置。

`Colors=null` / `Elevation=null` 使用当前外观默认值，各 nullable 槽跟随 Token 和内容环境。
普通内容色由启用容器的 contentColorFor 选择，不匹配主题颜色时继承外层前景；禁用内容色
默认替换 alpha 为 0.38。Filled 的禁用容器与**默认** Filled 容器合成，Elevated 与 Surface
合成；Outlined 的禁用 outline 与 Elevated 容器合成，保留上游关系。
Outlined 默认 press / focus / hover 高度都跟随 Default，刻意不消费 raw hover elevation Token。
原生 Background / Foreground / BorderBrush / BorderThickness / CornerRadius 的显式值或绑定
优先于默认值；圆角 Shape 通过 CornerRadius 配置。

阴影只使用 shadowElevation，不以其冒充 tonalElevation。可点击高度选择最后一个活动交互，
禁用时 snap；目标替换先采样当帧再取消旧效果，按 CardElevation 的前一目标高度推断退出交互。
模板替换、模式切换和 detach 会释放动画；应用按项目 README 初始化 VisualAnimationScope。
普通重载的 `CardElevation.shadowElevation(interactionSource=null)` 使用无参数 remember，
所以同一普通卡片会保留初始 Default，即使后来替换 Elevation 或其默认 Token；此处按固定
基线保留，双端已验证。模式切换或重新挂载建立新的状态；仅替换 Avalonia 模板保留当前控件状态。

## 已确认的适配与边界

2026-10-04，维护者选择独立 Card 控件与显式 IsClickable，并批准复用原生 Button 键盘语义。
Space 在松开时激活；Enter 在按下时激活，与固定 Compose 松开激活不同。切换到普通模式会
取消自身按压和焦点，下一次可点击输入重新建立反馈。

同日维护者批准增加 CardColumn，修复原生 StackPanel 在受限高度下无限测量子项的差别。
直接使用 StackPanel 仍保留其原生测量语义；需要 Compose 默认 Column 布局时使用 CardColumn。

同日维护者批准本次保留 ComposeAnimation 0.2.2-alpha 的 double / Tween / Bezier 精度。
固定 Compose 使用 Float，默认悬浮约相差 10⁻⁷ DIP，自定义高度中断的实测最大差约
0.0000022 DIP。这些样本的截图在既定容差内，原始 motion 事实仍报告 different；不舍入
读数或放宽比较门禁。配置替换的 effect 取消与首帧时序已修复，此决定只涵盖数值精度。

同日维护者说明透明卡片背后的阴影已有已知问题，本次无需修复，准确报告即可。
`static.custom.elevated.colors.enabled / disabled` 的半透明容器可见共享阴影差异，保留实际
失败用例及截图。此决定不改变 Card 的默认或自定义 elevation。

同日维护者选择本次暂缺并明确记录以下共享能力及布局 DSL，不将其视为已完整移植。
当前没有公开可注入的 MutableInteractionSource；Drag 的高度分支已有目标选择逻辑，但外部
Drag 注入入口缺失，未作端到端验证。默认 opacity 焦点反馈已接入，环境可配置的 inset focus
ring 未实现。任意路径 / 切角 Shape 未实现，CornerRadius 支持四角圆角。

## 验证

控件库测试覆盖三种外观的普通 / 可点击尺寸、子控件输入、Command.CanExecute、动态 automation、
Space / Enter / 禁用输入、颜色传播与原生 paint 优先级、受限 Column 布局、交互高度顺序、
禁用 snap、编译 XAML、模式 / 模板替换及动画清理。Catalog 展示三种普通与可点击外观和禁用项。

独立双端入口：

- `tests/VisualParity/Material3Expressive.Parity/Cases/CardCases.cs`
- `tests/VisualParity/reference/src/main/kotlin/cases/CardCases.kt`

85 项覆盖三种外观、普通 / 可点击、禁用、亮暗主题、自动 / 小 / 空尺寸、宽高约束、RTL、
圆角 / 描边 / 颜色 / elevation 配置，以及 hover、按压、快速点击、取消、禁用 / 恢复、
键盘、嵌套子按钮的输入归属、交互顺序、主题 / 颜色 / elevation / 模式替换。两端使用真实组件及独立布局，
测量实际几何、Surface 高度、回调和截图；未验证文字 raster、完整 ColumnScope 或真实桌面手动交互。
嵌套输入场景显式给两端真实子 Button 设置相同 Padding；库中 Button 保留的 Expressive 默认
Padding 与固定 Compose 普通 Button 的默认值不同，不把这一已有差别归因于 Card。

具体比较、重复性和未通过采样见交付 artifacts，不能将原始 different 宣称为通过。
