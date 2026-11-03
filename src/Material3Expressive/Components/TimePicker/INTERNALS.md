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
| `TimePickerTokens`、`TimeInputTokens` | [TimePickerTokenSource.cs](TimePickerTokenSource.cs)、[TimeInputTokenSource.cs](TimeInputTokenSource.cs) |
| 布局、变体、当前字段 | [TimePickerOptions.cs](TimePickerOptions.cs) |

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

## 已确认的适配

滚动承载和日期月份列表一起，于 2026-10-11 被接受为局部适配：不移植 Compose `VerticalPager` 或 LazyList 物理。
`time-picker` 视觉用例允许真实时间文字，失配不记为已接受差异。见 [AUTHORING.md](../../../../tests/VisualParity/AUTHORING.md)。

固定版本的 `isUpdatedTimepickerToggleEnabled` 默认为 true。`TimePeriodButton` 为组件内部受控 ToggleButton：
禁用原生 Toggle 的自写入，由 controller 在有效小时变更后同步两个 IsChecked，重复点击当前时段不取消选择。
主题基于现有 FilledToggleButton，静止 Circle、按压/选中 12dp，复用 FastSpatial 形变、反馈与按压 collector；
选中标签使用 Bold。保留 Invoke，同时提供 Toggle/selected 自动化状态，未新增公共交互框架。
标准间距 4dp；Vibrant 横向 16dp、纵向 8dp 独立消费组件 Token。原生 Enter/Space 激活沿用既有按钮约定。横向时段使用 LeftToRight 放置，保留上游 absolute place 的 RTL 顺序。
上游无容器 SideControlColumn 的 40dp 视觉/48dp 命中布局尚未补齐，此处不将它记作普通 contained toggle 的同一布局。

## 尚未实现或未验证

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
- `ClockFace` 混合绘制依赖 Avalonia.Skia，与本库其他自绘 Material 控件一致；当前仅消费实色画笔的 Color，未单独接入画笔 Opacity / Skia 租约 CurrentOpacity，非实色 selector 不绘制。
  非 Skia 后端、非默认父级 Opacity、半透明画笔和分数缩放未完成验证。

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
