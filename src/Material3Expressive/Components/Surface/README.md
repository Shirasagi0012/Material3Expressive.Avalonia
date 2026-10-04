# Surface

`MaterialSurface` 是带 Material 背景、圆角、描边及高度效果的单子元素容器。
它适合承载自定义面板，也用于组件内部绘制；可点击内容优先使用 [Button](../Button/README.md)
或 [Card](../Card/README.md) 的相应入口。先完成[公共接入](../../../../docs/GETTING_STARTED.md)。
内部绘制和共享交互约束见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

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

## 内容和外观

后续片段沿用以上命名空间。自定义背景时也应显式选择合适的内容色；Surface 不会根据任意
`Background` 自动推导对比色。内容色通过 `TextElement.Foreground` 继承，子控件自己的值仍优先。

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

- `CornerRadius` 同时影响背景、内容裁剪、描边和阴影；支持非对称圆角
- `Padding` 消耗内容布局空间；`BorderThickness` 只在表面内绘制，不缩小内容或增加期望尺寸
- `ShadowElevation` 是本层的阴影高度，单位 DIP，默认 0；`ShadowColor` 为 null 时取主题 Shadow
- `TonalElevation` 改变表面色，并在嵌套 Surface 中累积；只有背景为当前主题精确 Surface 颜色的纯色画刷才着色，SurfaceContainer 或自定义颜色不变
- tonal elevation 与 shadow elevation 相互独立；增加其中一个不会自动增加另一个

若描边需要与内容留出距离，请显式加 Padding。阴影绘制在布局边界之外，父容器裁剪可能截断它；
需要时在外层安排空间。颜色、Token 与主题覆盖规则见[接入指南](../../../../docs/GETTING_STARTED.md#主题与定制)。

## 交互与使用边界

独立 Surface 不提供 `Click`、`Command`、选中状态或按钮 automation，也不会因为设置
`StateLayerBrush` / `PressedCornerRadius` 自动成为交互控件。这两个属性服务于组件模板中的
共享反馈接入；应用无需为普通面板设置它们。

当前形状入口为圆角矩形，没有任意 Shape API。真实阴影依赖项目固定的 Skia 后端，
更换渲染后端不等于保留效果；共享适配和验证范围见 [INTERNALS](INTERNALS.md#边界与验证)。
Catalog 的 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml) 包含嵌套 tonal elevation 示例。
