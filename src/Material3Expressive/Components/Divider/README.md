# Divider

为原生 `Avalonia.Controls.Separator` 提供隐式 Material ControlTheme，
移植固定 Compose `HorizontalDivider` / `VerticalDivider`。不提供已废弃的 `Divider` 函数对应入口。

## 使用

引入 `MaterialTheme.axaml` 后，普通 Separator 默认为水平分割线。
`MaterialDividerTheme` 可显式引用或派生。`MaterialDivider.Orientation` 选择方向，
`Thickness` / `Color` 分别对应上游 thickness / color，支持绑定和原生属性优先级。
默认厚度 1 DIP、颜色动态引用 OutlineVariant；也可覆盖 `MaterialTokens.Comp.Divider`。
颜色采用 Color 参数，不使用 Separator.Background / Foreground 作为描边。

```xml
<Separator />
<Separator Margin="16,0" m3:MaterialDivider.Thickness="2"
           m3:MaterialDivider.Color="#6750A4" />
<Grid Height="48" ColumnDefinitions="*,Auto,*">
  <Border Grid.Column="0" Background="#E8DEF8" />
  <Separator Grid.Column="1" m3:MaterialDivider.Orientation="Vertical" Margin="16,0" />
  <Border Grid.Column="2" Background="#D0BCFF" />
</Grid>
```

```csharp
var divider = new Avalonia.Controls.Separator();
MaterialDivider.SetOrientation(divider, Avalonia.Layout.Orientation.Vertical);
MaterialDivider.SetThickness(divider, 2);
MaterialDivider.SetColor(divider, Avalonia.Media.Color.Parse("#6750A4"));
divider.ClearValue(MaterialDivider.ColorProperty); // 恢复动态 Token 默认色
```

水平线沿有界宽度填满，竖直线沿有界高度填满；在该方向无界时不填充，
应由父布局或显式 Width / Height 提供长度。Margin 对应外部留白，不增加内置 inset。
使用原生 Width / Height / MinWidth / MinHeight 指定外部布局约束；显式增加横截面尺寸
不会改变描边厚度或使线条在横截面居中，线的中心仍为 thickness / 2。
禁用不改变颜色，组件不接入交互反馈或动画。

## 源码与适配

基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)。AndroidX 与 Core 固定 commit 的
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Divider.kt` 相同，
`tokens/DividerTokens.kt` 为 v0_117。布局进一步对照 Core foundation-layout `Size.kt`，
绘制对照 `CanvasDrawScope.drawLine`。

维护者在本次任务提出 Separator 主题与方向附加属性方案；本实现沿用这一接入方式。
其他两个附加参数直接映射上游参数，不新增公共控件或通用布局层。
XAML 承担主题与绑定，内部 DividerPresenter 承担 Canvas 布局及绘制。
布局厚度按 Float density 和 roundToPx 取整、再服从约束；描边使用原始 Float thickness.toPx，
端帽为 Butt，保持上游从 (0, thickness / 2) 或 (thickness / 2, 0) 开始绘制的结构。
模板不裁剪，实际裁剪仍由调用方/祖先控制。需要 Avalonia.Skia。

`Thickness=0` 保留新 API 的零布局厚度和 Skia hairline，**不会**像废弃的 Divider
那样将布局厚度换成 1 / density。普通 Avalonia DrawingContext.DrawLine 会过滤零宽 Pen，
因此通过 Skia lease 绘制；绘制操作的可见边界包含画布外描边，保证零厚度或受限布局可渲染。
负厚度沿用上游的零布局及 Skia Paint 行为，不自行修正为默认厚度。

Separator 仍是原生控件；默认不可聚焦、不可命中，没有外部状态回调或交互语义。
这里的方向选择是两个 Compose API 在同一原生模板上的表达，不改变默认线条算法。

## 验证范围

- [DividerCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/DividerCases.cs)
  与 [DividerCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/DividerCases.kt)
  独立构造场景，参考端调用真实固定组件 API。
- 静态覆盖两个方向、亮暗主题、默认/零/负/小数/粗厚度、颜色与 alpha、
  显式长度和横截面、零尺寸、有限与无界约束、外部 inset、RTL。
- 动态覆盖方向、厚度、颜色覆盖/清除、主题及父布局尺寸变化；上游没有过渡动画。
  几何读取真实组件布局，截图检查实际线条，不将配置参数冒充绘制读数。
  idle-layout 在不送帧/不绘制的情况下检查最终布局；self-test 在此额外加入被动截图。
- [DividerTests.cs](../../../../tests/Material3Expressive.Tests/DividerTests.cs)
  检查实际模板、默认尺寸、参数变化的布局失效、Token/绑定优先级、受限尺寸和编译 XAML 接入。

视觉参考固定为 Windows x64、density=4；其他 DPI、平台及真实 OS 无障碍客户端未作双端验收。
本组件不新增已接受的视觉或算法偏离。
