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
填充可用高度，物理 `WindowInsets` 通过已有转换器接入。Modal 默认 LargeEnd 圆角与
SurfaceContainerLow，标准 Sheet 默认矩形 Surface，默认 tonal elevation 为 0。
Item 最小背景高度 56 DIP，标签采用 LabelLarge；12 DIP 的外侧 ItemPadding 是调用方的布局选择。

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

共享交互、键盘反馈与焦点规则见
[激活职责](../../../../docs/DECISIONS.md#共享交互反馈与键盘激活职责)和
[Core Desktop 焦点](../../../../docs/DECISIONS.md#core-desktop-原生焦点反馈)。
文字默认值见[聚合排版决定](../../../../docs/DECISIONS.md#聚合-textstyle-与-token-层文字单位换算)。

## 已确认适配

以下确认与历史观测依据来自[拆分前组件记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/NavigationDrawer/README.md)；不以当前代码反推维护者授权。
2026-10-04 维护者确认上述内容槽、State、Sheet 和外部选择 API：用 Avalonia 内容/模板表达 Compose lambda，
保持状态由应用持有，物理 WindowInsets 由宿主传入、默认零，不建立第二套 inset / overlay。
Modal 仍在本地布局中覆盖，不因名字带 Modal 而套用窗口级 Dialog/Popup 的焦点陷阱。

同日确认平台边界：Avalonia BackRequested 没有 predictive-back 连续进度流，因此实现离散 BackRequested
和抽屉内 Escape KeyUp，尚未实现预测返回的进度变形及取消恢复。
这是已确认的实现范围，不是宣称完整 predictive-back 等价；其他普通开关、拖动、过冲、scrim 与状态顺序仍对照固定源码。

同日确认本组件保留两类原始数值差异，未升级当时的 ComposeAnimation 或调整比较容差：

- 当时 ComposeAnimation 0.2.2-alpha 的 double tween/Bezier 对照 Compose Float，在部分手势收尾有约 0.000031 DIP offset 差异
- Avalonia double 变换矩阵对照 Compose Float，在打开过冲时有约 0.000032 DIP 原始布局框差异

范围限于这些数值来源；已核验采样的状态、回调与整数像素摆放不变，raster 通过既有比较。
SourceProbe 超出精度的字段仍应标为 different，不可把整族宣称严格一致。
这些是 Drawer 自身记录的确认，不能仅援引其他组件的共享精度决定扩大范围；当前依赖版本由中央文件管理。

## 尚未实现与未经验证

predictive-back 连续变形/取消恢复尚未实现；焦点陷阱与关闭后恢复焦点也不是当前容器能力。
真实字体排版、系统触摸投递、硬件触摸和屏幕阅读器未经验收。用几何标记替代文字的 parity 不证明真实文字一致。
旧完整双端自检在各用例有 equal、withinTolerance 和上述数值 different，重复捕获与额外被动截图稳定；
执行错误为零不代表所有事实严格相等。

## 验证入口

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

上述命令与覆盖说明不代表当前修订已执行全量验证。旧测试数量与未跟踪 artifacts 不作为当前验证结果；
详细历史结果见[固定修订的原记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/NavigationDrawer/README.md)，复核以跟踪测试和新生成报告为准。
`run` 执行完成、`self-test` 重复稳定与两端相等是不同结论，必须分别报告。
