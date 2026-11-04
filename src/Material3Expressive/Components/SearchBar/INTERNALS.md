# SearchBar 内部机制与维护说明

## 1. 上游基线与职责映射

固定版本见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 为逻辑依据，Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 为桌面参考。

| 上游符号 / 职责 | 本地类型 / 成员 | 对应说明 |
| --- | --- | --- |
| `SearchBar(state, ...)`、`SearchBarState` | `SearchBar`、`SearchBarState` | 收起 Surface 与坐标身份；独立进度/内容 Animatable，保留阈值、目标判定和中断 |
| `ExpandedDockedSearchBar`、`DockedSearchBarLayoutImpl` | `ExpandedDockedSearchBar`、`ExpandedSearchBarLayout` | 零布局声明、默认可聚焦弹层、以物理像素插值整个容器高度 |
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

状态复用窗口 AnimationChildScope，最后一个消费者卸载时取消自身任务，重挂载保留当前值；动画启动前必须已有挂载消费者和有效规格。这个生命周期遵循仓内其他应用持有状态的接入约定，不建立脱离窗口的公共时钟。跨 TopLevel 共享一个状态不属于当前支持范围。

展开容器复用现有 `ModalPresenter` 的焦点隔离、Tab 循环、嵌套 Popup 所有权、返回键和焦点恢复，只在组件内部增加物理位置与外部 PointerPressed 行为。Core 默认 Popup 的任意按钮外部按下会收起并消费事件；Escape 在 KeyDown 请求收起。输入在弹层创建时聚焦，窗口首次显示过早的请求只重试一次，进度更新不重新抢焦点。零布局声明的 peer 不重复导出 PopupAutomationPeer 路径，真实内容由 overlay host 导出。

仅当 CurrentValue 为 Expanded 才呈现弹层。内部关闭操作属于弹层生命周期，到 .02 阈值移除弹层并取消剩余动画，因此可能留下接近零的进度；应用直接调用的外部关闭任务仍运行至零。输入/结果模板随弹层释放，再次打开重新构造。新弹层最小 240 DIP 指整个 Surface，旧布尔容器则指结果区域；两者不能共用相同最小高度布局。上限取窗口高度 2/3，再和输入高度按 raw 进度进行整数像素插值。

固定 Core 将 LayoutCoordinates 对象写入状态；同一个 anchor 的位置/宽度改变并不使静止 popup 重新布局。本地同样只通知坐标身份首次就绪，进度、窗口尺寸或重新打开时读取最新位置；不能把这个上游行为“修好”后宣称一致。

应用拥有 `IsExpanded`，关闭请求不代写状态。输入槽保留自己的逻辑子节点和 ContentPresenter 注册；结果 ContentPresenter 位于稳定的 AnimatedVisibility 内，退出期间不替换 Content，动画完成才释放呈现。附加/分离负责窗口按键与尺寸订阅，重挂载按当前目标初始化。

窗口事件仅在 Bubble 阶段处理未消费的 Escape，视觉树最后一个已展开且可见的 DockedSearchBar 获得请求。它是组件内部的桌面接线，不建立通用返回键调度框架。上游的组合注册顺序与 Avalonia 视觉树顺序并不对任意重排严格等价；复杂多层返回栈不在这里近似实现。

布局遵循先测输入、再测剩余高度内的结果；结果又先测分隔线再测内容。输入与结果的内容对齐默认 Stretch，固定尺寸或显式对齐仍由槽内控件控制。动画状态、反向中断和作用域生命周期复用仓内 ComposeAnimation，不能用原生 IsVisible 代替 Visible。

输入字段继承现有 MaterialTextFieldBaseTheme，保留 PART_TextPresenter / PART_ScrollViewer 和原生编辑状态；不建立第二套文本引擎。两个图标复用原有 48 DIP 槽，仅加上游逻辑方向的 ±4 DIP 平移。容器透明、边线透明，默认文字与 leading 为 OnSurface，placeholder/trailing 为 OnSurfaceVariant，禁用色、选区、光标继续引用 FilledTextField Token。

清焦任务只在 IsExpanded 改变时启动，对应 `LaunchedEffect(expanded)`，用根 AnimationScope.DelayAsync 的可取消延迟。收起瞬间若聚焦，100ms 后调用窗口 FocusManager.Focus(null)；期间重新展开或分离取消。按上游语义，任务启动后即使焦点转向别处，也不额外重判原输入的 IsFocused。默认 accessibility 名称使用当前 UI Culture；展开状态映射到原生 ItemStatus 并发属性变化通知，未建立完整 Compose semantics 树。

## 3. 已确认决定与已知差异

- **授权范围**：本次任务明确授权上游语义内新增组件及非破坏性组件 API；没有新增公共共享框架或改动既有共享决定。
- **尚未实现**：新版状态 InputField、带间隙弹层、全屏/contained 搜索、TopSearchBar / AppBarWithSearch、任意 PopupProperties 与非圆角 Shape、移动端软键盘抑制及 IME Insets。独立弹层的内容槽不代表这些输入/平台协议已实现。
- **未经验证**：触摸输入、复杂多弹层返回栈及屏幕阅读器遍历顺序；桌面单窗口 Escape、基本多容器顺序与 handled 事件有行为测试。
- **共享适配**：动画尺寸沿用 ComposeAnimation 的 DIP/double 计算，Compose 的 IntSize 中间帧可能在像素取整上不同；不将检测到的视觉差异登记为新批准的近似。

**待决定的状态输入接入**：固定新版 `InputField(TextFieldState, SearchBarState)` 将键盘聚焦与展开解耦，并收集 Press.Release、文字长度增长和输入模式。原生 TextBox 的焦点在鼠标按下时改变，Core 此场景在释放时改变；Core 的新版字段即使 enabled=false 也可能通过 Release 收集器展开，而 Avalonia 禁用控件不投递该事件。直接添加 State 属性的原型在双端核验中暴露了这些差异，因此该 API 暂未交付，也未把差异登记为已接受。文字长度观察还涉及 SnapshotFlow 的提交与合并：换状态后在下一重组前增长文字，不能按每次同步 TextProperty 回调直接替代。

推荐维护者先决定 TextField 原生编辑边界下的共享输入事件与提交协议，再接入形如 `new SearchBarInputField { State = searchState, Text = query }` 的组件 API；否则需明确接受原生焦点/禁用投递语义。影响包含禁用字段的交互、Focus 与 Release 顺序、同一事件内多次编辑、InputTransformation/OutputTransformation，以及恢复到收起输入时是否再次展开。当前 Catalog 使用按钮打开和旧布尔输入，避免依赖这项待定方案。调查证据为本机 `20261104-104018-ce4abd78`：14 例执行错误 0 且重复稳定，禁用指针与紧接换状态的文字增长存在实质状态差异；该原型不计入已完成组件或正式回归数量。

## 4. 验证入口

- `tests/Material3Expressive.Tests/DockedSearchBarTests.cs`：约束、窗口尺寸变化、模板延迟创建、插槽更换、反向动画、分离清理、Escape 与 Token 更新。
- `SearchBarInputFieldTests.cs`：单行/宽度、受控事件、清焦时点/取消、图标偏移、显式属性优先级、原生可编辑值与本地化无障碍状态。
- `ExpandedDockedSearchBarTests.cs`：状态阈值/目标、整个弹层约束、反向/卸载、模板释放/状态替换、焦点恢复、嵌套菜单、handled Escape 和 UIA 单一路径。
- 同一测试类还覆盖零宽锚点与无锚点的区别、独立内容动画及调用方取消、右键/中键外部按下消费、呈现期间重新应用模板。
- `./tests/VisualParity/parity.ps1 self-test --family expanded-search-bar`：收起/展开、主题/RTL、窗口/输入尺寸、独立动画规格、过冲、锚点、内容改变、snap、Escape 和外部按下；比较实际 Core Popup，不用旧 DockedSearchBar 代替。
- `./tests/VisualParity/parity.ps1 self-test --family docked-search-bar`：真实 Compose 与 Avalonia 容器，静态深浅色/RTL/大内容/小窗口，以及展开收起、反向、内容尺寸改变、Escape、分离重挂载。结果以生成报告为准，不通过更改容差隐藏差异。
- Catalog：`SearchBarDemo`（布尔原位容器）、`ExpandedSearchBarDemo`（独立弹层）。

2026-11-04 新版弹层双端运行 `20261104-105548-ee5e34fc`：25 例执行错误 0、重复稳定；159 个图像观测中 151 个逐像素相等、3 个容差内、5 个超差。15 个静态图像中 14 个逐像素相等，包括深浅色、RTL、小窗口、自定义尺寸/颜色、无锚点和零宽锚点；3 DIP 阴影用例超差集中于轮廓外缘，几何和结果内部颜色一致，最大通道差 22。沿用 Surface 及 [共享阴影决定](../../../../docs/DECISIONS.md#20-本次保留阴影坐标接入差异) 的接入，不据此放宽容差或将本次超差改写为通过；具体光栅误差来源尚未独立隔离。

过冲规格与独立内容动画用例全部观测相等；普通展开/收起、反向、Escape、外部按下及 tween 图像相等，但部分属性存在约 6e-8 的 Float 尾差。锚点/内容修改、snap 的立即采样仍暴露重组与同步属性提交时点差异，保留这些样本和结果；下一帧不能替代立即采样来宣称一致。RTL 图像相等但物理角点排列不同。无锚点用例的图像、弹层和状态相等，缺失的收起锚点几何明确为 notCompared。

行为测试覆盖 16 个场景（含 theory 数据行），组件测试通过 16/16，完整控件套件通过 2023/2023；VisualParity 系统测试通过 124/124，解决方案构建成功。Windows Catalog 实测过滤、选择结果、输入自动聚焦、Escape、深色 RTL、外部右键关闭与焦点恢复；UIA 输入仅出现一次。触摸、屏幕阅读器及移动端 IME 不由这些测试证明。

布尔容器回归运行 `20261104-105722-a9ca644e` 的 12 例均稳定、执行错误 0，七个静态图像（含 RTL）逐像素相等。RTL 角点顺序仍因 Avalonia 镜像不同，保留四角读数。动态进入中间帧 Avalonia 比 Core 早约 16ms，终态相等，没有加入组件延迟去拟合报告。

输入字段回归运行 `20261104-105845-78467838`：12 例执行错误 0、重复/额外截图稳定；焦点、展开、请求次数在所有采样一致。十个静态图像仍 different：空编辑行使 Core 自动高度为 58.5 DIP、本地为 56 DIP，槽位竖直中心相差 1.25 DIP，窄父级宽度与图标横向位置相等；RTL 同时保留角点顺序差异。焦点动态采样沿用 TextField 的无图像观测，以免原生光标平台定时器干扰；静态保留完整图像。布尔容器的 84 个样本及输入字段的 31 个样本，其属性、图像比较数据和结论均与本次改动前报告一致。

**待维护者决定**：共享动画提交时点与原生编辑行度量的处理范围。推荐先在 ComposeAnimation 提交边界、TextField 原生度量能力内调查，而非搜索组件内补 16ms / 2.5 DIP；例如 `bar.IsExpanded = true` 在非帧时刻赋值，两端内容实现/目标提交不在同一帧。改变该共享协议会影响其他 AnimatedVisibility/TextField 调用方，需独立设计确认；上述差异没有登记为已接受。
