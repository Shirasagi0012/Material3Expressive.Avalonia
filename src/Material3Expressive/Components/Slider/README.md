# Slider / RangeSlider

单值水平、垂直滑块使用 `Avalonia.Controls.Slider` 和 Material 主题；区间滑块仍使用 `RangeSlider`。
外观依据 `docs/upstream-baseline.json`：AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`、
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
两端 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Slider.kt` 相同。
默认外观来自 `SliderDefaults.Thumb/Track/CenteredTrack` 和 `tokens/SliderTokens.kt`；
RangeSlider 的状态、键盘、semantics 和 pointer slop 也按此基线移植。
原生单值交互依据项目依赖 Avalonia **12.1.2**，对应源码 tag commit `d3c867a9e2de379249b03dbeb3495bd7f076a81a`。

## 使用与迁移

加载 `MaterialTheme.axaml` 后，原生 Slider 的水平、垂直 Material 模板和 RangeSlider 模板自动接入。
XAML 命名空间为 `xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"`。

```xml
<Slider Width="280" Minimum="0" Maximum="100" TickFrequency="10"
        IsSnapToTickEnabled="True" SmallChange="10" LargeChange="10"
        Value="{Binding Volume, Mode=TwoWay}" />
<Slider Height="240" Orientation="Vertical" Minimum="0" Maximum="1"
        SmallChange="0.01" LargeChange="0.1" Value="{Binding Level, Mode=TwoWay}" />
<m3:RangeSlider Width="280" Minimum="0" Maximum="100"
                RangeStart="{Binding Lower, Mode=TwoWay}"
                RangeEnd="{Binding Upper, Mode=TwoWay}" />
```

单值使用原生 `double` 数值；主题保留原生默认 Minimum=0、Maximum=100、SmallChange=1、LargeChange=10。
原生垂直默认向上增大，向下增大设置 `IsDirectionReversed="True"`；横向支持该属性和原生 RTL 镜像。
`Ticks` 优先于 `TickFrequency`，允许不规则刻度；`IsSnapToTickEnabled` 控制原生输入吸附。
键盘步长由应用显式设置，不会由 Material 主题自动计算。

| 原单值 API | 新入口 / 行为 |
| --- | --- |
| `m3:Slider`、`m3:VerticalSlider` | 原生 `Slider`，垂直设置 `Orientation="Vertical"` |
| `SliderState`、`State`、`ActualState` | 移除；由 ViewModel 持有并双向绑定 `Value` / `Minimum` / `Maximum` |
| `Steps` | 等间隔用 `TickFrequency=(Maximum-Minimum)/(Steps+1)`，按需启用 `IsSnapToTickEnabled`；不规则刻度用 `Ticks` |
| `TopToBottom` | 垂直使用 `IsDirectionReversed`，原生默认值为 False |
| `ShouldAutoSnap` | 移除；程序赋值和自动化赋值遵循原生范围约束，不吸附 |
| `OnValueChange` / 自定义 `ValueChanged` | 原生 `ValueChanged`：实际值改变才触发，也包含程序和绑定更新；没有调用者写回协议 |
| `ValueChangeFinished` / `OnValueChangeFinished` | 移除；不补统一完成事件，也不以 `ValueChanged` 代替完成通知 |
| `Colors`、Centered 和轨道绘制选项 | `SliderAppearance` 附加属性 |
| `Thumb` / `ThumbTemplate`、`Track` / `TrackTemplate`、`ThumbWidth` / `ThumbHeight` | 使用原生 `ControlTheme` / `ControlTemplate` 定制模板部件 |
| 单值控件的只读 `Interaction` | 移除；默认 `SliderThumb` 外观观察原生焦点和 `:pressed`；自定义模板可使用原生伪类 |

2026-10-03，维护者确认“原生 Slider + Material 主题，使用原生交互”及上述 State/API 调整，
并明确“不补 ValueChangeFinished”。适用范围是单值 Slider；**RangeSlider / RangeSliderState 的 API 与交互保留**。

## 单值外观与模板

`SliderAppearance` 只提供绘制选项：Colors、IsCentered、TrackHeight、TrackCornerSize（NaN 为半轨道高度）、
TrackInsideCornerSize、ThumbTrackGapSize、EnableCornerShrinking、TickSize、StopIndicatorSize、ShowStopIndicator。
默认轨道高 16、内侧圆角 2、间距 6、tick/stop 直径 4。
普通水平轨道默认不缩小端点圆角；垂直默认启用。Centered 或自定义圆角对应的上游重载需要显式启用缩小。

```xml
<Slider Width="280" Minimum="-1" Maximum="1" Value="{Binding Balance}"
        m3:SliderAppearance.IsCentered="True"
        m3:SliderAppearance.EnableCornerShrinking="True"
        m3:SliderAppearance.TrackCornerSize="4" />
```

命名主题 `MaterialSliderTheme` 可通过 BasedOn 派生。保持原生 `PART_Track`（Track）、
`PART_Thumb`（Thumb）和 Increase/Decrease Button 模板契约，让原生控件继续处理输入。
例如仅替换 Thumb 内容：

```xml
<ControlTheme x:Key="WideThumbSlider" TargetType="Slider"
              BasedOn="{StaticResource MaterialSliderTheme}">
    <Style Selector="^ /template/ Thumb#PART_Thumb">
        <Setter Property="Template">
            <ControlTemplate>
                <m3:SliderThumb Width="12" Height="32" />
            </ControlTemplate>
        </Setter>
    </Style>
</ControlTheme>
```

垂直自定义 Thumb 应交换尺寸轴；`SliderThumb` 会读取原生 owner 的方向、焦点和按压状态。
`SliderColors` 提供十个最终颜色；默认禁用 Thumb 与 Surface 合成，显式 DisabledThumbColor 直接消费。
默认画笔部件为 `PART_TrackVisual`（SliderTrack），支持 DrawTick / DrawStopIndicator 绘制回调；
回调接收轨道局部 DIP 坐标和实际状态颜色。

`MaterialSliderLayout` / `MaterialSliderTrack` 是模板内部的布局适配：保留 Compose 的物理像素取整、
离散圆角偏移、Thumb 间距、48 DIP 最小交互空间和受限尺寸；不处理指针、键盘或写入 Value。
原生 Slider 继续使用完整 Track 边界和原生 Thumb / Button 处理输入，外观观察其结果。

## RangeSlider

`Steps` 是端点之间的离散值数量，0 为连续轨道；便捷属性内部持有 State。
设置可选 `State` 后，数值、范围和 steps 由 State 拥有，控件便捷属性不参与更新。
默认输入直接写入 State；提供回调后由调用者写回，与 Compose 相同。

```csharp
var state = new RangeSliderState(0.2f, 0.8f, steps: 3);
var range = new RangeSlider { State = state, Width = 280 };
state.OnValueChange = value =>
{
    state.ActiveRangeStart = value.Start;
    state.ActiveRangeEnd = value.EndInclusive;
};
state.OnValueChangeFinished = () => Save(state.ActiveRangeStart, state.ActiveRangeEnd);
```

构造值先约束和排序但不吸附，setter 按源码顺序约束和吸附。
程序移动区间时，向上先设 End，向下先设 Start，避免旧的另一端限制新值。
范围和 steps 在 State 构造后固定，改变时创建新 State。ViewModel 负责跨重建保存。
`ValueChangeFinished` 在点击释放、拖动结束/取消、受支持键 KeyUp 或产生改变的自动化赋值后触发。
`StartThumb` / `EndThumb` / `Track` 及其内容模板、`ActualState`、只读 Start/EndInteraction 均保留。

2026-10-03 维护者确认的 Range API、显式 State 的调用者写回职责及内容模板方案仍有效。
`SliderLayout` 保留不等宽双 Thumb 校正、minimumInteractiveComponentSize 和居中溢出；
RangeSlider 自行处理 RTL 并绕过自动镜像。两端分别可聚焦并提供 `IRangeValueProvider`，父控件为 Group；
自动化边界沿轴扩展 10 DIP，可通过 Thumb 的 AutomationProperties.Name 提供本地化名称。
指针捕获在模板布局节点的 tunnel 阶段处理，保留 Compose 不自动请求鼠标焦点的行为。

**已接受的 Range 行为改进**：按下时两端重合，首次有效水平拖动按数值方向选择 Thumb，之后固定。
LTR 向左拉开起点、向右拉开终点，RTL 反转；仅释放事件出现水平位移也同样处理。
没有拖动的点击、非重合手势、键盘、自动化保留原规则，拖动中合并后不会切换 Thumb。
选择转移同时转移反馈并保留非交叉约束、slop、取消和完成回调。
固定 Compose 在按下时选定 Thumb，重合时只能朝被选端允许的方向拉开；真实差异仍保留。
2026-10-03 维护者确认“就按你说的改”，范围仅为 RangeSlider 的指针手势。

## 已接受差异、范围与验证

单值已接受原生行为：按下轨道立即改变数值，原生拖动和捕获规则、鼠标自动聚焦、原生键盘/RTL/修饰键规则、
double 数值、范围约束、程序与自动化赋值不吸附、原生自动化边界和通知、无统一完成回调。
这些语义会与固定 Compose 不同，parity 使用真实 Compose 组件保留差异。
`changes` 的两端定义分别是原生实际值改变事件和 Compose 请求回调，因此明确不可比较；单值 `finished` 标为不支持。
默认 Thumb 沿运动轴缩窄由原生焦点/按压状态即时驱动，没有人为引入帧延迟。

**尚未实现，维护者明确延期**：可选 `LocalRippleThemeConfiguration.Focus.InsetRing` 及其 padding/轨道间距。
固定基线该模式影响 Thumb 测量和轨道间距，共享 Ripple 没有对应配置；2026-10-03 维护者确认
“本次只交付默认焦点模式，明确记录 InsetRing 未实现”。不据此声明所有 Compose 焦点配置已移植。
**未经验证**：真实桌面、触屏/触控笔、原生屏幕阅读器、平台 numpad 映射；自动化证据来自 Windows Skia headless。

`tests/Material3Expressive.Tests/SliderTests.cs` 覆盖原生默认数值、普通原生模板对照输入与事件顺序、绑定、
程序/自动化赋值、模板、颜色 Token、几何与不规则 Ticks，以及完整 Range 状态/绑定/自动化/重合手势回归。
双端场景为 `tests/VisualParity/Material3Expressive.Parity/Cases/SliderCases.cs` 和
`tests/VisualParity/reference/src/main/kotlin/cases/SliderCases.kt`，suite 为 `slider`。
矩阵保留水平/垂直/区间 × 连续/离散 × 启用/禁用 × LTR/RTL，以及边界、主题、颜色、尺寸、圆角、Centered、
自定义 Thumb、程序赋值、拖动、键盘和中断。运行时改变圆角或 Ticks 会重新排列离散 Thumb。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~SliderTests
./tests/VisualParity/parity.ps1 self-test --family slider
```

结果从运行的 summary.md、overview.md 和逐样本 YAML/PNG 读取；执行、双端一致性和重复性分别评价。
`self-test` 新采集双端并检查重复捕获及额外被动截图，不发布参考缓存。
已接受差异与不可比较字段仍会被一致性门禁如实报告。
