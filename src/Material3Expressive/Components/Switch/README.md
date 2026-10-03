# Switch

为原生 `Avalonia.Controls.ToggleSwitch` 提供固定 Compose Material3 Switch 的 Material 模板，
不增加 Switch 子类。`MaterialTheme.axaml` 引入隐式主题；`MaterialToggleSwitchTheme` 可显式引用或派生。

## 使用

`IsChecked`、双向绑定、Click/Command、Content/ContentTemplate、键盘和 Toggle 自动化由原生控件负责。
`SwitchAppearance` 仅增加 `ThumbContent`、`ThumbContentTemplate` 和 `Colors` 三个附加属性。

```xml
<ToggleSwitch Content="通知" IsChecked="{Binding Enabled}">
  <m3:SwitchAppearance.ThumbContent>
    <m3:MaterialSymbol Symbol="Check" Width="16" Height="16" />
  </m3:SwitchAppearance.ThumbContent>
</ToggleSwitch>
<ToggleSwitch Content="连接" IsChecked="True" IsEnabled="False" />
<ToggleSwitch OnContent="开启" OffContent="关闭" />
```

`m3` 命名空间为 `https://github.com/Shirasagi0012/Material3Expressive.Avalonia`。
MaterialSymbol 和文字继承拇指的实际 iconColor；任意 Path 可这样消费同一内容环境：

```xml
<ToggleSwitch>
  <m3:SwitchAppearance.ThumbContent>
    <Path Width="16" Height="16" Data="M 3,8 L 7,12 L 13,4"
          Stroke="{Binding (TextElement.Foreground), RelativeSource={RelativeSource Self}}"
          StrokeThickness="2" />
  </m3:SwitchAppearance.ThumbContent>
</ToggleSwitch>
```

```csharp
using Avalonia.Controls;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Tokens;

var toggle = new ToggleSwitch { IsChecked = true, Content = "通知" };
SwitchAppearance.SetThumbContent(toggle, new MaterialSymbol
{
    Symbol = MaterialSymbols.MaterialSymbolCodepoint.Check, Width = 16, Height = 16
});
// Colors 为 null 时动态消费 Token，可局部覆盖单个颜色槽。
window.Resources[MaterialTokens.Comp.Switch.CheckedTrackColor] = Avalonia.Media.Colors.Teal;
```

`ThumbContentTemplate` 使用 Avalonia IDataTemplate，模板数据来自 ThumbContent。
Content 保留为轨道外的原生标签；OnContent/OffContent 默认 null，显式设置后显示对应状态标签。
默认标签间距为 8 DIP，Padding 控制外部内容留白，支持 RTL 和 access key。
Compose Switch 自身没有这些标签槽；通常由上层 Row 组合。

## 颜色、尺寸与动画

`SwitchColors` 保留上游 16 个最终绘制颜色槽，分别为 checked/unchecked、enabled/disabled 的
thumb、track、border、icon。`SwitchAppearance.Colors` 为 null 时使用动态组件 Token，
默认禁用颜色按上游关系预合成到当前 Surface；显式 SwitchColors 使用其最终颜色，不再合成。
状态切换直接选择颜色，不增加颜色动画。Foreground 控制外部内容和 ripple/state layer 的环境色，
拇指内容使用独立 iconColor。

轨道 52×32 DIP、描边 2 DIP，默认交互槽 52×48 DIP。
未选中且无有效 intrinsic 内容时拇指为 16 DIP，选中或有内容为 24 DIP，按压为 28 DIP。
按固定 ThumbNode 保留尺寸/偏移目标、物理像素截断、首次测量的惰性 Animatable，
FastSpatial spring、按压 SnapSpec 与中断时的速度延续。
动画通过现有 ComposeAnimation NuGet 与 VisualAnimationScope 接入和清理。
Native KnobTransitions 不控制 Material 的尺寸/偏移动画；原生 Canvas.Left 用于实际拖动。

已经历状态动画后，ThumbNode 在按压测量中先读取当前尺寸，再启动 snap；Compose 的首个 ripple draw
捕获该测量尺寸。Avalonia 可能在首次 render 前完成后续布局，因此 Switch 在该测量中调用共享控制器的
内部 `InitializeRippleDrawGeometry` 锁定首次几何。波纹播放和绘制仍归共享层，公开 API 不变；
2026-10-03，维护者确认“增加内部衔接方法，修复该波纹差异（推荐）”。
共享决定与适用范围见 [DECISIONS](../../../../docs/DECISIONS.md#ripple-首次绘制几何与尺寸动画衔接)。

轨道绘制顺序为背景、拇指内容及反馈、描边。拇指背景不裁剪内容，ripple 半径固定 20 DIP 且不裁剪。
内容接受 Avalonia 布局约束：自动大小的内容可缩小；显式 Width/Height 可像 Compose requiredSize 一样溢出。
不能将 Avalonia 显式大小等同于 Compose preferred size。

## 原生适配与确认范围

2026-10-03，维护者先确认“采用继承方案，保留原生拖动并接受额外行为”，
随后提出直接使用 ToggleSwitch、模板和附加属性，并确认“可以。继续”。
最终方案沿用原生控件，不新增子类，保留上述已确认的原生行为：

- IsChecked 由控件自动写回，Click/Command 时已更新；Compose 通过回调要求外部写回。
- Space 在 KeyUp 激活，Enter 在 KeyDown 激活；固定 Compose Enter 在 KeyUp 激活。
- 保留原生拇指拖动、3 DIP 阈值和半程状态选择；固定 Compose 尚未实现 swipe。
  原生拖动改变状态时不触发 Click/Command，应用可观察 IsChecked。
- 原生 nullable IsChecked / IsThreeState 仍可使用；null 按 unchecked 绘制，自动化保留原生三态。
  Compose Switch 仅接受 Boolean，这不是新增的 Material 三态外观。
- 固定 Avalonia 12.1.2 的原生 peer 报告 ControlType.Button，并提供 TogglePattern；
  保留这一原生自动化身份，没有另行映射 Compose Role.Switch。

2026-10-03，维护者确认“本次保留原生范围，记录 null 回调模式未实现（推荐）”。
因此不提供 Compose `onCheckedChange=null` 的完全等价模式：原生 ToggleSwitch 没有同时移除
输入、48 DIP 最小交互槽与 ToggleProvider 的开关。IsEnabled=false 仍完整支持。
关闭 IsHitTestVisible、Focusable 和 MaterialInteraction.IsInteractive 也不移除原生 ToggleProvider，
不能宣称等价于 null 回调模式。

同日维护者确认本次采用默认 ripple/focus 配置，将可配置 `Focus.InsetRing` 与外部
`MutableInteractionSource` 的对象映射记录为未实现。默认 Tab 焦点状态层和键盘反馈仍实现。

## 固定基线与验证入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)。
AndroidX 与 Compose Core 的 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Switch.kt`
在固定 commit 的 blob 相同；尺寸/色彩对应 SwitchTokens v0_210，桌面 ripple 进一步核对
Core 的 `material3-ripple/{CommonRipple,RippleAnimation}.kt`。

独立双端入口：

- [SwitchCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/SwitchCases.cs)
- [SwitchCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/SwitchCases.kt)
- [SwitchTests.cs](../../../../tests/Material3Expressive.Tests/SwitchTests.cs)

63 个 visual parity 用例覆盖开关、图标/空内容、亮暗主题、启用/禁用、全部自定义颜色槽、RTL、
受限尺寸与内容的 preferred/required 大小；动态覆盖切换/反向/标准 motion、主题、禁用、
按压/快速释放/取消/按压中禁用、动画后按压、hover、Space/Enter 和原生拖动。
参考端执行固定组件，读取实际 ThumbNode 布局；不复制拇指算法作为期望值。
已接受的 Enter 与三个 drag 用例仍精确比较并保留不同结果。

2026-10-03 本轮 self-test：63/63 执行无错误，双端重复与额外被动截图均稳定；
39 equal、20 withinTolerance、4 different（上述已接受用例）。全部 33 个静态用例 equal。
Compose 与 Avalonia 均为本轮真实执行；另新采集并发布全部 63 个 Compose 缓存参考。
控件库 399 项、Parity 框架 123 项测试通过，Release 整体构建通过（现有三个模块警告保留）。
共享层回归的 CheckBox press/quick、NavigationRail/NavigationBar press 完全相等。
两个 Button 和一个 IconButton press 用例仍有既有差异；41 个采样 YAML 与 36 张 Avalonia 图像
均与修改前的冻结产物逐文件相同，不扩大本次 Switch 移植范围修复它们。

headless 测试检查实际模板、绑定/事件/命令顺序、键盘、自动化、LTR/RTL 拖动、内容与颜色、
idle 后动画和模板替换/卸载/重新挂载清理；共享 ripple 另有首次绘制尺寸的回归。
视觉用例不包含文字。实际 OS 无障碍客户端、真实桌面手工交互与文字视觉未作双端验收。
