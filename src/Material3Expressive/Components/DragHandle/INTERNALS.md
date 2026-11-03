# DragHandle 移植说明

依据 `docs/upstream-baseline.json` 固定的 AndroidX `DragHandle.kt` 和 `tokens/DragHandleTokens.kt`。
固定 Core 的 DragHandle 实现与 AndroidX 相同。新增组件与非破坏性组件 API 由本次剩余移植任务授权；
没有引入公共共享框架，也没有使它承担上游没有的拖动识别职责。

## 布局与状态

- `VerticalDragHandle` 保存 `collectIsDraggedAsState` 对应状态及 DragStart 列表，按对象身份配对 Stop/Cancel。
  source 替换时清空 collector 的列表，但保留 remember 状态直到新 source 的第一条事件；
  任何事件都会重新求值。collector 位于 owner，不因内部模板重建丢失活动拖动。
- `DragHandleLayout` 对应外层 minimumInteractiveComponentSize，内部 measure 忽略传入约束，
  再处理 minimum 与父约束造成的居中溢出。位置及 alignment-line 留白按 Compose 像素规则舍入。
  绕过 Avalonia 自动 RTL 镜像，避免半像素居中时把 Compose 向正方向的舍入反转。
- `DragHandlePresenter` 对应 hoverable / pressable / graphicsLayer / layout / drawBehind / indication。
  Initial 对应 Tunnel，局部按下不消费、不捕获；释放、离开或失去捕获时清理。
  宿主消费移动时清除局部按下。拖动 > 按下 > 正常；尺寸、颜色和圆角没有额外插值。
- 共享 MaterialInteractionController 的内部 `ObservePointerPresses=false` 只用于此 presenter，
  禁止通用输入路径生成本地 Press；默认 true 保持既有控件行为。Hover、外部 source 和动画仍复用基础能力。
  外部 Press 只改变 indication，不改变局部 isPressed，这与上游独立 pressable 一致。
- `indication(source, ripple())` 在 source 更换时创建新 node，故调用已有 ResetSourceIndication 清除旧动画；
  它与 owner 的记忆状态有不同生命周期。卸载释放订阅、TopLevel 处理器和帧等待。

未使用上游未被 VerticalDragHandle 消费的 ContainerWidth Token。默认 4×48、按下/拖动 12×52，
颜色 Outline/OnSurface/OnSurface，形状 Full/Medium/Medium，最小交互布局 48。

## 边界与验证

当前 Shapes 使用既有 CornerRadius，只支持圆角形状；任意 Compose Shape 未实现。
若要接受任意 Shape，需要维护者决定全库形状/裁剪合约，再接入 `DragHandleShapes`，
不能把任意路径静默转换为圆角。触摸扩展命中及真实辅助技术尚未验证。

`DragHandleTests` 检查布局溢出、真实合成鼠标事件、无消费/Press、重叠拖动、source 替换、卸载和 Token。
`DragHandleCases` 的 29 例覆盖 light/dark、LTR/RTL、分数尺寸、极端约束、自定义三状态配置、
按下/移出取消、重叠拖动、source 替换、主题/配置切换、hover 和外部 Press。
2026-11-03 双端 self-test 全部重复稳定、结构化事实相等、图像 withinTolerance；未更改容差。
夹具明确使用 OnSurface 环境 content color，并在两端同一时刻送帧提交主题重组，再采样。
同日 Windows Catalog 实际鼠标拖动将面板宽度从 240 改到 353 DIP，释放后恢复正常外观；
共享示例又验证从 240 改到 313 DIP，Card 同步调整宽度，Chip 保持未选中。
这只是桌面鼠标路径检查，不代表触控/笔或屏幕阅读器验证。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~DragHandle
./tests/VisualParity/parity.ps1 self-test --family drag-handle --require-match
```
