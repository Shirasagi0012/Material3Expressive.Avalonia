# SwipeToDismissBox

横向拖动前景以露出固定背景。组件只报告消除完成，不自动从集合移除内容。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)，每个窗口需有动画作用域。
实现及验证边界见 [INTERNALS](INTERNALS.md)。

## 基础用法

```xml
<m3:SwipeToDismissBox xmlns="https://github.com/avaloniaui"
                     xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
                     Width="320" Height="72">
  <m3:SwipeToDismissBox.BackgroundContent>
    <Border Background="DarkRed" Padding="16">
      <TextBlock Text="移除" VerticalAlignment="Center" />
    </Border>
  </m3:SwipeToDismissBox.BackgroundContent>
  <Border Background="White" Padding="16">
    <TextBlock Text="向任意方向拖动" VerticalAlignment="Center" />
  </Border>
</m3:SwipeToDismissBox>
```

## 变体与属性

每个实例默认拥有独立 `State`，也可绑定应用持有的 `SwipeToDismissBoxState`。
以下 C# 中 `swipe` 是已挂载的控件：

```csharp
swipe.State = new SwipeToDismissBoxState(SwipeToDismissBoxValue.Settled,
    positionalThreshold: distance => distance * 0.5);
swipe.Dismissed += (_, e) => Console.WriteLine(e.Direction);
await swipe.State.DismissAsync(SwipeToDismissBoxValue.EndToStart);
await swipe.State.ResetAsync();
```

命名空间为 `Material3Expressive.Components`。异步操作接受 `CancellationToken`，新的拖动、
状态操作或卸载可取消旧动画，调用方应按需要处理 `OperationCanceledException`。
`SnapToAsync` 直接提交状态。`CurrentOffset` 与阈值回调均使用 DIP，首次布局前 offset 为 NaN，
`RequireOffset()` 在未初始化时抛异常。动画操作应在挂载并布局后调用。

`CurrentValue` 可在经过锚点间中点时改变；`SettledValue` 仅表示最后完成的锚点。
`TargetValue`、`Progress` 和 `DismissDirection` 可用于驱动应用内容。
默认低速非零释放阈值为 56 DIP；零速度释放选择最近锚点，125 DIP/s 起选择速度方向的下一锚点。

`EnableDismissFromStartToEnd`、`EnableDismissFromEndToStart` 控制可拖动方向；
`FlowDirection` 将逻辑方向映射到物理方向。`GesturesEnabled="False"` 只禁用手势，仍可程序化消除。
完成消除后手势暂停，调用 `ResetAsync()` 后恢复。方向开关不否决程序化写入不存在锚点的状态。

`Dismissed` 在已提交状态不是 Settled 时通知；初始已消除状态也会通知，首次布局前方向可能为 Settled，
处理器可更新业务集合，也可调用复位。

## 内容槽与定制

`Content` / `ContentTemplate` 是移动的前景，`BackgroundContent` / `BackgroundContentTemplate`
是固定背景。自然尺寸仅由前景决定；背景匹配最终容器大小。组件不裁剪移出的内容，
需要限制可见范围时在外层容器设置 `ClipToBounds="True"`。颜色、圆角和背景动作由应用提供。

## 使用限制与注意事项

动画操作应在控件挂载和布局后调用。首次布局前 CurrentOffset 为 NaN，RequireOffset 会报告未初始化。
消除动画进行中移除目标方向会触发已知异常，应等操作结束后再修改方向配置。
触屏、完整嵌套滚动仲裁及非 Windows 平台尚未双端验收。
[Catalog 示例](../../../Material3Expressive.Catalog/SwipeToDismissDemo.axaml)可用 `--swipe-to-dismiss` 启动。

内部机制、移植差异与验证范围见 [INTERNALS.md](INTERNALS.md)。
