# Menu 模板与移植

## 独立 DropdownMenu 的实现

维护者要求在 `dropdown-menu` 分支上继续独立菜单，尽可能直接对齐固定上游。
初始实现时视觉一致性系统暂不可用；随后按维护者要求合入 `review/linux-visual-parity`
并增加独立菜单双端用例。原生主题的已确认范围及旧视觉证据保留在下文，
不扩展为独立菜单的验收结论。

源码先于实现获取：AndroidX / Core 的 Material3 sources JAR 经基线 JSON 中的 SHA-256 核验，
两份 `Menu.kt` 内容一致。Avalonia 检出 `12.1.2`，commit
`d3c867a9e2de379249b03dbeb3495bd7f076a81a`，核对 Popup、OverlayPopupHost、Button、焦点与自动化实现。
另读取固定 Core 的 `Popup.skiko.kt`，保留任何鼠标按钮的外部按下关闭请求及返回键消费。

| 固定上游职责 | 独立实现 |
| --- | --- |
| `DropdownMenuPopup` / `DropdownMenuPopupContent` | `DropdownMenuPopup`、独立 XAML 模板、共同 Transition 的 scale/alpha 两通道 |
| Core `SkikoMenu.skiko.kt: DropdownMenu` | `DropdownMenu` 的 Surface、ScrollViewer、上下键 Next/Previous 和水平边距 0 |
| `internal/MenuPosition.kt` / `MenuAnchorPosition` / `calculateTransformOrigin` | `DropdownMenuPosition.cs`，像素候选顺序、边距、偏移、过大居中和交集原点 |
| `DropdownMenuGroup` / group `shapeByInteraction` | `DropdownMenuGroup` 的 hover 历史及 `AnimatedCornerRadius` |
| baseline / expressive `DropdownMenuItemContent` | 独立两套项目模板，`DropdownMenuItemLayout` 的顺序测量与 `MenuArrangement` 排列 |
| `CheckableDropdownMenuItem` / `SelectableDropdownMenuItem` | 受控的两种项目入口，不复用原生 MenuItem 的自动勾选与关闭 |
| leading icon `AnimatedVisibility` | `DropdownMenuLeadingIcon` 的宽度/alpha Transition，像素宽度舍入、End 对齐裁剪和退出内容保留 |

控件声明不占布局空间，模板的呈现部分迁入组件自己持有的 Popup；Items 仍由原 ItemsControl
持有逻辑关系，保留 DataContext 和资源。Popup 采用既有窗口内宿主决定；阴影所经过的
OverlayPopupHost 及 LayoutTransformControl 局部取消裁剪，不修改全局 Popup 主题。
需要消费外部指针输入时，在同一 PopupOverlayLayer 内、自己的 host 下方插入透明命中层，
因此点击、hover、滚轮和触摸均不会穿透；各 Popup 随自己的关闭释放该层，没有中央注册表。
进入从 scale=.8、alpha=0 开始，FastSpatial / FastEffects 分别求值；退出保留内容并允许中途反向。
外层 MutableTransitionState 的寿命与 popup 内 Transition 分开。固定上游在进入尚未完成时
关闭，会先移除 popup；旧 Transition.onDisposed 将 currentState 完成到原 target=true，
触发下一 composition 帧重建退出动画。这一不直观顺序已由双端 reverse 用例观测，
不将它改写为直接丢弃外层状态。窗口初始即打开时，宿主已挂接而其 Child 模板可能尚未挂接；
通过 Popup 指定的逻辑父关系定位宿主。WindowBase.HandleActivated 在恢复窗口焦点域后才设置
IsActive，因此在该属性变为 true 时恢复顶层菜单焦点，避免初始布局取得的焦点被窗口激活清空。
关闭请求与目标状态分离，点击也不代替应用改写状态，与现有 Dialog 的受控入口一致。

Avalonia 12.1.2 的 FocusHelpers 取逻辑父节点、枚举可视子节点；迁出的 ItemsPresenter 因而不能
直接依靠原生 Next/Previous。独立 Popup 在其实际可视内容中按 TabIndex 遍历启用的焦点目标，
使用原生 Focus(NavigationMethod.Tab) 保留反馈和 BringIntoView。RTL 的物理动画原点在接入
Avalonia 镜像子树时转换为逻辑 X；候选坐标和最终物理位置不变。

项的 Click/Command 使用 Button 的执行能力；Enter 改为按下记录、释放执行以对应 Compose clickable，
共享控制器仍负责键盘/指针反馈。形状与容器颜色复用既有 Surface / MenuItemChrome；
分组只有 hover 形变，不增加组级 ripple。圆角默认值保持 Token 绑定，首尾组/项的组合圆角
按上游直接使用 reference ShapeTokens 常数。

小尺寸图标使用局部 SlotPresenter 保留内容的实际测量尺寸：Compose Box(defaultMinSize) 不把
最小宽度传给内容，而 Avalonia ContentPresenter 默认 Arrange 会将自身最小宽度用于内容排列。
选中图标的可见宽度按动画排列，Popup 测量则对应 Column.width(IntrinsicSize.Max)，使用保留内容的
完整宽度；首次展开仍为零宽时也显式使父布局测量失效。目标变更按 composition 帧合并，
避免相对上游提前一帧启动。没有修改共用动画引擎或 Surface 来补偿菜单布局。
上游 MotionScheme 的泛型 spring 不指定 visibilityThreshold，因此像素宽度使用默认 0.01，
不是 expandHorizontally 默认 spec 中显式指定的 IntSize 阈值 1；误用后者会提前结束退出并收窄菜单。
最终一条通道结束后移除选中图标的呈现内容，双端用例直接检查它的挂接状态。

`MenuIntrinsicWidth` 对应上游 Column.width(IntrinsicSize.Max)：先以无界宽度查询内容尺寸，
仅在调用方约束更窄时重新测量。不能每次都按查询结果收窄子树的测量约束，否则后续内容变宽
可能被旧约束截断，DesiredSize 不变便无法触发 Avalonia 的父级重新测量。
原生两套 presenter、子菜单及独立 Popup 均接入；菜单内 Separator 使用 Stretch，
按最终列宽排列，与 HorizontalDivider.fillMaxWidth 一致。没有修改全局 Divider 的测量语义。
此前缺少这一步，Catalog 中的分隔线把宿主可用宽度直接作为菜单宽度；固定宽度的容器视觉用例
没有暴露问题。回归覆盖自动宽度、显式窄/宽约束、动态内容及两种原生分隔线，双端新增自动宽度场景。

固定 Core `GraphicsLayerModifier.kt` 规定默认合成策略在 alpha < 1 时使用自身边界大小的中间层，
越界内容会被隐式裁剪；菜单动画层对应切换 ClipToBounds，alpha 达到 1 后恢复外溢阴影。
最初 reverse 两端 scale/alpha 完全相等，但菜单边界外的阴影像素仍超容差；无阴影对照通过后，
据此定位并补齐这一合成规则，保留无阴影用例作为诊断对照。

固定上游 Left 与 Start、Right 与 End 的可执行候选相同，与它的 absolute-direction KDoc 不一致；
本轮保留源码，不自行修正。Popup 最终 `clipPosition` 在 provider 计算 pivot 之后执行。
普通菜单与组合 Popup 的水平边距分别为 0 和 8 DIP；垂直边距均为 48 DIP。

### 验证边界

`DropdownMenuTests` 包含真实 Popup、初始打开、尺寸、受控输入、定位、滚动、开闭/反向、分组 hover、图标动画、
模板替换、主题/RTL 与级联菜单测试；`MenuTests` 继续覆盖原生路径。
运行 `dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~MenuTests`。
本轮 46 项通过，包含初始打开的焦点与命中层、透明度层裁剪、选中图标及其模板的退出释放，
以及平台/overlay Popup、子菜单、分隔线和显式宽度约束的回归。
`dropdown-menu-item` 复用原生菜单项的同一上游场景，独立采集新控件；`dropdown-menu` 使用真实
Popup 覆盖静态明暗/RTL/边缘位置、开闭及进入中断、受控外部关闭、分组选中图标与 hover。
所有新增场景先运行 self-test。文字、真实桌面输入及无障碍仍不属于这些用例的证明范围。

```powershell
./tests/VisualParity/parity.ps1 self-test --family dropdown-menu-item
./tests/VisualParity/parity.ps1 self-test --family dropdown-menu
```

2026-10-11 Linux x64 复测：`dropdown-menu-item` 的 50 项中 12 项 equal、38 项 withinTolerance，
`dropdown-menu` 的 20 项全部 withinTolerance（含 2 项无阴影对照），实际 scale/alpha、内容尺寸、
受控状态及选中图标的退出保留读数一致；全部重复采集稳定且无执行错误。
原生 `menu` 同为 12/38；原生 `menu-container` 9 项 withinTolerance，
2 项仍为下文记录的共享 Surface 暗色圆角边缘差异。本轮未放宽 raster 门限、替换参考或修改共享绘制。
产物位于 `artifacts/visual-parity/runs/linux-dropdown-menu-complete`、`linux-dropdown-menu-item-final`、`linux-menu`、
`linux-native-menu-container-2`；各目录包含 run.yaml、summary.md、逐样本读数和原始截图。

Catalog 宽度修复后的 self-test：`linux-dropdown-menu-intrinsic-width` 为 24/24 withinTolerance，
`linux-menu-container-intrinsic-width` 为 11/13 withinTolerance；新增的 6 个分隔线/自动宽度场景
全部通过，37 个用例均重复稳定、无执行错误。剩余两个 Surface 暗色圆角用例的两端截图
SHA-256 与 `linux-native-menu-container-2` 完全相同，未产生新差异。Catalog 构建通过。

尚未实现：任意 Compose Shape / CutCornerShape（现有 Surface 只提供 CornerRadius）、可注入的
外部 InteractionSource、任意 Arrangement 对象、系统平台 inset 与预测返回。
项目间距可配置 LeadingSpacing/TrailingSpacing；更复杂排列可替换项目模板。
这些是当前基础能力/API 覆盖缺口，不标记为已接受的语义删减。
新增独立入口不提供已隐藏的二进制兼容重载，也不涉及 ExposedDropdownMenuBox。



## 原生主题此前的范围和已确认决定

维护者在本次 Dropdown 讨论中确认：为原生 `MenuFlyout` 提供 M3 外观；Vertical 与 Baseline
采用两套独立 `ControlTheme`，由 `FlyoutPresenterTheme` 选择。Vertical 的 Standard / Vibrant
是同一布局的配色差异，使用附加属性。菜单项和子菜单自动跟随，原生行为继续由 Avalonia 负责。

维护者明确允许按原生能力限定本轮范围：暂缓打开/关闭动画和独立 DropdownMenu；原生无法自然
表达的独立分组、组间空隙及组级动画留待后续实现。分隔线不重新解释为组边界。
本轮覆盖项目反馈、选中颜色和圆角动画；原生复选/单选标记仍由模板立即切换。

## 固定源码入口

基线身份见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)，本轮为
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`、Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。不使用浮动 HEAD。

| 上游职责 | 本地位置 |
| --- | --- |
| `Menu.kt` 的 baseline `DropdownMenuItemContent` | `MaterialBaselineMenuItemTheme`，原生项目模板与 `MenuItemChrome` 测量 |
| `Menu.kt` 的 selectable `DropdownMenuItemContent` / `shapeByInteraction` | `MaterialVerticalMenuItemTheme`，`IsChecked` 与 Surface 选中圆角 |
| `DropdownMenuContent` / `DropdownMenuGroup` 的静止 Surface | 两个 presenter 主题与子菜单 Surface |
| `MenuDefaults.kt`、`MenuTokens`、`StandardMenuTokens`、`VibrantMenuTokens`、`SegmentedMenuTokens` | `MenuTokenSource` 与 `MaterialTokens.Comp.Menu` |
| `Menu.kt` 的 `animateColorAsState(FastEffects)` | `MenuItemChrome` 的作用域、目标合并及颜色动画 |
| `internal/AnimatedShape.kt` / Surface 的状态层、ripple | 复用现有 Surface、AnimatedCornerRadius、MaterialInteractionController |
| Core `Menu.skiko.kt` 的 precision pointer 平台分支 | 固定实现不启用 precision pointer，使用相应默认尺寸 |

common Menu 实现在两套固定基线中一致；Core 的 precision pointer 初始 false，桌面 listener
函数仅执行 content，没有将状态设为 true。因此 Vertical 默认 padding 为 12、图标间距 8、
leading/trailing 图标默认尺寸 20；不使用未生效的 precision 分支。

## 原生框架适配

`MenuFlyout` 没有可替换的公开模板，实际模板属于 `MenuFlyoutPresenter`。
Avalonia 12.1.2 的 `MenuFlyout.CreatePresenter` 在 presenter 的 `ItemContainerTheme` 上
建立本地绑定，其默认值为 null；主题 Setter 无法覆盖该本地绑定。
`MenuThemeProperties.ItemTheme` 因此只传递内部默认值，后代 `MenuItem.Theme` 的样式绑定优先
消费实际父 ItemsControl 的非空 ItemContainerTheme。显式项目 Theme 仍以本地值优先。
不覆盖原生容器生成或输入处理器。

`MenuFlyout` 不在 presenter 的属性继承链上。`MenuAppearance` 在配色改变及每次 Opening
向实际 presenter 传递值，菜单项与子菜单再通过继承读取。原生 ItemContainerTheme 和 Theme
仍由调用方控制；这些主题默认值不替换调用方明确提供的主题。

`MenuItem.IsSelected` 是指针/方向键导航状态，不等价于 Compose selectable item 的 selected。
业务选中映射到原生 `IsChecked`，复选/单选执行顺序和互斥仍由 DefaultMenuInteractionHandler
负责。`PART_Popup` 与 `PART_ItemsPresenter` 保留原生模板合约；原生 Placement、LightDismiss、
焦点和键盘行为不重新实现。子菜单不强制 overlay，沿用原生 Popup 的平台宿主选择与
平台不支持时的 overlay 回退。主 flyout 为 PopupRoot 时没有 PopupOverlayLayer，强制 overlay
会使子菜单打开抛出异常。Headless 回归检查不强制 overlay 的模板策略及两层子菜单的回退打开；
Avalonia 12.1.2 的 HeadlessWindowImpl.CreatePopup 固定返回 null，不能据此声称覆盖真实平台弹窗。

子菜单箭头保留 24×24 图标画布，再缩放到 BaselineIconSize / TrailingIconSize（默认 24 / 20 DIP）。
实际箭头高度为 12 / 10 DIP；不能将 Path 的紧包围盒直接拉满图标槽，否则会放大笔画与箭头。
复选和单选的勾选标记同样保留 24×24 画布，分别消费 BaselineIconSize / LeadingIconSize，
保持原始图形比例与内留白；选中可见性作用于整个图标槽。

### 原生菜单不绘制阴影

2026-10-11 维护者确认：原生 MenuFlyout 的主菜单和子菜单均不绘制阴影。
两套原生模板的容器 Surface.ShadowElevation 固定为 0；主 Popup 沿用 PopupFlyoutBase 的
WindowManagerAddShadowHint=false，子菜单在 PART_Popup 上显式设为 false。
不增加阴影接入逻辑，不扩大透明客户区，不修改菜单定位或命中范围。

原因：原生 PopupRoot 按内容分配客户区，自绘阴影超出窗口边界会被裁剪；实际桌面试用中，
维护者确认平台阴影按矩形绘制，与菜单圆角不匹配，因此接受原生菜单无阴影这一移植差异。
独立 DropdownMenu / DropdownMenuPopup 不受此决定影响，继续使用窗口内宿主和 Material
自绘阴影；Menu.ShadowElevation Token 仍供独立菜单消费。

Headless 回归检查主菜单、子菜单的平台阴影提示为 false，且容器自绘阴影高度为 0。
此前 menu-container 视觉结果包含 Material 自绘阴影，不能作为当前无阴影外观的验收结果。
保留原上游参考与历史结果，不通过修改基线或比较门限隐藏已接受的无阴影差异。
下文两处圆角边缘差异是此前的独立记录。

## 布局和动画

Baseline 的项目高 48，容器纵向 padding 8，容器圆角 4。Vertical 的容器圆角 16、纵向
padding 2，项目左右外边距 4，内部最小高 44；Selectable Surface 的最小交互高 48
可以在外部占据 48 并把 44 高的可见表面居中。内容较高时由实际测量扩展。

上游 `sizeIn` 的 112/280 是受传入约束限制的首选最小/最大尺寸。`MenuItemChrome` 在测量
阶段应用它们，而不是给原生项目设置强制 MinWidth。分配了更大实际宽度时 fillMaxWidth
依然填充；可用空间小于 112 时不强撑出 popup 边界。

模板将原生目标 Background 保留为配置值，Chrome 的 PaintedBrush 供 Surface 绘制。
纯色、Opacity=1 的画刷通过 FastEffects spring 动画；自定义非纯色或带独立 Opacity 的画刷
保持原生画刷绘制。目标在下一次 composition 帧合并。固定基线的
`AnimateAsState.restartAnimation` 先取消 job 再启动 animateTo；本地通过 StopAsync 等待取消
清理后启动新动画，避免旧 mutation 的 finally 在新 mutation 后执行。

Surface 复用现有 ButtonSurfaceState 模板输入，按 `IsChecked` 在 4/12 圆角之间使用 FastSpatial，
反向切换保留上游形状进度/速度。没有复制一套 ripple 或 shape engine。
共享控制器只为 Button 接入键盘按压，因此 Chrome 只桥接原生 MenuItem 的 Enter 按压反馈；
激活、点击次数、Command 和关闭仍由原生 handler 决定。

attach 初始化当前颜色；detach / 模板替换释放 Chrome 作用域、订阅和键盘 handler，Surface
释放自己的呈现 attachment。PopupRoot 的时钟环境由既有 VisualAnimationScope 继承。
应用需在加载内容前初始化 TopLevel；[共享机制](../../../../docs/ARCHITECTURE.md)不在这里重定义。

## 验证与未实现范围

- [MenuTests](../../../../tests/Material3Expressive.Tests/MenuTests.cs)：真正 ShowAt 的原生 flyout、
  主题/配色和子菜单传播、生成容器、Command.CanExecute、复选/单选、方向键/Enter/Escape、
  StaysOpenOnClick、窄宽度、资源更新、重用 presenter、模板替换与动画清理。
- [MenuCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/MenuCases.cs) /
  `reference/.../cases/MenuCases.kt`：真实上游菜单项，三种外观、明暗、禁用/选中、RTL、图标、
  内容与宽度约束、悬停/按下、选中及中途反向、主题切换；直接读取两端实际可见圆角。
- [MenuContainerCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/MenuContainerCases.cs) /
  `reference/.../cases/MenuContainerCases.kt`：实际原生 presenter 对照真实 DropdownMenuGroup，
  两种 Vertical 配色、明暗/RTL、两种原生分隔线和最小宽度。

参考场景使用 Expressive motion，与本库的系统 motion Token 对应。隔离的 Baseline item 场景
通过 contentColorFor(MenuDefaults.containerColor) 提供真实外层 DropdownMenuContent Surface 的
内容环境；baseline ripple 在文字/图标颜色 provider 外，不能误用白色测试画布的默认黑色 ink。
夹具不重写上游项目布局/形状或传入 Avalonia 的计算结果。self-test 同时重新采集两端、检查
重复捕获和额外被动截图；结果阅读及复现命令见 [VisualParity README](../../../../tests/VisualParity/README.md)。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~MenuTests
./tests/VisualParity/parity.ps1 self-test --family menu
./tests/VisualParity/parity.ps1 self-test --family menu-container
```

原生主题路径尚未提供：打开/关闭和子菜单运动、独立分组/组间空隙/组悬停形变、Compose supportingText /
trailingContent 专用槽、选中 leading icon 的 AnimatedVisibility。这些能力的独立入口见本文开头。
这些边界不表示已经完整实现全部上游 Dropdown API。

未经验证：真实桌面后端与 DPI 切换、文字栅格一致性、所有原生定位边界、打开中的整套主题替换，
以及 Baseline 整个 popup 容器与上游 popup 的双端截图一致性。当前文字以确定尺寸色块验证内容
环境和布局，原生复选/单选标记通过集成测试验证行为；不把这些结果当作图标动画或文字视觉证明。

## 原生主题此前接受的视觉差异

2026-10-11 维护者选择“保留绘制现状，记录这两个视觉差异”，没有授权修改共享 Surface 绘制职责。
`menu-container.static.standard.dark.ltr` 和 `.rtl` 的外层圆角边缘仍未达到既定 raster 容差：
完整图在 1x box 平均后的最大通道差为 22，门限为 20，内容区域与布局读数相等。
现有 Surface 由带圆角且 ClipToBounds=true 的 Border 绘制背景，再裁剪该视觉；边缘覆盖与
Compose 的图层裁剪不同。原始 4x 图的代表边缘像素为 Avalonia 195 / Compose 88（红通道）。
不能以“其他项目在容差内”为依据忽略这两个失败，也不改变比较门限。

本次接受范围仅为上述两个菜单容器用例；不是对任意 Surface 栅格差异的通用豁免。
菜单项 50 个用例通过既定容差，容器其余 9 个用例通过既定容差；具体产物由交付报告指向。
