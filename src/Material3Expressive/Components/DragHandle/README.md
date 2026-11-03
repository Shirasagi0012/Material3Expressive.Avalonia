# VerticalDragHandle

指示横向拖动的竖向把手。控件显示正常、按下、拖动状态；宿主负责识别拖动、移动内容和指针捕获。
默认外观宽 4、高 48；按下和拖动为 12 × 52，外围保留最小交互布局空间。

```xml
<m3:VerticalDragHandle x:Name="Handle" />
```

将宿主的拖动交互传给同一个 source。结束事件必须引用对应的 start；取消时发 `DragCancel`。

```csharp
var source = new MaterialInteractionSource();
Handle.InteractionSource = source;
var drag = new MaterialInteractionEvent.DragStart();
source.Emit(drag); // 宿主开始拖动
// 更新宿主的布局或位置……
source.Emit(new MaterialInteractionEvent.DragStop(drag));
```

三个配置值保留上游同名数据关系，分别为正常、按下、拖动；拖动优先于按下。
配置是完整值对象，也支持 XAML 属性元素；未指定成员为其类型零值，不逐字段回退主题。

```csharp
Handle.Sizes = new(new Size(4, 48), new Size(8, 50), new Size(16, 60));
Handle.Colors = new(Colors.SlateBlue, Colors.DarkSlateBlue, Colors.MediumPurple);
Handle.Shapes = new(new CornerRadius(4), new CornerRadius(8), new CornerRadius(12));
```

默认值通过 `Comp.DragHandle` Token 引用系统颜色/形状，跟随主题更新。
`Foreground` 对应环境 content color，用于共享状态层和 ripple；容器颜色由 `Colors` 控制。
控件自己的按下不产生 `Press` 交互；外部 source 的 `Press` 仍能产生 ripple。
`MaterialInteractiveSize.Minimum` 只控制布局留白，不会自动扩大鼠标命中或提供拖动逻辑。
调用者需要更大的拖动区时，可像 Catalog 那样把它放入透明背景的宿主区域。

Catalog 的 **Drag handle demo**（命令行 `--drag-handles`）展示两个面板的真实拖动、取消和共享 source。
任意路径 Shape 尚未支持，当前形状 API 使用仓内既有 `CornerRadius`；实现和验证范围见 [INTERNALS](INTERNALS.md)。
