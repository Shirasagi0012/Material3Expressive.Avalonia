# Switch

用 Avalonia 原生 `ToggleSwitch` 表达开关设置；Material 主题提供轨道、拇指、动画与反馈，
不增加 Switch 子类。先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
原生适配、拖动交接和验证边界见 [INTERNALS](INTERNALS.md)。

## 最小示例

在已加载 Material 主题的应用中：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel>
    <ToggleSwitch Content="接收通知" IsChecked="True" />
    <ToggleSwitch Content="显示状态" OnContent="开启" OffContent="关闭" />
    <ToggleSwitch Content="暂不可用" IsEnabled="False" />
  </StackPanel>
</UserControl>
```

隐式主题是 `MaterialToggleSwitchTheme`，也可显式引用或派生；后续片段使用同样的命名空间。

## 绑定、点击与拖动

```xml
<ToggleSwitch Content="接收通知"
              IsChecked="{Binding NotificationsEnabled, Mode=TwoWay}"
              Command="{Binding SaveNotificationCommand}" />
```

假设 `DataContext` 提供可写的状态属性和 `ICommand`；编译绑定需相应 `x:DataType`。
`IsChecked` 原生默认双向绑定，点击时先切换再触发 `Click` / `Command`。
事件方式可用 `Click="OnNotificationClick"`，处理器需在该视图 code-behind 中实现。
Enter 按下激活，`Space` 默认松开激活。

原生拇指拖动跨过 3 DIP 阈值后按半程选择状态；拖动改变状态不会触发 `Click`/`Command`，
需要保存每种输入造成的更改时，应观察或绑定 `IsChecked`，不能只依赖点击命令。
`IsChecked=null` 按未选中绘制，原生 `IsThreeState` 和三态自动化仍存在，但没有 Material 第三种外观。
`IsEnabled="False"` 禁止输入并显示禁用状态。

## 标签与拇指内容

`Content` / `ContentTemplate` 是轨道外的标签，默认 BodyLarge；
`OnContent` / `OffContent` 默认 null，显式设置后显示当前状态标签，也有对应 `Template`。
默认内容间距 8 DIP，`Padding` 控制外部内容留白，支持 RTL 和 access key。

`SwitchAppearance.ThumbContent` / `ThumbContentTemplate` 是拇指内部槽，模板数据来自 `ThumbContent`：

```xml
<ToggleSwitch Content="已连接" IsChecked="True">
  <m3:SwitchAppearance.ThumbContent>
    <m3:MaterialSymbol Symbol="Check" Width="16" Height="16" />
  </m3:SwitchAppearance.ThumbContent>
</ToggleSwitch>
```

`MaterialSymbol` 和文字继承当前 iconColor。自定义 Path 可绑定同一内容环境：

```xml
<ToggleSwitch>
  <m3:SwitchAppearance.ThumbContent>
    <Path Width="16" Height="16" Data="M 3,8 L 7,12 L 13,4"
          Stroke="{Binding (TextElement.Foreground), RelativeSource={RelativeSource Self}}"
          StrokeThickness="2" />
  </m3:SwitchAppearance.ThumbContent>
</ToggleSwitch>
```

默认轨道为 52×32 DIP，外部布局槽 52×48 DIP；拇指未选中且无有效 intrinsic 内容时为 16 DIP，
选中或有内容时为 24 DIP，按压为 28 DIP。自动尺寸的内容接受布局约束，显式 `Width`/`Height` 可溢出；
拇指背景不裁剪内容。原生 `KnobTransitions` 不控制 Material 拇指尺寸/位置动画。

## 颜色与使用边界

`Foreground` 控制轨道外内容和交互反馈；拇指内容使用独立 iconColor。
不指定 `SwitchAppearance.Colors` 时动态读取组件 Token，例如：

```xml
<ToggleSwitch IsChecked="True">
  <ToggleSwitch.Resources>
    <Color x:Key="{x:Static m3:MaterialTokens+Comp+Switch.CheckedTrackColor}">#008080</Color>
  </ToggleSwitch.Resources>
</ToggleSwitch>
```

完整配色用 C# 的 `SwitchAppearance.SetColors(toggle, colors)`，`SwitchColors` 在
`Material3Expressive.Components` 中提供 checked/unchecked、enabled/disabled 的 thumb/track/border/icon
十六个最终绘制色；传 null 恢复 Token。默认禁用色合成至 Surface，显式颜色不重复合成。
状态颜色即时切换，不做颜色渐变。

不提供完全等价的 Compose `onCheckedChange=null` 模式；关闭普通输入仍保留原生 `ToggleProvider`。
默认原生自动化 peer 的 ControlType 为 Button 并提供 TogglePattern，不另外映射为 Compose Role.Switch。
可配置 inset focus ring 和外部 MutableInteractionSource 尚未实现；文字、实际屏幕阅读器和真实桌面
双端体验未经完整验收。示例见 [Catalog MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml)。
