# BottomSheet 内部机制与维护说明

## 1. 上游基线与职责映射

固定 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`、
Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。两端 commonMain `BottomSheet.kt` 相同，逻辑以 AndroidX 为准。

| 上游职责 | 本地对应 |
| --- | --- |
| `BottomSheet` / `BottomSheetImpl` | `BottomSheet`、模板、`BottomSheetLayout` |
| `BottomSheetScaffold.kt` / `StandardBottomSheet` | `BottomSheetScaffold`、布局及 BottomSheet 的内部 standard 分支 |
| `SheetDefaults.kt` / `SheetState` | `SheetState`、`BottomSheetTokenSource` |
| Core Foundation `AnchoredDraggable.kt` / `SnapFlingBehavior.kt` | `SheetState` 私有锚点与 fling 算法 |
| Core 桌面 `Draggable.kt`、backhandler | 鼠标/触屏输入、窗口 Escape |
| Core skikoMain `l10n/*.kt` | `BottomSheetStrings.json` |

## 2. 关键机制与架构契约

- 锚点顺序为 Hidden、PartiallyExpanded、Expanded；等距时后者优先。内部 Float 物理像素，公开 DIP。
  固定基线 deterministic 标志为 true：partial = full − min(full/2, sheet)，expanded = max(0, full − sheet)。
  短内容的两个展开锚点合并，后续尺寸变更将原 partial 目标提升为 expanded。
- CurrentValue 对应 settledValue；IsVisible 使用已跨越的锚点。带目标程序化动画与无目标 fling 保持各自语义。
  显示/展开使用 SpatialDefault，隐藏/部分收起使用 EffectsFast；fling 使用 SpatialDefault，限制跨越目标，保留剩余速度规则。
  低速非零位移阈值默认 56 DIP，速度阈值 125 DIP/s；接近底边的向下 fling 在 125 DIP 范围内平方衰减，保留速度下限。
- Surface 允许弹簧越过顶部锚点，按 overflow 扩大表面并反向缩放内层内容。ContentInsets 先扣除已消费值，再传播实际消费。
  控件外层占满有限宿主，内部 Surface 自然高度并居中；这是 Avalonia 用于承载偏移布局的组件内部包装。
- 复用动画子作用域、MaterialSurface、VelocityTracker 和 MaterialWindowInsets，不引入公共共享锚点框架。
  同一状态一次挂载一个组件；卸载或换状态取消所有任务。鼠标 slop 0.125 DIP，触屏 18 DIP；子内容先消费移动事件。
- 手柄点击不受 GesturesEnabled 禁止，默认 TooltipBox + PlainTooltip；关闭完成后通知，调用 HideAsync 本身不产生关闭回调。

- Standard 分支仅由 Scaffold 模板设置内部 StandardPeekHeight，不公开通用布局策略。
  锚点依次 partial、hidden、expanded；partial = full − peek，expanded = full − sheet。
  初次布局或稳定 partial 时允许 peek=0 的模糊 partial；peek 等于 sheet 高度时排除 partial；
  sheet=0、peek=0 或 Hidden 已启用时添加 hidden。该规则不复用树内面板的 deterministic 合并/提升逻辑。
  CurrentValue / IsAnimationRunning 改变时重新布局，以移除不再允许的零 peek 模糊锚点。
  Hidden 锚点在恢复非零 peek 时消失，保留原 offset、CurrentValue 和 TargetValue；后续 Show 请求再选择实际存在的展开锚点，避免 NaN 污染布局。
- Scaffold 先量面板、顶部栏，再以扣除顶部栏后的高度量正文，最后量 Snackbar。
  绘制顺序正文、顶部栏、面板、Snackbar；部分展开时 Snackbar 跟随实际 offset，其他状态放到宿主底部。
  ContentPadding 仅发布底部 peek，正文自行应用与消费；默认 SnackbarHost 连接组合状态中的队列。
- Standard 手柄在 Expanded 时按 Hidden 是否启用选择隐藏或 partial；直接拖动不受 CurrentValue==Hidden 禁止，
  fling 不应用树内面板的底边衰减，也不发出 DismissRequested。标准面板不安装返回键处理。

## 3. 已确认决定与已知差异

- 本次任务授权非破坏性组件 API、内部适配与已有能力复用；不据此宣称所有上游差异已批准。
- 尚未实现 ModalBottomSheet 及 Android 连续预测返回形变。树内 BottomSheet / BottomSheetScaffold 不包含模态行为。
- 自动嵌套滚动桥接尚未实现。推荐由现有滚动基础能力统一提供 pre/post scroll 和 fling 分发，再接组件已有入口；
  例如父层先调用 `sheet.OnPreScroll(deltaY)`，子层消费余量后交 `sheet.OnPostScroll(remainingY)`。
  这需要新的公共共享协议，影响 ScrollViewer、AppBar、PullToRefresh 等，待维护者决定；未在组件内复制框架。
- 无障碍使用 Avalonia Invoke / ExpandCollapse；Avalonia 现有 provider 没有 Compose 命名的多个自定义动作槽。独立 dismiss 动作仍缺失。
  推荐评估手柄 peer 的虚拟动作子节点，例如 `GetChildrenCore() → DismissPeer`、`DismissPeer.Invoke() → BottomSheet.Dismiss()`；
  这会增加屏幕阅读器遍历节点，而不是原位置的自定义动作，需要维护者确认语义取舍，未实现或记为已批准。
- 未经验证：真实触屏、多指、DPI 切换、非 Windows 桌面、屏幕阅读器及多面板返回仲裁。

- 复用 TooltipBox 已确认的动画目标提交时序差异，确认范围见 [Tooltip INTERNALS](../Tooltip/INTERNALS.md#3-已确认决定与已知差异)。不将此结论扩展到其他视觉差异。
- 双端仍有严格数值差异：弹簧中断后的 offset 及越界缩放几何出现 Float 量级误差。
  本地保留上游 Float 运算顺序，再交给 Avalonia 的 Double 矩阵；没有通过修改容差、基准或舍入采样值隐藏差异，亦未将其记为已批准。

## 4. 验证入口

- `BottomSheetTests` / `BottomSheetScaffoldTests`：锚点、程序化与拖动、拒绝、中断、卸载、嵌套滚动消费。
- 双端 `BottomSheetCases.cs` / `BottomSheetCases.kt`，族名 `bottom-sheet`。
- Catalog：`dotnet run --project src/Material3Expressive.Catalog -- --bottom-sheet`。
  常驻面板使用 `--bottom-sheet-scaffold`。

当前验证（2026-11-04）：解决方案构建 0 错误；组件行为测试 30 项通过，整库 2095 项通过。
双端 50 场景、272 个采样：14 equal、30 withinTolerance、6 different，执行错误为 0。
严格差异保留在 expand、resize、handle、handle-disabled、scaffold.expand、scaffold.handle；
几何与 offset 最大差值均为 0.000030517578125 DIP，状态属性一致。
三个 handle 场景另有 32ms / 96ms 的 Tooltip 光栅差异，适用上节已确认时序差异。
零 peek 隐藏再恢复用例通过容差检查且重复运行稳定；resize、scaffold.expand、handle 的重复性检查也稳定。
完整报告位于 `artifacts/visual-parity/runs/20261104-145837-6030f816/summary.md`，这不是所有严格一致性检查均通过的声明。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~BottomSheet
./tests/VisualParity/parity.ps1 prepare-reference --family bottom-sheet
./tests/VisualParity/parity.ps1 run --family bottom-sheet
```
