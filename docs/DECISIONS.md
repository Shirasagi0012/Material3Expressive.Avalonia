# 跨组件共享能力已确认决定

本文记录跨组件共享能力的已确认技术决定。组件私有机制、专属适配与确认依据严格随各组件的 `INTERNALS.md` 保存；架构基石见 [ARCHITECTURE.md](ARCHITECTURE.md)，测试端决定见 [VisualParity 测试目录文档](../tests/VisualParity/DECISIONS.md)。

---

## 1. 内缩焦点环的呈现与生命周期

- **状态**：生效中
- **决定方案**：通过可继承 `MaterialInteraction.FocusFeedback` 选择透明度或内缩焦点环模式，默认维持透明度；尺寸集中在不可变 `InsetFocusRingMetrics`，颜色可继承覆盖并在空值时消费实时次要色。独立内部呈现部件持有浮点动画器、子动画作用域、交互订阅与圆角环缓存。共享控制器提供配对指示事件，订阅者各自按桌面平台规则过滤焦点，原始交互仍驱动形状与高度。焦点环不占用唯一的 Surface 波纹挂载点；Surface 内置视觉子节点，Switch 在轨道单独承载。
- **原因与动机**：Switch 的焦点环与滑块波纹分属不同区域；原生焦点装饰层的显示与即时移除规则无法保留上游的输入模式过滤及退出弹簧。四角不等圆角由绘图上下文轮廓相减支持。纯视觉部件显式桥接继承上下文与资源通知，保留动态主题更新。
- **适用范围与影响**：圆角矩形反馈（含矩形与圆形）。任意路径、Slider 专用间距在范围之外。
- **2026-11-06 扩展**：本轮维护者授权自主补齐剩余移植及修订历史范围。原范围排除 Slider 是因其专用间距与双端源尚无适配；现已按固定 Slider.kt 在组件层补齐 4 DIP 外围空间及 Track 间距。共享呈现器可直接订阅既有端点源，避免再建输入控制器；其他调用方的控制器入口不变。任意路径仍不在本决定范围，无破坏性 API 迁移。
- **确认依据**：2026-10-17 维护者确认方案并要求实施。使用与覆盖边界见 [Interaction 说明](../src/Material3Expressive/Foundation/Interaction/README.md)。

---

## 2. 列表项的交互源与最小交互环境

- **状态**：生效中
- **决定方案**：提供公共 `MaterialInteractionSource`，使用按下、焦点、悬浮、拖拽开始对象与配对结束或取消事件；通过可继承的 `MaterialInteractiveSize.Minimum` 提供布局尺寸环境；Surface 增加内部关闭色调着色入口。
- **原因与动机**：ListItem 的外部拖拽、侧槽最小交互尺寸及自身颜色过渡需要这三项共享能力，组件内重复实现会割裂已有输入和绘制机制。
- **适用范围与影响**：ListItem 率先公开交互源，其他控件不自动增加新 API；最小尺寸接入已有相关布局机制，未设置时维持组件默认值。源不缓存状态，消费者维护自身存活事件；指示层重建只接收新事件，不影响独立形状状态。最小尺寸仅为布局环境，不增加自动触摸命中扩张，不改写组件绘制盒。关闭色调着色的入口保持内部可见。
- **确认依据**：2026-10-17 维护者在 ListItem 移植任务中确认包含以上共享能力的建议范围，并明确暂缺任意形状与内缩焦点环。组件详细契约见 [ListItem 内部说明](../src/Material3Expressive/Components/ListItem/INTERNALS.md)。

---

## 3. 图标槽与默认尺寸

- **状态**：生效中
- **决定方案**：图标槽统一采用对象与数据模板成对入口，保留槽的语义前缀；IconButton 保留专门的 Symbol API。`SymbolExtension` 创建 `MaterialSymbol`，`MaterialSymbol.DefaultSize` 作为可继承的默认尺寸提示，由组件在图标呈现位置提供。
- **原因与动机**：原有枚举、手动构建控件和缺少模板的入口难以一致使用；单一尺寸提示避免额外图标准备层或复杂呈现框架。
- **适用范围与影响**：默认尺寸只被 `MaterialSymbol` 的测量消费，显式宽高优先；保留各组件的布局约束、交互区域及动画。数据模板在内容为空时亦能创建图标，控件模板可复用而控件实例不可共享。完整按钮等交互槽继续组合实际控件。2026-11-03 维护者确认将原 MaterialIcon 合并到 MaterialSymbol：默认尺寸仅由 MaterialSymbol 及其派生控件消费，因此附加属性与静态访问器归入 MaterialSymbol，移除 MaterialIcon 类型；旧调用迁移到 MaterialSymbol，尺寸继承与测量语义不变。
- **确认依据**：2026-10-06 维护者批准统一槽与可继承默认尺寸设计；2026-11-03 维护者批准合并 MaterialIcon 到 MaterialSymbol。共享规则见 [架构说明](ARCHITECTURE.md#颜色图标与阴影)。
- **2026-11-08 本轮授权补充**：历史合并消除了只供 MaterialSymbol 消费的默认尺寸中间层，这个理由仍成立。此次新增的 `MaterialIcon` 是固定上游 Icon 的 IImage 呈现控件，负责位图/矢量 Fit、着色和图片语义，并不恢复旧附加属性类。旧 `MaterialIcon.DefaultSize` 调用仍按原迁移方式使用 `MaterialSymbol.DefaultSize`；新的图片入口使用 `MaterialIcon.Source`，两者不互相替代。

---

## 4. Padding 消费向嵌套 Scaffold 传播

- **状态**：生效中
- **决定方案**：`MaterialWindowInsets.ConsumePadding` 为控件提供逻辑方向的已应用内边距输入，按固定上游规则逐层累加，转换为物理边并向后代传播。输入本身不自动应用内边距；Scaffold 按物理边扣除祖先消费量并钳制到零。
- **原因与动机**：原有物理厚度转换不包含祖先消费环境，无法独自保留 Scaffold 的嵌套窗口插桩语义。
- **适用范围与影响**：Scaffold 槽与内边距消费的共享入口，不扩展为通用系统栏或输入法框架，不改变既有导航栏或导轨的行为。
- **确认依据**：2026-10-03 维护者确认采用推荐 API 和共享 ConsumePadding。具体示例见 [Scaffold 说明](../src/Material3Expressive/Components/Scaffold/README.md)。

---

## 5. 普通协程延时与动画帧时钟

- **状态**：生效中
- **决定方案**：ComposeAnimation 的根 `AnimationScope` 接入 .NET `TimeProvider`，子作用域和 Popup 继承；`DelayAsync` 使用同一时间源，并连接调用方及作用域取消。保留旧初始化重载默认使用系统时间。
- **原因与动机**：加载指示器并发运行弹簧与固定毫秒协程延时，延时并非底层动画核心求解器，不受动画帧或时长缩放控制。用补间动画或下一帧计时替代会改变循环及稀疏帧行为。
- **适用范围与影响**：普通协程等待与确定的测试时间源；不改写动画引擎算法，不引入完整 Compose 运行时。
- **确认依据**：2026-10-04 维护者在说明协程与动画职责后明确授权修改。

### 帧末目标提交边界

- **决定**：本轮依据用户扩大授权重新决定，新增 `AnimationScope.WaitForFrameEndAsync(ct)`，复用根 FrameClock 的当前批次状态和 UI dispatcher。帧内调用等当前批次，帧间调用等下一批次，随后允许调用方读取合并后的最新目标；取消同时连接作用域和调用方。
- **原问题及依据**：PullToRefreshArrow 原先无条件 NextFrame + Yield，在距离动画当前帧越阈值时晚提交一帧。固定 AndroidX/Core 的 CircularArrowProgressIndicator 相同，animateFloatAsState 经 SideEffect 在本帧采样后提交。当前双端回弹复现了该问题。
- **替代方案**：固定减帧或按 IsAnimating 猜测不能区分外部修改与帧内采样；另建调度器会重复根时钟；公开整个 FrameClock 无必要。新增能力只识别现有批次，不重写 Animatable 或重组系统，也不自动合并目标。
- **调用与范围**：`await scope.WaitForFrameEndAsync(ct);` 后读取最新目标并执行已有 AnimateToAsync。首个真实调用方为下拉刷新箭头；其他组件不机械迁移。FrameScope 最小运行接口与旧 NextFrameAsync 语义不变，无破坏性迁移。
- **验证**：作用域测试验证采样顺序、帧间等待、不追加帧、调用方取消和卸载；真实高回弹 State 验证越阈值首帧，既有合并/反向测试继续运行。帧末边界不是布局完成或渲染完成屏障。
- **分配回归的夹具修正**：全量及独立运行曾报每帧额外 14.7–18.6 B。独立计数证明测量区间混入 HeadlessRenderTimer 的实时 tick；现仅在性能测量中停止该定时器并在 finally 恢复，同时断言测量区间 tick 为零。手动帧→dispatcher→动画链照常执行，原每帧 1024 B 上限和增量 <8 B 门禁均未修改；修正后动画模块 938 项通过。

---

## 6. 最小交互布局与工具栏对齐读数

- **状态**：生效中
- **决定方案**：IconButton 和 Button 共用已有 `ButtonLayout`，尺寸 Token 提供可见容器的默认尺寸，外层默认分配至少 48 DIP；显式宽高与最小宽高保留调用方约束。`ButtonLayout` 与 `FabLayout` 提供内部最小交互对齐读数，Foundation 合并后代读数，浮动工具栏按上游平衡内边距规则动画补齐两轴留白差。
- **原因与动机**：原有小型图标按钮缺少外层布局分配，工具栏也无法读取可见容器和交互分配的差值。此处仅预留布局空间，不实现额外的触屏目标扩张。
- **适用范围与影响**：Button、IconButton、FAB 的内部读数与布局；颜色、描边及选择语义不随之改变。
- **确认依据**：2026-10-04 维护者接受浮动工具栏方案并要求完成相关回归。

---

## 7. Dialog 与 Popup 的窗口内宿主与模态职责

- **状态**：生效中
- **决定方案**：组件各自持有原生 Popup，显式启用图层覆盖选项，接入当前顶层窗口的覆盖图层，不创建独立操作系统窗口，不引入中央管理器。内部模态呈现器复用客户区覆盖、焦点约束与还原、退出按键与清理；组件保留各自的状态与动画。基础 Dialog 区分打开目标与实际呈现状态，请求关闭事件交给调用方决定，退出动画期间保留模态层。
- **原因与动机**：复用弹层定位与承载，同时支持嵌套菜单和模态组件共享输入语义。
- **适用范围与影响**：基础对话框与模态宽导航轨；不替代导航轨的拖动与遮罩，不包含文本输入框。
- **确认依据**：2026-10-04 维护者明确选择覆盖图层宿主、拒绝独立窗口、认可组件独立持有弹层。接口与细节见 [Overlays 说明](../src/Material3Expressive/Foundation/Overlays/README.md)。

---

## 8. Dialog 的平台宽度和 Escape 时机

- **状态**：生效中
- **决定方案**：对话框默认启用平台默认宽度属性，按客户区短边自适应选择 320、440 或 580 DIP 平台上限；显式关闭该属性可保留宽内容行为。对话框系列对未处理的 Escape 按下事件发送关闭请求；内部控件已处理该按键时不重复发送。
- **原因与动机**：上游基线提供平台宽度限制，桌面端在按键按下阶段处理未消费的退出指令。
- **适用范围与影响**：各类对话框组件；不改变导航轨的动画和关闭时机。
- **确认依据**：2026-10-04 维护者批准默认平台宽度属性及对话框对齐按键按下时机。细节见 [AlertDialog 内部说明](../src/Material3Expressive/Components/AlertDialog/INTERNALS.md)。

---

## 9. Dialog 的首帧与退出中重开

- **状态**：生效中
- **决定方案**：对话框按上游基线帧顺序启动动画；退出保留冻结内容和独立动画作用域，重开创建新入场，旧冻结层继续退出。入场中关闭时保留关闭重组帧的最终入场进度，退出时长按该进度比例缩减。
- **原因与动机**：双端用例确认，提前启动、取消旧退出层或提前截断入场会改变时序与叠层画面。
- **适用范围与影响**：通过位图冻结内容并启用整层透明度合成，保持背景与内容一起淡入淡出语义。
- **确认依据**：2026-10-04 维护者批准首帧对齐与保留旧退出层。

---

## 10. 聚合 TextStyle 与 Token 层文字单位换算

- **状态**：生效中
- **决定方案**：通过不可变文字样式、文字角色绑定及派生标记扩展，让普通文本控件消费聚合排版，不引入公开的专用文本控件。默认样式合并环境，显式样式直接生效。相对单位在 Token 消费位置换算为像素，保持资源覆盖与字体缩放的动态性，样式对象本身仅保存像素值。
- **原因与动机**：保持样式选择、合并与提供默认值语义，同时通过属性优先级保留原生覆盖。
- **适用范围与影响**：聚合排版接口与当前可投影字段。使用方式见 [Typography 说明](../src/Material3Expressive/Foundation/Typography/README.md)。
- **确认依据**：2026-10-03 维护者认可接口方案并要求落地。

---

## 11. ComposeAnimation 颜色转换与原始动画终值

- **状态**：生效中
- **决定方案**：在动画引擎中修正颜色转换器，保留公开接口，按上游转换路径保留矩阵计算、精度边界与通道存储。基于目标的动画保留原始起点和目标，结束时严格返回原目标，避免有损颜色往返改变终值。
- **原因与动机**：上游中间色彩空间转换可能微幅改变动画颜色，反向插值时暴露色值偏移。
- **适用范围与影响**：基础色彩动画接口，统一引擎依赖版本。
- **确认依据**：2026-10-03 维护者批准修复方案。

---

## 12. 共享交互反馈与键盘激活职责

- **状态**：生效中
- **决定方案**：`MaterialInteractionController` 的键盘输入处理只维护交互反馈，激活由控件自身负责。共享控制器不拦截 Enter，不通过自动化接口代为执行点击，亦不维护组件类型排除名单。
- **原因与动机**：反馈接入不应改变应用命令的执行时机；接管按键会与原生空格键和导航项的键盘处理产生冲突。
- **适用范围与影响**：共享控制器的按键反馈接入遵循上述职责；普通按钮保留原生激活时机，空格键遵循原生点击模式。
- **确认依据**：2026-10-03 维护者要求移除共享控制器中非必要的按键拦截处理。

---

## 13. Ripple 首次绘制几何与尺寸动画衔接

- **状态**：生效中
- **决定方案**：波纹动画在首次绘制时锁定起始半径、原点与目标中心。Switch 启用内部跟随中心开关，其无界按压波纹每次绘制都跟随表面当前中心，与变化中的滑块及光圈保持同心；其他组件保留基线的中心锁定和有界漂移。
- **原因与动机**：首绘锁定的小尺寸中心会在滑块放大或移动后产生错位。
- **适用范围与影响**：波纹绘制计算与中心微调，不向外部暴露多余接口。
- **确认依据**：2026-10-03 维护者选择修复波纹差异；2026-10-04 维护者进一步确认滑块同心优化方案。

---

## 14. Surface 圆角描边

- **状态**：生效中
- **决定方案**：内部表面轮廓部件对均匀边框厚度直接移植基线的像素取整与内缩描边；描边作为内容上方的视觉层，不挤占内容布局尺寸。均匀圆角采用内缩算法，非对称圆角以外轮廓减去内缩轮廓进行绘制。非均匀厚度保留原生边框路径。
- **原因与动机**：深色静态用例表明原生边框不符合固定上游的像素取整与角部渲染。
- **适用范围与影响**：所有使用 Surface 描边的组件与动画过渡。
- **确认依据**：2026-10-03 维护者批准表面描边修正；2026-10-04 维护者批准补齐非对称圆角算法。

---

## 15. Surface 圆角求值与背景裁剪

- **状态**：生效中
- **决定方案**：内部以形状对象表达绝对像素与百分比圆角。普通像素值不提前截断，静态与动态轮廓都在求值插值后统一归一化。表面负责绘制阴影和背景，内部容器只裁剪内容，独立描边、交互层与色调继承保持原有层级。
- **原因与动机**：提前截断会导致大圆角动画偏离上游；背景置于自身圆角裁剪内会使边缘半透明混合产生重复叠加伪影。
- **适用范围与影响**：Surface 及其形状动画调用方。
- **确认依据**：2026-10-11 维护者审阅方案后要求实施。

---

## 16. Core Desktop 原生焦点反馈

- **状态**：生效中
- **决定方案**：共享控制器观察所有原生焦点事件；指针按下反馈在原生处理之后观察，保留焦点到按下的交互顺序。同时按桌面端规则过滤指示层的焦点源：首次鼠标聚焦不进入状态层，已经显示的键盘焦点层保留到失焦；输入模式改变不合成虚拟焦点事件。
- **原因与动机**：上游移动端与桌面端对点击时获取焦点的默认行为不同，需对齐桌面端行为避免点击后错误恢复悬浮高度。
- **适用范围与影响**：所有接入共享控制器的组件交互反馈。
- **确认依据**：2026-10-04 维护者批准按桌面端基线修正共享焦点反馈。

---

## 17. AppBar 与原生滚动接入

- **状态**：生效中
- **决定方案**：标准 Scaffold 模板内置滚动作用域，自动引用实际呈现栏目的滚动行为。公开栏目状态以物理像素表达，正文输入接力和惯性由平滑滚动引擎接入，直接拖动栏目及其定位保留动画引擎驱动。作用域仅连接正文槽内唯一的主滚动视口，移出或替换时释放连接。
- **原因与动机**：保证标准栏目与正文滚动的紧密联动，同时支持模板自定义与生命周期管理。
- **适用范围与影响**：标准 Scaffold 与应用栏组件联动。
- **确认依据**：2026-10-04 维护者批准接入方案；2026-10-06 维护者确认行为与内部作用域的接口形式。

---

## 18. 普通物理滚动与 AppBar 的合成接入

- **状态**：生效中
- **决定方案**：普通滚动容器使用自有主题与平滑滚动呈现器，顶栏与物理正文共享滚动事务、动态范围和视觉输出。顶栏只返回实际消费的位移，剩余量在同一帧交给正文；底栏观察正文消费并执行弹簧吸附。主正文内的嵌套滚动器独立滚动，到边界后下一笔输入才驱动正文与栏目。
- **原因与动机**：在保持原生控件契约的同时，提供物理惯性与无缝折叠联动。
- **适用范围与影响**：物理主视口、内置滚动行为与标准栏目模板。
- **确认依据**：2026-10-04 维护者明确授权实现；2026-10-05 维护者批准完成公开协议与辅助接口的命名规范化。

- **搜索顶栏扩展（2026-11-09 本轮授权）**：原范围只含普通 Top/BottomAppBar，因为搜索的 DIP 状态和整笔消费不能套用普通栏适配。此次维护者授权必要共享能力及修订历史决定；在同一连接/参与器加入搜索分支，Scaffold 自动选择实际呈现的普通或搜索顶栏。搜索严格保留整笔 pre 消费、reverse post 剩余量和 contentOffset 生命周期；旧普通顶栏的实际位移消费仍只适用于普通栏。正文惯性、嵌套视口隔离及支持范围沿用本决定；搜索自定义 fling 仍用于直接拖动/显式 post-fling，自动 snap 支持 spring，非 spring 不接入。没有破坏公共 API，新增 `AppBarScrollScope.SearchScrollBehavior`；布局与输入例见 SearchBar README。

---

## 19. Tween / Bezier 精度：历史保留与固定算法补齐

- **状态**：2026-11-06 部分替代；未迁移的 double 规格仍单独报告精度差异。
- **历史决定方案**：保留动画引擎单精度浮点运算与特定求根路径引起的极微小数值差异（约 0.0000006 DIP 的高度微差）。原始动效事实继续精确比较并如实记录差异。
- **原因与动机**：底层数学库的浮点实现与上游存在极微小舍入差别，不影响可见视觉效果与时序。
- **适用范围与影响**：微小数值精度记录，不接受时序、布局或可见效果的其他偏离。
- **确认依据**：2026-10-04 维护者确认保留该精度差异记录。

- **本轮依据用户扩大授权重新决定**：重新读取固定 AndroidX/Core 的 `Easing.kt`、`Bezier.kt`、
  `MathHelpers.kt` 与 `FloatAnimationSpec.kt` 后，确认差异包含本地 Newton/二分替代上游 Cardano 求根、
  Float/Double 运算边界和加权 lerp 的实现差异，不能全部归因于底层数学库。
  `CubicBezierEasing` 改为固定上游的求根顺序、fastCbrt、1 ULP 输入平移和垂直极值限制；
  `TweenSpec<float>` 使用内置 `FloatConverter` 时保留 Float 纳秒比例、加权 lerp 和速度差分。
- **方案与影响**：`new TweenSpec<float>(duration, easing: ComposeEasings.FastOutSlowIn)` 无需新参数；
  FAB 高度是真实调用方。没有把所有 double 向量强行缩为 Float，因为 Avalonia 的几何/自定义转换器
  已依赖其精度；`TweenSpec<double>`、公开标量/向量规格构造及未知转换器保持 double 插值。
  不保留旧求根分支作为兼容开关，因为它会继续阻止固定算法对齐且没有已知独立需求。
  公共签名和控制点相等性不变；外部调用方无需迁移源码，但依赖旧数值快照者应重新核对采样。
- **验证依据**：`animation-tween` 双端 self-test 的 10 例、150 个采样全部精确相等且重复稳定，
  包含延迟、零时长、退化曲线、平坦端点、上下过冲、速度和实际图像；本地 `EasingsTest`
  固化固定 Core 的独立采样，并检查 double 插值仍保留 2²⁴ 以上的单位变化。
  该结论不扩张到 spring、关键帧或其他仍使用 double 插值的规格。

- **共享波纹接入：本轮依据用户扩大授权重新决定。** 固定两端 `material3-ripple/RippleAnimation.kt`
  的三个 Animatable、半径和中心均为 Float；本地 Double 动画及 DIP 几何使实际中间读数不同。
  复用上述 Float tween，内部在实际 density 的物理像素中保留 `ui-util.lerp` 加权运算与 Float 距离，
  绘制时才换回 DIP。公开 `RippleAnimation(origin, bounds, scope, bounded, radius)` 签名与 DIP 单位不变；
  组件 controller 通过内部入口提供实际 density。全库强制缩窄 Double 求值器会影响无关几何，未采用。
  六个真实 Surface 双端场景的结构化读数由不同变为精确相等，119 项波纹/交互/生命周期/开关回归通过；
  Catalog 提供边缘按压和短按示例。重叠波纹的 Core 被动截图不稳定仍保留，非 Windows 与运行中 DPI 切换未经验证。
  不扩大 §13 的首绘锁定和 Switch 跟随中心适配；无需源码迁移，精确数值快照需要按固定上游重核。

- **Float spring 补齐：本轮依据用户扩大授权重新决定。** 上述历史许可未证明 spring 的参数和
  时间边界等价。固定两端 `FloatAnimationSpec.kt` / `SpringSimulation.kt` 先以 Float 相减，
  参数也是 Float，采样时间截为整毫秒，解析求解保留 Double 后输出 Float。本地此前只有最终
  converter 缩窄，Chip 161/176ms 圆角存在 1 ULP 差异。
  仅 `SpringSpec<float>` + 内置 `FloatConverter` 进入等价路径，复用现有求解器和时长估计；
  其他规格/转换器保持原 Double 契约。替代的组件圆角取整会掩盖求解差异，全引擎缩窄会丢失
  Avalonia 几何精度，均不采用。用法为 `new SpringSpec<float>(.9, 700.25, .01f)`，无源码迁移；
  精确样本和亚毫秒时间行为变化需更新调用方快照。
  `spring-float-current` 六例 90 个采样全部 equal 且稳定，含值、速度、时长、完成状态和实际图像；
  `chip-spring-current` 的实际禁用/反向形变圆角与高度精确一致、图像在原容差内，不能推论其他
  未运行组件全部一致。固定数值回归同时确保 Double 保留亚毫秒求值和 2²⁴ 以上单位精度。

---

## 20. 阴影坐标接入与固定原生曲线分支

- **状态**：2026-11-06 替代坐标接入方案；未验证平台继续单独报告。
- **历史决定方案**：保留阴影库设备独立像素路径及变换矩阵与上游物理像素路径的光栅化渲染差异。公开高度数值、光照与布局保持一致。
- **原因与动机**：原生路径细分器对路径坐标的离散敏感性导致两端存在细微光栅差异。
- **适用范围与影响**：阴影光栅比对差异记录，不影响其他属性。
- **确认依据**：2026-10-04 维护者确认保留并记录阴影光栅差异。

- **本轮依据用户扩大授权重新决定**：当前 FAB 高高度阴影与 Surface 透明轮廓存在可见差异，
  超出了原决定中“细微光栅差异”的依据。独立调用固定 Skiko `0.152.0-alpha02` 的 `ShadowUtils`
  发现二次曲线与控制点/终点折线的阴影逐像素相同，对应固定 `SkShadowTessellator::handleQuad`
  的 `!SK_GANESH` 分支；原本认为该产物采用 Ganesh 细分的假设不成立。
  同一个 72 DIP 圆形的原生 ambient 绘制，与固定源码规定的 blur fallback 逐像素一致，
  说明“所有圆形必须生成有效网格”的旧测试断言也不成立。上述证据由独立 `NativeShadowTests.kt`
  验证，不读取本地组件或预期截图。
- **方案与替代方案**：共享 tessellator 保留非 Ganesh 控制点顺序、轮廓分类、inset 和失败回退；
  renderer 按固定 Core 的 physical-pixel outline 生成路径，并从 CTM 中仅消去 density。
  只切换曲线分支仍保留 DIP 路径曾引入其他失败，不能据此否定原生分支；同时核对坐标后，
  `surface-native-curve` 的 55 例由 10 个 different 改为 26 equal / 29 withinTolerance，全部重复稳定。
  不采用人为增加 alpha、调整容差或按 elevation 特判，因为问题来自可直接保留的路径语义。
- **调用与影响**：`MaterialSurface.ShadowElevation = 40` 及 `ComposeShadowRenderer.Draw(...)` 不变；
  调用方仍传 DIP，由 renderer 统一换算，不能自行再乘 density。无源码迁移；依赖旧阴影外观的
  应用应重新核对截图。GPU 快速解析绘制仍保留独立原生算法；当前验证是 Windows raster，
  未把历史 GPU 探针或本次 raster 结果追认为当前所有 GPU/平台已验证。

---

## 21. 文字样式基于原生继承

- **状态**：已批准方案
- **决定方案**：`MaterialText.Style` 把已指定字段以样式优先级写入元素自身的原生继承属性，由原生属性继承机制传递给子孙元素；废除作用域内层层投影样式的做法。部分样式采用合并语义，未指定字段继续继承；需要替换时传入完整角色样式。
- **原因与动机**：原生继承天然覆盖内容呈现器的多层嵌套，旧实现逐层投影样式阻断了原生继承链并带来诸多变通补丁。
- **适用范围与影响**：Foundation 排版模块与所有文字排版使用者。
- **确认依据**：维护者批准重构方案，待全量迁移后标记为生效中。

---

## 22. ComposeAnimation 的动画属性贡献

- **状态**：生效中
- **决定方案**：动效引擎驱动 Avalonia 属性时，采样必须以动画优先级贡献，严禁使用普通属性赋值。沿用单向采样绑定与独立撤销机制，释放后恢复底层绑定、样式或本地值，不覆盖原有属性源。
- **原因与动机**：普通赋值无法在动画结束后正确恢复控件先前的样式或绑定值，破坏了 Avalonia 属性系统的优先级覆盖契约。
- **适用范围与影响**：所有依赖动效引擎驱动属性的组件动画。
- **确认依据**：2026-10-14 维护者明确拒绝使用普通赋值，并确认必须采用动画优先级贡献形式。
- **2026-11-05 接入补齐**：本轮依据用户扩大授权，为现有 `BindAnimatedValue` 增加纯采样投影重载，解决 Float 动画到 Avalonia double/Transform 属性的类型转换，不另建动画、观察器包装或时间通道。SnackbarHost 的 opacity/scale、ChipIconSlot 的 opacity 与 FloatingToolbar 的 shadow elevation 迁移到该入口；移除会覆盖底层值的初始化赋值。动画中仍覆盖普通配置，作用域释放后恢复其最新值。源算法、规格、采样和中断顺序保持不变，已有同类型重载继续可用；无调用方迁移要求。Catalog 的 Point→X/Y 及组件卸载/过渡重建回归验证实际调用。

---

## 23. AnimatedVisibility 的协作式布局与状态所有权

- **状态**：生效中
- **决定方案**：显隐过渡状态由各效果宿主持有，模板布局仅负责连接并应用；模板替换时保留动效。可见性节点直接加入祖先作用域，目标隐藏时外层保持零占位，内部退出内容的完整几何予以保留。
- **原因与动机**：避免退出期间的实际收缩尺寸污染共享元素的最终目标，同时保留已有隐藏测量契约与动画中断连续性。
- **适用范围与影响**：可见性容器与相关过渡效果。
- **确认依据**：2026-10-16 维护者确认类型与调用顺序方案并要求实施。
- **AnimatedContent 方向适配（2026-11-09 本轮授权）**：新增独立 SegmentedButtonIcon 的不同尺寸双槽验证发现，ContentSlotPanel / ContentViewport 在原生 Arrange 中先按 RTL 解读 Start/End，随后 Avalonia Visual 又对 RTL 子树施加镜像，导致内容错误地靠向另一侧。保持原有显隐状态所有权与完整退出几何；仅这两个布局点改用逻辑 LTR 坐标，由原生镜像完成方向解析。变换与动画参数不变，也不改变 EnterExitMotion.Align 的物理坐标定义。检查现有 Crossfade 消费者 SegmentedButton、PullToRefresh 与 ClockFace，无手工反向 alignment 补偿。不同尺寸 Start/End/Center 的 6 项方向回归先复现两个 RTL 失败，修后共享 AnimatedContent/Crossfade 100 项测试通过。依据为固定 Core AnimatedContent.kt 的逻辑 Alignment 及锁定 Avalonia Visual.InvalidateMirrorTransform；此次修订已由维护者明确授权。
- **AnimatedVisibility 物理效果层（同轮授权）**：FloatingToolbar 公开过渡工厂的非对称双端 marker 发现 RTL 的 Start 展开裁掉了错误一侧；Surface.Bounds 的旧测试没有包含原生镜像，未能发现此问题。固定 Core EnterExitTransition.kt 明确以 LayoutDirection.Ltr 计算尺寸对齐，再以 placeWithLayer 放置物理 offset，scale pivot 也属于物理坐标。仅将 AnimatedVisibility 默认模板的动画 layer 固定为 LTR，内容 presenter 显式绑定宿主 FlowDirection；内容仍按其方向布局，效果不再受原生镜像反转。保留原状态、测量与退出几何决定，不改变 AnimatedContent 的逻辑槽对齐，也不扩大到其他效果宿主。新增 Start/End/Slide/Scale × LTR/RTL 的真实 TransformToVisual 测试先复现 4 个 RTL 失败，修后完整 ComposeAnimation 1010 项通过。调用方 FloatingToolbar、DatePicker、Expander、SearchBar 没有方向补偿；SegmentedButtonIcon 原单槽的物理 LTR 设置仍兼容，无公开 API 迁移。

---

## 24. 自定义内容的显式文字基线

- **状态**：生效中；2026-11-05 本轮依据用户扩大授权重新决定。
- **原决定与问题**：Snackbar 原移植将手动基线覆盖留作 backlog，不建立共享 AlignmentLine 框架；当消息为复杂容器、动作有文字基线时，Legacy 布局会出现异常对齐。RichTooltip 已有原生 BaselineOffset 入口，两个组件各自读取文字及 ContentPresenter。原始依据保留在 Snackbar INTERNALS，未追认为过去已批准新 API。
- **决定方案**：新增 `MaterialTextBaseline.First/Last` 附加属性，供自定义内容根发布实际首末基线；共享原生文字/内容呈现器读取器由 Snackbar 与 RichTooltip 使用。`SetFirst(content, 14); SetLast(content, 34);` 表示根顶边下 14/34 DIP，Margin 在消费时加入。属性不继承；null 恢复自动读取；变化使祖先重测，支持尺寸不变的基线更新。保留原生 BaselineOffset，显式 First 优先。
- **替代方案**：三项 Snackbar 专属覆盖属性无法复用到 Tooltip；全布局树的 AlignmentLine 合并/传播需要重建任意面板定位规则，当前两个显式入口没有该需求。采用受限共享事实与既有组件布局，标准 Button 模板度量仍属 Snackbar。
- **证据与影响**：固定 AndroidX/Core Snackbar.kt 的 FirstBaseline/LastBaseline 分支及 Tooltip.kt 的 paddingFromBaseline 构成消费契约；真实两组件、XAML/绑定、动态更新和双端几何用例验证该入口。无现有公开 API 删除；已有原生文字/原生 BaselineOffset 不需迁移。自定义内容需发布真实布局所得基线，不能拿常量伪造实际文本；未发布时仍保留上游缺基线分支。任意容器自动传播、Badge 文字判断及文字栅格一致性不在此决定范围。
- **2026-11-05 BadgedBox 接入：本轮依据用户扩大授权重新决定。** Badge 原 10 月决定因全库 alignment-line 成本推迟转发，历史依据保留在其 INTERNALS。现在真实 Snackbar/RichTooltip 组合可以使用既有读取器；增加内部可选 `IMaterialTextBaselineProvider`，仅由了解自己测量布局的 BadgedBox 实现，按固定两端 Badge.kt 转发锚点而排除徽标。固定高度的居中偏移从当前测量尺寸得出，支持嵌套、原生多行文字、动态基线和内容替换；显式覆盖仍优先。用法为 `new Snackbar { Content = new BadgedBox { Content = text, Badge = badge } }`，无需手工同步基线。替代的任意树遍历无法知道布局语义，逐实例绑定则重复组件内部知识，均不采用。无需调用方迁移；旧手工覆盖可清除以恢复转发。仍不提供任意 Panel 的自动传播或文字栅格等价保证。


## 25. AnimatedState 的提交阶段与取消后重启

- **状态**：生效中；本轮依据用户扩大授权重新决定。
- **原决定**：NavigationBar / Rail / CommandBar 于 2026-10-15 采用 AnimatedState 的帧前采样与速度接续，目标是复用长期状态及属性贡献，不再为每个控件包装任务。当时明确未证明固定 Compose 中断轨迹一致；原由保留在组件说明与 ComposeAnimation 的 ANIMATED_STATE 设计文档。
- **证据**：固定 AndroidX `f3ed195f…` / Core `a9e47e8…` 的 `animation-core/AnimateAsState.kt` 通过 SideEffect 更新；`restartAnimation` 在新 `animateTo` 前取消旧 Job，`Animatable.endAnimation` 清零速度和 lastFrameTime。当前导航源禁用场景复现 128ms alpha 197/165；只改帧末提交解决首次禁用，但 256ms 恢复仍为 161/189。再保留取消顺序后，两处颜色读数相等，原超差图像进入既有门禁。
- **决定**：为 AnimatedState 增加显式 `AnimationCommitPhase.AfterFrame` 及独立 `AnimationInterruptionMode.Restart`；每种阶段共享根队列与唤醒，复用既有 FrameClock/dispatcher 的帧末边界。旧动画先采样；Restart 等其取消清理后再起新运行，从新真实帧计时。重入、断开、Scope 恢复与异常仍走原有所有权协议。
- **用法及范围**：`AnimatedState.Create(owner, initialColor, AnimationCommitPhase.AfterFrame, AnimationInterruptionMode.Restart)`。NavigationBar/Rail 与复用它们轨迹的 CommandBar 切换项、AppBarWithSearch 的容器颜色采用此组合。其他调用方及原重载继续 BeforeFrame + Continue；不扩大到 Transition、动画输入状态或完整 Compose 重组。
- **替代方案**：统一延迟所有动画会破坏已有布局目标合同；组件各自 NextFrame/Stop 会重复状态合并、异常和生命周期；只延后首帧不能保留取消顺序，不能修复反向轨迹。两个选项分别表达提交与中断，避免把速度策略隐含在阶段名称里。
- **显隐入口扩展（2026-11-09 本轮授权）**：原范围不含 Transition，是为了不改变其显式状态/时钟所有权，理由仍保留。新 FloatingToolbar 公开过渡工厂的双端用例明确复现默认 AnimatedVisibility.Visible 比 Core boolean composable 提前一帧；新增宿主级 TargetCommitPhase，默认 BeforeFrame 保留即时目标，AfterFrame 复用同一根队列合并并捕获本地 Visible 请求。固定 Core AnimatedVisibility(bool)→updateTransition.animateTo 在 composition 更新目标，随后注册帧循环；新模式只投影此提交边界，Source 仍独立拥有目标和时钟，不改 Runtime.Transition.UpdateTarget。内部工具栏移除自己的下一帧等待并采用显式新模式；DatePicker/Expander 及其他默认调用方不需迁移。帧末重入属于下一批，卸载/换源撤销队列；行为与真实两端工厂用例验证，仍不提供完整重组运行时。
- **兼容及边界**：原有公共构造函数和全部工厂重载签名保留，无外部强制迁移。采用新模式才改变时序。非帧输入后的原生 enabled 即时读数与 Core 重组缓存仍分别保留；不为它们延迟原生交互，也不将残留差异改记为通过。真实消费者、模块行为回归和双端 self-test 是验证依据，未改变基线或容差。

## 26. 重组消费的动画值与帧提交 effect

- **状态**：生效中；本轮依据用户扩大授权重新决定。
- **原问题及依据**：FAB 原设计直接绑定 Animatable 值，配置单独等待下一帧；历史未证明呈现阶段等价。
  固定 AndroidX/Core `FloatingActionButton.kt` 在重组中读取 `shadowElevation(...).value`，随后
  `LaunchedEffect(this)` 调用 `updateElevation` / snap。当前双端复现 224ms 参考仍约 39.23、本地已为 5。
- **决定**：扩展既有 AnimatedState 帧末队列，以 `BindFrameCommittedValue` 捕获本批全部显示值，
  `ScheduleFrameCommit` 随后执行配置 effect。effect 写入运行时值立即生效，但显示进入下一批。
  重入不加入当前快照；同根共享唤醒，scope/返回句柄释放取消待提交项并恢复底层属性。
  首值立即生效，不提供完整 Compose 重组、布局屏障或源替换的节点重建协议。
- **用法与替代方案**：`scope.BindFrameCommittedValue(surface, MaterialSurface.ShadowElevationProperty, value, v => (double)v)`；
  `scope.ScheduleFrameCommit(updateConfiguration)`。仅额外 NextFrame 会使间隔内输入读取旧值；各组件复制呈现队列
  会重复生命周期与唤醒；统一改变 BindAnimatedValue 则破坏直接绘制消费，因此均不采用。
- **验证与影响**：FAB 普通/Extended/Content 的配置、连续替换、effect 后立刻输入已实际双端验证；
  模块覆盖注册顺序、帧内加入、重入、取消、底层绑定恢复及异常。现有 API 无强制迁移，FAB 的时间线
  不增加帧。投影失败仅释放自身贡献并上报，不能取消同批其他绑定或 effect。
  后续 §19 的固定求根/Float 插值和 FAB 自身换源提交分别消除了已观测的高度余差及换源当帧差异；
  大高度阴影另由 §20 的原生分支与坐标修正解决，不扩大历史接受范围。换源连接的组件职责及调用方时序迁移见 FAB INTERNALS。

- **Button / Card / Chip 接入：本轮依据用户扩大授权重新决定。** 固定 AndroidX/Core 的三个组件
  都在组合读取高度后启动目标 effect。旧 Card/Chip snap 临时撤销绑定并写普通属性，导致底层绑定
  被覆盖；旧 Button 禁用首帧提前呈现新高度。复用本节两个入口并保留 Float Dp 运算，移除组件内
  手工 NextFrame/解绑/重连补偿。Button 取消并等待旧 effect，Card 保留目标高度反推，Chip 保留
  成功完成后更新 lastInteraction 的区别，不合成通用高度控制器。
  用法仍为 `chip.Elevation = new(2, 3, 4, 5, 9, 1)` 或 `MaterialButton.SetElevation(button, values)`；
  公开 double 配置不变，显示在帧批之间与固定重组顺序一致，无外部源码迁移。
  九个真实组件双端配置/禁用/合并场景高度精确一致，实际底层绑定、模板和卸载回归验证所有权；
  Button 的独立墨色图像差异及原生 enabled 非帧即时读数继续报告，不由此扩大通过范围。

## 27. Tooltip 并集轮廓与路径阴影入口

- **决定**：本轮依据用户扩大授权，MaterialSurface 增加内部 `SetContainerGeometry`，供 Tooltip 同时提供 Avalonia 几何与阴影路径快照；不增加公开 Shape 系统。Tooltip 的默认三角形与圆角容器做真实 Union，背景、内容裁剪及阴影使用相同外形。
- **原限制与新依据**：原先只有圆角路径，Tooltip 因而暂缺 caret。阴影 tessellator 已具备直线、二次曲线、conic 与凹多边形能力，缺少的只是输入契约。`ComposeShadowShape.FromPath(SKPath)` 在调用点复制托管命令及 conic 权重，绘制时生成物理像素路径，调用者可立即释放原 SKPath。
- **边界**：保留一个轮廓的原生 tessellation 分支；固定 Skia `1f14f1166a847cb0e148f981600bf1e3e6ca6e46` 的 SkShadowTessellator 在第二个 Move 返回失败，因此多轮廓整体走现有 blur fallback。不能分开画各轮廓阴影。三次曲线、透视等原有限制继续显式拒绝；不将默认三角形支持概括为完整任意 Shape。
- **2026-11-08 本轮授权修订**：原先拒绝三次曲线是因路径快照与 tessellator 尚未移植该分支。自定义 Tooltip caret 现需要此能力：快照保留两个控制点和终点，固定 Skiko 的非 Ganesh `handleCubic` 按两个控制点、终点形成阴影多边形，spot clip 保留 t=5/16、11/16 采样；背景/裁剪仍使用完整三次曲线。多轮廓仍整体 fallback，透视等原限制不变。`CaretShape` 仅扩展 Tooltip，不引入通用 Surface Shape API；现有 CaretSize 调用无需迁移。
- **影响**：现有圆角调用不变，无迁移。Surface 内部入口只用于无独立描边、波纹或焦点环的 Tooltip；公开任意形状仍须补齐这些职责。Tooltip 挂载生命周期和固定定位器语义见其 INTERNALS。

## 28. ExposedDropdownMenu 的字段焦点与同窗口弹层

- **依据与授权**：2026-11-06 本轮明确授权新增独立 ExposedDropdownMenuBox / Menu。固定 Core 的 Popup 有独立 focus owner，菜单导航期间父场景 TextField 仍保持原 FocusInteraction；本地同窗口 overlay 共享一个 FocusManager。真实只读空字段双端用例确认，直接沿用 native LostFocus 会令标签回落和指示线失焦。
- **决定**：容器在可聚焦菜单存活期间，通过内部 TextFieldThemeDefaults.RetainPopupFocus 保留字段已有的 native FocusInteraction。它不合成新的 Focus，也不阻止实际键盘焦点转入菜单。模式切换先保留交互再移动焦点；关闭后等原生焦点恢复完成再释放保留标记，避免无意义的 Unfocus/Focus 对。
- **范围与清理**：仅 ExposedDropdownMenuBox 内标记为 primary 的 TextBox；禁用、换源、模板卸载仍按 TextField 原有配对清理，容器卸载/移除锚点清除标记。外部 source 不被替换，普通 TextBox、原生 ComboBox 和其他 Popup 不自动采用此策略。没有公共焦点协议或破坏性 API 迁移。

- **状态搜索输入接入（本轮授权）**：新增 `SearchBarInputField.State` 后，真实组合回归发现窗口内搜索弹层关闭恢复锚点 focus 会再次展开。沿用本节的原始理由和同一内部 RetainPopupFocus，只在展开时保存与弹层共享状态、当前真正聚焦的搜索输入；关闭恢复焦点后释放。输入识别被保留的 focus 恢复，不把它当新指针聚焦。原生键盘焦点仍进入弹层，父源保留原 FocusInteraction；指针收起的 100ms 清焦和键盘收起保留焦点均按 SearchBar.kt 执行。该适配扩展到停靠、gap 和全屏搜索，不扩展为任意 TextBox 或全局弹层焦点策略。

## 29. Carousel 路径描边

- **依据与授权**：本轮剩余移植任务明确授权必要共享能力。固定 AndroidX/Core 的 MultiAspectCarousel.MaskShape 总是产生 Outline.Generic，即使输入是圆角形状。Border.kt 的该分支采用离屏双宽描边，再稍微放大反向路径并清除外侧笔画；现有 SurfaceOutline 的圆角直接描边不能替代此算法。
- **决定**：SurfaceOutline 增加内部显式 UsePathOutline，复用 Foundation/Rendering/PathBorderMask 生成物理像素 alpha 遮罩，纯色描边按上游方式着色并直接裁取位图，其他画笔通过 opacity mask 绘制。保留 BorderCache 对原始布局尺寸与路径尺寸的不同处理，缩小时复用较大缓冲区；缓存键包含实际/原始尺寸、圆角、描边、density、颜色和填充模式，卸载释放位图。PathLayoutSize 保留原始布局尺寸对描边上限的影响。
- **范围与影响**：当前仅 Carousel 的圆角 MaskShape 开启此分支；普通 Surface 和其他调用方不改变默认路径、布局或公开 API，无仓内调用迁移。它不是任意 Shape API。颜色与画笔透明度只施加一次；复杂画笔及第三方渲染后端仍需专项验证。
- **范围扩展（2026-11-08 本轮授权）**：原限制源于当时没有公共 MaterialShape，理由保留。两类 Carousel 现接入 §35 的既有形状入口，任意遮罩边框直接消费 SurfaceOutline.ShapePath，使用完整项目布局盒及平移后的路径，保留同一 Generic Border 算法，无共享绘制实现变更。传统 CornerRadius 分支继续有效，无破坏性迁移。形状记忆与 NonZero 组合规则属于组件，见 Carousel INTERNALS。
- **验证**：真实双端边框用例原最大降采样通道差 48，新增上游路径后通过既有容差；没有更改基准或放宽门禁。


## 30. Pager 组件的吸附动画复用

- **依据与授权**：2026-11-06 剩余移植任务授权新增组件及必要共享能力。ScrollField 和 Carousel 都消费固定 Foundation SnapFlingBehavior 的 approach → snap 流程；原实现位于 Carousel 目录，新的调用方构成实际复用需求。
- **决定**：将无组件依赖的内部算法原样移至 `Foundation/Input/SnapFling.cs`，保留消费回调、衰减/吸附选择、速度与取消规则。Carousel 的模式枚举和定位算法仍留在组件，ScrollField 的固定页定位由其状态负责。
- **影响**：仅迁移内部调用，不新增公共滚动框架，不改变 Carousel 规格或边界。ScrollField 使用 Pager 默认的整数可见阈值，Carousel 继续使用原 Float 规格；两者不能因共享流程而统一参数。
- **目标观察**：补上固定源码已有的 remaining-distance 回调，供 ScrollField 的 PagerWrapper 更新目标页。Carousel 不提供该回调，行为保持不变。

## 31. 取消后立即重启动画的帧请求

- **依据与授权**：2026-11-06 本轮授权必要共享修复。新 ScrollField 反向动画的真实双端采样暴露一帧延迟；两个独立 FrameClock 回归证明，最后一个 waiter 取消后、旧 dispatcher continuation 执行前注册的新 waiter 没有向 source 请求下一帧。取消 continuation 内注册新 waiter 还会被旧 wasLast 标志误取消。
- **决定**：仅当旧 source task 已完成取消时同步启动新 pump；generation 防止旧 continuation 清除新 pump 的状态。source 尚在等待时不并发请求；原 D4 单次 dispatcher hop、批处理次序与闲置零请求理由保持不变。取消 waiter 后重新检查队列，保留 inline continuation 注册的新 waiter。
- **影响与验证**：没有公开 API 或采样补偿；ScrollField 反向轨迹恢复精确相等。FrameClock 21 项及 ComposeAnimation 全模块 974 项测试通过。旧决定不是改成即时派发，修订仅解决完成取消与新注册之间的空窗。

## 32. 共享时钟直接推进与显式停止结果

- **依据与授权**：2026-11-07 维护者明确要求共享时钟推进动画、Task 只等待结果，正常中断和卸载通过状态返回。Debug 构建附加 Visual Studio 时，时间选择器普通切换会产生密集取消异常，显著增加等待；关闭异常断点并不能消除 first-chance 处理。
- **决定**：Animatable、SuspendAnimation、Transition 和 InfiniteTransition 以可复用回调注册进入已有 FrameClock 批次。保留单源请求、单次 dispatcher hop、注册顺序、帧前/帧末提交与闲置零请求；不以每帧 Task/WhenAny 或固定 16ms Delay 驱动动画。一次性帧提交返回可内联恢复的 ValueTask<bool>；普通计时和信号等待返回 bool，竞争只发生在等待开始时。
- **结果与异常**：动画结果增加 Interrupted（替换/Stop/拒绝）、Canceled（调用方）、ScopeEnded。Snap 和 seek 返回 bool。真实采样、转换、帧源、用户回调和清理错误保留；无关 OCE 仍是故障，采样和清理同时失败时保留两个错误。兼容入口 NextFrameAsync 及未提供 interruptedResult 的通用 MutatorMutex 业务调用仍保留原取消契约，不在内部动画热路径使用。
- **顺序与上游边界**：停止先撤销后续采样，当前回调退出后清理，再释放互斥区、允许替换运行采样。保持起始时间、速度快照、播放倍率、弹簧/衰减算法、边界钳制和时钟批次；保留 TimePicker 的 100ms 选择等待。组件后续步骤显式检查停止，避免已卸载或被替换的旧操作继续切换、吸附或发布完成。这是 C# 异步控制流适配，不修改固定 Compose 动画参数或视觉目标。
- **迁移**：`var result = await value.AnimateToAsync(scope, target, cancellationToken: ct); if (result.EndReason != AnimationEndReason.Finished) return;`。按场景单独处理 BoundReached。不能再用 Task.IsCanceled 判断动画替换；ScrollField 的公开滚动入口返回 bool，Tooltip 的 ShowAsync 停止返回 false。新增结果签名需要调用方重新编译。

## 33. Pager 组件的桌面滚轮算法复用

- **依据与授权**：本轮维护者明确授权新增交互和必要共享能力。固定 Core 的 `MouseWheelScrollingLogic.kt`、`NonTouchScrollingLogic.kt` 与 `DesktopScrollable.desktop.kt` 同时支撑 ScrollField 的 VerticalPager 和 Carousel 的 HorizontalPager；新增 Carousel 滚轮复用同一内部算法。
- **决定**：原 ScrollFieldWheel 的排队、差分速度、6 DIP 即时位移、最长 100ms 线性动画与 50ms 结束等待移到 Foundation/Input/MouseWheelScroll。组件继续拥有输入路由、滚动取消、范围消费与完成后的 fling，不创建公共 Pager 或新的滚动框架。原 ScrollField API 与调用方无需迁移。
- **平台边界**：保留原生 PointerWheelEventArgs 缺少 AWT scrollAmount、block-scroll 和精确设备标记的原理由，沿用 Core 无元数据事件的默认换算；不能据此声称支持专用 Pan/惯性协议。轴向判定在换算后执行，绝对值相等时归纵轴；不把纵向滚轮自动映射为横向。
- **影响**：Carousel 的三种 TargetedFlingBehavior 均在滚轮结束后接入现有吸附/衰减。键盘 PageUp/PageDown 则按固定 AndroidX/Core ScrollableNode 即时消费一个视口，不混用 fling。现有触摸、程序导航与辅助技术入口保持各自语义。

## 34. 显式精确指针尺寸环境

- **原问题与理由**：Button 维护说明此前延后 precision 分支，因为固定 Core 的监听器为空，不能凭 Avalonia 鼠标自动开启；按钮局部 bool 又会分裂上游 Button/ToggleButton/Menu/ListItem/AlertDialog 的共享环境。
- **本轮授权与新决定（2026-11-08）**：使用 `MaterialPrecisionPointer.IsEnabled` 继承附加属性，默认 false，作用域由应用显式选择，允许局部关闭及运行时切换。它表达输入尺寸环境，不是通用密度倍率，也不改最小交互布局。维护者本轮已授权必要公共能力与历史待定事项决策，无需再次逐项确认。
- **实施范围**：本批接入 Button 五种主题、ToggleButton 四种主题、AlertDialog 及独立 Expressive 菜单项；Small 普通按钮最小高 36 DIP，Toggle 仍为 40 DIP，二者 Small 竖向留白为 8 DIP、图标侧 12 DIP。AlertDialog 的容器/正文下留白为 20/16 DIP，标题合并 20/26 字号/行高覆盖。独立 Expressive 菜单项选择 12 DIP 内容间距与 16/12/10/12 留白，原有高度规则不变。依据固定两端 Button.kt、ToggleButton.kt、AlertDialog.kt、MenuDefaults.kt 与 Core PrecisionPointer.skiko.kt，未混入其他提交。
- **默认值与覆盖**：precision 候选使用独立 Token，主题内部选择写到既有默认配置流程；普通样式、显式 Padding/MinHeight 和绑定优先。默认 false 以及其他尺寸的既有 Token 不变，没有破坏性迁移。
- **尾部标签接入（本轮剩余移植授权）**：`DropdownMenuItemTrailingLabel` 复用同一继承环境，选择固定 MenuDefaults 的末尾 6 DIP 留白；显式 Padding 优先，无公共 API 迁移。分组标签不使用 precision 分支。
- **剩余范围**：原生 MenuItem、推荐尾部图标大小与 ListItem 尚未消费此环境，当前不宣称已实现全库 precision 分支；后续复用同一个环境，不为各组件再建开关。ListItem 的 ContentPadding 与对齐阈值是初始化值，并非实时 getter，不能直接套用动态选择。真实指针设备监听也未实现。

## 35. Surface 与 Card 的显式路径形状

- **原限制**：§27 仅开放 Tooltip 内部背景/裁剪/阴影入口，§29 仅供 Carousel 的圆角路径描边；原理由是共享描边、波纹和焦点环尚无完整的任意轮廓接入。两个原入口及其调用契约继续保留。
- **本轮授权与依据（2026-11-08）**：维护者明确授权新增共享能力、API 和历史范围修订。固定两端 Surface.kt/Card.kt 接受 Shape；Foundation CornerBasedShape/CutCornerShape、Border.kt 和 material3-ripple Border.kt 提供现成算法。补齐其实际消费方，不另造形状动画框架。
- **决定**：`MaterialShape : AvaloniaObject` 以 `CreateOutline(Size, FlowDirection, density)` 返回新闭合 `PathGeometry`；`CutCornerShape` 使用原生 RelativeScalar 表达 DIP/百分比，并保留 Float 物理像素计算、两侧独立归一化及逻辑四角。Surface、三个交互 Surface、三种 Card 的 `Shape` 为普通样式属性，null 保留原 CornerRadius，现有调用方无需迁移。自定义路径采用物理方向，Surface 抵消 Avalonia 子树镜像，内容仍沿原生逻辑方向布局。
- **共享管线**：路径在消费点复制，背景/内容 clip/波纹/均匀边框/焦点环/阴影共用该快照；阴影继续使用既有 FromPath 托管快照。描边复用 §29 的 doubled stroke、逆路径 Clear、整数缓存与 Float 运算。焦点路径新增 inset stroke Clear；路径分支按固定 RippleNode 的单一可重录图层保留两次最终外环回放，而非擅自为内外环各建图层。矩形/圆角仍沿原分支。
- **初始范围及边界**：初始接入上述 Surface/Card，后续本轮扩展的入口见下文；其他组件未自动获得 Shape。显式路径要求均匀 BorderThickness，非均匀输入明确拒绝；原生圆角的非均匀分支不变。属性变化、布局尺寸、方向和重挂重新求值；卸载断开形状订阅并释放位图缓存。不改变布局、输入状态、Card 高度和 CornerRadius 动画，不提供任意路径之间的形变。
- **验证**：`MaterialShapeTests` 与双端 `material-shape` 验证切角、百分比、RTL、曲线/凹轮廓/偏移/孔洞、内容裁剪、阴影、波纹、焦点、动态尺寸与输入；实际差异留在组件 INTERNALS，不以本决定免除门禁。
- **本轮后续接入**：普通 Button 五种主题新增 `MaterialButton.Shape / PressedShape`，对应固定 Button.kt 的静态/交互形状；两个切角形状复用现有 AnimatedCornerRadius 的进度与取消时序，其他路径直接切换。内部 CornerShape 新增不可变切角尺寸快照，保留百分比尺寸和第三目标 Blend；未引入第二套动画框架。公开圆角 API 和仓内调用方无需迁移。上述“无跨路径形变”仍适用于任意路径，不限制同族切角尺寸插值。
- **Chip 固定重载接入**：四类 Chip 与 Elevated 外观新增 `Shape`，对应固定 Chip.kt 的 shape 入口；显式 Shape 优先于 Shapes，取消圆角播放器，清除后恢复当前选择状态。保留原 Shapes 仅管形变、颜色/间距/图标动画独立的维护者决定，不以本次形状入口捆绑切换这些行为，无破坏性迁移。
- **图标按钮接入**：六种 IconButton 主题复用 MaterialButton.Shape/PressedShape，IconToggleButton 新增 CheckedShape；同族切角的三状态继续使用原播放器。普通路径切换按 Core 的 focusRingShape 身份变化重建 indication；RTL 显式形状缩放补偿原生子树镜像，保证波纹首次绘制后保留物理中心。圆角分支未扩大变更范围。
- **原生 ToggleButton 接入**：四种主题同样接入 MaterialButton.Shape/PressedShape，CheckedShape 置于既有 ToggleButtonAppearance；保留固定 ToggleButton.kt 的 FastSpatial 规格、三状态优先级和边框动画。原生先切换 IsChecked 再 Click 的已确认适配不变，未改变颜色、尺寸或圆角 API，无迁移步骤。
- **下拉刷新接入**：PullToRefreshIndicatorBox.Shape 对应固定 PullToRefresh.kt 的 IndicatorBox shape 参数，复用 Surface 路径，不改变外层顶部裁剪、平移和高度条件。两个内置指示器继承此属性是本地便利入口（固定上游这两个函数不公开 shape），已由本轮授权采纳；自动滚动容器所缺的 pre-fling 协议仍未实现，不以本次接入宣称完成。
- **拖动手柄与分段按钮接入（2026-11-08 本轮授权）**：两者复用既有 MaterialShape，无破坏性迁移。DragHandle 的三态路径直接切换，固定源码以 graphicsLayer 裁剪形状、indication 使用无形状参数的 ripple；Surface 新增内部 ResetIndicationOnShapeChange，手柄显式关闭，避免误取消仍存活的波纹/hover。其他调用方默认保留原行为。SegmentedButton 的 Surface chrome 因 physical place 内容布局固定 LTR，新增内部 ShapeLayoutDirection 仅为轮廓生成传入真实逻辑方向，实际树镜像仍以 Surface.FlowDirection 为准，不改变内容布局或创建第二套路径管线。
- **列表项六状态形状（同轮授权）**：ListItem / SegmentedListItem 以六个 MaterialShape 可选属性覆盖原六圆角状态，复用现有配置 key、优先级、反向与第三目标动画。CornerShape/AnimatedCornerRadius 新增圆角与切角的类型判定，按固定 Interpolatable.lerp 在不同族之间选择较近端点，同族仍逐角插值；原有纯圆角和纯切角消费者不变。Surface.SetAnimatedCorners 扩展为可向切角绘制传入同一动画的已求值角，先由 ListItem 接入，后由本轮菜单分组和菜单项复用。ListItem 自己维护 indication 身份，关闭 Surface 的重复重建；无公开动画 API 或破坏性迁移。
- **菜单分组形状（同轮授权）**：DropdownMenuGroup 新增 Shape/InactiveShape，分别覆盖原圆角；复用上述混合类型动画和 Surface 外部切角求值。保持 hover 历史、source 替换及 shape key 语义，不扩大到弹层和菜单项的形状 API，无破坏性迁移。
- **单一 ripple 的焦点环裁剪补齐（同轮授权）**：固定 ListItem.kt 在 clip(shape) 内放置 combinedClickable/ripple，Surface.kt 的独立焦点 indication 则位于裁剪外。共享 Surface 根据已有 HasSeparateFocusIndication 选择是否裁剪环，不统一抹平顺序。Ripple Border.kt 的 inset=0 仍执行 Clear hairline，PathBorderMask 增加明确的 eraseInset 输入，只有焦点边框使用；普通 Foundation border 保持原算法。
- **独立菜单容器与菜单项（同轮授权）**：DropdownMenu.Shape 复用 Surface；ExposedDropdownMenu 继承。专用 DropdownMenuItem/Checkable/Selectable 的 Shape/SelectedShape 由菜单项按固定 shapeByInteraction 管理，原模板的 selected radius/spec 迁至内部属性，仓内已迁移，公开 Shapes 不变。Baseline 项的 Shape 属本地便利扩展，上游该重载不公开形状。角形配置使用值快照相等性，任意路径使用实例身份，避免等值切角实例替换误重启动画。
- **RTL 首次键盘波纹**：Core AbstractClickableNode 的 centerOffset 在首次 pointer bounds 更新前为物理零点；Avalonia RTL 呈现层本地坐标镜像，键盘起点需使用其本地右上角后映射回 owner。只修正无 pointer 历史且无自定义 PressPositionMapper 的初始点，pointer 更新后的中心及导航项映射不变。
- **Snackbar 容器路径（同轮授权）**：槽和 Data 入口新增 Shape，按固定 Snackbar.kt 传至 Surface；Host 通过 SnackbarTemplate 复用，不增加 Host 形状代理、不改变 FIFO、计时、基线布局或进出动画。null 保留 CornerRadius，无调用迁移。
- **Snackbar Host 完成条件与 RTL 槽（同轮授权）**：本轮路径 Host 用例发现 §32 的显式取消结果尚未在 opacity 完成回调检查，入场中断会提前移除退出层；现在仅 Finished 且 token 未取消才调用移除，恢复固定 LaunchedEffect 取消时不执行后续代码的语义。RTL presenter 已由 Avalonia 镜像，原 Right 对齐在宽约束槽内重复反转，改为保留逻辑起点 Left。原物理 SnackbarLayout 排列与动画参数不变，无公开迁移。

- **SplitButton 路径与状态形状（2026-11-08 本轮授权）**：四组主副主题接入既有 MaterialButton.Shape/PressedShape、ToggleButtonAppearance.CheckedShape；不创建第二套公共配置，也不改变原生子按钮 owner。保留 Split 按 spec 记忆、按选中目标启动 effect 的规则，corner/generic 入口改变才重建。Surface 为纯切角的固定形状保留 AnimatedShape 光学 clamp，任意路径不做光学校正；选中覆盖层直接绘制当前轮廓。null pressed 优先回退 rest，不穿透 checked。无公共迁移，其他组件原重置策略保留。
- **普通与 Extended FAB 容器形状（同轮授权）**：FloatingActionButton.Shape 由 Extended 继承，对应固定 FloatingActionButton.kt 各尺寸/内容重载的 shape；两个模板直接复用 Surface，不改变原有测量、内容宽度过渡、可见性或 elevation 生命周期。Toggle FAB 和 FAB 菜单项没有相应上游公开参数，不增加便利代理。默认 null 保留 CornerRadius，无调用迁移。

- **公开逻辑圆角形状（2026-11-08 本轮授权）**：此前只公开 CutCornerShape，圆角通过 CornerRadius 兼容接口表达；这保留了旧调用，但不能表达独立百分比角和圆/切角状态组合。本轮新增 RoundedCornerShape，与 CutCornerShape 同用逻辑四角与 RelativeScalar，百分比按短边解析，实际 DIP 1000 不再被当作 Full。CornerShape 仅增加值快照入口，复用已有同族插值、异族过半切换和各组件配置 key 规则。Surface 对已知圆角保持原 RoundRect 描边/阴影/反馈分支，避免降为 Generic path；独立 CreateOutline 仍返回闭合 PathGeometry。Button/Icon/Toggle/Split/ListItem/Menu 状态消费同一类型。保留原 CornerRadius API 与 Full 适配，无破坏性迁移；自定义任意路径之间仍不插值。

- **FloatingToolbar 容器形状（同轮授权）**：固定横/纵、普通/FAB 重载均公开 shape；新增 FloatingToolbar.Shape 并传入两套 Surface 模板，专用 Standard/Vibrant FAB 模板传递已有 FAB.Shape。保留工具栏与 FAB 独立轮廓、现有尺寸/占位/展开/滚动协议，不建立新形状动画，无破坏性迁移。

- **IconButton 自定义内容（2026-11-08 本轮授权修订）**：原默认模板只消费 Symbol，原因是以仓内字体图标便利入口承接图标按钮，README 曾明确 Content 被忽略。时间默认模式按钮的固定 Filled 矢量不能用近似字体替换，因此本轮补齐上游 content lambda。非空 Content 或 ContentTemplate 优先，清除后恢复 Symbol；内容使用原生 presenter、自然尺寸居中，无新公共属性。行为迁移：依赖旧忽略行为的调用方须清除 Content/ContentTemplate；仓内图标捕获夹具保留新内容层，Catalog 增加组合图标。点击、命令、automation 与状态 owner 不变。

- **四类 Dialog 容器形状（同轮授权）**：固定 AlertDialog、DatePickerDialog、TimePickerDialog、VibrantTimePickerDialog 均提供 shape。四个类分别 AddOwner 同一 Surface.Shape，模板只转发；BasicAlertDialog 不自带表面，保持组合职责。退出冻结和再次打开复用现有 presentation 生命周期，日期布局仍按固定 Core 分支，无公开迁移。


## 36. 组件横向排列能力

- **原范围与原因**：Chip 使用六值枚举，Menu 使用专用 Leading/TrailingSpacing；各组件先保留自己的测量与动画，避免为假设需求建立通用布局框架。
- **本轮授权与依据（2026-11-09）**：维护者明确授权缺失 API、共享能力与破坏性迁移。固定 AndroidX/Core 的 Foundation `Arrangement.kt` 一致；Chip.kt 和 Menu.kt 均公开 Horizontal。新增共享排列对象是为这两个实际消费方补齐 SpacedBy、Absolute 与应用自定义算法。
- **决定**：`MaterialHorizontalArrangement` 是不可变 record，Spacing 为 DIP，Arrange 接收密度、整数物理总宽/子项宽及 FlowDirection，写入物理左坐标。内置六种分布与 Absolute 保留上游 Int/Float、反向遍历、单项和越界规则；SpacedHorizontalArrangement 保留正负间距、逐项截断与组对齐。组件自己负责测量及抵消原生镜像，不引入公共 Row/Column 布局器。
- **范围与迁移**：Chip.HorizontalArrangement 从枚举改为该类型，C# 常量迁移到 Foundation.MaterialHorizontalArrangement；XAML 原六种字符串保持可用。菜单新增同名可选入口，仅 Expressive/Checkable/Selectable 消费。null 保留各自专用间距，其他组件的既有 Arrangement API 没有自动迁移。
- **验证与边界**：组件双端用例覆盖真实槽、约束、RTL、动态排列及图标动画；自定义算法的正确性由应用负责。当前不声明完整 Foundation Row、Column、intrinsic/weight 公共 API 已移植。

### ButtonGroup Arrangement 迁移（2026-11-09）

继 Chip/Menu 接入后，ButtonGroup 的固定上游同样接受 Arrangement.Horizontal。
此前枚举加独立 Spacing/SpacingAlignment 便于 XAML 表达常用排列，但无法保留任意排列与其 spacing 的一体语义。
依据本轮用户明确授权，HorizontalArrangement 改共享 MaterialHorizontalArrangement；删除旧枚举、两个分离属性及 Spacing Token，
默认 SpacedBy(12) 通过 HorizontalArrangement Token 提供。该迁移减少重复算法并新增绝对方向/自定义布局能力，
未改变菜单、权重和按压状态的职责。迁移示例见 ButtonGroup README；ConnectedButtonGroup.Spacing 保留。


原生 Expressive MenuItem 同轮通过 MenuAppearance.HorizontalArrangement 接入该能力，NativeMenuRow 的
默认首尾布局继续由 null 保留。主内容行消费排列，尾部组合槽内部的快捷键/chevron 排列不被替换；
Baseline/MenuBar 不采用此附加属性。不改变上文关于权重、intrinsic 公共 API 的范围。

### BottomAppBar Arrangement 迁移（2026-11-09）

本轮维护者明确授权缺失 API 与破坏性迁移。固定 AppBar.kt 的 FlexibleBottomAppBar 接收任意 Horizontal，
故将 BottomAppBar.HorizontalArrangement 与两个排列 Token 接到现有共享类型，删除 AppBarArrangement 与 Spacing。
原枚举只覆盖常用排列的理由保留；共享能力现已存在，继续维护局部算法反而遗漏绝对方向和自定义排列。
默认 Start/SpaceBetween、槽与滚动职责不变；迁移为同名共享成员，固定间距改 SpacedBy/SpacedHorizontalArrangement。
仓内 Catalog、配置测试与双端调用已迁移，不扩展本决定为完整 RowScope weight/alignment。

## 37. 色调高度环境

本轮维护者明确授权缺失 API 与共享能力。固定 ColorScheme.kt 的 LocalTonalElevationEnabled 是默认 true 的
子树环境；此前只有 Surface 的内部 ApplyTonalElevation 特殊门控，应用无法表达该功能。
新增 MaterialTonalElevation.IsEnabled 继承附加属性，所有现有 MaterialSurface 消费同一入口，不创建第二套绘制。
开关只决定是否着色，累计高度和阴影独立；后代显式 true 使用完整累计值。内部非 Surface 容器的门控保留，
不能以环境重新启用它。无破坏性迁移；ColorScheme 两仓库的 toString 差异不涉及该行为。

## 38. 物理 Insets 并集与自动留白容器

2026-11-09 本轮维护者明确授权共享能力与历史决定修订。§4 原先仅提供 ConsumePadding，
是为了先保留 Scaffold 的调用方消费顺序，避免假设系统 Insets 服务。现在固定 Foundation WindowInsetsPadding.kt
提供明确的 union 与 padding 算法，新增 ConsumeInsets 和 MaterialWindowInsetsPadding 完成这两个实际入口。
复用原 PaddingScope 继承/重挂链，ConsumePadding 的 add 不变；同一元素的顺序明确为 add 后 union，
自动容器应用剩余物理 Insets。新入口不读取 OS 状态，不改变 Scaffold 默认正文布局。
同轮后续依据三个导航组件固定源码的 windowInsetsPadding 接入 NavigationBar、ShortNavigationBar、NavigationRail；
消费归属控件，模板仅执行布局，以覆盖真实逻辑内容。普通 WideNavigationRail 后续按相同固定算法接入。
当时 ModalWideRail 因弹层搬移尚未纳入，避免把声明 owner 的消费误当成独立 Dialog 消费；保留此原理由。
同轮在维护者明确授权下补齐 Modal：固定 Core Dialog 创建独立 SceneLayer，Foundation 消费节点遍历 LayoutNode
祖先而非 CompositionLocal；实际双端 proof 也确认声明祖先消费只影响 inline。模板两处各用现有
MaterialWindowInsetsPadding，Dialog 根消费清零，声明 owner 自身的 add/union 在浮层重放；Header/移动项目
真实内容根复用 BindConsumptionParent 指向实际呈现作用域，移出或 detach 立即解绑。保持其他继承和状态身份。
未新建通用环境或更改 Dialog 本身，其他浮层必须各自按固定上游验证，不能从此推断全部弹层都继承或清零。
底部 AppBar 后续同样接入发布阶段，使 Actions/FAB/CustomContent 不重复消费。
顶栏双行最初延后，是因为其标题与直接 Action 逻辑父级为 ItemsControl，不能在 owner 上统一发布全部 Insets：
固定 `AppBar.kt` 的上行消费全部四边，下行只消费左右。2026-11-09 本轮授权后补齐内部
`BindConsumptionParent`，仅将模板实际内容根的消费源绑定到所属行，保留该根自己的 ConsumePadding/ConsumeInsets。
两行各自使用已有 AppliedInsets 并集；模板替换、换槽与卸载释放绑定恢复原继承链，不增加公共 API。
该能力不迁移逻辑/视觉树、不更改布局尺寸，也不将某行已消费的垂直 Insets 传给另一行。
无签名迁移；原先手工从子内容请求中扣除消费的调用方应改传完整 Insets。示例和双端证据由各组件文档维护。
