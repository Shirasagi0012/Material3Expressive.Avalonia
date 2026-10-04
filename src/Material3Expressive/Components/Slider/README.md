# Slider / RangeSlider

单值水平/垂直滑块使用 Avalonia 原生 `Slider` 配合 Material 主题；双端区间使用库的 `m3:RangeSlider`。
两者的状态和事件规则不同，接入前先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
维护依据与迁移决定见 [INTERNALS](INTERNALS.md)。

## 最小示例

在已加载 Material 主题的应用中：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel Spacing="16">
    <Slider Width="280" Minimum="0" Maximum="100" Value="35" />
    <Slider Height="160" Orientation="Vertical" Minimum="0" Maximum="1"
            SmallChange="0.01" LargeChange="0.1" Value="0.5" />
    <m3:RangeSlider Width="280" Minimum="0" Maximum="100"
                    RangeStart="20" RangeEnd="80" Steps="9" />
  </StackPanel>
</UserControl>
```

后续片段使用相同命名空间。单值命名主题为 `MaterialSliderTheme`；主题保留原生默认
`Minimum=0`、`Maximum=100`、`SmallChange=1`、`LargeChange=10`，使用 double。
`RangeSlider` 默认范围 0..1，值为 float；当前区间控件为水平方向。

## 单值绑定、刻度与事件

```xml
<Slider Minimum="0" Maximum="100" TickFrequency="10"
        IsSnapToTickEnabled="True" SmallChange="10" LargeChange="10"
        Value="{Binding Volume, Mode=TwoWay}" />
```

假设 `DataContext` 提供可写 `Volume`；编译绑定需对应 `x:DataType`。
`Ticks` 优先于 `TickFrequency`，可表达不规则刻度；`IsSnapToTickEnabled` 控制输入吸附，
键盘步长由 `SmallChange`/`LargeChange` 显式设定，不由 Material 自动推算。
程序和自动化赋值按原生范围约束，不做刻度吸附。
垂直默认向上增大，向下增大用 `IsDirectionReversed="True"`；水平支持反向和原生 RTL 镜像。

原生 `ValueChanged` 只在实际值改变时发生，也包含程序/绑定更新；它不是统一的交互完成通知。
事件片段 `<Slider ValueChanged="OnValueChanged" />` 需要视图 code-behind 实现处理器。
单值没有 Material 的 `OnValueChange` 写回协议，也不提供 `ValueChangeFinished`。
禁用使用 `IsEnabled="False"`；原生指针、键盘、焦点和自动化继续有效。

## 单值外观与模板定制

`SliderAppearance` 附加属性只改变绘制，例如中心轨道：

```xml
<Slider Minimum="-1" Maximum="1" Value="0.25"
        m3:SliderAppearance.IsCentered="True"
        m3:SliderAppearance.EnableCornerShrinking="True"
        m3:SliderAppearance.TrackCornerSize="4" />
```

常用入口包括 `TrackHeight`（默认 16）、`ThumbTrackGapSize`（6）、`TrackInsideCornerSize`（2），
`TickSize` / `StopIndicatorSize`（默认直径 4）和 `ShowStopIndicator`。
`TrackCornerSize` 默认 NaN 表示半轨道高。普通水平默认不缩小端点圆角，垂直默认启用；
Centered 或自定义圆角需要这种行为时显式启用 `EnableCornerShrinking`。

`SliderAppearance.Colors` 接收 `SliderColors` 的十个最终颜色；默认禁用 Thumb 合成 Surface，
显式 DisabledThumbColor 直接使用。通过组件 Token 覆盖个别颜色可继续跟随主题，见
[公共 Token 接入](../../../../docs/GETTING_STARTED.md)。

更换单值 Thumb 或 `Track` 应派生原生主题，保留 `PART_Track`（`Track`）、`PART_Thumb`（Thumb）和
Increase/Decrease Button 的原生模板契约。以下资源片段放在视图 Resources 中，再将 `Slider.Theme`
设为 `{StaticResource WideThumbSlider}`：

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

垂直 Thumb 需交换尺寸轴，`SliderThumb` 会读取 owner 方向和反馈。
默认画笔部件 `SliderTrack` 的 `DrawTick` / `DrawStopIndicator` 回调接收轨道局部 DIP 坐标及实际状态颜色。
替换完整模板需要自行保留这些视觉与交互连接。

## 区间状态与完成通知

```xml
<m3:RangeSlider Minimum="0" Maximum="100" Steps="9"
                RangeStart="{Binding Lower, Mode=TwoWay}"
                RangeEnd="{Binding Upper, Mode=TwoWay}" />
```

这里假设 `DataContext` 提供 `Lower`/`Upper`；`Steps` 是端点之间的离散值数量，0 为连续。
便捷属性由内部 `State` 管理。若设置 `State`，范围、steps 和两端值由 `State` 拥有，
控件 `Minimum`/`Maximum`/`RangeStart`/`RangeEnd` 等便捷属性不参与更新。
默认输入直接写 `State`；设置 `OnValueChange` 后由调用方写回：

```csharp
using Material3Expressive.Components;

var state = new RangeSliderState(0.2f, 0.8f, steps: 3);
var range = new RangeSlider { State = state, Width = 280 };
state.OnValueChange = value =>
{
    state.ActiveRangeStart = value.Start;
    state.ActiveRangeEnd = value.EndInclusive;
};
```

`State` 构造值先约束和排序但不吸附，setter 按顺序约束、吸附。
程序移动整个区间时向上先设置 `ActiveRangeEnd`，向下先设置 `ActiveRangeStart`，避免旧的另一端限制新值。
范围和 steps 构造后固定，需要更改时创建新 `State`，并由 ViewModel 保存状态。

`ValueChangeFinished` / `State.OnValueChangeFinished` 在点击释放、拖动结束/取消、受支持键 KeyUp 或
产生改变的自动化赋值后触发。`StartThumb`、`EndThumb`、`Track` 和各自 `Template` 提供内容定制，
模板未给显式内容时以 `ActualState` 为数据；`StartInteraction`/`EndInteraction` 是只读反馈。
两端独立聚焦并提供 RangeValue 自动化，可在自定义 Thumb 上设置 AutomationProperties.Name。

两端在按下时重合，会按首次有效水平拖动方向选择端点：LTR 左拉起点、右拉终点，RTL 相反；
此次手势确定后不再换端。普通点击、非重合手势、键盘和自动化仍沿原规则。

## 迁移与边界

- 旧 `m3:Slider` / `m3:VerticalSlider` 改为原生 Slider / `Orientation`；单值 SliderState、`State`、`ActualState` 已移除，状态交给 ViewModel
- 旧单值 `Steps` 可换为 `TickFrequency=(Maximum-Minimum)/(Steps+1)`，按需打开吸附；TopToBottom 改为 `IsDirectionReversed`
- 单值 ShouldAutoSnap、只读 Interaction 和统一完成回调已移除；Thumb/`Track` 内容槽改为原生主题定制。`RangeSlider` 已有的 `State`、完成回调、双端反馈和内容槽不随此迁移删除
- 可配置 Focus.InsetRing 及其轨道间距尚未实现；真实桌面、触屏/笔、系统屏幕阅读器及平台 numpad 映射未完整验证

水平、垂直、离散、中心与区间示例见 [Catalog MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)。
