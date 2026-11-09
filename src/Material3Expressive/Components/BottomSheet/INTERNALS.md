# BottomSheet 内部机制与维护说明

## 1. 上游基线与职责映射

固定 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`、
Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。两端 commonMain `BottomSheet.kt` 相同，逻辑以 AndroidX 为准。

| 上游职责 | 本地对应 |
| --- | --- |
| `BottomSheet` / `BottomSheetImpl` | `BottomSheet`、模板、`BottomSheetLayout` |
| `BottomSheetScaffold.kt` / `StandardBottomSheet` | `BottomSheetScaffold`、布局及 BottomSheet 的内部 standard 分支 |
| `ModalBottomSheet.kt` / Core `ModalBottomSheet.skiko.kt` | `ModalBottomSheet`、既有 `ModalPresenter` 与 `Scrim` |
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
- 父级取消候选拖动时仅释放自己的 capture；子按钮持有的捕获不被手势开关取消，保留原来的 Click 周期。
  原生 Thumb 的移动事件不置 Handled，容器改由其 `DragStarted` 结束候选手势，保留 Slider 原生输入职责。

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

- Modal 复用零布局声明、窗口内 overlay Popup 与 ModalPresenter；后者隔离焦点、仲裁最上层返回并恢复原焦点。
  内层 BottomSheet 不再单独接收窗口返回，避免同次 Escape 同时收起与隐藏。展开且有 partial 时先收起，其余可见状态请求隐藏。
  IsOpen 对应是否保留上游 composition：程序 HideAsync 和 DismissRequested 均不擅自写回它，应用在关闭通知中设 false。
  首次出现 expanded 锚点后在下一帧调用 ShowAsync；已恢复的可见目标保留，随后程序隐藏不会重新触发自动显示。
  状态替换、模板替换和卸载结束本次呈现及动画；owner 动画作用域重建时重新连接呈现与遮罩。
  模板完成 attach/detach 后再将内容移入 Popup。
- 模态内容预消费的 top inset 是 offset 物理像素取整数，再与祖先已消费值逐侧取最大；默认 TonalElevation 为 0。
  ScrimColor=null 省略遮罩绘制与点击，透明色仍可点击。点击开关不影响 Escape；CloseSheet 来自固定 Core 的 86 份本地化表。
- 遮罩目标在状态写入后的下一重组帧合并，使用 DefaultEffects。固定两端 `AnimateAsState.kt` 的
  `restartAnimation` 先取消旧 job 再 animateTo，旧任务会清除速度和 LastFrameTime；本地使用既有 Animatable 的
  StopAsync → AnimateToAsync 保留此顺序，不能用保留中断速度的 AnimatedState 等价替换。

## 3. 已确认决定与已知差异

- 本次任务授权非破坏性组件 API、内部适配与已有能力复用；不据此宣称所有上游差异已批准。
- 尚未实现 Android 连续预测返回形变。树内 BottomSheet / BottomSheetScaffold 不包含模态行为。
- 原先因缺少共享 pre/post scroll 与可等待的 fling 分发而延期自动桥接，避免在组件内复制框架。
  本轮维护者明确授权补齐基础能力与修订待定决定：现在复用内部 MaterialScrollCoordinator，
  在原异步 pre-fling 事务后增加正文真实惯性与 post-fling 阶段，见 [共享决定 §39](../../../../docs/DECISIONS.md#39-物理滚动共享参与与异步释放)。
  BottomSheet.Scroll.cs 的 compositor participant 仅保存 Float 像素 offset/锚点并执行同一 raw-delta clamp；
  有序快照在 UI 调用 SheetState.ApplyScrollOffset，不改变 settled/current anchor 或 velocity。
  程序状态变更以代次同步到 participant，过期快照不覆写新状态；确认回调和吸附动画仍在 UI。
  默认只自动发现唯一纵向物理视口，三种面板共用同一连接；AutoConnectScroll=false 保留原手动宿主。
  上游 Column 对无权重子项传剩余有限高度，本地新增内部 BottomSheetContentLayout 替代无限主轴 StackPanel，
  使默认 ScrollViewer 可直接使用；普通短内容仍按自然高度测量。触摸/笔由正文负责 slop、捕获和速度，
  面板不并行启动第二条直接拖动；鼠标左键直接拖动维持原行为。
- 原实现使用 Invoke / ExpandCollapse，因 Avalonia provider 缺少多个命名动作槽而延期独立 dismiss；
  当时提出虚拟动作子节点但尚无授权。本轮按用户明确授权采用此方案，见下文；
  原理由保留：增加辅助技术遍历节点属于平台语义适配，并非原位置的 Compose 多动作槽。
- 未经验证：真实触屏、多指、DPI 切换、非 Windows 桌面、屏幕阅读器及多面板返回仲裁。

- 复用 TooltipBox 已确认的动画目标提交时序差异，确认范围见 [Tooltip INTERNALS](../Tooltip/INTERNALS.md#3-已确认决定与已知差异)。不将此结论扩展到其他视觉差异。
- 双端仍有严格数值差异：弹簧中断后的 offset 及越界缩放几何出现 Float 量级误差。
  本地保留上游 Float 运算顺序，再交给 Avalonia 的 Double 矩阵；没有通过修改容差、基准或舍入采样值隐藏差异，亦未将其记为已批准。

## 4. 验证入口

- `AnchoredContainerInputTests` 和双端 `anchored-child-input` 覆盖按住内容按钮时关闭父手势及原生 Thumb 拖动；
  行为测试使用真实纵向 Slider。Sheet 的 LTR/RTL 四例结构化事实 equal、图像 withinTolerance，
  重复和额外截图稳定。与 Swipe 共用场景只复用测试过程，
  生产代码没有新增共享手势机制。入口：`./tests/VisualParity/parity.ps1 self-test --family anchored-child-input`。
- `BottomSheetTests` / `BottomSheetScaffoldTests` / `ModalBottomSheetTests`：锚点、程序化与拖动、拒绝、中断、卸载、嵌套滚动消费，及模态自动显示、恢复状态、返回/外部点击策略、焦点恢复、换状态、Inset 预消费与遮罩反转。
- `BottomSheetScrollTests` 验证真实正文触摸、消费顺序、取消吸附、有限高度、开关、状态/内容替换与三个宿主接线；
  双端 `bottom-sheet-scroll` 从真实触摸输入采集面板 offset、正文 offset、current/target 和过渡图像。
  该接线的真实桌面合成时序及多层滚动设备仍需验证，行为通过不等于所有 fling 图像轨迹一致。
- 双端 `BottomSheetCases.cs` / `BottomSheetCases.kt`，族名 `bottom-sheet`。
- Catalog：`dotnet run --project src/Material3Expressive.Catalog -- --bottom-sheet`。
  常驻面板使用 `--bottom-sheet-scaffold`，模态面板及独立遮罩使用 `--modal-bottom-sheet`。

当前双端验证（2026-11-04）：83 场景、394 个采样：34 equal、40 withinTolerance、9 different，执行错误为 0。
其中新增模态 33 场景、122 个采样为 20 equal、10 withinTolerance、3 different；新增行为测试 10 项通过。
严格差异保留在 expand、resize、handle、handle-disabled、scaffold.expand、scaffold.handle，
以及 modal.auto-show、modal.auto-show-no-partial、modal.expand；
几何与 offset 最大差值均为 0.000030517578125 DIP，状态属性一致。
三个 handle 场景另有 32ms / 96ms 的 Tooltip 光栅差异，适用上节已确认时序差异。
零 peek 隐藏再恢复用例通过容差检查且重复运行稳定；resize、scaffold.expand、handle 的重复性检查也稳定。
模态反转的遮罩任务取消顺序已修正，反转场景图像通过现有容差；自检 `20261104-154746-243ee3d8` 重复及额外截图稳定。
没有修改参考源码版本、容差或采样读数。锁定模式还原与解决方案构建成功；整库 2115 项通过。
完整报告位于 `artifacts/visual-parity/runs/20261104-154125-449d4ad8/summary.md`，这不是所有严格一致性检查均通过的声明。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~BottomSheet
./tests/VisualParity/parity.ps1 prepare-reference --family bottom-sheet
./tests/VisualParity/parity.ps1 run --family bottom-sheet
```

### 2026-11-09 显式形状入口

固定 AndroidX 的 BottomSheet / ModalBottomSheet 的 shape 与 BottomSheetScaffold 的 sheetShape 均直接交给 Surface；Core common 的对应入口采用同一语义。
此前仅暴露 CornerRadius，足以表达默认 Token，但不能表达切角、相对角和任意路径。
本轮用户授权补齐 API，复用已确认的 MaterialSurface.Shape；没有新增形状框架或改变状态职责。
Shape 非空优先，null 回退原有 CornerRadius；逻辑方向、可变形状订阅、阴影和裁剪由 Surface 负责。
动画仍在原有 Surface 层缩放，保留内容反缩放和锚点协议。无破坏性迁移。

`SheetShapeTests` 验证形状突变、运行时替换/清空、锚点不变、模态重开、抽屉方向与项目点击/选择职责。
原双端族新增静态切角/百分比圆角及动态展开、拖动、切换/恢复；抽屉还覆盖开关反向与项目按压。

### 2026-11-09 手柄命名无障碍动作

固定 BottomSheet.kt 的 DragHandleWithTooltip 提供 dismiss 与按当前状态选择的 expand/collapse；
BottomSheetScaffold.kt 额外要求 anchors.size > 1，并仅在 !skipHiddenState 时提供 dismiss。
本轮按用户授权修订上面的历史待定事项：HandlePeer 缓存三个虚拟 ActionPeer，按这些条件投影可用动作，
保留父节点 Invoke / ExpandCollapse。动作只调用原状态协议；失效后的旧 peer 引用再次调用也重新检查可用性。
不新增可视子控件或键盘停靠点。名称复用既有固定上游本地化表，禁用/换 State/锚点/当前状态变化使树失效。

保留上游确认顺序：标准 Scaffold collapse/dismiss 先查询 confirm，再无条件启动会二次确认的状态操作；
普通面板 dismiss 通过原 Dismiss 流程，完成后发请求。无 API 迁移，辅助技术树增加命名子节点。
`SheetAutomationTests` 验证动作变化、失效 peer、禁用、veto、父关系、Scaffold 隐藏策略与 dismiss 回调。
`bottom-sheet-semantics` 使用两端真实自动化/semantics 动作启动状态动画，并比较实际输出；不以直接赋状态代替调用。

### 本轮双端验证记录

`bottom-sheet-shapes` 全部 125 例：68 equal、43 withinTolerance、14 different，执行错误 0、重复稳定。
其中新增形状 42 例为 27 equal / 9 withinTolerance / 6 different；24 个静态端点无 different，
拖动和形状替换无 different。6 个展开差异均保留实际几何比较：例如 416ms content 高度
Core=320、Avalonia=319.99999131744516 DIP，sheet 高度相差约 0.000004 DIP；该帧全图逐像素相同。
没有放宽数值门限。原有 8 个不同场景仍在报告中，不归于新形状的验收范围。

`bottom-sheet-semantics` 15 例为 8 equal / 4 withinTolerance / 3 different，执行错误 0、重复稳定。
三类 expand 的 different 同属展开变换的浮点读数，实际调用状态与帧图已检查；真实系统屏幕阅读器仍未经验证。
产物位于 `artifacts/visual-parity/runs/<运行名>/summary.md`；用例与行为测试跟踪在仓内，未改变容差或参考。
