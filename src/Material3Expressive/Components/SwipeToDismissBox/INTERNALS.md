# SwipeToDismissBox 内部机制与维护说明

## 1. 上游基线与职责映射

固定基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)，AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与 Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
两端 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SwipeToDismissBox.kt` 的当前非废弃 API 一致，平台拖动依据 Core Foundation。

| 上游符号 / 职责 | 本地类型 / 成员 |
| --- | --- |
| `SwipeToDismissBox` / `SwipeToDismissBoxState` | 同名控件及状态类型 |
| Foundation `AnchoredDraggable.kt` 的锚点、互斥与重启 | `SwipeToDismissBoxState` 私有状态算法 |
| `AnchoredDraggableDefaults.flingBehavior` / `SnapFlingBehavior` | 最近锚点、位置与速度阈值、300ms tween |
| `DraggableAnchorsNode` / matchParentSize | `SwipeToDismissBoxLayout` |
| Core `Draggable.kt` | 控件手势与共享 Lsq2 `VelocityTracker` |

## 2. 关键机制与架构契约

- 锚点按 Settled、StartToEnd、EndToStart 排列，距离相等时后者胜出。内部使用 Float 物理像素，公开 offset 和阈值使用 DIP。RTL 在 roundToInt 前反转物理 offset。
- 零速度释放取最近锚点，低速非零释放使用位置阈值，默认 56 DIP；125 DIP/s 起选择速度方向的下一锚点。鼠标 slop 为 0.125 DIP，触屏为 18 DIP。上述数值属于行为配置，布局尺寸由内容决定。
- 程序化动画持有 dragTarget，普通 fling 使用无目标 anchoredDrag。CurrentValue 在半程更新，SettledValue 在提交时更新，取消拖动以零速度 settle。
- 尺寸或方向变化在重组对应帧先采样已有动画，再提交锚点并从最新 offset 重启。ResetAsync / DismissAsync 直接取得互斥操作。
- 背景匹配最终布局，前景决定自然尺寸并独立移动。背景槽按 BadgedBox 的 presenter 注册与 LogicalChildren 模式维护 DataContext 和父级归属。
- 状态持有窗口子作用域，控件另持配置和回调作用域。状态替换、卸载与调用者取消结束旧操作。Bubble 阶段处理移动，保留子控件消费优先级；按下可立即打断程序化动画，尚未开始拖动时保留子按钮点击。
- IsEffectivelyEnabled 变化立即取消捕获并以零速度 settle，覆盖祖先禁用。GesturesEnabled 的取消在下一对应帧处理。
- Dismissed 观察提交状态。初始已消除状态可能在首次测量前通知，此时 direction 可为 Settled；动画提交同帧通知，外部 snap 在后续重组帧通知。卸载、换状态或回到 Settled 撤销待发通知。

## 3. 已确认决定与已知差异

- 引入提交 `b806d7f3` 保留组件私有锚点算法及现有动画、速度追踪能力。
- 未解决的 `dynamic.remove-anchor` 会使固定 Core 抛出 `AnchoredDraggableUninitializedException`。带目标操作重启后仍使用原始 target 提交，目标已删除时 offset 变为 NaN。本地保留该算法及严格检查，改变取消结果、最终状态或回调次数仍待维护者决定。
- Tween/Easing 的 Double 中间运算与 Compose Float 存在精确数值差异，普通消除 64ms 样本约为 0.000008 DIP。共享运行时的精度变更尚未批准。
- 尚未移植废弃 confirmValueChange API。触屏、多指、完整嵌套滚动仲裁、跨屏 DPI 及非 Windows 桌面未经双端验证。
- 已有 Windows 样本的可完成场景画面一致，严格状态比较仍有上述差异。原验证使用仓外缓存及同版本依赖产物，正式 locked restore 的包哈希验证受包源鉴权阻塞。

## 4. 验证入口

- `tests/Material3Expressive.Tests/SwipeToDismissBoxTests.cs` 覆盖状态、阈值、布局、反向、中断、禁用、子控件输入和回调中卸载。
- 双端用例为 `SwipeToDismissBoxCases.cs` / `SwipeToDismissBoxCases.kt`，族名 `swipe-to-dismiss`。
- 目标锚点移除场景单独执行，避免参考端异常影响其他用例。Windows Catalog 可检查鼠标消除、复位和 RTL。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~SwipeToDismissBoxTests
./tests/VisualParity/parity.ps1 self-test --family swipe-to-dismiss
./tests/VisualParity/parity.ps1 self-test --case swipe-to-dismiss.dynamic.remove-anchor
```
