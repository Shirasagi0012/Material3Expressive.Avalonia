# SmoothScroll.Avalonia

Material3Expressive 使用的物理滚动、漫游、缩放与合成线程交互模块。
源码来自 [Shirasagi0012/SmoothScroll.Avalonia](https://github.com/Shirasagi0012/SmoothScroll.Avalonia)
固定提交 `429c3c685a1e630337024c2500a9814b96fcc10c`，对应原 NuGet `12.0.0.1`；
保留 [MIT 许可证](LICENSE.txt)及[原始使用说明](README.upstream.md)。
仓内补丁版本为 `12.0.0.2`，通过 ProjectReference 构建，不依赖原制品覆盖或浮动源码。

## 仓内扩展

`InteractionTracker.ConfigureVerticalScroll(factory, canConsumeAtBoundary, preFling)`
增加可选异步释放回调。距离为 DIP、速度为 DIP/s，正 Y 增加滚动 Offset。
回调在 UI 线程接收可用纵向速度，返回其消费量；不得超过可用量或改变符号。
消费后剩余速度交回原有惯性实现，横向速度保持原值。
`ConfigureVerticalScrollRelease(callback)` 可单独有序更新该回调，保留原参与者与动效；
移除最后一个释放消费者时传 null，恢复原直接释放路径，不引入无意义的 UI 往返。

释放请求先停留在有身份的事务中。此前滚动快照经原通知队列发送到 UI 后才调用回调，
即使零速度也等待回调完成。组合线程不等待 UI 或 Task，也不从 UI 回读控件。
新输入、显式取消、绝对定位、重配置或卸载取消令牌并废弃旧事务；忽略取消的旧回调
完成后不能启动旧惯性。没有回调时保留原释放路径。

`ScrollReleaseFailed` 在 UI 线程报告非取消异常或非法消费量；无订阅者时异常通过 UI
dispatcher 重新抛出。失败不视为零消费成功，也不会继续启动惯性。
该入口覆盖真实指针释放与显式 `CompleteVerticalScroll`，没有新增滚轮会话结束推断。
固定 Core `Scrollable.kt::onScrollStopped` 在普通滚轮使用默认 FlingBehavior 时直接返回，
不会向父级 dispatchPreFling；`AbstractScrollableNode.kt::shouldBeTriggeredByMouseWheel`
仅允许自定义 fling 行为进入该路径。原 SmoothScroll 滚轮物理惯性仍属既有原生适配，
不把其 Inertia 位移重标为 Core 的 UserInput 线性滚轮动画。

`ScrollPresenter.VerticalTouchSlop` 是可选的垂直 touch/pen 识别阈值（DIP）。仅横轴禁用、
纵轴启用的平移手势消费该配置；首笔跨越阈值的输入扣除阈值后立即投递余量，速度仍记录真实指针位置。
默认 null 保留原有识别方式。Material PullToRefreshBox 临时贡献固定 Core 的 18 DIP，断开后恢复原属性源，
对应 `DragGestureDetector.calculatePostSlopOffset`，不改变普通 ScrollViewer 的默认体验。

有异步释放消费者时，已开始的指针拖动失去捕获按零速度释放，映射固定 Core
`DragGestureNode.processDragCancel` → `ScrollableNode.onDragStopped`；这使已拉出的刷新状态仍能回弹。
无释放消费者时保留原有参与者取消行为。显式 CancelScroll、重配与卸载继续取消事务，
不会把卸载的旧状态重新启动为刷新。

## 构建与验证

模块跟随主仓库的 .NET 10、Avalonia 和中央依赖配置；不保留原 net8.0 多目标。
合成 API 的内部访问使用原来的 `IgnoresAccessChecksToGenerator`，版本固定为 1.0.0。
库保留原测试，并补充 `ScrollReleaseTests` 验证顺序、消费、零速度、取消、错误和旧路径。

```powershell
dotnet test modules/SmoothScroll.Avalonia/tests/SmoothScroll.Avalonia.Tests/SmoothScroll.Avalonia.Tests.csproj
```

组件层的 nested-scroll 顺序、刷新算法、状态与视觉由主库负责；此模块不引用 Material3
组件，不改变其动画规格，也不声称原物理滚动算法等同 Compose Foundation 的惯性算法。
