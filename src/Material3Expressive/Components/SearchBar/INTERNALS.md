# SearchBar 内部机制与维护说明

## 1. 上游基线与职责映射

固定版本见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 为逻辑依据，Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 为桌面参考。

| 上游符号 / 职责 | 本地类型 / 成员 | 对应说明 |
| --- | --- | --- |
| `SearchBar(state, ...)`、`SearchBarState` | `SearchBar`、`SearchBarState` | 收起 Surface 与坐标身份；独立进度/内容 Animatable，保留阈值、目标判定和中断 |
| `ExpandedDockedSearchBar`、`DockedSearchBarLayoutImpl` | `ExpandedDockedSearchBar`、`ExpandedSearchBarLayout` | 零布局声明、默认可聚焦弹层、以物理像素插值整个容器高度 |
| `ExpandedDockedSearchBarWithGap` | 同名控件、`SearchBarPopupPresenter` | 独立输入/结果背景，间隙、遮罩点击、半内容高度滑动；整体最大窗口高度 1/2 |
| `ExpandedFullScreenSearchBar` / `ExpandedFullScreenContainedSearchBar`、`FullScreenSearchBarLayout` | 同名控件及内部 `FullScreenSearchBarLayout` | 物理像素布局、输入与表面独立插值、安全边距消费、形状与透明度 |
| `rememberContainedSearchBarState` | `SearchBarState.CreateContained` | FastSpatial；内容进入 50ms 延迟 + 100ms StandardAccelerate，退出 100ms StandardDecelerate |
| `rememberSearchBarWithGapState` | `SearchBarState.CreateWithGap` | DefaultSpatial 展开 / FastSpatial 收起；独立内容淡入淡出与 contained 相同 |
| `AppBarWithSearch` | 同名 ItemsControl、`AppBarWithSearchLayout` / `Row` | 导航/操作先测量，搜索占剩余权重空间；Insets、颜色、elevation 路由和全屏收起时的内部搜索隐藏 |
| `SearchBarScrollState` / `EnterAlwaysSearchBarScrollBehavior` | `SearchBarScrollState` / `SearchBarScrollBehavior` | DIP 状态与显式嵌套滚动回调；复用 AppBarVerticalGesture、spline decay 与已有 spring 求值 |
| `SearchBar.kt / DockedSearchBar(inputField, expanded, ...)` | `DockedSearchBar`、`SearchBar.axaml` | 双插槽、受控展开、Surface 与分隔线；这是固定版本仍保留但已弃用的布尔状态重载，不冒充新版 `SearchBarState` 组合 |
| `Column`、`SearchBarMinWidth`、`heightIn` | 两个内部布局 Panel | 360 首选宽度，结果最小 240、最大窗口高度 2/3，服从父级约束 |
| `DockedEnterTransition` / `DockedExitTransition` | `SearchBarMotion`、现有 `AnimatedVisibility` | 延迟 100ms；进入 600ms `(0.05,0.7,0.1,1)`，退出 300ms `(0,1,0,1)`，淡入淡出叠加底部对齐的纵向展开收缩 |
| `SearchBarDefaults` / `SearchBarTokens` / `SearchViewTokens` | `SearchBarTokenSource` | SurfaceContainerHigh、Outline、ExtraLarge；默认 tonal/shadow 均为 Level0，不误用未被默认参数采用的 Level3 |
| Core `SearchBar.skiko.kt / getWindowContainerHeight` | `DockedSearchResultsLayout` | 观察 TopLevel.ClientSize，以布局比例将窗口高度和 DIP 常量转换至像素取整 |
| Core `BackHandler.skiko.kt`、`BackNavigationEventInput` | `CollapseRequested` | 未消费的 Escape KeyDown 请求关闭 |
| `SearchBarDefaults.InputField(query, expanded, ...)` | `SearchBarInputField` | 同一布尔状态重载；原生单行 TextBox、受控展开/搜索事件、收起延迟清焦 |
| `SearchBarSearch` / `SuggestionsAvailable` | 组件内部 peer 与 `SearchBarStrings.json` | 固定 Core `skikoMain/.../l10n` 的 86 个地区映射；Name / ItemStatus 投影，保留原生 IValueProvider，应用显式值优先 |

## 2. 关键机制与架构契约

新版组合由应用持有 `SearchBarState`，收起和展开控件引用同一个实例。状态保留两个 Float Animatable：展开先启动主进度再启动内容，收起顺序相反；同次操作中任一任务取消也取消另一个。`Progress` 只在公开读取时限制至 0–1，布局继续读取未裁剪的正向过冲；`CurrentValue` 使用 raw <= .02 的条件，`TargetValue` 仅在目标恰为 1 时为 Expanded。`SnapToAsync` 只改变主进度，不代写独立内容进度。默认 SlowSpatial / DefaultSpatial 由主题配置进入控件，再由状态读取；内容默认 snap。

状态从 `AnimationScope.GetRoot(topLevel)` 创建窗口 AnimationChildScope，最后一个消费者卸载时取消自身任务，重挂载保留当前值；动画启动前必须已有挂载消费者和有效规格。这个生命周期遵循仓内其他应用持有状态的接入约定，不建立脱离窗口的公共时钟。同一状态同时接入不同 TopLevel 会抛出异常。状态不依赖首个消费者的控件 Scope；移除任一单独消费者不能结束其余消费者的状态动画，回归覆盖两种挂载顺序。

`CreateWithGap` / `CreateContained` 只选择默认 motion 分支，不把主题解析时机移到工厂。实际主进度规格仍从挂载消费者模板的有效配置读取；内容 tween 保留上游固定时长和 easing。初始 Expanded 同时初始化主进度和内容至 1。

搜索顶栏先测导航与操作，再为搜索分配剩余空间；搜索实际首选宽度仍由输入决定，不因为 Row 有权重就强行填至 720 DIP。默认左右搜索 padding 为 8、上下为 4，导航 start / 操作 end 为 4。外层 Surface 是矩形；`CornerRadius` 配置内部搜索。外层颜色复用 `AnimatedState<Color>` 的 AfterFrame + Restart，按固定 animateColorAsState 在旧采样后提交并先取消旧任务；内部 scrolled 搜索颜色按上游直接切换；初次挂载显式发布动画初值，避免初始 contentOffset 已重叠时错误显示基础色。颜色与 elevation 均以 Animation 优先级贡献投影，不覆写应用配置。直接操作项绕过 presenter 的逻辑继承，沿用 AppBar 已有的低优先级样式投影，移除项、换模板和卸载释放这些样式。

`SearchBarState.expandsToFullScreen` 在全屏呈现控件接入时标记并保留，和上游一致。顶栏内部搜索在全屏收起途中隐藏，直到 CurrentValue 回到 Collapsed；导航和操作不随它隐藏。表面、输入和状态仍由现有 SearchBar / 全屏控件承担。

滚动布局测量完整栏高并设置负 limit，再按物理像素舍入 offset 减少外部高度、平移和裁剪内容；不以当前可见高度重写 limit。pre-scroll 在非反向且 offset 发生变化时消费完整 availableY；反向 post-scroll 只消费正向 availableY。吸附目标使用 decay **之前**捕获的 fraction；保留源代码的 signed delta 与绝对 consumed 比较、0.5px 截止及 1px/s 速度门槛。默认衰减在物理像素执行再转回 DIP；调用方自定义规格以公开 DIP 单位求值。`CanScroll` 在每个嵌套回调中读取，不取消已启动的吸附。上游 `searchBarScrollBehaviorModifier` 在 behavior 构造时捕获一次 draggable.enabled；本地在首次挂载时完成这项初始化，以兼容 C# 对象初始化器，之后同一实例的直接拖动不读取运行时 CanScroll。新直接按下会中断已有吸附；取消拖动对应 Core `processDragCancel` 的零速度 settle，替换 behavior 或卸载则终止相关任务。没有创建新的公共滚动协议。

展开容器复用现有 `ModalPresenter` 的焦点隔离、Tab 循环、嵌套 Popup 所有权、返回键和焦点恢复，只在组件内部增加物理位置与外部 PointerPressed 行为。Core 默认 Popup 的任意按钮外部按下会收起并消费事件；Escape 在 KeyDown 请求收起。输入在弹层创建时聚焦，窗口首次显示过早的请求只重试一次，进度更新不重新抢焦点。零布局声明的 peer 不重复导出 PopupAutomationPeer 路径，真实内容由 overlay host 导出。

仅当 CurrentValue 为 Expanded 才呈现弹层。内部关闭操作属于弹层生命周期，到 .02 阈值移除弹层并取消剩余动画，因此可能留下接近零的进度；应用直接调用的外部关闭任务仍运行至零。输入/结果模板随弹层释放，再次打开重新构造。新弹层最小 240 DIP 指整个 Surface，旧布尔容器则指结果区域；两者不能共用相同最小高度布局。上限取窗口高度 2/3，再和输入高度按 raw 进度进行整数像素插值。

带间隙变体仅在 `Progress > .1 && TargetValue == Expanded` 时让 AnimatedVisibility 进入。输入背景先绘制并按外层形状裁剪；结果背景在独立内容 alpha 之前，退出结果时背景不跟随淡出。滑动规格在组件内部将 Float 规格映射到物理像素 IntOffset；采样位置取整，速度不取整，并保留 SpringSpec 身份以复用已有反向中断机制。遮罩使用现有 MaterialInteraction / Surface 波纹；其按钮是搜索内容的祖先，指示层绘制在内容之后，颜色从声明父级继承而非搜索 Surface 的 contentColorFor。按钮自身的 Click 才关闭，子按钮的冒泡 Click 不关闭搜索；无罩时按钮仅作为布局宿主。有遮罩时主按钮 Release 关闭，无遮罩时沿用 Core Popup 外部任意按钮 Press 关闭。透明色哨兵省略遮罩，其他指定颜色（即便 alpha 为零）仍保留点击语义。有罩时严格保留 Core 的 relative offset + TopStart 定位：RTL 下会与收起锚点错位；无罩使用物理锚点坐标。该上游行为由用例保留，不擅自改成视觉对齐。

全屏复用相同的零布局声明、ModalPresenter 焦点隔离与生命周期。Core `BasicEdgeToEdgeDialog.skiko.kt` 强制关闭平台默认宽度、平台 Insets、软件键盘 Insets、平台 scrim 与平台动画；本地窗口内 overlay 沿用仓内已确认桌面呈现边界，不创建新的公共弹层框架。桌面 Escape 对应离散返回，预测返回手势未实现。

普通全屏的表面宽高/偏移使用 clamped progress，输入宽度与中心使用 raw progress，保留过冲；输入高度使用锚点高度，零/无锚点分别回退到 360×56 DIP。默认 Full 圆角从 28 DIP 插值至矩形；自定义圆角在 .5 切换至矩形，不能擅自改成连续插值。contained 表面从一开始占满客户区并按 Progress 淡入，输入以相同中心算法移动，结果按独立 ContentProgress 淡入且无分隔线。两个全屏模板把 Insets 的 padding 放在输入 clip 外面，内容按先水平/底部 padding 再纵向 Column 的顺序测量；复用 MaterialWindowInsets 的消费协议。所有动画样式值使用 Animation 优先级贡献，分离时释放。

结果与表面透明度对应 Compose 的整层 graphicsLayer；复用 Tooltip/Menu 已采用的 `RequiresFullOpacityHandling`，设置在透明度层的祖先 SearchBarPopupPresenter 上，使自定义 Skia 分隔线和重叠内容一起合成。只设置在透明度层本身会晚于 Opacity 入栈，不能满足这一语义。

固定 Core 将 LayoutCoordinates 对象写入状态；同一个 anchor 的位置/宽度改变并不使静止 popup 重新布局。本地同样只通知坐标身份首次就绪，进度、窗口尺寸或重新打开时读取最新位置；不能把这个上游行为“修好”后宣称一致。

应用拥有 `IsExpanded`，关闭请求不代写状态。输入槽保留自己的逻辑子节点和 ContentPresenter 注册；结果 ContentPresenter 位于稳定的 AnimatedVisibility 内，退出期间不替换 Content，动画完成才释放呈现。附加/分离负责窗口按键与尺寸订阅，重挂载按当前目标初始化。

窗口事件仅在 Bubble 阶段处理未消费的 Escape，视觉树最后一个已展开且可见的 DockedSearchBar 获得请求。它是组件内部的桌面接线，不建立通用返回键调度框架。上游的组合注册顺序与 Avalonia 视觉树顺序并不对任意重排严格等价；复杂多层返回栈不在这里近似实现。

布局遵循先测输入、再测剩余高度内的结果；结果又先测分隔线再测内容。输入与结果的内容对齐默认 Stretch，固定尺寸或显式对齐仍由槽内控件控制。动画状态、反向中断和作用域生命周期复用仓内 ComposeAnimation，不能用原生 IsVisible 代替 Visible。

输入字段继承现有 MaterialTextFieldBaseTheme，保留 PART_TextPresenter / PART_ScrollViewer 和原生编辑状态；不建立第二套文本引擎。两个图标复用原有 48 DIP 槽，仅加上游逻辑方向的 ±4 DIP 平移。容器透明、边线透明，默认文字与 leading 为 OnSurface，placeholder/trailing 为 OnSurfaceVariant，禁用色、选区、光标继续引用 FilledTextField Token。

清焦任务只在 IsExpanded 改变时启动，对应 `LaunchedEffect(expanded)`，用根 AnimationScope.DelayAsync 的可取消延迟。收起瞬间若交互源的焦点收集状态为 true，100ms 后调用窗口 FocusManager.Focus(null)；期间重新展开或分离取消。按上游语义，任务启动后即使焦点转向别处或交互源改变，也不额外重判原输入的 IsFocused。默认 accessibility 名称使用当前 UI Culture；展开状态映射到原生 ItemStatus 并发属性变化通知，未建立完整 Compose semantics 树。

旧布尔 `SearchBarDefaults.InputField(query, ..., expanded, interactionSource)` 的共享源直接复用 TextField 模板观察器，公开 `InteractionSource` 与 `MaterialTextField.InteractionSourceProperty` 共用属性身份。上游 `collectIsFocusedAsState` 决定清焦任务，`onFocusChanged` 决定展开请求，两者不能混为一谈。外部 Focus 不请求展开；对真实焦点开始实例发送 Unfocus 后，原生字段即使仍聚焦，收起也不启动任务。源替换不改变以 expanded 为键的任务生命周期。新版状态输入的提交问题仍按下节暂缓，此接入没有改变其批准状态。

## 3. 已确认决定与已知差异

- **授权范围**：本次任务明确授权上游语义内新增组件及非破坏性组件 API；没有新增公共共享框架或改动既有共享决定。
- **尚未实现**：新版状态 InputField、搜索顶栏自动 ScrollViewer / SmoothScroll 接入、任意 PopupProperties 与非圆角 Shape、移动端软键盘抑制及 IME Insets。独立弹层的内容槽不代表这些输入/平台协议已实现。
- **未经验证**：触摸输入、复杂多弹层返回栈、屏幕阅读器遍历顺序及带间隙滑动的任意自定义 Keyframes 等规格；已双端验证的自定义规格为 tween、spring、snap 与独立内容淡入淡出。桌面单窗口 Escape、基本多容器顺序与 handled 事件有行为测试。
- **共享适配**：动画尺寸沿用 ComposeAnimation 的 DIP/double 计算，Compose 的 IntSize 中间帧可能在像素取整上不同；不将检测到的视觉差异登记为新批准的近似。

**待决定的状态输入接入**：固定新版 `InputField(TextFieldState, SearchBarState)` 将键盘聚焦与展开解耦，并收集 Press.Release、文字长度增长和输入模式。原生 TextBox 的焦点在鼠标按下时改变，Core 此场景在释放时改变；Core 的新版字段即使 enabled=false 也可能通过 Release 收集器展开，而 Avalonia 禁用控件不投递该事件。直接添加 State 属性的原型在双端核验中暴露了这些差异，因此该 API 暂未交付，也未把差异登记为已接受。文字长度观察还涉及 SnapshotFlow 的提交与合并：换状态后在下一重组前增长文字，不能按每次同步 TextProperty 回调直接替代。

推荐维护者先决定 TextField 原生编辑边界下的共享输入事件与提交协议，再接入形如 `new SearchBarInputField { State = searchState, Text = query }` 的组件 API；否则需明确接受原生焦点/禁用投递语义。影响包含禁用字段的交互、Focus 与 Release 顺序、同一事件内多次编辑、InputTransformation/OutputTransformation，以及恢复到收起输入时是否再次展开。当前 Catalog 使用按钮打开和旧布尔输入，避免依赖这项待定方案。调查证据为本机 `20261104-104018-ce4abd78`：14 例执行错误 0 且重复稳定，禁用指针与紧接换状态的文字增长存在实质状态差异；该原型不计入已完成组件或正式回归数量。

**待决定的自动滚动接入**：现有 `AppBarScrollScope` / `AppBarSmoothScrollConnection` 仅连接 Top/BottomAppBar，包含 SmoothScroll 合成线程状态、消费顺序与 Scaffold 的类型约束，不能把 SearchBarScrollBehavior 直接塞入任一槽。建议扩展现有接入点，例如 `new AppBarScrollScope { SearchScrollBehavior = scrolling, Child = viewer }`（示意 API，尚不存在），明确与其他顶栏共存时的先后消费、剩余速度传递、状态归属和卸载中断，再连接同一套同步机制。影响已有嵌套滚动及 Scaffold；未新建共享协议或把方案记为已批准。当前组件可用显式 pre/post-scroll、post-fling 回调和直接拖动独立使用。

**尚未实现：布局后的指针重送。** 固定 Core 的 SyntheticEventSender 在布局移动后重送同时间戳的鼠标 Move；
本地场景输入没有对应协议。真实 `pointer-velocity` 对照的静止目标五种输入均相等，移动内容释放速度为
Core 约 -0.000584 / Avalonia -1250.000732 DIP/s；`search-app-bar.dynamic.drag` 的 64ms 偏移为
Core -17.1870956 / Avalonia -39.8286819 DIP。该差异未获接受，不能在 SearchBar 内重置速度或补造重复样本。
后续共享输入接入需同时验证原生捕获、点击身份和其他拖拽组件。

**输入验证边界。** 主键夹具现在使用与 Compose 相同的逻辑毫秒。旧 Headless 鼠标入口使用墙钟，
所以下文历史“直接拖动相等”不再代表当前结论。禁用策略行为测试明确暂停超过 40ms 后释放以隔离速度影响；
立即释放的差异保留在上述真实双端用例中。夹具依据见 [VisualParity DECISIONS](../../../../tests/VisualParity/DECISIONS.md#主键指针时间与原生路由2026-11-05)。

## 4. 验证入口

- `tests/Material3Expressive.Tests/DockedSearchBarTests.cs`：旧布尔容器约束、窗口变化、模板延迟创建、反向动画、卸载、Escape 与 Token。
- `SearchBarInputFieldTests.cs`：受控展开、清焦时点/取消、图标偏移、原生编辑值与本地化无障碍状态。
- `ExpandedDockedSearchBarTests.cs`：状态阈值/目标、布局、调用方取消、状态替换、模板释放、焦点恢复、嵌套菜单、handled Escape 和 UIA 单一路径。
- `SearchBarGapTests.cs`：间隙/高度上限、RTL 有罩/无罩定位、主按钮 Release 与右中键、透明色哨兵、独立内容 alpha、Token 更新、状态/模板生命周期、嵌套控件事件及外层前景色更新。
- `AppBarWithSearchTests.cs`：槽位约束、Insets/RTL、初始滚动颜色、elevation 路由、局部前景覆盖、全屏收起隐藏、滚动消费、禁用中继续吸附、直接拖动的初始 enabled 语义、状态/模板替换及清理。
- `FullScreenSearchBarTests.cs`：输入/表面独立插值、零/无锚点回退、物理 Insets/祖先消费、RTL、窗口缩放、形状切换、contained 布局/延迟淡入、反向、Escape/焦点恢复。
- `./tests/VisualParity/parity.ps1 self-test --family <suite>`：使用下表 suite。真实 Core Popup/Dialog 与 Avalonia overlay 双端捕获、重复和额外被动截图检查，图像均为真实 4× 渲染。
- Catalog：`SearchBarDemo`、`ExpandedSearchBarDemo` 的四种展开组合，以及 `SearchAppBarDemo` 的顶栏、搜索和显式滚动组合；可使用 README 中的启动参数。

固定提交下本机最终核验（2026-11-04）：

| Suite / 运行 ID | 用例 | 图像：逐像素相等 / 既定容差内 / 超差 |
| --- | --- | --- |
| `search-bar-gap` / `20261104-130615-49033dba` | 38 | 194 / 48 / 10 |
| `expanded-search-bar` / `20261104-131150-e47b2e49` | 26 | 169 / 3 / 6 |
| `full-screen-search-bar` / `20261104-131552-e39b16b2` | 32 | 248 / 15 / 6 |
| `contained-search-bar` / `20261104-132304-de2e19cd` | 32 | 259 / 3 / 7 |
| `search-app-bar` / `20261104-131513-f56388e8` | 51 | 235 / 45 / 17 |
| `search-app-bar` 直接拖动禁用两例 / `20261104-133024-34c9fabb`、`20261104-133037-70c744f1` | 2 | 24 / 0 / 0 |

以上运行执行错误均为零，重复/额外截图检查稳定；图像计数不是组件完成率。全屏与 contained 的 16 个静态图像各自全部逐像素相等，包括深浅色、RTL、Insets、小窗口、自定义尺寸/颜色和零锚点。无锚点时缺失的收起几何为 notCompared，不补造几何字段。其余不同属性包括浮点约 6e-8 尾差和 RTL 四角顺序，保留原始读数。

带间隙的 22 个静态图像中 21 个逐像素相等、1 个在既定容差内；覆盖有罩/无罩的 RTL 位置。十个图像超差覆盖移动锚点、增长结果和移除锚点后的立即样本，以及 tween / 工厂默认运动退出和中断的中间帧。既有 AnimatedVisibility 目标提交边界造成的位置误差保留在报告中；默认关闭在 1088000µs 的结果滑动相差 4.5 DIP，没有组件延迟或容差调整。遮罩 Release、波纹与 hover 层在同一外层内容色下已相等或处于既定容差内；仍有不可见的退出子节点几何与 Float 尾差，并非所有字段都相等。

普通全屏的六个图像超差分别是结果增长后的立即样本、三条 Insets/带背景裁剪用例的首次展开帧、snap 后立即创建弹层以及移除锚点的立即样本。contained 的七个超差包含同类情况、立即关闭和移除锚点样本。后续帧一致：Core 的 `onConsumedWindowInsetsChanged` 先更新 top 的读取，再经重组把 nonTopInsets 提交到 padding；本地现有消费协议同步传播。不能把立即样本换成下一帧，或在组件内加入固定 16ms 延迟。

普通停靠的六个超差包含 3 DIP 阴影轮廓，以及锚点/内容改变、snap 和移除锚点的立即采样。阴影继续复用 Surface 和 [共享阴影决定](../../../../docs/DECISIONS.md#20-本次保留阴影坐标接入差异)，没有为此放宽容差。旧布尔容器 `20261104-105722-a9ca644e` 的七个静态图像相等，进入中间帧仍存在约一帧提交差；旧输入字段 `20261104-105845-78467838` 保留原生编辑行 56 DIP 与 Core 58.5 DIP 的度量差异。

搜索顶栏的静态不透明背景、初始重叠颜色、Insets、小窗口和槽位尺寸均逐像素相等；RTL 图像相等或在既定容差内，四角几何顺序仍不同。正负 fling、吸附、直接拖动、反向布局、禁用及吸附中禁用用例的结构化值和图像全部相等。直接拖动中禁用和松手吸附中禁用的两例同样全部相等；原拖动及嵌套吸附禁用在最终实现下另以 `20261104-133051-e76e5146`、`20261104-133103-d237b3e7` 复核相等。17 个超差图像分别来自颜色反向/滚动的 6 帧、动态改宽立即样本 1 帧、透明背景 elevation 的共享阴影 2 帧，以及 contained 展开组合的收起/反向 8 帧。

contained 与顶栏组合在 1088000µs 的关闭样本中，两端主进度均为 0.500186920166，full-screen 标记为 true、请求的 search-visible 为 false，但 Core 图像仍绘制收起搜索胶囊，Avalonia 已隐藏。固定源码与实际参考字节码的可见性条件相同；持续绘制的具体提交/缓存原因尚未确定。`search-visible` 只比较当前状态表达式 / 本地 Opacity 请求值，不冒充 Core 实际图层 alpha；实际可见性由 Raster 独立验证。测量来源补充后的单例复核 `20261104-132247-f102bcef` 保留相同差异，执行错误为零且重复稳定。保留源代码条件和这些失败样本，不为匹配参考帧改写为延迟隐藏。

**共享提交边界的当前范围**：本轮依据用户扩大授权重新决定，AppBarWithSearch 的动画容器颜色已使用共享 AnimatedState 帧末提交及取消后重启，颜色反向场景所有帧采样图像相等；非帧输入时内部搜索背景即时变化的两张图像仍不同。其他属性/Insets 的提交边界与原生编辑行度量仍待解决。推荐在 ComposeAnimation、MaterialWindowInsets 和 TextField 的现有能力内调查；例如 `state.SnapToAsync(.021f)` 在非帧时刻执行后，本地立即显示而 Core 在重组时显示，或 `WindowInsets="12,20,24,16"` 的初始传播顺序不同。其共享影响需要独立验证；不在搜索组件中补帧、补 2.5 DIP 或把差异记为已接受。新版状态 InputField 的具体推荐接入及影响见第 3 节。

搜索遮罩的采集场景显式把 Avalonia 根内容色设为黑色，匹配固定 Core `LocalContentColor` 的 Black 默认；Core 参考端未改动。Avalonia Window 主题会默认注入 OnSurface，而测试中的 Core Box 不提供 Surface 的内容色；不对齐这个外层环境会把正确继承外层色的波纹误判为组件颜色差异。组件自身并未固定为黑色，行为测试验证父级 Blue/Red 的实时继承。

Windows Catalog 已实测自动聚焦、查询过滤、选择结果收起及焦点恢复、返回按钮、深色/RTL、遮罩右键不关闭和主按钮关闭；搜索顶栏演示额外验证显式上滚减少栏高及停止后吸附。输入只导出一次；触摸、屏幕阅读器遍历及移动端 IME/预测返回仍未经这些桌面测试验证。

完整控件测试通过 **2065/2065**，VisualParity 系统测试通过 **124/124**，解决方案与双端 Release 捕获端构建成功。构建仍有既有 Avalonia 资源、TextField 空引用分析等警告，未为清理警告扩大本组件改动范围。验证日志与捕获产物保存在本机 `artifacts/`；上述图像超差仍未解决，执行成功不代表所有上游行为和图像一致。

2026-11-05 输入交互源补齐：`SearchBarInputFieldTests` **13/13** 通过，Catalog 构建成功。双端 `search-bar-input` 运行 `20261105-063820-587b4028` 共 15 例，执行错误 0，全部重复稳定。新增外部 Focus、真实焦点的配对 Unfocus、延迟执行中换源三个场景，实际 focused / expanded / requests / other-focused 全部相等，包括清焦前 99ms 与 100ms 边界；既有编辑行 56 / 58.5 DIP 及插槽纵坐标差异仍存在，15 例整体状态均为 different。聚焦场景沿用原套件的结构化测量，未把不可虚拟化的原生光标时钟纳入图像通过结论；10 个静态场景仍捕获完整图像。
