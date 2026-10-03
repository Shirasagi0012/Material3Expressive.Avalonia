# Slider / VerticalSlider / RangeSlider

依据 `docs/upstream-baseline.json`：AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`、
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
两端 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Slider.kt` 相同。
对应 `SliderImpl`、`RangeSliderImpl`、两个 State、键盘与 semantics helper、`SliderDefaults.Thumb/Track/CenteredTrack`。
默认值来自固定 `tokens/SliderTokens.kt`；鼠标阈值依据 Core foundation 的 pointer slop 和 Desktop 的密度配置。

## 使用

加载 `MaterialTheme.axaml` 后，三个控件的模板和 Token 来源自动接入。
XAML 命名空间为 `xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"`。

```xml
<m3:Slider Width="280" Minimum="0" Maximum="100" Steps="9"
           Value="{Binding Volume, Mode=TwoWay}" />
<m3:VerticalSlider Height="240" TopToBottom="False"
                   Value="{Binding Level, Mode=TwoWay}" />
<m3:RangeSlider Width="280" Minimum="0" Maximum="100"
                RangeStart="{Binding Lower, Mode=TwoWay}"
                RangeEnd="{Binding Upper, Mode=TwoWay}" />
```

`Steps` 是端点之间的离散值数量；0 为连续轨道。`ValueChangeFinished` 在点击释放、拖动结束/取消、
受支持键的 KeyUp 或产生改变的自动化 SetValue 后触发。垂直默认从上向下增加；RTL 只反转横向。
Home/End、方向键、PageUp/PageDown 保留上游规则；连续键盘步长是完整数值范围的 1%。

需要持有状态时，设置可选 `State`。此时数值、范围和 steps 由 State 拥有，控件的便捷属性不参与更新。

```csharp
using Material3Expressive.Components;
using MSlider = Material3Expressive.Components.Slider;

var state = new SliderState(value: 0.35f, steps: 4, minimum: 0, maximum: 1);
var slider = new MSlider { State = state, Width = 280 };
state.OnValueChangeFinished = () => Save(state.Value);
// 默认输入直接写入 State。提供回调后，调用者负责写回（与 Compose 相同）。
state.OnValueChange = value => { state.Value = value; Preview(value); };
state.ShouldAutoSnap = false; // 允许程序赋值不吸附；指针操作仍按轨道 tick 吸附

var rangeState = new RangeSliderState(0.2f, 0.8f, steps: 3);
var range = new RangeSlider { State = rangeState, Width = 280 };
```

State 构造器和 setter 的行为保持上游区别：single 构造值不吸附；range 构造值先约束和排序但不吸附。
setter 按源码顺序约束和吸附。程序更新 range 两端时，向上移动区间先设 End，向下移动先设 Start，
避免旧的另一端限制新值。范围和 steps 在 State 构造后固定；修改它们应创建新 State。
`rememberSaveable` 的跨重建保存由应用/ViewModel 持有数值承担。

## Thumb 和 Track

`Thumb` / `ThumbTemplate`、`Track` / `TrackTemplate` 可以替换内容；range 分别提供 Start/EndThumb 和模板。
模板的数据是当前实际 State。`ActualState` 可用于绑定默认轨道。`Interaction`、`StartInteraction`、
`EndInteraction` 是只读共享交互标记，可绑定到自定义 Thumb；反馈跟随实际输入和焦点。

```xml
<m3:Slider Value="0.5">
    <m3:Slider.ThumbTemplate>
        <DataTemplate>
            <m3:SliderThumb Width="12" Height="32"
                Interaction="{Binding Interaction, RelativeSource={RelativeSource AncestorType=m3:Slider}}" />
        </DataTemplate>
    </m3:Slider.ThumbTemplate>
    <m3:Slider.TrackTemplate>
        <DataTemplate>
            <m3:SliderTrack State="{Binding}" IsCentered="True"
                            EnableCornerShrinking="True" TrackCornerSize="4"
                            ThumbTrackGapSize="6" TrackInsideCornerSize="2" />
        </DataTemplate>
    </m3:Slider.TrackTemplate>
</m3:Slider>
```

垂直自定义 Thumb 需设置 `IsVertical="True"` 并交换尺寸轴。`SliderColors` 提供十个最终颜色；
默认禁用 Thumb 先与 Surface 合成，显式提供的 DisabledThumbColor 直接消费。
`SliderTrack` 还提供 TickSize、StopIndicatorSize、ShowStopIndicator、DrawTick、DrawStopIndicator。
绘制回调接收轨道局部 DIP 坐标和实际状态颜色。Centered 仅对 single State 有效。
普通 single Track 默认不缩小端点圆角；垂直/Centered/自定义圆角对应的上游重载启用缩小，需设置
`EnableCornerShrinking="True"`。range 的圆角 alignment line 与绘制圆角保持上游各自的计算规则。

## 框架适配与范围

- `SliderLayout` 用物理整数像素还原测量、离散圆角偏移、不等宽双 Thumb 校正及 minimumInteractiveComponentSize。
  小于 requiredSizeIn 的外部约束仍保留内部最小尺寸，居中溢出；默认不裁剪，与 Compose 一致。
  组件和绘制内容自行处理 RTL，绕过 Avalonia 自动镜像，避免重复反转。
- 指针捕获位于模板布局节点；在 tunnel 阶段消费按压，保留 Compose 不自动请求鼠标焦点的行为。
  键盘和程序聚焦仍有效。range 两端分别可聚焦，并各自提供 `IRangeValueProvider`；父控件为 Group。
  自动化边界按上游沿对应轴扩展 10 DIP，不增加视觉尺寸；应用可为 Thumb 提供本地化的 AutomationProperties.Name，
  例如 `<m3:SliderThumb AutomationProperties.Name="下限" />`；没有显式名称时使用英文默认值。
  替换 State 时通知自动化数值和边界变化。
- 沿用 float 运算、不同入口的 tie 规则、原始偏移累积与非交叉约束。默认 Thumb 的按压、拖动、焦点
  宽度/高度减半是即时状态变化，上游没有尺寸过渡动画。
- **尚未实现，维护者明确延期**：可选 `LocalRippleThemeConfiguration.Focus.InsetRing` 及其焦点 padding/轨道间距。
  本次范围为上游默认焦点模式，决定见 `docs/DECISIONS.md`。不提供近似焦点环或新共享配置。
- **已接受的差异**：single 拖动中禁用，当帧恢复 Thumb 尺寸；Compose 的子组合下一帧才恢复。
  四个 `slider.dynamic.disable.horizontal/vertical.0/3` 的 `disabled` 栅格差异保留。
  同时采集的 feedback、数值、回调与布局相同，释放及后续帧一致。决定见 `docs/DECISIONS.md`。
- **未经验证**：真实桌面、触屏/触控笔、原生屏幕阅读器与平台 numpad 映射。当前自动化证据来自 Windows Skia headless。

## 验证入口

`tests/Material3Expressive.Tests/SliderTests.cs` 覆盖状态、绑定、键盘、自动化、取消、生命周期、模板和 Token。
独立双端场景为 `tests/VisualParity/Material3Expressive.Parity/Cases/SliderCases.cs` 与
`tests/VisualParity/reference/src/main/kotlin/cases/SliderCases.kt`，suite 为 `slider`。
核心矩阵包括三种形态 × 连续/离散 × 启用/禁用 × LTR/RTL，并补充端点、主题、颜色、圆角、间距、
Centered、长短及自动尺寸、受限/扩展尺寸、负数范围、不等宽 Thumb、程序赋值、拖动、键盘和中断。
运行时修改 TrackCornerSize 同时更新绘制和离散 Thumb 的 alignment line。
结果按本次运行的 `summary.md`、用例 `overview.md` 与逐样本 YAML/PNG 读取；重复性与双端一致性分别评价。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~SliderTests
./tests/VisualParity/parity.ps1 list --family slider
./tests/VisualParity/parity.ps1 self-test --family slider
```

`self-test` 新采集双端并检查重复捕获及额外被动截图，不发布缓存参考；四处已接受差异仍会被严格匹配门禁拒绝。
