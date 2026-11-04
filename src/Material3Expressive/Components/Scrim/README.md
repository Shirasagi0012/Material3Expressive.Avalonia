# Scrim

`Scrim` 在其布局区域绘制遮罩，可用于自定义浮层的背景。它是独立控件，不创建弹层或焦点约束。

## 基础用法

```xml
<Grid xmlns="https://github.com/avaloniaui"
      xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia" Height="240">
  <TextBlock Text="背景内容" />
  <m3:Scrim AutomationProperties.Name="关闭预览" Click="OnClosePreview" />
</Grid>
```

使用前引入 `MaterialTheme`，接入方式见[公共接入](../../../../docs/GETTING_STARTED.md)。

## 变体与属性

默认使用主题 Scrim 色和 32% 颜色透明度。`Color` 覆盖颜色；`Alpha` 是额外的绘制透明度，
绘制时限制在 0–1，不改写绑定值。透明遮罩和 `Alpha=0` 仍可点击。
`Color=null` 省略绘制、自然尺寸及点击语义；`IsDismissEnabled=False` 保留绘制但不接收点击。
`Click` 由指针点击或无障碍 Invoke 发出；控件不获取键盘焦点，也不响应 Enter/Escape。

## 内容槽与定制

没有内容槽。将需要覆盖的内容与 Scrim 放在同一个 Grid，浮层正文放在 Scrim 后面。
装饰遮罩使用 `IsDismissEnabled=False`，使背景仍可操作。
可通过 `MaterialTokens.Comp.Scrim.ContainerColor` 改变默认色；清除本地 Color 恢复主题值。

## 使用限制与注意事项

有界方向填满可用空间，无界方向不自行选择长度。独立 Scrim 不阻止背景键盘输入、不提供焦点陷阱，
需要模态面板时使用 `ModalBottomSheet` 等已有模态组件。点击在释放时通知；指针移出区域后再移回不会触发。
默认没有波纹或动画，调用方可用既有动画能力驱动 Alpha。
Catalog 的 `--modal-bottom-sheet` 包含独立遮罩预览。维护与验证范围见 [INTERNALS.md](INTERNALS.md)。
