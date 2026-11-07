# Surface

`MaterialSurface` 是带 Material 背景、圆角、描边及高度效果的单子元素容器。
它适合承载自定义面板，也用于组件内部绘制。`ClickableSurface`、`SelectableSurface` 和
`ToggleableSurface` 提供点击、单选和切换入口，适合不需要 Button/Card 默认尺寸、颜色和排版的自定义内容。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
内部绘制和共享交互约束见 [INTERNALS.md](INTERNALS.md)。

## 基础用法

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:MaterialSurface Padding="24" CornerRadius="28" TonalElevation="3">
    <TextBlock Text="一个 Material 表面" />
  </m3:MaterialSurface>
</UserControl>
```

直接写入的控件是 `Child`。这里只支持一个 `Control`；多个元素先放入 Grid 或 StackPanel，
没有 `Content` / `ContentTemplate` 数据槽。默认背景是 Surface、继承的文字色是 OnSurface，
无阴影、无额外 Padding、圆角为 0。上例显式选择了 Padding、圆角和 tonal elevation。

## 变体与属性

- `CornerRadius` 同时影响背景、内容裁剪、描边和阴影；支持非对称圆角
- RTL 下描边与交互反馈跟随表面的同一轮廓，运行时改变 `FlowDirection` 也会同步；非对称圆角示例见 Catalog 的 **Inset focus ring / RTL outline**
- `Padding` 消耗内容布局空间；`BorderThickness` 只在表面内绘制，不缩小内容或增加期望尺寸
- `ShadowElevation` 是本层的阴影高度，单位 DIP，默认 0；`ShadowColor` 为 null 时取主题 Shadow
- `TonalElevation` 改变表面色，并在嵌套 Surface 中累积；只有背景为当前主题精确 Surface 颜色的纯色画刷才着色，SurfaceContainer 或自定义颜色不变
- tonal elevation 与 shadow elevation 相互独立；增加其中一个不会自动增加另一个

三个交互组件继承原生 Button 的 `Content` / `ContentTemplate`、`Click`、`Command`、`CommandParameter`
和 `IsEnabled`，默认无 Padding、无圆角、无高度效果。它们保留 48 DIP 最小交互布局留白，
可通过继承的 `MaterialInteractiveSize.Minimum` 调整；留白不扩大绘制表面。禁用不自动改变表面色或内容色。

`SelectableSurface.IsSelected` 和 `ToggleableSurface.IsChecked` 均由应用管理。
点击不自动写回；切换组件通过 `CheckedChangeRequested` 提供目标值。状态不附带默认颜色或形状变化，
可用 `:selected` / `:checked` 样式配置。Enter / Space 在松开时激活，失焦或禁用取消尚未完成的键盘操作。
同时按住两个激活键时，各自释放各发起一次操作；重复 KeyDown 不增加次数。换源也会取消未释放的键盘操作。

```xml
<m3:ClickableSurface CornerRadius="16" Padding="24" TonalElevation="3"
                     Command="{Binding OpenWorkspace}" Content="打开工作区" />
<m3:SelectableSurface IsSelected="{Binding IsSelected}" Click="SelectWorkspace"
                      CornerRadius="16" Padding="24" Content="工作区" />
<m3:ToggleableSurface IsChecked="{Binding IsPinned}" CheckedChangeRequested="PinWorkspace"
                      CornerRadius="16" Padding="24" Content="固定工作区" />
```

例如 `PinWorkspace` 将 `SurfaceCheckedChangeEventArgs.Value` 写回视图模型的 `IsPinned`。
完整的选择、切换和禁用示例见 Catalog 的 **Interactive surfaces**。

若描边需要与内容留出距离，请显式加 Padding。阴影绘制在布局边界之外，父容器裁剪可能截断它；
需要时在外层安排空间。颜色、Token 与主题覆盖规则见[接入指南](../../../../docs/GETTING_STARTED.md#主题与定制)。

## 内容槽与定制

后续片段沿用以上命名空间。自定义背景时也应显式选择合适的内容色；Surface 不会根据任意
`Background` 自动推导对比色。内容色通过 `TextElement.Foreground` 继承，子控件自己的值仍优先。

上述说明针对静态 `MaterialSurface`。三个交互组件的 `Foreground` 会根据 `Background` 匹配主题颜色角色，
非主题颜色继承父级内容色；显式 `Foreground` 优先。交互组件的 `InteractionSource` 可与其他反馈共享，
null 使用私有源。外部事件只驱动反馈，不点击、不更改选择；换源结束自身旧输入且不重放先前事件。

```xml
<m3:MaterialSurface Background="#EADDFF" TextElement.Foreground="#21005D"
                    CornerRadius="16" Padding="16"
                    BorderBrush="#6750A4" BorderThickness="1"
                    ShadowElevation="3">
  <StackPanel Spacing="8">
    <TextBlock Text="自定义容器" />
    <TextBlock Text="内容通过 Child 承载" />
  </StackPanel>
</m3:MaterialSurface>
```

## 使用限制与注意事项

静态 `MaterialSurface` 不提供 `Click`、`Command`、选中状态或按钮 automation，也不会因为设置
`StateLayerBrush` / `PressedCornerRadius` 自动成为交互控件。这两个属性服务于组件模板中的
共享反馈接入；应用无需为普通面板设置它们。

交互组件提供 Invoke，单选/切换分别提供 SelectionItem/Toggle automation pattern，但不默认指定
Button/RadioButton/CheckBox 角色；请给自定义内容设置有意义的 `AutomationProperties.Name`。
应用负责组内互斥，组件不自动寻找或修改相邻项。内容布局沿用 Avalonia 原生尺寸与对齐属性，
子控件显式 Width/Height 仍按原生约束处理。

用于交互模板时，可继承 `MaterialInteraction.FocusFeedback="InsetRing"` 启用内缩焦点环；
支持四角不等圆角并跟随当前形变，不增加 Padding 或期望尺寸。默认仍为 Opacity。
配置与组件接入边界见 [交互反馈](../../Foundation/Interaction/README.md)。

`Shape` 可设置 `CutCornerShape`，覆盖背景、内容裁剪、均匀描边、阴影、波纹和焦点环的外形；
静态与三种交互 Surface 都支持。四角按逻辑 Start/End 定义，随 RTL 交换；百分比以短边为基准：

```xml
<m3:ClickableSurface Width="160" Height="80" Padding="24" Content="切角表面">
  <m3:ClickableSurface.Shape>
    <m3:CutCornerShape TopStart="24" TopEnd="10%" BottomEnd="24" BottomStart="10%" />
  </m3:ClickableSurface.Shape>
</m3:ClickableSurface>
```

形状是可共享的 Avalonia 对象，四角可绑定并在运行时修改。清除 `Shape` 恢复 `CornerRadius`。
自定义轮廓可派生 `MaterialShape`，实现 `CreateOutline(Size, FlowDirection, double density)`，
返回新建的闭合 `PathGeometry`；坐标为物理左上角起算的 DIP，调用方在方法中处理方向。
支持直线、曲线、凹轮廓与带孔路径。形状参数使用 Avalonia 属性时会自动通知已挂载的消费方。
显式路径只支持均匀 `BorderThickness`；路径不改变内容布局，内容留白仍由 Padding 控制。
Catalog **Interactive surfaces / Cut corners** 展示切角 Surface、Card、RTL 与键盘焦点环。

未设置 `Shape` 时，普通圆角数值按 DIP 表达，超过可绘尺寸时按当前尺寸归一化。现有 Full Token 的 1000 值是
兼容约定，按当前短边的 50% 解析；它不能同时用于表达真正的 1000 DIP 圆角。
真实阴影依赖项目固定的 Skia 后端，
更换渲染后端不等于保留效果；共享适配和验证范围见 [INTERNALS](INTERNALS.md)。
Catalog 的 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml) 包含嵌套 tonal elevation 示例。
`SurfaceDemo` 同时提供透明低高度轮廓及 16/40 DIP 自定义阴影；例如 `ShadowElevation="40"`
仍使用 DIP，调用方不应自行乘设备缩放。静态高阴影与透明轮廓已在固定 Windows raster 参考端验证，
不扩大为所有 GPU 后端已验证。

内部机制、移植差异与验证范围见 [INTERNALS.md](INTERNALS.md)。
