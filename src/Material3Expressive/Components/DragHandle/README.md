# VerticalDragHandle

库内竖向手柄，用于提示面板可横向调整。控件呈现正常、按下和拖动外观，宿主负责拖动识别、指针捕获及内容移动。

## 基础用法

完成[公共接入](../../../../docs/GETTING_STARTED.md)后，可在拖动区域中放置手柄。

```xml
<m3:VerticalDragHandle xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
                      x:Name="Handle" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
```

## 变体与属性

`Sizes`、`Colors`、`Shapes` 分别配置正常、按下和拖动三种状态，拖动优先于按下。它们是完整值对象，显式设置时需提供所有成员。

`InteractionSource` 可连接宿主共享的交互源。结束或取消事件应引用同一个开始对象。

```csharp
using Material3Expressive.Foundation.Interaction;

var source = new MaterialInteractionSource();
Handle.InteractionSource = source;
var drag = new MaterialInteractionEvent.DragStart();
source.Emit(drag);
// 宿主更新内容位置，结束时发送配对事件。
source.Emit(new MaterialInteractionEvent.DragStop(drag));
```

## 内容槽与定制

手柄通过三组配置定制外观，默认值来自 `Comp.DragHandle` Token。默认正常尺寸为 4×48 DIP，按下和拖动为 12×52 DIP。

```csharp
Handle.Sizes = new(new Avalonia.Size(4, 48), new Avalonia.Size(8, 50), new Avalonia.Size(16, 60));
Handle.Shapes = new(new Avalonia.CornerRadius(4), new Avalonia.CornerRadius(8), new Avalonia.CornerRadius(12));
```

`Foreground` 控制交互反馈颜色，`Colors` 控制手柄本体颜色。`MaterialInteractiveSize.Minimum` 设置外层最小布局留白。

## 使用限制与注意事项

宿主可用透明背景提供较大的拖动区域。最小布局留白本身仅影响布局，形状配置使用 `CornerRadius`。
本地按压改变手柄外观，外部源的 Press 可产生波纹反馈。

[Catalog 示例](../../../Material3Expressive.Catalog/DragHandleDemo.axaml)可用 `--drag-handles` 启动，包含拖动、取消与共享交互源。实现和验证范围见 [INTERNALS.md](INTERNALS.md)。
