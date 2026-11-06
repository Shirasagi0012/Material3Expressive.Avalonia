# 架构与共享能力

本文描述系统核心分层、共享引擎与基础技术契约，为全库的只读技术规范。具体控件的实现、上游基线映射与内部差异，严格保存在各组件目录下的 `INTERNALS.md` 中；跨组件的共享能力已确认决定见 [DECISIONS.md](DECISIONS.md)。

---

## 整体分层与目录职责

系统采用单向依赖结构，职责划分如下：

| 位置 | 核心职责 |
| --- | --- |
| [Baseline/](../src/Material3Expressive/Baseline) | 颜色调色板、形状、字体、状态、高度与动效的初始默认资源定义。 |
| [Tokens/](../src/Material3Expressive/Tokens) | 强类型 Token 键定义，分为参考级、系统级与组件级三层键。 |
| [Foundation/](../src/Material3Expressive/Foundation) | 跨组件复用的核心支撑能力，包含输入、交互、动效、排版、渲染、弹层与 Token 解析器。 |
| [Components/](../src/Material3Expressive/Components) | 具体控件的实现、主题样式与模板。由各组件目录下的 `README.md` 与 `INTERNALS.md` 自行维护。 |
| [Primitives/](../src/Material3Expressive/Components/Primitives) | 常见原生基础控件（如文本标签、窗口、网格分割条等）的 Material 默认样式。 |
| [MaterialTheme.axaml](../src/Material3Expressive/MaterialTheme.axaml) | 主题总装配入口，合并基础资源并引入各组件样式。 |

### Foundation 子系统职责

- **Input**：内部手势与速度追踪等底层指针支持。
- **Tokens**：Token 身份模型、资源树级联组合、引用链路解析与多重绑定转换。
- **Typography**：文字角色聚合、动态继承合并与文字度量换算。
- **Interaction**：指针与按键事件接入、状态层驱动、波纹动画编排与内缩焦点环呈现。
- **Motion**：Material 动效参数转换与过渡规格桥接。
- **Rendering**：可变字体图标渲染、几何路径生成与颜色算法桥接。
- **Overlays**：窗口内对话框宿主、模态遮罩与弹层管理。

---

## Token 与主题资源系统

Token 系统采用对象引用作为唯一身份标识，支持动态解析与局部覆盖：

- **键模型**：`MaterialTokenKey<T>` 采用实例引用作为键身份，名称仅作诊断用途。
- **资源组合与通知**：`MaterialResource` 支持多源合并，后注入的源优先级更高，源变化时向 Avalonia 资源系统广播失效通知。
- **解析机制与跳数限制**：`{m3:Token}` 标记扩展在消费元素处执行上下文解析，沿引用链路动态向上回溯，支持派生转换（如透明度叠加、弹簧规格派生等）。解析器设有最大跳数限制以阻断循环引用。
- **绑定优先级合约**：模板内使用 `{m3:Token}` 时默认赋予模板级优先级；手写多重绑定时，外层必须显式声明优先级，避免中间态覆盖低优先级样式。

---

## 排版系统

排版层通过角色聚合与就近合并提供统一的文字规格：

- **角色聚合**：使用 `MaterialText.Style` 指定文字排版角色，按需将字体族、粗细、行高与字号批量投影到目标控件。
- **默认值级联**：容器通过 `MaterialText.DefaultStyle` 逐字段向下游提供环境继承，已显式声明的字段优先，未声明字段保留原生继承。
- **单位换算**：排版配置统一以设备独立像素存储，比例缩放与相对单位在解析消费点执行换算，不替换原生文本排版引擎。
- **文字基线**：自定义槽根通过 `MaterialTextBaseline.First/Last` 发布相对自身顶边、排除 Margin 的 DIP 基线，供 Snackbar / RichTooltip 消费；属性不继承，变化使祖先重新测量。共享读取器处理显式值、原生文字、内容呈现器及内部可选提供接口；BadgedBox 通过该接口仅转发锚点的测量基线。显式覆盖优先，不推断任意容器内部布局。用法见 [Typography](../src/Material3Expressive/Foundation/Typography/README.md#自定义内容的文字基线)。

---

## 颜色、图标与阴影

- **颜色算法**：通过接入颜色工具算法生成调色板，`ColorScheme` 将算法计算结果桥接到系统 Token。
- **可变字体图标**：由底层图标库提供字体轴支持，`MaterialSymbol` 作为统一控件入口。`MaterialSymbol.DefaultSize` 作为可继承的尺寸提示属性，由各组件在呈现位置提供默认值，显式宽高优先。
- **阴影绘制**：基于 Skia 图元绘制阴影，支持平面的线段、二次曲线与局部轮廓；Surface 传入 DIP 几何，MaterialSkiaShadow renderer 统一生成物理像素路径并从 CTM 消去 density。固定原生曲线分支、blur fallback 与平台验证范围见 [决定 §20](DECISIONS.md#20-阴影坐标接入与固定原生曲线分支)。

---

## 表面容器与轮廓几何

`MaterialSurface` 是全库统一的承载容器，承担背景、边框、阴影与圆角过渡：

- **绘制管线**：底层先绘制阴影与背景几何，中间层处理内容与裁剪，顶层叠加交互反馈与内缩描边，避免内容圆角与阴影产生重叠伪影。
- **几何与形状插值**：形状支持绝对像素与百分比混合定义，在动画过渡中按比例归一化，各绘制层共享同一轮廓。
- **Tooltip 路径并集**：内部几何入口让 Tooltip 的容器和箭头共用背景、内容裁剪及路径阴影；不改变测量，也不扩展圆角描边/交互契约。阴影快照及多轮廓回退见 [决定 §27](DECISIONS.md#27-tooltip-并集轮廓与路径阴影入口)。
- **描边处理**：均匀圆角采用内缩算法计算外框，非对称圆角通过内外轮廓布尔相减绘制，保证高精度对齐。

---

## 交互反馈与状态驱动

- **事件流模型**：`MaterialInteractionSource` 提供 UI 线程上的配对事件流，结束事件引用开始对象，订阅从后续事件开始。`MaterialInteractionController` 将原生输入接入该流；组件独立维护形状、高度等收集状态，源替换和模板重建的生命周期见各组件内部契约。
- **状态层与波纹**：状态层按存活顺序选择交互，并以开始对象身份判断动画重启，同类型的新交互也会重新启动动画。波纹动画独立持有子生命周期，按压释放与取消时执行平滑淡出。
- **焦点环机制**：`MaterialInteraction.FocusFeedback` 提供透明度与内缩焦点环两种模式，独立呈现器负责动画与四角不等圆角绘制。Surface / Switch 切换模式时重建 indication；CheckBox / RadioButton / ListItem 更新同一 ripple 配置，保留活动反馈。配置与缓存契约见 [Interaction](../src/Material3Expressive/Foundation/Interaction/README.md)。
- **最小交互留白**：通过 `MaterialInteractiveSize.Minimum` 建立可继承的布局环境，为触摸与无障碍操作预留外层空间，不强行扩张绘制边界。

---

## 动效运行时接入

动画系统统一接入底层动画引擎：

- **作用域生命周期**：每个顶层窗口在初始化时调用 `AnimationScope.Initialize(topLevel)` 建立根时钟；控件通过 `AnimationScope.Get(owner)` 接入，并通过 `CreateChild(...)` 为独立动效派生子作用域。
- **属性输出**：`scope.BindAnimatedValue(target, property, source)` 以 Animation 优先级贡献采样；源/目标类型不同时使用带纯 `project` 的重载。释放恢复底层值。`SubscribeValue` 适用于绘制失效或普通运行时字段，不用它直接覆写动画目标的 Avalonia 属性。
- **重组消费的显示值**：`BindFrameCommittedValue` 与 `ScheduleFrameCommit` 共用现有帧末队列，先捕获本批显示值，再执行配置 effect；effect 的写入进入下一批。FAB 高度使用它区分运行时 snap 与已提交显示值，普通直接绘制消费不迁移。不是完整重组或渲染完成屏障。
- **规格转换**：Token 中的弹簧参数通过规格引用在消费点转换为求值器，支持平滑中断与重定向。
- **释放保证**：控件从视觉树断开或模板重建时，必须释放关联的子作用域，避免后台时钟泄漏。
- **帧末目标提交**：`AnimationScope.WaitForFrameEndAsync(ct)` 在帧内调用时等待当前采样批次结束，帧间调用时等待下一批次结束；不额外推进动画帧，随作用域及调用方取消。它不等待布局或渲染，也不提供 Compose 重组运行时。下拉刷新箭头用它合并最新透明度目标，仍由调用方管理目标与中断策略。
- **保留状态的提交模式**：`AnimatedState.Create(owner, value, AnimationCommitPhase.AfterFrame, AnimationInterruptionMode.Restart)` 对应固定上游 animateAsState 的帧末提交及先取消后重启。默认重载保留帧前提交与速度接续。导航选择轨迹和搜索顶栏颜色显式采用新模式；不是统一延迟所有动画，具体边界见 ComposeAnimation 的 animated-state 使用说明。

---

## 弹层与滚动体系

- **弹层宿主**：模态弹窗通过顶层窗口的 `PART_VisualLayerManager` 接入统一的覆盖图层，共享遮罩与输入阻挡行为。
- **平滑滚动**：为滚动容器提供物理惯性支持，滚动条与视口测量保留原生行为，通过统一的增量接入点处理位移积分。
- **视口联动**：应用栏等顶部栏目通过滚动作用域与主滚动视口建立协同，在统一帧周期内分配栏目收起位移与正文位移，实现无缝折叠联动。
