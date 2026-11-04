# TimePicker / TimeInput / TimeScroll

这三个控件对应 Compose Material3 的时间内容。`TimePicker` 是钟面，`TimeInput` 是小时和分钟文本框，`TimeScroll` 是上下滚动的小时和分钟。
它们都精确到分钟，不包含秒。打开、关闭和确认由应用负责，对话框容器是 [TimePickerDialog](../TimePickerDialog/README.md)。
Catalog 的 “Picker dialogs” 页面用模式按钮在 `TimePicker` 和 `TimeInput` 之间切换，确认后才写回时间。

Avalonia 原生时间滚轮，包括秒和步长，使用 [原生日期与时间主题](../DateTimePicker/README.md)，不要把 `TimeScroll` 当成那个控件。

首次使用请完成[公共接入](../../../../docs/GETTING_STARTED.md)。

## 最小示例

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel Spacing="24">
    <m3:TimePicker Name="Dial" SelectedTime="{Binding Time, Mode=TwoWay}" Is24Hour="True" />
    <m3:TimeInput SelectedTime="{Binding #Dial.SelectedTime, Mode=TwoWay}"
                  Is24Hour="{Binding #Dial.Is24Hour}" />
    <m3:TimeScroll SelectedTime="{Binding #Dial.SelectedTime, Mode=TwoWay}"
                   Is24Hour="{Binding #Dial.Is24Hour}" />
  </StackPanel>
</UserControl>
```

`Time` 是 `TimeOnly`。绑定同一个属性时，三个控件同步有效时间。
某一侧还没形成合法小时或分钟时，该控件的 `IsInputValid` 为 false，不会把草稿写进 `SelectedTime`。
换一个控件实例后，未完成的输入不会自动恢复。

## 常用属性

- `Is24Hour`：24 小时不显示 AM/PM。默认跟随当前文化的短时间格式里有没有 AM/PM 标记。
- `Selection`：`Hour` 或 `Minute`。指针选择小时后会自动切到分钟；键盘确认小时保持当前字段。文本框在小时输完两位后也会切过去。
- `TouchExplorationEnabled`：钟面和 `TimeInput` 由宿主绑定触摸探索状态。启用后指针及辅助技术选择小时、输入小时数字都不自动切到分钟，用户仍可显式激活分钟字段。默认 false，不代表库已经探测系统辅助技术状态。
- `Layout`：`Vertical`、`Horizontal` 或 `Automatic`。水平布局只影响钟面，把数字和时间盘左右排列。`Automatic` 在宿主宽度大于高度时使用水平布局。
- `Variant`：`Standard` 或 `Vibrant`。`TimeScroll` 默认是 `Vibrant`。
- `Toggle` / `ToggleTemplate`：放在时间数字旁边的应用内容，例如切换钟面和输入的按钮。库不替应用切换控件。

12 小时模式的 AM/PM 是互斥选择：重复激活当前时段不会取消；切换时只改变小时的半天部分，保留分钟。选中时段加粗，按钮在按压和选中状态使用 12dp 圆角。

`TimeInput` 只有当前 `Selection` 对应的字段是编辑器；点击另一字段的数字会切换选择并聚焦编辑器，保留已有草稿与选区。
Standard 显示小时/分钟提示，Vibrant 在有效输入时不显示提示。无效输入显示对应 12/24 小时制或分钟错误，并以 polite live region 提供给辅助技术。
在完整有效小时的末尾继续键入数字会转入分钟；分钟开头的 Backspace 不切换字段。Enter 可确认有效小时并转入分钟。

Vibrant 输入及滚动模式可只提供 `ToggleTemplate`：AM/PM 和模式按钮组成侧栏；24 小时制保留居中的模式按钮。
例如由应用命令切换输入方式：

```xml
<m3:TimeInput Variant="Vibrant" SelectedTime="{Binding Time, Mode=TwoWay}" Toggle="{Binding}">
  <m3:TimeInput.ToggleTemplate>
    <DataTemplate>
      <m3:IconButton Symbol="Schedule" AutomationProperties.Name="切换到滚动输入"
                     Command="{Binding SwitchEntryMode}" />
    </DataTemplate>
  </m3:TimeInput.ToggleTemplate>
</m3:TimeInput>
```

模板的绑定上下文来自 `Toggle`；命令来自视图模型时，将 `Toggle="{Binding}"` 一并设置，或使用视图中的命名控件绑定。
不需要数据的模板可省略 `Toggle`。Catalog 的 “Date / time pickers” 用事件展示模板按钮在输入和滚动模式之间切换，二者共享有效时间。

12 小时模式下，小时文本接受 1 到 12，午夜写回 `TimeOnly` 的 0 点，中午写回 12 点。分钟只接受 0 到 59。
钟面 24 小时的外圈是 0 到 11，内圈是 12 到 23。拖动分钟可以落到任意一分钟；手柄连续跟随拖动，选中文字颜色只覆盖手柄经过的部分。点击按 5 分钟取整。
点击小时会先完成指针旋转，再等待 100ms 切到分钟；拖动小时在松手时切到分钟。两个刻度盘交叉淡入淡出。
捕获丢失不会自动确认或切换字段，移出视觉树会取消动画和延迟。

键盘先聚焦小时或分钟选择器，再按 Enter/Space 进入当前刻度。右/下移到下一刻度，左/上移到上一刻度；
24 小时制在外圈和内圈之间接续循环，方向键不会直接改变时间。Enter/Space 松开时确认当前刻度，
确认小时不会自动跳到分钟。Tab 离开钟面，Shift+Tab 回到当前字段；分钟之后进入 AM/PM，24 小时制则进入后续控件。
字段切换的指针动画未结束时，上游会拒绝刻度确认；动画结束后可以再次确认。

宿主已有触摸探索状态时可直接绑定：

```xml
<m3:TimePicker SelectedTime="{Binding Time, Mode=TwoWay}"
               TouchExplorationEnabled="{Binding IsTouchExplorationEnabled}" />
```

Catalog 的时间页面提供该状态开关，并展示绑定到同一时间的 Standard 与 Vibrant 钟面。

## 限制

钟面选择器使用实色画笔的颜色和透明度；修改画笔或相关颜色资源时，静止钟面也会刷新。
需要将整个控件作为一层淡出时，可在父容器启用 Avalonia 的完整透明度合成：

```xml
<Border xmlns="https://github.com/avaloniaui"
        xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
        RenderOptions.RequiresFullOpacityHandling="True">
  <m3:TimePicker Opacity="0.5" />
</Border>
```

默认逐次绘制的透明度不等价于整层合成，尤其影响手柄与文字的混合；非实色 selector 仍未支持。
Catalog 的钟面透明度滑块使用上述父容器配置。双端透明像素和字体仍存在差异，详见维护说明。

滚动列使用 Avalonia 的循环选择面板，不是 Compose `VerticalPager`。拖动和惯性不保证相同。
滚动数字在选中时从 DisplayMedium 过渡到 DisplayLargeEmphasized，颜色从 Outline 过渡到 OnSurface；切换途中可以反向。
可覆盖对应的 Typography、颜色和 FastSpatial 动效 Token。滚动项默认高度为 `200 / 3` DIP，可通过 `Comp.TimeScroll.ItemHeight` 覆盖，改变高度不会改变所选时间。
原生滚动选中提交与参考端的动画启动仍有时序差异。侧栏 48dp 是布局分配，现有输入层尚不把触摸命中扩展到 40dp 视觉外缘。
视觉用例包含真实时间数字；失配需要单独看，不表示外观已经被接受。维护说明见 [INTERNALS.md](INTERNALS.md)。
Catalog 的 “Date / time pickers” 页面同时放了这三种入口。确认对话框见 “Picker dialogs”。
