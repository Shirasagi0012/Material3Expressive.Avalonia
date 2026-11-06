# ScrollField

循环滚动选择字段，适用于数量、编号或自定义选项。`ScrollFieldState` 保存当前选项、滚动目标和运行状态。

## 基础用法

完成公共主题和动画初始化后，给字段有限高度，并在代码或 ViewModel 中创建状态：

```xml
<m3:ScrollField xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
                x:Name="Amount" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                Width="120" Height="200" AutomationProperties.Name="数量，零到五十九" />
```

```csharp
Amount.State = new ScrollFieldState(itemCount: 60, index: 12);
```

## 变体与属性

默认以本地化的两位数字显示索引，选中项目有字号、字重和颜色过渡。上下方向键按当前目标连续翻页；点击相邻项目选择相应选项，触摸拖动、滚轮平滑滚动后吸附。

`State.SelectedOption` 可双向绑定；赋值立即取消已有滚动并定位。`TargetOption` 表示当前动画目标，`IsScrollInProgress` 表示拖动或动画正在执行。

```csharp
Amount.State.SelectedOption = 20;
if (!await Amount.State.AnimateScrollToOptionAsync(45, cancellationToken)) return;
await Amount.State.ScrollToOptionAsync(0);
```

索引限制在 `0..ItemCount-1`，项目数构造后不变。选项定位在当前循环内，不自动选择跨循环的最短路径。
新的同优先级操作、用户拖动、状态替换或卸载会取消原动画；两个异步入口返回 `Task<bool>`，完成返回 true，中断或卸载返回 false。
拖动和滚轮输入的优先级高于两个异步导航方法，此时异步导航返回 false；`SelectedOption` 赋值可取消当前滚动并定位。
赋值不会松开仍按住的手指，后续触摸位移仍可继续拖动。

## 内容槽与定制

`ItemTemplate` 接收 `ScrollFieldItem`，其中 `Index` 为选项索引，`IsSelected` 为当前选中状态。模板不必使用数字，可显示单位或业务内容。

```xml
<m3:ScrollField State="{Binding UnitState}" Height="200" Width="160"
                AutomationProperties.Name="单位">
  <m3:ScrollField.ItemTemplate>
    <DataTemplate DataType="m3:ScrollFieldItem" x:DataType="m3:ScrollFieldItem">
      <TextBlock Text="{Binding Index}" FontSize="28" />
    </DataTemplate>
  </m3:ScrollField.ItemTemplate>
</m3:ScrollField>
```

自定义内容应同步设置 `OptionDescription = index => ...`，使辅助技术读出的选项与画面一致。
`Background`、`CornerRadius`、`SelectedContentColor` 和 `UnselectedContentColor` 可覆盖默认外观。
`InteractionSource` 提供字段的成对焦点事件；项目点击反馈由各项目独立处理。

`ScrollFieldNumber` 是可独立复用的默认数字项目。`Index` 是显示数值，`IsSelected` 驱动排版与颜色动画；
它与选项索引可以不同，例如模板转换索引为年份。默认使用当前 Culture 的数字符号，不分组，至少两位。

```xml
<m3:ScrollFieldNumber Index="2026" IsSelected="True"
                      SelectedContentColor="Black" UnselectedContentColor="Gray" />
```

## 使用限制与注意事项

高度必须有限，200 DIP 可完整显示约三项；项目高度可通过 `Comp.ScrollField.ItemHeight` 资源调整。
同一个状态只能挂载到一个字段，动画方法需要字段已挂载并完成布局。未挂载时可以设置初始选项。
字段的辅助技术范围使用虚拟页面，选项名称通过 `OptionDescription` 提供；模板子节点不重复导出。

桌面鼠标拖拽不是翻页手势。滚轮接收 Avalonia 标准 Delta；原生设备元数据、触控板专用协议及嵌套滚动的当前范围见 [INTERNALS](INTERNALS.md)。
Catalog 入口为 **Scroll fields**，也可用 `--scroll-fields` 启动。
