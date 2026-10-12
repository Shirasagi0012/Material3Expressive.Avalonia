> **历史设计稿**：本文早于 2026-10 的动画基础重构，名称已过时。现行名称见 `/workspace/m3e-anim-refactor/ROADMAP.md`：`SharedAnimationFrameClock` → 内部 `FrameClock`，`VisualAnimationScope` → `AnimationScope`（`AnimationScope.Get`），`AnimationScope<T>` → `AnimationRun<T>`。

# VisualAnimationScope 实施与验收记录

日期：2026-09-30。设计依据：[原始设计](draft/VISUAL_ANIMATION_SCOPE.md)。当前入口：[使用与破坏性迁移](usage/visual-animation-scope.md)。

本文保留实施时的包版本与打包记录。当前仓库接入和发布方式见[根 README](../../../../README.md#版本与发布)。

## 实现

- Runtime 提供 IAnimationValue 和不依赖 Visual 的内部生命周期契约。Animatable、Transition、InfiniteTransition 及其节点使用 Scope.Clock 时继承生命周期；保留独立显式时钟构造。
- Avalonia 提供固定根环境、每次新建的控件 Scope、逻辑子 Scope、内联 Launch、同步失效/异步等待清理、源身份诊断和受管值消费。
- 根帧分发按需共享原始时间戳，并固定 Normal 调度上下文。属性会话继续保留自己的帧前目标检查、同帧重定向与发布重入顺序。
- PopupRoot 通过 ParentTopLevel 解析根环境；派生环境结束后复用弹出宿主可以建立新的派生 Scope。Overlay 使用现有 TopLevel。显式 Lookahead.MoveContent 中的内部 AnimatedBounds 生命周期保留既有同步移动契约；公开 Create(owner) 的 Scope 仍随每次 detach 结束。
- 订阅先失效，动画贡献随后撤销，再取消运行。根主动结束后，内置布局消费者不通过排队布局任务重新创建动画。
- 诊断处理器错误隔离为根 LastDiagnosticError，继续处理其他处理器和清理。受管操作关系及保留的 await/EDI 异常传播轨迹参与去重，弱异常记录不持有永久 Scope 或任务历史；重新 throw 同一异常实例的独立操作仍得到新失败身份。
- Animated.SnapToTargetAsync 读取实际基础目标并立即结束当前属性贡献，不修改声明、规格或普通绑定。
- 播放速度通过 Scope.DurationScale / 继承的 Animated.DurationScale 表达。Catalog 保留 0.25×、0.5×、1× 和重置；原始帧时间戳不缩放。
- ComposeAnimation.TestSupport 独立输出手动单帧时钟、根环境测试窗口和有上限的 Dispatcher 条件等待。

旧的 Animated.FrameClock、FrameClock Get/Set、Animated.LastError / GetLastError 入口已移除。默认 Avalonia 帧源与 SharedAnimationFrameClock 改为内部实现，没有兼容别名。所有窗口示例在 XAML/Content 前初始化环境。

## 自动化结果

| 验证 | 结果 | 覆盖 |
| --- | --- | --- |
| ComposeAnimation.slnx Release 构建 | 通过 | Runtime、Avalonia、TestSupport、测试、Catalog、解决方案内 Scratchpad |
| ComposeAnimation.Test | 766 / 766 通过 | 既有布局/过渡回归及新 Scope 契约 |
| pack.ps1 -Version 0.2.0-alpha.1 | 通过 | Runtime、Avalonia、TestSupport 三个包；包清单的内部依赖版本一致 |
| 相邻 Material3ExpressiveRefactor 构建 | 通过 | 实际库、Catalog、测试工程，使用新 Scope 的本地 NuGet 包 |
| Material3Expressive.Tests | 163 / 163 通过 | state layer、elevation、Ripple、shape、禁用、模板替换、卸载与重挂 |
| 真实 Material 按钮连续 Ripple | 1,500 次通过 | 等待实际任务退出后，任务、子 Scope、订阅、绑定及帧等待回到首次交互初始化后的空闲基线 |
| Scope 临时效果压力 | 1,500 次通过 | 工厂、订阅、绑定、任务、子 Scope 和手动帧等待登记回收 |
| Catalog --run --all | 20 个动作全部通过 | 截图、逐帧时间与断言；既有 demo ID / action 保持 |
| Catalog --verify-ui | 40 项通过 | Windows 真实桌面窗口，浅深主题、导航、展开栏反转、速度、重置、最小窗口尺寸 |

Scope 专项还验证：同步启动和释放重入、排队 Snap 在结束后不提交、取消与 Faulted OCE 区分、混合聚合失败去重、此前创建的子任务被父任务等待时的去重、独立复用异常、诊断处理器失败保护、自身/祖先 DisposeAsync 拒绝、异步 finally 等待、初始值绑定同步结束 Scope、空属性名通知、基础属性贡献恢复、直接动画与属性动画共用一条根请求、帧源故障只报告一次且不重试，以及有限和无限播放策略变化。

Popup 首次动画、Overlay、嵌套弹出层、关闭和重开在 Avalonia Headless 中验证。Checkbox、ProgressBar 和 RotateTransform 的 Transition / InfiniteTransition 值消费在 ComposeAnimation.Test 中验证；这不表示 Material 新增了尚不存在的 Checkbox 或进度组件。

Catalog 两种自动化模式在加载内容前选择固定的手动根帧源；界面验证推进同一时钟，不在中途换钟。截图与运行日志位于本地 `artifacts/visual-animation-scope/`，该目录不纳入版本控制；抽查了展开中间帧的桌面截图。测试结果在各测试工程的 TestResults 目录。

## 验证边界

以上是源码、自动化测试及 Windows 桌面程序运行结果。未进行 Linux/macOS 原生弹出宿主的人工验收，也不声称与 AndroidX 的完整像素级视觉一致。任意业务代码忽略取消、自行丢弃内部任务，或异步清理重新读取已指向新模板的字段，仍由消费者负责。

Material 保留领域规则并移除通用 MaterialMotionController。该消费者通过已有本地 NuGet 源引用 `0.2.0-alpha.1` 的 Avalonia 和 TestSupport 包，Runtime 由包依赖传递引入；不再使用跨仓库 ProjectReference 或 ComposeAnimationSource 配置。`pack.ps1` 将三个包统一打包到 `packages/`，未推送到远程包源。切换后重新还原并构建 Material 解决方案，163 项测试全部通过；核对库、Catalog 和测试的 project.assets.json，ComposeAnimation 依赖均为该版本的 package，且没有对应 projectReferences。
