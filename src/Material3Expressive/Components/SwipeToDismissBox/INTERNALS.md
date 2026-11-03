# SwipeToDismissBox 实现说明

使用入口见 [README](README.md)。本组件不引入公共 AnchoredDraggable 框架。

## 固定参考与职责

依据 [baseline](../../../../docs/upstream-baseline.json) 的 AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与 Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
两端 Material3 `SwipeToDismissBox.kt` 的当前非废弃 API 一致；拖动平台行为另查 Core Foundation。

- `SwipeToDismissBox.kt` / `SwipeToDismissBoxState` → `SwipeToDismissBox.cs` / `SwipeToDismissBoxState.cs`。
- Foundation `AnchoredDraggable.kt` 的 anchors、target/current/settled、restartable 与 animateTo → 私有状态算法。
- `AnchoredDraggableDefaults.flingBehavior` / `SnapFlingBehavior` → 零速度最近锚点、56 DIP 位置阈值、125 DIP/s 速度阈值与 300ms tween；此分支不走 decay。
- `DraggableAnchorsNode` / Box(matchParentSize) → `SwipeToDismissBoxLayout.cs`。
- Core `Draggable.kt` 的鼠标 slop 为 0.125 DIP，触屏为 18 DIP；速度复用共享 Lsq2 `VelocityTracker`。

## 必须保留的行为

锚点顺序为 Settled、StartToEnd、EndToStart，最近距离相等时后者胜出。内部以 Float 物理像素计算，
公开 offset/阈值为 DIP。RTL 必须在 roundToInt 前反转物理 offset，不能先正向取整再镜像。
背景不参与自然尺寸，不随前景移动；前景不自动裁剪。
背景槽沿用仓内 BadgedBox 的 ContentPresenter 注册及 LogicalChildren 归属方式，
让直接控件内容在模板创建前后均继承宿主 DataContext，替换时释放旧逻辑父级。

程序化动画持有 dragTarget，普通 fling 使用无目标 anchoredDrag，TargetValue 随最近锚点变化。
CurrentValue 在半程更新，SettledValue 仅在提交时更新；拖动取消仍以零速度 settle。
尺寸/方向变化在对应重组帧提交锚点：先运行该帧已有动画，再从最新 offset 重启动画。
调用 `ResetAsync` / `DismissAsync` 则直接取得互斥操作，不额外等待重组帧。

状态持有挂载窗口的子作用域。替换状态、卸载和调用者取消会结束旧操作；控件的配置与回调等待
另由可释放子作用域管理。指针 motion 在 Bubble 阶段处理，让已消费运动的子控件优先；
程序化动画被按下打断时立即捕获，未开始拖动时保留子按钮点击。
Avalonia 的 IsEffectivelyEnabled（含祖先禁用）停止原生输入分发，因此其变更立即取消捕获并以零速度
settle；仅监听自身 IsEnabled 会遗漏祖先禁用。此宿主输入适配与上游 GesturesEnabled 的下一帧
取消分开，行为测试覆盖禁用后复位及重新启用后的新拖动。

固定 Compose 的 LaunchedEffect(settledValue) 观察提交阶段：初始已消除状态的回调可能先于首次测量，
其 direction 仍为 Settled；动画提交在同帧通知，外部 snap 在后续重组帧通知。
卸载、状态替换或再次回到 Settled 会撤销尚未发出的通知。

## 未解决问题与维护者决定

`dynamic.remove-anchor` 会使固定 Core 抛出 `AnchoredDraggableUninitializedException`：
target-accepting anchoredDrag 虽按新 anchors 重启，最后仍用最初 target 提交；目标不存在导致 offset=NaN，
布局节点的严格 offset 检查随后失败。本地保留这一算法及严格检查，未宣称该场景受支持。
建议后续独立决定：在目标锚点移除时取消旧任务，并提交新的有效 target。
调用例为 `var task = state.DismissAsync(StartToEnd); box.EnableDismissFromStartToEnd = false;`；
该方案将改变 task 结果、最终 SettledValue 与 Dismissed 次数，属于上游语义偏离，当前未批准。
失败用例保留，单独执行以免参考端进程失败污染后续用例。

共享 ComposeAnimation 的 Tween/Easing 使用 Double 中间运算，固定 Compose 使用 Float。
已采样动画的 offset/progress 仍有严格数值差异（例如普通消除 64ms 的 offset 相差约 0.000008 DIP），
未提高比较容差，也未在组件内复制动画运行时。统一 Float 精度的运行时方案需单独评估所有调用方；
本组件没有批准共享运行时精度变更。

尚未移植废弃 confirmValueChange API。触屏、多指、完整嵌套滚动仲裁、跨屏 DPI 和非 Windows
桌面未经双端验证；不存在对这些范围的已接受近似声明。

## 验证

[控件测试](../../../../tests/Material3Expressive.Tests/SwipeToDismissBoxTests.cs) 覆盖状态、阈值、方向、
布局、反向、中断、尺寸变化、取消、卸载重挂、子控件输入与事件生命周期。
双端 [C#](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/SwipeToDismissBoxCases.cs) /
[Kotlin](../../../../tests/VisualParity/reference/src/main/kotlin/cases/SwipeToDismissBoxCases.kt) 覆盖静态锚点组合、
RTL、受限/零宽布局及消除、复位、snap、拖动、禁用和重启。重复捕获与被动截图均实际运行。
除上游异常场景外，当前采样画面一致，但严格状态数值比较仍有上述浮点差异，不能称整族通过。
2026-11-03 Windows 补充验证：31 项控件测试通过，包含消除回调直接移除控件的生命周期；
43 个可完成的双端用例共 181 张采样画面严格一致，重复与被动截图稳定。
背景槽修正后的布局/拖动回归位于 `20261103-104347-b3c73f24` 至 `20261103-104408-73128419`；
目标锚点移除的参考端异常独立产物为 `20261103-104247-caf6055b`（均在 `artifacts/visual-parity/runs/`）。
真实 Windows Catalog 已检查鼠标消除、复位与 RTL 消除；这不是双端桌面验收。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~SwipeToDismissBoxTests
./tests/VisualParity/parity.ps1 self-test --family swipe-to-dismiss
./tests/VisualParity/parity.ps1 self-test --case swipe-to-dismiss.dynamic.remove-anchor
```

本机包源鉴权阻塞正式 locked restore；本次构建与运行使用仓外缓存及相同版本/提交的依赖产物，
没有修改仓库依赖版本或 lock 文件。此补充验证不替代正式包哈希验证。
