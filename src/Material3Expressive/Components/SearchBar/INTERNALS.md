# SearchBar 内部机制与维护说明

## 1. 上游基线与职责映射

固定版本见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 为逻辑依据，Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 为桌面参考。

| 上游符号 / 职责 | 本地类型 / 成员 | 对应说明 |
| --- | --- | --- |
| `SearchBar.kt / DockedSearchBar(inputField, expanded, ...)` | `DockedSearchBar`、`SearchBar.axaml` | 双插槽、受控展开、Surface 与分隔线；这是固定版本仍保留但已弃用的布尔状态重载，不冒充新版 `SearchBarState` 组合 |
| `Column`、`SearchBarMinWidth`、`heightIn` | 两个内部布局 Panel | 360 首选宽度，结果最小 240、最大窗口高度 2/3，服从父级约束 |
| `DockedEnterTransition` / `DockedExitTransition` | `SearchBarMotion`、现有 `AnimatedVisibility` | 延迟 100ms；进入 600ms `(0.05,0.7,0.1,1)`，退出 300ms `(0,1,0,1)`，淡入淡出叠加底部对齐的纵向展开收缩 |
| `SearchBarDefaults` / `SearchBarTokens` / `SearchViewTokens` | `SearchBarTokenSource` | SurfaceContainerHigh、Outline、ExtraLarge；默认 tonal/shadow 均为 Level0，不误用未被默认参数采用的 Level3 |
| Core `SearchBar.skiko.kt / getWindowContainerHeight` | `DockedSearchResultsLayout` | 观察 TopLevel.ClientSize，以布局比例将窗口高度和 DIP 常量转换至像素取整 |
| Core `BackHandler.skiko.kt`、`BackNavigationEventInput` | `CollapseRequested` | 未消费的 Escape KeyDown 请求关闭 |
| `SearchBarDefaults.InputField(query, expanded, ...)` | `SearchBarInputField` | 同一布尔状态重载；原生单行 TextBox、受控展开/搜索事件、收起延迟清焦 |
| `SearchBarSearch` / `SuggestionsAvailable` | 组件内部 peer 与 `SearchBarStrings.json` | 固定 Core `skikoMain/.../l10n` 的 86 个地区映射；Name / ItemStatus 投影，保留原生 IValueProvider，应用显式值优先 |

## 2. 关键机制与架构契约

应用拥有 `IsExpanded`，关闭请求不代写状态。输入槽保留自己的逻辑子节点和 ContentPresenter 注册；结果 ContentPresenter 位于稳定的 AnimatedVisibility 内，退出期间不替换 Content，动画完成才释放呈现。附加/分离负责窗口按键与尺寸订阅，重挂载按当前目标初始化。

窗口事件仅在 Bubble 阶段处理未消费的 Escape，视觉树最后一个已展开且可见的 DockedSearchBar 获得请求。它是组件内部的桌面接线，不建立通用返回键调度框架。上游的组合注册顺序与 Avalonia 视觉树顺序并不对任意重排严格等价；复杂多层返回栈不在这里近似实现。

布局遵循先测输入、再测剩余高度内的结果；结果又先测分隔线再测内容。输入与结果的内容对齐默认 Stretch，固定尺寸或显式对齐仍由槽内控件控制。动画状态、反向中断和作用域生命周期复用仓内 ComposeAnimation，不能用原生 IsVisible 代替 Visible。

输入字段继承现有 MaterialTextFieldBaseTheme，保留 PART_TextPresenter / PART_ScrollViewer 和原生编辑状态；不建立第二套文本引擎。两个图标复用原有 48 DIP 槽，仅加上游逻辑方向的 ±4 DIP 平移。容器透明、边线透明，默认文字与 leading 为 OnSurface，placeholder/trailing 为 OnSurfaceVariant，禁用色、选区、光标继续引用 FilledTextField Token。

清焦任务只在 IsExpanded 改变时启动，对应 `LaunchedEffect(expanded)`，用根 AnimationScope.DelayAsync 的可取消延迟。收起瞬间若聚焦，100ms 后调用窗口 FocusManager.Focus(null)；期间重新展开或分离取消。按上游语义，任务启动后即使焦点转向别处，也不额外重判原输入的 IsFocused。默认 accessibility 名称使用当前 UI Culture；展开状态映射到原生 ItemStatus 并发属性变化通知，未建立完整 Compose semantics 树。

## 3. 已确认决定与已知差异

- **授权范围**：本次任务明确授权上游语义内新增组件及非破坏性组件 API；没有新增公共共享框架或改动既有共享决定。
- **尚未实现**：新版 SearchBarState / ExpandedDockedSearchBar、全屏搜索、TopSearchBar / AppBarWithSearch；本组件的布尔状态 API 不代表这些能力已移植。
- **未经验证**：触摸输入、复杂多弹层返回栈及屏幕阅读器遍历顺序；桌面单窗口 Escape、基本多容器顺序与 handled 事件有行为测试。
- **共享适配**：动画尺寸沿用 ComposeAnimation 的 DIP/double 计算，Compose 的 IntSize 中间帧可能在像素取整上不同；不将检测到的视觉差异登记为新批准的近似。

## 4. 验证入口

- `tests/Material3Expressive.Tests/DockedSearchBarTests.cs`：约束、窗口尺寸变化、模板延迟创建、插槽更换、反向动画、分离清理、Escape 与 Token 更新。
- `SearchBarInputFieldTests.cs`：单行/宽度、受控事件、清焦时点/取消、图标偏移、显式属性优先级、原生可编辑值与本地化无障碍状态。
- `./tests/VisualParity/parity.ps1 self-test --family docked-search-bar`：真实 Compose 与 Avalonia 容器，静态深浅色/RTL/大内容/小窗口，以及展开收起、反向、内容尺寸改变、Escape、分离重挂载。结果以生成报告为准，不通过更改容差隐藏差异。
- Catalog：`SearchBarDemo`。

2026-11-04 双端验证：容器运行 `20261104-044046-6980d7a5` 的 12 例均稳定、执行错误 0，七个静态图像（含 RTL）逐像素相等。RTL 角点顺序仍因 Avalonia 镜像不同，保留四角读数。动态进入中间帧 Avalonia 比 Core 早约 16ms，终态相等，没有加入组件延迟去拟合报告。

输入字段运行 `20261104-044224-c34401a6`：12 例执行错误 0、重复/额外截图稳定；焦点、展开、请求次数在所有采样一致。十个静态图像仍 different：空编辑行使 Core 自动高度为 58.5 DIP、本地为 56 DIP，槽位竖直中心相差 1.25 DIP，窄父级宽度与图标横向位置相等；RTL 同时保留角点顺序差异。焦点动态采样沿用 TextField 的无图像观测，以免原生光标平台定时器干扰；静态保留完整图像。初次输入用例误用系统延迟时间，已改为 AvaloniaHost 现有手动时间提供器后重新运行，未改变组件、基线或比较容差。

**待维护者决定**：共享动画提交时点与原生编辑行度量的处理范围。推荐先在 ComposeAnimation 提交边界、TextField 原生度量能力内调查，而非搜索组件内补 16ms / 2.5 DIP；例如 `bar.IsExpanded = true` 在非帧时刻赋值，两端内容实现/目标提交不在同一帧。改变该共享协议会影响其他 AnimatedVisibility/TextField 调用方，需独立设计确认；上述差异没有登记为已接受。
