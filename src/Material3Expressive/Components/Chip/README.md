# Chip

`AssistChip`、`SuggestionChip`、`FilterChip`、`InputChip` 对应固定 Compose `Chip.kt` 的四个家族。
Assist / Suggestion / Filter 的 Elevated 入口合并为 `IsElevated`；Filter / Input 的形状重载通过
非空 `Shapes` 启用。依据为 `docs/upstream-baseline.json` 中 AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，桌面交互依据 Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。两端 Chip.kt 相同。
主要入口为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Chip.kt`
中的 Chip / SelectableChip、ChipContent、AnimatingChipContent、ChipArrangement 与 ChipElevation；
默认值来自同目录 `tokens/` 的四个家族 Token 和 ChipsTokens。

## 默认值与接入

控件继承 Avalonia Button，提供原生 `Content` / `ContentTemplate`、`Click` / `Command`、
`LeadingIcon` / `TrailingIcon` 及对应 Template；Input 额外提供 `Avatar` / `AvatarTemplate`。
Suggestion 不显示 trailing 槽。Avatar 优先于 leadingIcon，默认圆形裁剪，禁用时仅 avatar 包装层
应用 0.38 opacity。图标 / avatar 尺寸由内容决定，建议使用默认 Token 所对应的 18 / 24 DIP。

默认容器高度 32 DIP、交互最小高度 48 DIP、圆角 Small（8 DIP）、LabelLarge 排版。
`Shapes=null` 使用前后 8 DIP 间距；启用 Shapes 时随槽存在与否使用 4 / 8 DIP 的紧凑间距。
Input 的内容 padding 保留 avatar / leading / trailing 的组合规则。
布局按物理像素舍入，leading / trailing 先测量，标签消费剩余约束；selectable 默认最大宽度
1000 DIP，显式 Width / MinWidth 保留外层约束语义。RTL 通过 Avalonia 的子树反射实现。

默认形状没有按压变形。`Shapes="{m3:ChipShapes}"` 使用 Medium / Full / Small 的
未选中 / 选中 / 按压圆角，按 FastSpatial spring 变化。
Filter / Input 的槽变化使用 Transition 的 size / alpha 轨迹，退出保留最后非空内容，
反向中断保留运行状态；固定与形状重载使用各自上游 motion Token。
形状配置按解析后的三个圆角值比较，等值配置不重置动画；不同配置对应上游 key(shapes)
重建状态。容器尺寸改变时重新计算轮廓，模板替换时释放调用方的图标控件与旧动画。
Elevation 选择最后活跃的 press / focus / hover / drag；禁用直接到目标。
进入 120ms FastOutSlowIn，退出 150ms Bezier(0.4,0,0.6,1)，hover 退出为 120ms。

应用管理 `IsSelected`。点击、命令或 automation Toggle 仅请求动作，不自动改选中状态。
Filter / Input 的 automation 类型为 CheckBox，ToggleState 反映 IsSelected。
外部 selection 更新向 automation 客户端发送 ToggleState 变更。
Trailing 槽仍属于 Chip 的点击区域，移除动作由应用处理。

```xml
<!-- xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia" -->
<m3:FilterChip Content="Filter" IsSelected="{Binding IsSelected}"
               Command="{Binding SelectCommand}" Shapes="{m3:ChipShapes}"
               IsElevated="True">
  <m3:FilterChip.LeadingIcon>
    <m3:MaterialSymbol Symbol="Check" Width="18" Height="18" />
  </m3:FilterChip.LeadingIcon>
</m3:FilterChip>
```

```csharp
public partial class ChipViewModel : ObservableObject
{
    [ObservableProperty] public partial bool IsSelected { get; set; }
    [RelayCommand] private void Select() => IsSelected = !IsSelected;
}

var chip = new InputChip
{
    Content = "Contact",
    Shapes = new ChipShapes { SelectedShape = new CornerRadius(16) },
    Spacing = new ChipSpacing(4, 8),
    Colors = new ChipColors { SelectedContainerColor = Color.Parse("#E8DEF8") },
    Elevation = new ChipElevation(Hovered: 3),
};
chip.Click += (_, _) => chip.IsSelected = !chip.IsSelected;
```

`Colors` 的空槽跟随 Token，`Elevation=null` 使用当前变体默认值；全零 ChipElevation 关闭阴影。
`Spacing=null` 使用当前重载默认间距。原生 Background / Foreground / BorderBrush /
BorderThickness / Padding 的显式值与绑定优先于组件默认值。
Surface 的 ripple 墨色通过实时 contentColorFor 绑定选择，透明或非主题背景沿用 Chip 外层
内容环境，标签和图标各自拥有颜色 provider。
资源与模板已由 MaterialTheme 装配；应用仍需按项目 README 初始化 VisualAnimationScope。

## 已确认的适配与边界

2026-10-03 维护者批准四个 Button 派生控件、外部选中状态、IsElevated、内容槽及状态颜色 /
elevation 配置的 API；ChipShapes 本次支持圆角，ChipSpacing 提供前后间距。
任意路径 / 切角 Shape 和通用 Compose Arrangement **尚未实现**。
没有公开 hoisted MutableInteractionSource；输入与反馈消费现有共享控制器。
原生 Enter 在按下时激活，与 Compose 松开激活不同，沿用共享键盘职责的已确认适配。
默认 opacity 焦点反馈已接入；可配置 inset focus ring 尚未实现。

2026-10-04 维护者选择本次保留 ComposeAnimation 0.2.1-alpha 的 Tween / Bezier 数值精度。
它使用 double 运算，固定 Compose 使用 Float；例如 64ms elevation 约相差 0.0000006 DIP。
这是已接受的数值差异，原始事实比较继续报告 different，不舍入读数或放宽门禁。
共享描边与 Core Desktop 鼠标焦点的决定见 [共享决定](../../../../docs/DECISIONS.md)。
Core Desktop 的完整焦点事件影响 elevation；indication 单独过滤首次鼠标聚焦，已显示的
键盘焦点层保留到失焦，输入模式切换不合成焦点事件。

2026-10-04 维护者选择本次保留阴影坐标差异。透明背景与自定义 6 DIP elevation 下，
内部阴影 raster 与 Compose 不同；同一 Skiko 原生探针已复现 DIP 路径 + 4× CTM 与
物理像素路径 + 1× CTM 的不同结果。本次保留共享模块现有接入，继续报告比较差异。

## 验证与剩余问题

控件库测试覆盖默认尺寸、avatar 优先级 / opacity、外部选中、automation、实时内容色、
原生 paint 优先级、编译 XAML 配置、detach / reattach、模板替换、形状替换与完成后的尺寸更新、
elevation 顺序 / 禁用 snap 和宽度约束。
Catalog 展示四个家族、Elevated、形状、avatar 和禁用状态。

VisualParity 的独立入口为
`tests/VisualParity/Material3Expressive.Parity/Cases/ChipCases.cs` 与
`tests/VisualParity/reference/src/main/kotlin/cases/ChipCases.kt`，共 203 项。
覆盖变体 / 主题 / 选中 / 禁用、槽与 RTL、尺寸约束、自定义颜色 / padding / border /
elevation，以及 hover、按压、取消、禁用、中断、外部 selection、形状配置替换、
完成变形后的尺寸更新、主题和键盘。
使用真实组件和无文字的 marker，未验证文字 raster / 字体差异或真实桌面手动交互。

保留的数值精度、阴影坐标及原生 Enter 激活时机差异分别按上面的决定处理。
具体运行结果及未通过采样见交付时给出的 artifacts 路径，不能将这些用例宣称为一致。
