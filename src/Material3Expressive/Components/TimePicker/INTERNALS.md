# TimePicker / TimeInput / TimeScroll 实现说明

使用方式见 [README](README.md)。

## 固定上游与本地入口

基线为 AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 的 `TimePicker.kt`，
与 Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的同名文件一致。
身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)。

| 上游 | 本地 |
| --- | --- |
| `TimePicker`、钟面、`AnalogTimePickerState` | [TimePicker.cs](TimePicker.cs)、[ClockFace.cs](ClockFace.cs) |
| `TimeInput`、`timeInputOnChange`、小时/分钟合法性 | [TimeInput.cs](TimeInput.cs)、[TimeEntryController.cs](TimeEntryController.cs) |
| `TimeScroll` | [TimeScroll.cs](TimeScroll.cs)，列用 Avalonia `DateTimePickerPanel` |
| `ScrollField.kt` / `ScrollFieldDefaults.Item` | [TimeScrollPanel.cs](TimeScrollPanel.cs)，按页面身份保留的选中排版与颜色动画 |
| `TimePickerTokens`、`TimeInputTokens` | [TimePickerTokenSource.cs](TimePickerTokenSource.cs)、[TimeInputTokenSource.cs](TimeInputTokenSource.cs) |
| 布局、变体、当前字段 | [TimePickerOptions.cs](TimePickerOptions.cs) |
| `SideControlColumn` / `SideControlItem` | [TimeSideControlLayout.cs](TimeSideControlLayout.cs)、`MaterialTimePeriodTheme.side-control` |

三个控件共用 `TimeEntryController`，但各自持有一份。公开值是 `TimeOnly` 和 `Is24Hour`，没有可在控件之间传递的 `TimePickerState`。
时间输入字段在派生 TextField 主题中设置 `MaterialText.Style`：Standard 使用 TimeInput 的 DisplayMedium，Vibrant 使用 DisplayLarge，并由原生继承传给 TextPresenter。旧 `DefaultStyle` 会被基础 TextField 的 BodyLarge `Style` 覆盖，不用于这两个字段。
字段的 `HorizontalContentAlignment` 使用 `Stretch`，由 `TextAlignment.Center` 在完整编辑视口内居中文字。若把窄 TextPresenter 本身设为 `Center`，SmoothScroll 12.0.0 的合成平移会抵消其布局居中位移，出现布局坐标居中但实际数字靠左的情况；回归检查同时读取实际渲染像素。
维护者于 2026-10-11 确认这种直接绑定。`SelectedTime` 会被收成整分。无效草稿留在文本里，并用 `IsInputValid` 表示；AM/PM 在小时无效时禁用。

程序构造的字段、时段和间距通过组件局部动态绑定消费尺寸 Token；使用既有 TokenBindingConverter 解析引用链，资源变化原位更新布局，不重建输入字段。纵向时段高度仍跟随字段高度，时段按钮扣除的间距与容器 Spacing 使用同一 Token。钟面标签、中心和手柄尺寸可配置，半径、命中区域及 256 归一化基准保留原算法。

## 钟面与滚动

绘制角度以 3 点为 0，顺时针增加；初始 12 点为 -π/2。24 小时根据触点是否落在内圈决定加 12。
`ClockFace.Pointer.cs` 直接保留 `ClockDialNode` 的 down 坐标、累积位移与扣除 slop 后的拖动增量。
Core 鼠标 slop 为 0.125dp，触摸为 18dp；计算在物理像素 Float 坐标中进行。
点击按 5 分钟取整，拖动按 1 分钟写回时间，同时保留连续手柄角度。点击小时完成旋转后再延迟 100ms 自动切到分钟；
拖动在 release 时切换字段并以 PreventUserInput 收拢。release 不额外读取一个拖动坐标，捕获丢失不收拢或自动切换。
24 小时切换内外圈与旋转的调用顺序分别沿用 tap 和 drag 的上游顺序；低优先级旋转被拒绝时不先写离散时间。
指针保留 `Animatable<float>` 的即时 `SnapToAsync` 与弹簧 `AnimateToAsync`，不改成帧前合并目标的 `AnimatedState`。
控件通过 `AnimationScope.Get` 取得框架管理的作用域，在独立子作用域中运行、订阅角度和等待切到分钟；运行显式传入作用域。
分离视觉树时只释放自己的子作用域，立即取消表盘工作；重新附着从当前选中时间建立角度状态。
祖先作用域重建时通过 `ScopeChanged` 重建子作用域及订阅，从保留的角度重新运行到当前目标。
重建还会清除旧作用域留下的字段提交等待标记，并重新连接 Crossfade 的映射 Source，保留组件拥有的父 Transition。
回归覆盖字段提交前显式结束作用域，以及随后重挂载。此手动恢复不承诺内置动画宿主的速度及帧基准恢复。
`ClockFace.Keyboard.cs` 对照 `ClockText` / `ClockFace`：选择器激活后，只有当前钟面的标签可聚焦，
初始焦点在选中刻度；方向键按外圈、内圈的顺序循环，仅移动焦点。Enter/Space KeyUp 用 SnapSpec 语义确认，
不触发自动切到分钟。Avalonia 选择器在 KeyDown 激活并转移焦点；新刻度不消费该次 KeyUp 作为第二次激活，
仅配对钟面自身收到的 KeyDown/KeyUp，避免抢先 Snap 并取消字段切换。Tab/Shift+Tab 交回选择器、时段按钮或后续控件，离开后钟面标签退出 Tab 序列。
外部时间改变时，已激活钟面的焦点跟随新的选中刻度。模板复用现有 MaterialSurface 圆形状态层，
不再继承原生日期按钮的矩形 focus-visible 描边；ClockText 的 ripple 半径是 MinimumInteractiveSize / 2。

角度动画接入已有 `MutatorMutex`：程序/字段切换动画是 PreventUserInput，指针、键盘和辅助技术确认是 UserInput，
低优先级确认在动画期间被拒绝，不排队。作用域重建后取消的动画即使 TargetValue 未变仍从当前位置恢复，
不能把 isUpdated 的短路直接用于框架作用域重建。

`ClockFace.Labels.cs` 复用现有 `Transition<TimeSelection>` 与 `Crossfade`，使用 DefaultEffects；指针旋转使用 DefaultSpatial。
新字段在下一帧提交节点更新，保留动画中断与当前速度。旧屏保留原数字，但内圈是否存在仍读取实时 Selection；内圈数字始终是 12–23。
文字采用统一未选中色，`ClockFace.Drawing.cs` 以 Skia 的 Clear → 内容 → Xor → 线/中心 → DstOver 绘制手柄和被覆盖文字，
不再给整个离散刻度套选中前景色。自动化 `ISelectionItemProvider` 的 selected 根据标签矩形是否包含即时 selectorPos 更新，
与键盘使用的最近离散刻度分开。标签的辅助技术 Invoke 与键盘激活复用 tap 逻辑，使用瞬时角度确认。

自动切换同时检查既有窗口 `MaterialInputMode` 与宿主提供的 `TouchExplorationEnabled`，沿用 FloatingToolbar 的宿主状态入口约定。
固定 Core 的 `AccessibilityServiceStateProvider.skiko.kt` 始终返回 false，因此真实桌面辅助技术状态未能由参考端验证；
本地禁用自动切换路径有行为测试，不宣称已经完成系统服务探测。

Standard 字段使用 `TimeSelectorContainerWidth`（96dp），不消费虽然存在于 Token 表中的 114dp 24h Token。
Vibrant 使用宿主竖向且 24 小时的 132dp 分支，否则 100dp；这是 `defaultTimePickerLayoutType()` 的窗口方向，
与用户显式选择的钟面 Layout 分开。宿主方向改变时重建相关布局；其他尺寸 Token 变化仍原位更新。

`TimeScroll` 的小时列在 12 小时模式是 1 到 12 且循环，24 小时模式是 0 到 23。分钟列是 0 到 59。
12 小时的 12 写回 0 或 12，取决于当前是 AM 还是 PM。
初始化原生滚动承载前同时设置面板的 SelectedValue 和 ScrollViewer.Offset；否则 ScrollContentPresenter 的零偏移会覆盖初始有效时间。原生滚动适配的范围不变。
ItemHeight 在附着或资源变化后重新解析时，同步面板重新计算的 Offset 到 ScrollViewer，避免旧像素偏移反写选中时间。
内部 TimeScrollPanel 保留每个已实现页面的两条 AnimatedState：选中分数与颜色均使用 FastSpatial，分数钳制到 0–1 后对 DisplayMedium / DisplayLargeEmphasized 插值。
字号、行高、字距及字重按上游插值，其余已支持排版字段按 0.5 切换；颜色独立保留弹簧和中断速度。
原生容器改变 Tag 时转接页面状态的 Animation 优先级绑定；循环偏移重新归一化不更换页面身份，离开实现区或卸载时释放状态和绑定。
页面高度消费 `Comp.TimeScroll.ItemHeight`，默认保留 Float `200f / 3f`；容器消费 SurfaceContainerLowest / Large，选中与未选中色分别为 OnSurface / Outline。

`TimePickerTextField` 保留编辑器实例与草稿，只显示当前字段；另一字段使用 TimeSelector 按钮。选择变更在视觉树附着后请求焦点，分离后的延迟请求不再抢占焦点。
输入路径保留 `timeInputOnChange` 的解析顺序、三字符且光标位于 1 时的覆盖分支、前导零、无效草稿、拒绝替换时的旧选区和有效小时自动推进。
Kotlin/JVM 接受的 Unicode 十进制数字先按数字值解析，编辑器仍保留原文；负数选择器按包含符号的最小两字符宽度格式化。
小时末尾数字键沿用 `shouldSwitchFocusToMinute`；触摸探索只抑制输入值变化触发的自动推进，不拦截显式选择、Enter 或该末尾数字键分支。
`SupportingText` 按 Standard / Vibrant 与合法性显示，保留 7dp 上间距和至少两行；有效提示排除内容自动化，错误为 polite live region。
24 小时错误文案补入现有本地化表，逐语言取自固定 Core 提交；不改既有翻译。原生 TextBox 的 IME、撤销栈及光标绘制仍由 Avalonia 承载。
光标画笔按上游保留 Primary 色（包括错误态）及上下各 10% 的透明区，动态跟随消费位置的颜色资源；原生文本布局和闪烁时钟没有替换。
普通/输入数字选择器通过内部 `TimeSelectorButton` 提供 RadioButton role 与受控 SelectionItem，Select 走同一个原生 Invoke 路径，不另写时间值。

## 已确认的适配

滚动承载和日期月份列表一起，于 2026-10-11 被接受为局部适配：不移植 Compose `VerticalPager` 或 LazyList 物理。
`time-picker` 视觉用例允许真实时间文字，失配不记为已接受差异。见 [AUTHORING.md](../../../../tests/VisualParity/AUTHORING.md)。

固定版本的 `isUpdatedTimepickerToggleEnabled` 默认为 true。`TimePeriodButton` 为组件内部受控 ToggleButton：
禁用原生 Toggle 的自写入，由 controller 在有效小时变更后同步两个 IsChecked，重复点击当前时段不取消选择。
主题基于现有 FilledToggleButton，静止 Circle、按压/选中 12dp，复用 FastSpatial 形变、反馈与按压 collector；
选中标签使用 Bold。保留 Invoke，同时提供 Toggle/selected 自动化状态，未新增公共交互框架。
标准间距 4dp；Vibrant 横向 16dp、纵向 8dp 独立消费组件 Token。原生 Enter/Space 激活沿用既有按钮约定。横向时段使用 LeftToRight 放置，保留上游 absolute place 的 RTL 顺序。
无容器 SideControlColumn 由内部 Panel 保留固定 48dp 子项分配、整数物理像素居中和三个子项时额外 3dp 偏移。AM/PM 实际视觉为 40dp，鼠标命中不扩展到外侧空白；24 小时仅居中模式内容。
Toggle 与仅有 ToggleTemplate 都参与侧栏分支；字段高度与 120/140dp Token 保持动态绑定，普通 contained toggle 的布局独立保留。
输入 Standard 的 `PeriodSelectorSpacing` 为 4dp；Vibrant 使用新增 `VibrantPeriodSelectorSpacing`（8dp），滚动列的 `PeriodSelectorSpacing` 也为 8dp。对应 120dp 字段的两个普通时段按钮各 56dp；修改资源仍原位更新，不清除草稿。

## 尚未实现或未验证

- 新增 `time-picker-side-control` 双端场景覆盖 12/24 小时、RTL、明暗、模板模式内容、按压、外缘鼠标命中及移除/恢复。
  `time-input-presentation` 覆盖 Standard/Vibrant、错误提示、焦点切换、无效草稿和解析边界；动态编辑用例只比较真实状态与布局，因原生光标时钟未虚拟化不采动态图像，静态有效/错误状态保留完整图像。
  28 个输入用例覆盖包括 Unicode 数字的解析与外部字段更新。除下述外部同步细节外，选择/焦点/文本/有效时间/合法性/错误 live region 数量一致；错误提示的整体高度仍有 1 DIP 差异，未改容差或补偿高度。
- 连续在小时、分钟输入无效的 `99`、`75` 后设置 `SelectedTime = new(5, 30)`：Core 的 `userOverride` 为 false，会跳过该次 hourValue 同步；回到小时仍显示 `99`，但有效时间已为 05:30。本地原有绑定行为显示 `05`，本次仅让未变更的分钟继续保留 `75` 与选区。
  推荐维护者保留按实际变化字段同步外部绑定的行为；严格复现上游会使有效值、合法性与编辑文本脱节。这个具体差异尚未批准，不能由既有 TimeOnly API 决定推断已接受。
- 真实触摸外缘用例保留了 Core 命中、本地不命中的差异。现有共享决定只允许 `MaterialInteractiveSize.Minimum` 预留布局，未提供触摸命中扩张。
  推荐在共享输入层支持触摸专用的最小目标搜索，保留鼠标的 40dp 视觉边界、重叠候选的距离与绘制顺序；调用方继续使用 `MaterialInteractiveSize.Minimum="48"`，不在每个按钮内补发点击。
  这会改变现有共享决定的能力范围并影响所有小按钮，需要维护者决定，本组件没有自行接管。侧栏图像自检中仍保留部分 Avalonia 光标闪烁造成的不稳定证据。
- RTL 侧栏的实际包围盒与尺寸一致，但原生镜像变换使四角顺序与 Core 不同；模式内容移除/恢复及按压提交存在帧前即时更新和 Compose 帧提交的时序差异，原始读数保留。
- `TimeScroll` 的项目动画、页面高度、颜色与容器形状已实现。`time-scroll` 双端 13 例无执行错误，重复及额外截图稳定；9 个静态配置的已采集几何、字号、字重和颜色相等，全部图像仍有差异。
  外部 SelectedTime 改变时，原生面板立即更新页面；Core 经过 TimeScroll 的 LaunchedEffect、pager 测量和后续重组后才提交项目动画，动态中间值因此不同，640ms 的选中切换终值相等。
  该时序差异没有通过延迟本地事件或调整采样隐藏，也不由原生滚动适配自动追认为已接受。字体行布局和页面像素舍入仍需后续核验，不能宣称整项视觉一致。
  参考夹具显式使用 `Typography(fontFamily=...)`，因为固定上游的无参旧构造函数会把强调角色初始化为普通角色；两端因此消费相同的完整角色配置。
- 本批次在隔离 worktree 使用本机临时依赖缓存完成补充验证。伙伴更新 NuGet 源后再次执行正式 `--locked-mode` 还原仍失败：公开源缺少锁定 MaterialColorUtilities CI 版本和 MaterialSymbols.Avalonia.Rounded 包。
  临时缓存的两个 MaterialSymbols 包内容哈希又与仓库锁文件不符，故补充构建/测试不能替代固定 NuGet 身份的最终验收；仓库锁、版本及参考基线均未修改。
  补充构建包含 Catalog；全库行为/渲染测试 1902 项通过，VisualParity 系统测试 124 项通过。定向双端输入/侧栏/键盘用例分别为 28/38/4，均无执行错误；输入用例重复稳定，4 个侧栏用例和 4 个既有键盘用例仍有图像重复性问题，键盘结构化状态一致。
  双端执行成功不代表像素已验收；字体、本地化、颜色与上述差异仍未全部消除。

- 已实现指针 slop、连续角度、优先级、标签 Crossfade 与 selector 混合绘制；双端尚未全部相等，不能由行为测试通过推断像素验收。
- Core 的 `SyntheticEventSender.skiko.kt` 在坐标改变的鼠标 release 前合成 move，Avalonia Headless 的同一次 release 不合成。`release-jump` 用例保留不同时间结果，尚未批准合成额外输入的适配。
  推荐正常轨迹显式发送 `Move(x, y)` 再 `Up(x, y)`，保留跳坐标 release 作为平台差异证据；若统一共享 Headless 合成器，
  会影响所有拖动组件，需要维护者决定，不能在 ClockFace 内补发事件来隐藏。
- 程序外部修改 `SelectedTime` 的角度更新与固定上游不同：`AnalogTimePickerState` 只重写 `hourInput/minuteInput`，
  `TimePickerImpl` 的外部变更 effect 却写委托的 `hour/minute`，使保存的 angle 保留旧值。例：初始 00:00，设置 00:30 并选择 Minute，
  Core 仍指向 0，本地转向 30；因此在该过程点击 15，Core 接受、本地被 PreventUserInput 拒绝。
  推荐维护者明确决定是否保留本地外部绑定同步；严格复现该上游行为会使 `picker.SelectedTime = new(0, 30)` 后的手柄与显示脱节。
  本次保持既有本地绑定行为，`reject` 用例保留证据；`field-transition` 则以初始 00:30 只切字段验证真实优先级。此推荐未获确认。
- 切换后立即读取的新标签 selected 语义存在一次提交时序差异；中断后个别 Float 角度有最大约 1.2e-7 rad 的差异，未放宽精确门禁。
- 选择器 Clear/Xor 的边缘透明度不同：Core PNG 有 alpha<255 的像素，Avalonia 捕获对应像素为 alpha=255，RGB 接近其预乘值。
  例如 Core (223,218,232,185) 对应 Avalonia (162,158,168,255)。尚未证明是捕获目标或控件合成层的哪一环造成；不能按普通字体噪声忽略。
- 时段静态顺序已修正；按压过程的形变/反馈图像、本地化文本（参考进程“上午/下午”，本地“AM/PM”）、
  字段数字基线及焦点描边差异仍保留证据。分隔符沿用上游 -4dp 偏移。
- `Automatic` 在钟面上比较 TopLevel 客户区的宽高，并在客户区尺寸变化后，仅当横竖结果改变时重建。初次套用模板时如果还没有 TopLevel，会保持纵向，直到控件进入窗口。
- 秒、分钟步长只存在于原生 `TimePicker` 主题。
- `ClockFace` 混合绘制依赖 Avalonia.Skia，与本库其他自绘 Material 控件一致；消费实色画笔的 Color × Brush.Opacity × Skia 租约 CurrentOpacity，非实色 selector 不绘制。
  后内容绘制层独立绑定画笔和尺寸，附着期间监听可变画笔，卸载取消监听，避免静止时保留旧显示列表。
  非 Skia 后端及分数缩放未完成验证；父级逐次绘制的透明度与 Compose 整层 alpha 仍有合成差异，调用方可用已有 RenderOptions.RequiresFullOpacityHandling 显式选择整层合成，未改全库默认值。

`ClockDrawingTests` 的 5 项真实 Skia 像素测试通过，覆盖画笔/祖先透明度相乘、可变颜色和透明度、替换画笔、轨道宽度及选中文字色的静止重绘。
新增 `time-picker-alpha` 自检 `20261104-155644-a400d926`：10 场景、14 采样，无执行错误，重复及额外截图稳定；几何全部相等，整图仍全部 different。
显式整层合成的两例中，已检查的轨道与手柄内部像素分别精确对齐；默认逐次绘制模式仍保留合成差异，未改用例输入或容差来消除它。
两画笔各 .5、没有父级 alpha 时，手柄空白点 Core 为 (0,0,85,192)，本地 PNG 为 (0,0,64,255)，后者符合预乘 RGB 被按不透明输出的特征。
本机 Avalonia 源码 `HeadlessWindowImpl.GetLastRenderedFrame` 以 Opaque 构造副本；仍需核验锁定包及原 framebuffer，以区分捕获与组件混合层。
推荐维护者决定共享捕获入口是否改为保留原 framebuffer 的 AlphaFormat（被动复制，不追加 Draw/Frame），这会影响所有含透明像素的 VisualParity 产物与比较；本次未更改测试宿主、基准或规范。
字体布局、标签 Crossfade 及上文既有状态/输入差异不因本次画笔修复而消除。

逻辑检查在 `tests/Material3Expressive.Tests/DateTimeEntryTests.cs` 和 `ClockKeyboardTests.cs`。
双端用例是 `tests/VisualParity/Material3Expressive.Parity/Cases/TimePickerCases.cs` 与 reference 侧同名 Kotlin。
`TimePickerKeyboardCases` 使用真实按键，读取实际 focused 语义、时间及字段，覆盖 12/24 小时、LTR/RTL、
切换动画期间的拒绝和结束后的确认；图像失配和 Core 重复截图不稳定保留在报告中，不作为已接受差异。
运行 `20261103-142211-539f9114` 的 4 个用例、68 个采样的焦点刻度、时间和字段读数均相等；
图像仍全部 different。Avalonia 重复及额外截图均稳定，Core 的 12h.ltr 在重激活/离开分钟的截图不稳定：
被动额外截图对照中有 60 个物理像素发生最大 1 通道值差异，集中在分钟选择器边缘。
尚不能宣称动画图像验收通过，不通过降低门禁消除该证据。


本次新增 `ClockPointerTests` 覆盖 tap 的旋转/延迟顺序、连续 drag、触摸 slop、捕获丢失、内外圈、优先级拒绝、
辅助技术 Invoke、触摸探索、反向 Crossfade、作用域替换与 detach 清理；`TimeDensityTokenTests` 检查 Standard/Vibrant 字段宽度和宿主方向。
`TimePickerPointerCases` 双端读取真实角度、时间、字段、当前渲染标签数及 selected 语义，覆盖 LTR/RTL、12/24 小时、
Expressive/Standard motion、连续拖动、tap 中断和字段往返。新用例显式对齐字体和相关色彩输入，另留默认平台字体/颜色分支；
既有 time-picker 与 time-picker-keyboard 的参考配置及比较容差不变。

`TimeScrollTests` 检查初始 12/24 小时时间、页面复用、循环边界、反向、排版/颜色/形状/高度资源变化、配置缺失和卸载后的属性贡献恢复；还遍历全部 60 分钟的正反向滚动及不同 ItemHeight 下的程序更新，防止原生循环 offset 回写改变时间。
双端入口：`./tests/VisualParity/parity.ps1 self-test --family time-scroll`；案例文件为 `TimeScrollCases.cs` / `.kt`。

TimeScroll 最新验证 `20261104-044242-adbed8d6`：13 例执行错误 0、重复及额外截图稳定，图像均 different。静态选中字号/字重/颜色及布局读数相等；数字和冒号在本地约低 1 DIP，非选中数字存在更小的基线差；动态选中提交的动画启动与 Core 约差 32ms，终态一致。未修改容差或增加视觉补偿。原生滚轮是已有批准的适配，不由此声称 Compose Pager 的手势/惯性一致。

参考端另一次运行 `20261104-041703-d9b8493f` 在反向场景的额外截图出现 `performMeasureAndLayout called during measure layout`，栈涉及 SwingDispatcher 定时任务与 Pager.scrollToPage；随后完整重跑得到上述稳定结果。若重现，推荐调查参考宿主 UI/主协程调度隔离，影响 VisualParity 共用时钟与线程模型，须按其设计约定处理；不通过固定源升级、删除额外截图或重写门禁规避。

时段切换用例覆盖 Standard/Vibrant、Horizontal/Vertical、LTR/RTL 的按压、选中形变和重复点击；相关行为由 `ClockPointerTests` 的 period theory 覆盖。


2026-11-03 新指针/时段双端运行 `20261103-152618-7f42848e`：32 用例、417 采样，执行错误 0，重复及额外截图稳定；32 用例均为 different，不能宣称视觉验收。
结构化差异集中于上述外部更新/release 语义、立即提交的标签及少量 Float 角度；时段用例的时间、字段、角度和 selected 读数相等。
RTL 时段布局及分隔符修正后，`20261103-153813-33a5c30e` 单例再次稳定、无执行错误，图像仍 different。
参考端普通增量目录遇到 Windows 文件占用，本次用独立 Gradle buildDirectory、禁用增量编译并保持 strict 依赖校验构建固定来源 JAR；未改变依赖锁、参考版本或门禁。

键盘最新回归 `20261103-154100-dcdf2810`：4 用例、68 采样的焦点、小时、分钟和字段读数全部相等，执行错误 0；
图像全部 different。Avalonia 重复和额外截图均稳定；Core 四例在 reactivate-minute / leave-minute 的重复或额外截图有波动。
这替代旧键盘结果作为本次状态，不把参考端波动或图像差异视为已接受。

复现入口：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter 'FullyQualifiedName~ClockPointerTests|FullyQualifiedName~ClockKeyboardTests|FullyQualifiedName~TimeDensityTokenTests'
./tests/VisualParity/parity.ps1 self-test --family time-picker-pointer
./tests/VisualParity/parity.ps1 self-test --family time-picker-keyboard
```

本次最终本地验证：解决方案构建 0 错误（7 条现有警告），控件库测试 1,856/1,856，VisualParity 系统测试 124/124；`git diff --check` 通过。
这些构建/行为结果不替代上面的双端图像验收，也不扩展已确认适配的范围。
