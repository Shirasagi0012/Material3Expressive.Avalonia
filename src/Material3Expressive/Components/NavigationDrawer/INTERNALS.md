# NavigationDrawer 实现说明

## Token 配置路径

主题为抽屉开/关运动、Sheet 宽度/Insets/elevation 与样式提供有效配置。过冲补偿与布局共用有效 ContainerWidth；State 在原来的动作时机读取原来选中 owner 的有效 motion 属性，不再直接查询 Token。共享 owner 选择、锚点、取消语义、256ms 手势 tween 与 decay 保持不变。

本文面向维护者；使用入口见 [README.md](README.md)。固定基线以
[upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`（Material3 1.5.0-alpha27），
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`（v1.13.0-alpha01）。
默认逻辑对照 AndroidX，桌面输入与平台实现对照 Core，不用浮动 HEAD 替代。

## 上游与本地职责

主要入口是固定提交的 [AndroidX NavigationDrawer.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/NavigationDrawer.kt)
及 [Core NavigationDrawer.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/NavigationDrawer.kt)。
下文 Material3 路径均以 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/` 为前缀。

原移植记录核对 NavigationDrawer.kt 两端相同；拖动、速度估算和桌面键盘另外对照固定 Core Foundation/UI。

| 上游符号 / 文件 | 本地职责 |
| --- | --- |
| ModalNavigationDrawer、DismissibleNavigationDrawer、PermanentNavigationDrawer | [NavigationDrawer.cs](NavigationDrawer.cs)、[NavigationDrawerLayout.cs](NavigationDrawerLayout.cs)、[NavigationDrawer.axaml](NavigationDrawer.axaml)：两槽容器、手势、scrim、页面排列 |
| DrawerState、DrawerValue、anchoredDraggable 状态 | [DrawerState.cs](DrawerState.cs)：锚点、动画、取消、确认回调与已完成状态 |
| ModalDrawerSheet / DismissibleDrawerSheet 的有/无状态重载、PermanentDrawerSheet | [DrawerSheet.cs](DrawerSheet.cs)：宽度、Insets、过冲补偿、BackRequested |
| NavigationDrawerItem、NavigationDrawerItemColors | [NavigationDrawerItem.cs](NavigationDrawerItem.cs)：标签/图标/徽标、外部选择、键盘及自动化 |
| tokens/NavigationDrawerTokens.kt | [NavigationDrawerTokenSource.cs](NavigationDrawerTokenSource.cs)、[组件 Token](../../Tokens/MaterialTokens.Comp.NavigationDrawer.cs) |
| Core 速度追踪、桌面 slop | 共享 [VelocityTracker](../../Foundation/Input/VelocityTracker.cs)，在组件层提供真实输入历史 |

## 状态与布局不变量

Modal 在本地布局中覆盖页面并绘制 scrim；Dismissible 推动保留原测量宽度的页面；
Permanent 为页面分配抽屉之外的剩余宽度。容器不裁剪推出的页面，未使用 Popup 或窗口级模态宿主。
Sheet 自动宽度限于 240–360 DIP，显式 Width 保留上游先行 `Modifier.width` 的约束语义；
填充可用高度，物理 `WindowInsets` 通过共享消费作用域和模板 `MaterialWindowInsetsBorder` 接入。Modal 默认 LargeEnd 圆角与
SurfaceContainerLow，标准 Sheet 默认矩形 Surface，默认 tonal elevation 为 0。
Item 最小背景高度 56 DIP，标签采用 LabelLarge；12 DIP 的外侧 ItemPadding 是调用方的布局选择。

固定两仓 `DrawerSheet` 的内部 Column 使用 `windowInsetsPadding(windowInsets)`。Sheet owner 先把
调用方的逻辑 ConsumePadding 加入祖先消费，再对 ConsumeInsets 取物理并集，最后仅应用 WindowInsets
尚未消费的部分。owner 发布累计消费，使直接内容与 ContentTemplate 生成内容都继承相同环境；
模板边框只负责实际布局，不重复建立消费作用域。测量时按当前 density 刷新舍入；RTL 只在边框处
抵消 Avalonia 的原生镜像，公开 Insets 始终为物理边。卸载、换内容和移入新作用域沿共享生命周期重接。
树内 ModalNavigationDrawer 保留声明祖先消费，不采用独立 Dialog 的清零规则。

`CurrentValue` 是最后已完成的状态，拖动/动画途中保持该值；`TargetValue` 是当前目标，
`CurrentOffset` 为逻辑起始轴 DIP，首次布局前为 NaN。内部按实际密度使用 Float 物理像素，
摆放时按 Compose 的整数像素舍入，RTL 由 Avalonia 镜像布局适配。
Open 使用 DefaultSpatial，Close 使用 FastEffects；手势收尾保留固定上游在 anchored state
构造时捕获的 256ms tween / exponentialDecay，不随之后赋值的 motion spec 改变。
拖动保留 slop、半程位置阈值、400 DIP/s 速度阈值、LSQ2 速度估算、锚点变更和中断语义。

confirmStateChange 保留上游的调用顺序：程序化 open/close/snap 可先改变 offset，再因 veto
保留原 settled value；因此不要将 CurrentValue 与当前绘制位置混为一谈。Scrim 点击和拖动收尾
有各自的前置确认分支。GesturesEnabled 关闭拖动与 scrim 点击，不禁用程序化或键盘关闭。
关闭的抽屉退出命中和 Tab 导航；打开完成后请求抽屉中的焦点。未增加焦点陷阱或关闭后的焦点恢复。

Container 与 Sheet 不是隐式绑定的整体；Sheet 仅在显式绑定同一 State 时消费过冲和 BackRequested。
状态实例与挂载消费者关联，动画需要有效作用域；状态替换、detach、新任务及外部取消须释放旧订阅/任务。
Sheet 的表面放大与内容逆缩放必须一起保留，不能通过缩放标签内容掩盖过冲。

DrawerItem 的标签为 Content，图标/徽标单独测量，标签获得剩余宽度。
Icon / badge spacer 是无权重内容，受窄约束限制；不能让标签先拿宽度而挤掉整个 badge。
Select / Click / Command 只请求激活，不更改 IsSelected 或关闭 Drawer；Page 导航属于调用方。
Item 的图标可由内容或模板提供；只设置 IconTemplate 也使图标 presenter 可见。无显式尺寸的 MaterialSymbol 继承通用 24 DIP 默认值，自定义图标仍按实际尺寸参与布局。

固定 NavigationDrawerItem 的 interactionSource 传给 selectable Surface。本地公开 `InteractionSource`，
挂载连接共享交互控制器、卸载断开；null 使用私有源，替换源取消自身 starts 并重建 indication。
外部事件只驱动反馈，不修改 IsSelected。组件没有额外颜色或形状交互收集器，不复制普通 Button 的 elevation 状态。

共享交互、键盘反馈与焦点规则见
[激活职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)和
[Core Desktop 焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)。
文字默认值见[聚合排版决定](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)。

## 已确认适配

以下确认与历史观测依据来自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/NavigationDrawer/README.md)；不以当前代码反推维护者授权。
2026-10-04 维护者确认上述内容槽、State、Sheet 和外部选择 API：用 Avalonia 内容/模板表达 Compose lambda，
保持状态由应用持有，物理 WindowInsets 由宿主传入、默认零，不建立第二套 inset / overlay。
Modal 仍在本地布局中覆盖，不因名字带 Modal 而套用窗口级 Dialog/Popup 的焦点陷阱。

本轮维护者明确授权新增能力和必要共享接入，补齐三种 Sheet 的嵌套 Insets 消费。原决定保留宿主提供
物理 Insets、默认零且不另建 inset 框架的理由；现直接使用已存在的共享能力。旧实现直接把完整 Insets
作为 Padding，只覆盖孤立 Sheet；没有将重复留白视为已接受差异。公开 API 不变，原先自行扣减祖先
消费的调用方应改传完整 Insets，仓内 Catalog 提供组合示例。共享规则见 [决定 §38](../../../../docs/DECISIONS.md#38-物理-insets-并集与自动留白容器)。

同日确认平台边界：Avalonia BackRequested 没有 predictive-back 连续进度流，因此实现离散 BackRequested
和抽屉内 Escape KeyUp，尚未实现预测返回的进度变形及取消恢复。
这是已确认的实现范围，不是宣称完整 predictive-back 等价；其他普通开关、拖动、过冲、scrim 与状态顺序仍对照固定源码。

同日确认本组件保留两类原始数值差异，未升级当时的 ComposeAnimation 或调整比较容差：

- 当时 ComposeAnimation 0.2.2-alpha 的 double tween/Bezier 对照 Compose Float，在部分手势收尾有约 0.000031 DIP offset 差异
- Avalonia double 变换矩阵对照 Compose Float，在打开过冲时有约 0.000032 DIP 原始布局框差异

范围限于这些数值来源；已核验采样的状态、回调与整数像素摆放不变，raster 通过既有比较。
SourceProbe 超出精度的字段仍应标为 different，不可把整族宣称严格一致。
这些是 Drawer 自身记录的确认，不能仅援引其他组件的共享精度决定扩大范围；当前依赖版本由中央文件管理。

## 手势仲裁与生命周期

拖动输入在 Bubble 阶段检测 slop，对应 Foundation Main pass 的后代优先消费。
Avalonia 原生 Thumb 通过 `DragStarted` / `DragDelta` 处理拖动，移动事件不置 Handled；
容器监听其 `DragStarted` 结束候选手势，保留子项的隐式捕获。这是原生输入到消费语义的适配，
不修改已确认的单值 Slider 输入职责；不把普通 Button 的 capture 当作拖动消费。
取消父级候选手势只释放由 Drawer 自身持有的 capture，不取消子按钮尚未完成的按压。
GesturesEnabled / FlowDirection 更新在下一动画组合帧取消旧周期；周期序号防止旧任务取消同一鼠标的下一次按压。
焦点等待与配置取消使用控件子作用域，卸载立即释放等待，不依赖再投递一帧；模板替换断开旧布局 Owner。

**固定来源差异**：AndroidX 的 Foundation `gestures/Draggable.kt::processDragCancel` 调用零速度
`onDragStopped`，锁定 `foundation-desktop-1.13.0-alpha01.jar` 的字节码和实测亦如此；固定 Core Git
同一函数没有这次调用。本组件保留 AndroidX 逻辑及现有零速度回弹，不改参考依赖，也不把 Core 源码静默混入。

## 尚未实现与未经验证

**已决定的状态替换适配**：本轮依据用户扩大授权重新决定，在 `AttachState` 中先结束旧输入，
再解绑旧状态并连接新状态。旧输入沿既有零速度取消路径收尾；旧状态失去最后一个容器时由已有
`Detach` 取消动画并保留最后 offset，仍有容器持有时允许其完成收尾。子按钮的独立 capture 不被释放。
调用仍为 `drawer.State = nextState`，下一次按下才开始新状态的拖动，无公共 API 迁移。
这是对原待定方案的当前决定，不追认为过去已经批准。维持原顺序会使旧 move/up 修改新状态；
延迟换状态会让公开属性和实际输入目标分离，额外保活旧状态则违反既有最后消费者卸载规则，均未采用。
固定 Core `AnchoredDraggableNode.update` 先替换 state 再取消输入；实际锁定制品在
`processDragCancel → settle → requireOffset` 读取尚未布局的新状态并抛异常。
`drawer-state-replacement` 当前复跑确认该异常，故此边界明确属于已选择的本地适配，不能宣称双端一致。
四个原失败场景继续保留；普通拖动、手势开关和方向取消不因该适配改变。

predictive-back 连续变形/取消恢复尚未实现；焦点陷阱与关闭后恢复焦点也不是当前容器能力。
真实字体排版、系统触摸投递、硬件触摸和屏幕阅读器未经验收。用几何标记替代文字的 parity 不证明真实文字一致。

## 验证入口

`NavigationDrawerInsetsTests` 覆盖三种 Sheet 的祖先扣除、逻辑 add/物理 union 顺序、动态方向与 Insets、
直接/模板/替换内容、卸载重挂和三种容器内嵌 Scaffold。`navigation-drawer-insets` 双端族以实际三种
容器和 Sheet 覆盖有无后代消费、父级/Sheet 自身消费、动态替换及 RTL，同时读取 Sheet、内容和标记几何。
运行 `./tests/VisualParity/parity.ps1 self-test --family navigation-drawer-insets`；实际结果以新生成报告为准。
`drawer-insets-delivery` 的 36 例、66 个采样均为严格 equal，执行错误为零，重复采集与额外截图稳定；
已检查实际双端内容替换/RTL 图像。新增行为测试 18/18，既有抽屉及 Sheet 形状回归 51/51 通过。
该结果只证明上述嵌套布局与消费范围，不扩展到真实系统 Insets 来源、文字或预测返回。

2026-11-05 本轮状态替换修复由 `NavigationDrawerTests` 45 项通过验证，包含 Modal/Dismissible × LTR/RTL
的拖动中替换、旧指针继续移动/释放、新状态布局前安全性及后续新拖动，并保留子按钮按压。
当前 `drawer-lifecycle-regression` 与 `lifecycle-isolated-navigation-drawer-gesture.*` 合并覆盖全部 28 例：
8 equal、8 withinTolerance、8 different、4 参考端异常；可完成场景全部重复/额外截图稳定。
新增 state-child-press 四例均 withinTolerance，子按钮点击数一致。原 state 四例仍在固定 Core 抛初始化异常，
单独运行避免污染后续用例，不算双端通过；`lifecycle-actual-{1,2}-navigation-drawer-gesture.*.state.*`
两次本地捕获的图像及样本 YAML 哈希逐一相同（每例 20 个文件）。
RTL 子按压夹具原本把局部 x=80 镜像成物理 x=412，而 Compose 从包围框左侧取 x=236；
现统一从场景包围框左上角取偏移，保留两端相同输入定义。修复前图像的波纹方向相反、trace 坐标不同，
修复后实际图像及读数一致；没有改变控件命中算法或比较容差。

前轮容器修订执行过 `navigation-drawer` 原有 110 例完整 self-test：31 equal、65 withinTolerance、14 different，
全部零执行错误、重复捕获与额外被动截图稳定。different 字段为手势收尾 offset 与打开过冲的变换布局框，
分别在约 0.000031 / 0.000032 DIP 范围；截图均通过既有比较。保留上述原始数值报告，不宣称整族严格一致。

`NavigationDrawerTests` 补充方向/手势开关中断、后代消费移动、真实 Slider 拖动、子按钮 capture 和卸载时取消焦点等待。
前轮 `NavigationDrawerGestureCases.cs` / `.kt` 的 24 个用例覆盖 Modal / Dismissible × LTR / RTL：
4 个后代消费与 4 个原生 Thumb 场景 equal，4 个子按钮场景 withinTolerance；8 个方向/开关中断场景 different，
均零执行错误且重复/额外截图稳定。中断后的几何与截图一致，方向修改事件当帧仍有即时属性与重组提交差异，
后续 offset 存在约 0.000004 DIP 的 Tween 精度差，未修改采样、容差或基准。
其余 4 个 State 替换场景为上文参考端执行错误，不计为通过。运行入口：
`./tests/VisualParity/parity.ps1 self-test --case navigation-drawer-gesture.dynamic.modal.child-press.ltr`。

`NavigationInteractionSourceTests` 包含 DrawerItem 的外部/原生输入、替换和重挂；
双端 `navigation-source.dynamic.drawer.*` 覆盖重叠、替换、悬停/焦点、原生换源和无附加槽 RTL。
这五个用例的当前捕获为 1 equal、2 withinTolerance、2 different，执行错误为零；
两个换源用例只在换源事件当帧有截图差异，后续帧收敛。重叠及无附加槽 RTL 的 Compose 自检存在像素不稳定，
其余三例的重复采集与额外被动截图稳定。该结论只覆盖 DrawerItem 交互源，不扩展到抽屉容器的手势或预测返回。

从仓库根目录执行，环境与结果含义见 [VisualParity](../../../../tests/VisualParity/README.md)：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~NavigationDrawerTests
./tests/VisualParity/parity.ps1 prepare-reference --family navigation-drawer
./tests/VisualParity/parity.ps1 run --family navigation-drawer
./tests/VisualParity/parity.ps1 self-test --family navigation-drawer
```

固定参考源码与配置不变时，无需每次 prepare-reference。
[NavigationDrawerTests.cs](../../../../tests/Material3Expressive.Tests/NavigationDrawerTests.cs)、
[Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/NavigationDrawerCases.cs)和
[Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/NavigationDrawerCases.kt)是已跟踪的复现入口。
Catalog 入口为 [NavigationDrawerDemo.axaml](../../../Material3Expressive.Catalog/NavigationDrawerDemo.axaml)。

测试覆盖三容器、五种 Sheet 入口/重载、槽/选择、主题、RTL/Insets、宽度边界，及开关/反向/重开、
snap、拖动/速度、禁用/veto/scrim/键盘和动态配置。配置变更在下一重组帧读取已提交布局，保留相同事件与送帧历史。
Catalog 绑定与开关的旧 headless 集成不替代真实系统输入。

当前验证限于上文实际运行的 suite 与字段，不代表所有组件、字体和平台均已验收。旧测试数量不作为当前验证结果；
详细历史结果见[固定修订的原记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/NavigationDrawer/README.md)，复核以跟踪测试和新生成报告为准。
`run` 执行完成、`self-test` 重复稳定与两端相等是不同结论，必须分别报告。

### 2026-11-09 显式形状入口

固定 AndroidX 的 三种 DrawerSheet 重载的 drawerShape 与 NavigationDrawerItem 的 shape 均直接交给 Surface；Core common 的对应入口采用同一语义。
此前仅暴露 CornerRadius，足以表达默认 Token，但不能表达切角、相对角和任意路径。
本轮用户授权补齐 API，复用已确认的 MaterialSurface.Shape；没有新增形状框架或改变状态职责。
Shape 非空优先，null 回退原有 CornerRadius；逻辑方向、可变形状订阅、阴影和裁剪由 Surface 负责。
动画仍在原有 Surface 层缩放，保留内容反缩放和锚点协议。无破坏性迁移。

`SheetShapeTests` 验证形状突变、运行时替换/清空、锚点不变、模态重开、抽屉方向与项目点击/选择职责。
原双端族新增静态切角/百分比圆角及动态展开、拖动、切换/恢复；抽屉还覆盖开关反向与项目按压。

### 本轮双端验证记录

`navigation-drawer-shapes` 142 例为 37 equal / 89 withinTolerance / 16 different，执行错误 0、重复稳定。
新增 32 例原始结果 5 equal / 17 withinTolerance / 10 different；所有 12 静态形状场景通过。
其中 RTL press 的夹具原先对镜像局部坐标求点，未落在 Core 的同一物理位置，改用 item 物理 AABB 后，
`drawer-shape-press-modal`、`drawer-shape-press-dismissible` 两例均 withinTolerance、重复稳定，原报告保留。
剩余新增差异包括 open 的 Float 位移读数以及 shape-change 在 32ms 的 focus 状态
（Core=false / Avalonia=true；帧图相同），尚未消除，不把稳定捕获称为一致。
已检查 RTL 模态切角实际双端图；报告路径为 `artifacts/visual-parity/runs/<运行名>/summary.md`。
