> **历史设计稿**：本文早于 2026-10 的动画基础重构，仅作历史参考，名称与行为可能已过时。现行用法见 [`docs/usage`](../usage/README.md)；迁移见 [迁移指南](../usage/migration-from-visual-animation-scope.md)。主要改名：`VisualAnimationScope` → `AnimationScope`（`AnimationScope.Get`），`SharedAnimationFrameClock` → 内部 `FrameClock`，`IAnimationFrameClock` → `IFrameSource`，`AnimationScope<T>` → `AnimationRun<T>`。

# 局部 Lookahead 实施状态

目标：落实 [设计稿](LOCAL_LOOKAHEAD_DESIGN.md) 与 [实施计划](LOCAL_LOOKAHEAD_IMPLEMENTATION_PLAN.md) 的 A–F 阶段。更新日期：2026-09-22。状态：本轮范围已完成并通过集成验证；所有 Lookahead API 仍为实验性。

## 已落地的架构

| 阶段 | 结果 |
| --- | --- |
| A：Context | 目标快照、订阅、版本、失效、重入保护、移动事务从 Root 移入内部 LookaheadContext；Root 保留有限视口兼容入口 |
| B：域继承与 Boundary | 内部可继承的 Context 附加属性传播稳定域身份；Boundary 的私有内容出口设置本地 null 截断外域，普通属性继承继续正常工作 |
| C：无 Root 自动入口 | AnimatedBounds 在普通父级下自动建立局部域；Measure 产生候选，Arrange 原子提交完整目标矩形；宿主原生槽位与动画内容层分离 |
| D：独立内域与协作 | Box、StackPanel、ScrollViewport、Size、Position 支持自动入口；实现 ParentLayoutMode、域归属迁移、完整约束缓存及内外域目标依赖 |
| E：Shared 与生命周期 | SharedBounds.RemeasureToBounds 支持无 Root 和跨局部域配对；目标转换到 SharedHost 坐标；handoff 验证域身份、目标版本、session generation 与实际 capture 几何 |
| F：公开 API 与扩展 | 几何 DirectProperty、ReferenceElement、Lookahead 查询/失效/事件门面、单策略协议、三个辅助基类、低层桥、示例与桌面验证 |

继承环境只是候选域。只有沿声明的参与边注册的控件才是域成员；普通子树不会被隐式穿透。Boundary 自身属于外域，其内容不在外域快照中。内容内支持自动入口的节点可以建立独立域，不需要重启开关。

Boundary 的原生内容按终点约束测量和排列，动画帧只改变承载层位置及外框裁剪。内容或内域仍可正常失效、更新；内部目标尺寸变化仅在影响对外 DesiredSize 时继续影响外域。

## 最终公开 API

- `AnimatedBounds`：`ParentLayoutMode`、`CurrentBounds`、`TargetBounds`、`IsRunning`、`ReferenceElement`；保留 `BoundsTransform`、`TargetSize`、`IntermediateLayout`、`ResetAnimation`。`ReferenceScope` 是新名称的兼容别名。
- `BoundsParentLayoutMode.TargetSize` 为默认值，父级获得终点尺寸；`AnimatedSize` 仅用于同域协作父级，可用中间尺寸推动兄弟布局。
- `Lookahead`：`GetSnapshot`、`TargetFor`、`InvalidateTargetLayout`、`InvalidateApproachMeasure`、`InvalidateApproachArrange`、`MoveContent`，以及 Direct 路由的 `TargetLayoutUpdatedEvent` 与 Add/Remove handler。
- `LookaheadLayoutSnapshot`：不可变目标结果、`Version`、`Viewport`、`Entry`、`CoordinateReference`。准备中或已失效的目标不通过公开查询返回。
- `LookaheadCoordinates.TryGetTargetBounds` / `TryGetActualBounds`：显式参考元素和 motion-frame 选项；Actual 表示宿主布局，动画显示几何读取 `CurrentBounds`。
- `ILookaheadLayoutPolicy`：一个 `MeasureLayout(context, constraints)`，通过 `LookaheadLayoutPhase` 区分 Native、Target、Approach，使用 Context / Result / Placeable / PlacementContext。接口同时具备完整约束与 placement host 能力。
- `LookaheadPanel`、`LookaheadDecorator`、`LookaheadControl`：自动入口辅助基类；已有继承体系使用 `LookaheadLayout.TryMeasureCore` / `TryArrangeCore` / `Forget`。

最终采用新增 `ILookaheadLayoutPolicy` 的方式保留旧 `ILookaheadLayoutNode`、`ILookaheadConstrainedLayoutNode` 和 `LookaheadMeasure` 双策略兼容路径。BoundsLayout 的单子项恰好测量一次契约保留；通用策略允许顺序重测同一子项，只有最新句柄有效。

目标快照只发布本次 placement 选中的分支；未选择的候选测量可被后续 placement 复用，不生成虚假的目标 Rect。跨阶段句柄、重复放置、逃逸的 placement context、布局期间修改子树和同域重入会被拒绝。目标通知抛出异常时，撤销公开目标、清理动画等待并允许下一次布局恢复。

## 兼容性变化与使用入口

1. `AnimatedBounds` 与 `LookaheadBoundary` 从 Decorator 改为 Control，使用原有 Child/Padding 属性 AddOwner，保留 XAML 内容语义。将它们强制转换为 Decorator 的代码需要迁移。用户 Child 的逻辑父级仍是公开控件，视觉及属性继承经过私有宿主。
2. AnimatedBounds 默认宿主 Bounds 表示原生布局槽位；显示矩形请读 CurrentBounds。旧的逐帧父尺寸行为需在协作父级下显式设置 AnimatedSize。
3. 普通内容通过 Boundary 接入，协作内容直接参与中间布局。AnimatedContentSize 和 SharedBounds.ScaleToBounds 保留普通布局路径，不会仅因兼容接口而主动建域。
4. Root 继续可用，但应用层不再必须创建 Scope/Root。公开查询不会触发布局。

当前说明：[AnimatedBounds](ANIMATED_BOUNDS.md)、[布局协议](LOOKAHEAD_PROTOCOL.md)、[实验概览](LOOKAHEAD_EXPERIMENT.md)、[SharedTransition](SHARED_TRANSITION.md)。

当时的 `Scratchpad/AnimatedBoundsDemo` 主界面使用自动入口；`--local` 展示普通 Grid 中的局部 AnimatedBounds + Boundary + 实际 Button。`--verify-local <目录>` 执行真实桌面几何、命中、焦点与布局次数验证。该工程和上述入口现已移除，当前演示与验证用法见 [Catalog](../../Catalog/README.md)。

## 验证证据

- 全库：`dotnet test ComposeAnimation.Test/ComposeAnimation.Test.csproj --no-restore --nologo -v quiet --logger 'trx;LogFileName=local-lookahead-final.trx'`，**729 passed，0 failed，0 skipped**。
- 整个解决方案及所有示例：`dotnet build ComposeAnimation.slnx --no-restore --nologo -v quiet`，**0 warnings，0 errors**。
- 最终二进制的真实窗口探针：`AnimatedBoundsDemo.exe --verify-local <目录>`，进程退出码 0，JSON 中 **11 项检查均为 true，success=true**；已检查起点、中间帧和终点 PNG。
- `git diff --check` 通过；测试结果及桌面验证生成物不进入源码变更。

回归覆盖：自然尺寸、无限测量与有限排列、Stretch、Margin/对齐/舍入、首次不动画、完整约束首次进入与 reparent、尺寸位置共同变化、反向与改目标、准备阶段直接重测、五类内外域变化、候选环境与实际成员区分、模板与生命周期、motion frame、移动事务、Shared 跨域交接和相同矩形的新目标代次。

真实桌面探针已验证：内容在动画中保持终点排版；宿主保留终点槽位；中间帧无需重测/重排 Boundary 原生内容；点击命中随显示位置移动且受裁剪；焦点持续保留；动画结束无残留帧等待。生成物位于 `artifacts/local-lookahead/`，已加入忽略规则。

## 当前边界

- 不预测任意普通祖先的未来布局，也不自动把普通 Grid/模板/虚拟化控件变为协作策略。需要完整约束时使用协议节点或显式 Boundary。
- 独立域不共享目标树。SharedHost 可配对不同域，普通目标坐标查询和 MoveContent 仍要求同域及有效参考关系。
- 目标几何不包含用户 RenderTransform；跨窗口、RTL 镜像及任意视觉变换不属于当前坐标协议。
- LookaheadScrollViewport 是协作滚动适配器，并非完整 ScrollViewer 替代；惯性、滚动条、虚拟化及 BringIntoView 等尚未适配。
- 内部 Context 附加属性负责框架域身份，不是应用层可以任意设置的启用开关。是否参与仍由协议能力、声明子项与 Boundary 决定。

## 工作区保护

开始实现时用户已有修改：`ComposeAnimation.Test/SharedBoundsBoundaryRealizationTest.cs` 与 `Scratchpad/SharedTransitionDemo/MainWindow.axaml`、`.axaml.cs`、`MainWindowViewModel.cs`。本次没有改写这些文件。未创建提交或 PR。
