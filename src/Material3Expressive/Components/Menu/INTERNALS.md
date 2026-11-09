# Menu 内部机制与维护说明

## 1. 上游基线与职责映射

固定基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)，AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与 Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
Material3 路径为 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`，两端 `Menu.kt` 内容一致。桌面弹层行为另取 Core 的 `Popup.skiko.kt`、`SkikoMenu.skiko.kt`。
原生适配以 Avalonia 12.1.2 的 Popup、MenuFlyoutPresenter、MenuItem 和焦点实现为依据。

| 上游符号 / 职责 | 本地类型 / 成员 |
| --- | --- |
| `DropdownMenuPopup` / `DropdownMenuPopupContent` | `DropdownMenuPopup`、模板及共同 Transition 的 scale / alpha |
| Core `DropdownMenu` | `DropdownMenu` 的 Surface、滚动与键盘导航 |
| `internal/MenuPosition.kt` | `DropdownMenuPosition` 的候选坐标与 transform origin |
| `DropdownMenuGroup` / `shapeByInteraction` | `DropdownMenuGroup` 的 hover 历史和 AnimatedCornerRadius |
| baseline / expressive item content | 独立项目两套主题、`DropdownMenuItemLayout`、`MenuArrangement` |
| checkable / selectable item、leading AnimatedVisibility | 受控项目子类与 `DropdownMenuLeadingIcon` |
| 原生菜单的表面及项目 | `Menu.axaml`、`MenuItemChrome`、`MenuThemeProperties` |
| MenuDefaults 与 Menu / Standard / Vibrant / Segmented Token | `MenuTokenSource`、`Comp.Menu` 与主题绑定 |
| `MenuDefaults.DropdownMenuGroupLabel` / `DropdownMenuItemTrailingLabel` | 两个同名 ContentControl、`DropdownMenuLabels.cs` 与主题 |

## 2. 关键机制与架构契约

### 独立弹层与输入

控件声明本身为零布局，模板呈现内容迁入各自持有的窗口内 Popup，Items 的逻辑归属留在原 ItemsControl。
宿主局部解除阴影裁剪。需要阻止外部输入时，在自己的 host 下方加入透明命中层，关闭时释放。
请求关闭与 IsOpen 目标分离，项目激活使用 Button 的执行能力，Enter 在释放时执行。外部任意鼠标按钮按下可请求关闭。
Popup 可视内容按 TabIndex 遍历启用目标，使用原生 Tab Focus 保留反馈与 BringIntoView。窗口激活完成后恢复顶层菜单焦点。

scale 从 .8、alpha 从 0 入场，分别使用 FastSpatial / FastEffects。外层 MutableTransitionState 与内部 Transition 分属不同生命周期。
入场中关闭保留固定上游的移除、onDisposed 提交和下一重组帧重建退出顺序。alpha < 1 时按层边界裁剪，达到 1 时恢复外溢阴影。
定位先求候选和 pivot，再应用最终 clipPosition。普通菜单 / 组合 Popup 的默认水平边距分别为 0 / 8 DIP，垂直边距为 48 DIP。
固定上游的 Left / Start、Right / End 使用相同可执行候选。RTL 的物理动画原点转换为逻辑 X 后接入镜像子树。

### 布局、状态与交互源

`MenuIntrinsicWidth` 先以无界宽度查询自然尺寸，仅当调用方约束更窄时重新测量，保证动态内容增宽能触发父级测量。
Separator 按最终列宽 Stretch。小图标槽保留内容实际测量尺寸，箭头和勾选标记保留 24×24 画布后缩放到槽尺寸。
首尾组 / 项的组合圆角取 reference ShapeTokens，普通圆角保持 Token 绑定。Core precision pointer 分支默认未启用。

沿用共享决定 §34 的显式继承环境，独立 Expressive / Checkable / Selectable 及原生 Vertical 主题
直接选择固定两端 `MenuDefaults.kt` 的实时 getter：
`itemHorizontalArrangement` 间距 12 DIP、`DropdownMenuSelectableItemContentPadding` 为 16/12/10/12。
本轮维护者授权补齐原生分支及默认尾图标尺寸；既有 Padding 和 spacing 属性承载主题默认值，
调用方样式/局部值优先，无新菜单状态或高度算法。
原先默认关闭的理由仍成立：固定 Core 桌面监听为空，不能自动检测鼠标开启。
此前原生模板未消费此环境，现在复用相同 converter 与 Token 关系；Baseline 和菜单栏顶层保留自身常量路径。
`MenuDefaults.TrailingIconSize` 通过已有 MaterialSymbol.DefaultSize 提示提供 20/24 DIP，原生 chevron 使用同一有效值。
显式尺寸和任意内容保持自身测量，不改变尾槽 20 DIP 默认最小宽度；它直接来自固定 SegmentedMenuTokens，
并非 precision 的推荐图标大小。独立尾槽改用语义对应的 TrailingIconSize Token，默认值仍为 20；
此前只覆盖 LeadingIconSize 来同时修改尾槽的调用方，须显式覆盖 TrailingIconSize，precision 推荐尺寸另用 PrecisionTrailingIconSize。
未增加公开专用图标类型。原生实际 Popup 和子菜单通过框架继承保留运行时更新与关闭重开。

本次 `precision-menu-item-delivery` 16 例 / 32 个采样全部 equal，零执行错误，重复/额外截图稳定；
已查看首尾图标场景的真实双端图像。相关菜单/精确指针行为测试 112 项通过，包含真实 Popup 打开后的继承和局部覆盖。
最终解决方案构建通过，保留 6 项既有警告。这是先前范围的记录，不覆盖后补的尾图标默认尺寸。
新的尾图标实际测量参与项目高度计算，原有选中图标动画参数不变。

选中图标的可见宽度按动画排列，弹层自然宽度读取保留内容的完整宽度。进入初始零宽也需使父级测量失效。
目标在 composition 对应帧合并，宽度 spring 使用上游泛型规格的 0.01 阈值。退出完成后释放内容和模板，反向保留运行状态。
普通 leading 槽从无变有时，上游会移除 AnimatedVisibility 分支。本地同时释放该分支的作用域、延迟目标提交、值订阅和 Transition，
防止旧 alpha/宽度继续写入静态替换图标；再次仅有 selected-leading 槽时按当前 selected 建立新分支。作用域随分支而非整个模板存活。
原生 Vertical CheckBox / Radio 复用这一分支，IsChecked 提供目标，Icon 提供普通图标；
选中勾使用固定 Core `internal/Icons.kt` 的 24×24 Check 路径。透明度以 Animation 优先级绑定，结束分支恢复调用方的基础值。
`EnterExitTransition` 的 Alignment.End 固定使用 LayoutDirection.Ltr，RTL 子树需抵消镜像，保持物理右端展开。
原生自然宽度测量保留完整图标宽度；spring 超出该宽度时，行内重新约束内容，避免标签越出分配宽度。
弹层重开导致祖先作用域在帧前重建时，ScopeChanged 重新订阅宽度并续跑原 Transition，保留采样和速度。
`MenuItemChrome` 同样在作用域替换后恢复容器颜色订阅和未完成目标，避免重开的菜单停留在旧选中色。
Baseline 的静态勾也使用非镜像向量，RTL 只改变槽位位置。
项目默认尺寸和间距由组件 Token 绑定，默认 sizeIn 的 112 / 280 在测量阶段受调用方约束限制。

`DropdownMenuItem.InteractionSource` 由勾选 / 单选子类继承，null 使用内部源。替换源重置 indication，卸载取消原生按压，重挂和模板替换仅接收后续外部事件。
原生 Baseline / Vertical 项通过 `MenuAppearance.InteractionSource` 接入同一控制器，
`MenuItemChrome` 随模板连接源、观察附加属性变化，卸载断开源及属性订阅；null 使用模板内部源。
来源为固定 `Menu.kt` / Core `SkikoMenu.skiko.kt` 的 interactionSource 参数，本次非破坏性 API 授权内
采用附加属性适配原生 MenuItem。外部事件不调用激活或改变 IsChecked；原生 handler 的执行顺序保持不变。
`DropdownMenuGroup` 独立收集 hover，替换源清空列表但保留 remembered bool 至新源任意事件。hasBeenHovered 在下一组合帧观察，同帧 enter / exit 不计为曾悬停。
原生 hover 的 exit 发往旧源，卸载重置两种记忆。组级反馈仅负责形变。
普通 Expressive 项目需显式选择 `MaterialExpressiveDropdownMenuItemTheme`。

### 原生菜单与菜单栏

MenuFlyout 的模板属于 MenuFlyoutPresenter。其 ItemContainerTheme 本地绑定可能为 null，内部 ItemTheme 只提供默认候选，实际非空父级 ItemContainerTheme 和显式项目 Theme 优先。
MenuAppearance 在配色变化及 Opening 时把值传到 presenter，再向项目和子菜单继承。
原生 IsSelected 表达导航位置，业务选中使用 IsChecked，原生 handler 保有复选 / 单选、命令及关闭顺序。
PART_Popup / PART_ItemsPresenter 保留原生合约，子菜单使用原生平台宿主及 overlay 回退。

Vertical 项的 `MenuAppearance.SupportingText / SupportingTextTemplate` 对应上游
`LabelWithSupportingText`：LabelLarge 与 BodyMedium 纵向排列、无额外间距，内容或模板存在时选用
SupportingItemMargin，否则使用 ItemMargin。Margin 仍是主题 Setter，普通 Style / 本地值优先。
`TrailingContent / TrailingContentTemplate` 复用 DropdownMenuSlotPresenter 保留 Box 的最小宽度及小内容居中，
使用 TrailingIconSize 和 trailing content 颜色。原生 InputGesture 与子菜单箭头仍单独保留，
三者按 VerticalContentSpacing 排列；尾部容器可见性绑定为 Template 优先级，允许原生子菜单触发器显示箭头。
独立 Expressive 默认行与内部 NativeMenuRow 按 leading → label/supporting → trailing 顺序测量，
每项测量后才为下一间距保留剩余空间；不得提前从正文约束扣除尾间距。精确模式窄行的正文和零宽尾槽仍按此顺序测量。
NativeMenuRow 的间距受剩余宽度约束；
主行把最后的尾部槽放到末端。不能用 Auto,*,Auto Grid 先预留尾部宽度。
尾部 presenter 的 PreferredMinWidth 只影响自身首选尺寸，不向内容传播，且受传入约束限制；
这保留上游 defaultMinSize 的小内容居中与窄行收缩。内部行也用于组合尾部内容，不扩展公共布局框架。
Baseline 模板不读取这四个附加属性。这是本次非破坏性组件 API 授权内的原生框架适配。

原生 Background 保持目标配置，Chrome.PaintedBrush 提供呈现值。仅纯色且 Opacity=1 的画刷使用 FastEffects，其他画刷直接呈现。
重定向先等待旧任务取消清理再启动新动画。Surface 复用 ButtonSurfaceState 选择选中形状，Chrome 桥接原生 MenuItem 的 Enter 反馈。
卸载和模板替换释放动画、订阅及键盘处理器，PopupRoot 继承既有动画作用域。

菜单栏顶层主题 BasedOn Vertical 项主题，模板部件覆盖使用 StyleTrigger 优先级。
Menu.ItemContainerTheme 只应用于顶层，后代样式按父 ItemsControl 的显式主题或继承 ItemTheme 选择。
顶层 Popup 使用 BottomEdgeAlignedLeft、light dismiss，OverlayInputPassThroughElement 指向 Menu。
原生 RTL 定位交换左右锚点。导航高亮仅在 selected 且未聚焦、未悬浮时补充，避免与状态层叠加。

## 3. 已确认决定与已知差异

- 本轮剩余移植授权新增分组标题和尾部标签 ContentControl，直接消费固定两端 MenuDefaults.kt。
  原生 Content/ContentTemplate 表达上游内容 lambda；LabelLarge 通过现有 MaterialText 合并，前景色继续继承。
  GroupLabel 的 sizeIn 在测量中约束默认最小/最大尺寸，不能写为原生 MinWidth=112 而溢出窄父容器；
  显式尺寸位于默认约束外，内容不继承最小尺寸。TrailingLabel 的 precision padding 为实时 getter，
  通过主题优先级选择 Token；RTL 使用现有逻辑布局镜像。没有破坏性 API 迁移。

- 维护者批准原生 MenuFlyout 采用 Vertical / Baseline 两套 ControlTheme，Standard / Vibrant 用附加属性切换，原生选择、定位、焦点和关闭行为保留。
- 原生主菜单及子菜单的自绘阴影和平台阴影提示均关闭。这项已确认适配针对平台矩形阴影与圆角不匹配，独立 DropdownMenu 继续采用窗口内 Material 阴影。
- 独立菜单及交互源入口的授权和验证依据保存在引入历史中，source 扩展见 `b806d7f3`。共享弹层、输入、轮廓和动画契约见 [DECISIONS.md](../../../../docs/DECISIONS.md)。
- 菜单栏的公开 `MaterialMenuBarItemTheme` 名称仍待维护者确认，可选容器式菜单栏也处于未实现提议阶段。
- 独立容器、分组、专用菜单项已接入任意 Shape / CutCornerShape（见下文），原生 MenuItem 的 Expressive 主题现已接入（见下文）。横向 Arrangement 已接入独立 Expressive 项（见下文）；系统平台 inset 与预测返回仍未实现。原生主题路径还缺少开闭及子菜单动效、独立分组与组形变。
- `menu-source` 的部分 Core 重复及额外截图存在最大单通道 1 的不稳定像素，稳定性门禁保留失败。原生无阴影外观与上游仍有已接受差异，历史暗色圆角边缘结果不能作为当前 Surface 裁剪的隔离证据。
- 真实桌面后端、DPI 切换、文字栅格、全部原生定位边界、打开中的整套主题替换及 Baseline 完整弹层双端一致性未经验证。Headless 原生 Popup 回退测试仅覆盖 overlay 路径。

## 4. 验证入口

- `NativeMenuPrecisionTests` 覆盖原生与独立项的尺寸环境、真实 Popup/子菜单继承、重开、局部关闭、
  样式与 Token 覆盖、默认尾图标/显式尺寸及窄约束；`NativeMenuPrecisionCases.cs/.kt` 的
  `native-menu-precision` 族覆盖四入口、LTR/RTL、深色、支持内容、显式 padding 和按压期间切换。
  `native-menu-precision-verified` 的 61 例为 45 equal / 16 different，执行错误为零且重复稳定。
  已检查实际原图；固定宽度/窄约束的 45 个静态用例均逐像素相等。8 个动态用例在非帧 setter 当刻，
  本地已更新布局、Core 仍使用旧重组值；下一帧几何相等，后续按压图像相等或在原容差内。
  另 8 个没有外层宽度约束的裸项目，Core 的 fillMaxWidth 填满 320 DIP，而本地按内容收缩为 136/150 DIP；
  这属于此前自然测量边界，尚未接受，不由精确模式的新静态通过结果消除。
  原始失败、采样和门禁保留；相关行为测试 196 项通过，Catalog 构建通过。

- `MenuLabelTests` 覆盖真实主题尺寸、窄约束、显式尺寸、继承文字/颜色、模板及覆盖优先级；
  `menu-label` 双端族覆盖两个标签、LTR/RTL、自定义留白、宽高边界和真实菜单组合的动态尺寸环境。
  运行 `./tests/VisualParity/parity.ps1 self-test --family menu-label`；几何标记不证明真实文字栅格一致。

- `DropdownMenuTests` 检查独立 Popup、受控输入、定位、滚动、反向、分组和选中图标生命周期。
- 选中图标回归覆盖进入/退出途中添加普通内容或模板，随后移除、重进和卸载；静态分支不得再收到旧动画透明度写入。
- `MenuTests` 检查原生 flyout、菜单栏、子菜单、主题优先级及原生行为，新增槽的排版、Token 更新、快捷键与子菜单并存和原生激活。
- 原生勾选动画覆盖 CheckBox / Radio、快速反向、普通 Icon 增删、ToggleType 变化、尺寸 Token、模板替换及快速重开；卸载恢复透明度基础值并释放动画任务，重开后背景色继续响应选择。
- `MenuInteractionSourceTests` 检查项目和组的源替换、原生配对与模板重建，`CommandMenuDensityTokenTests` 检查尺寸覆盖。
- `MenuTests.NativeInteractionSource*` 使用真实 MenuFlyout 检查两套主题的外部反馈、原生按压配对、换源取消、模板重建和关闭重开；键盘回归同时检查 Enter 的源事件配对。
- 双端族为 `dropdown-menu-item`、`dropdown-menu`、`menu`、`menu-container`、`menu-source`、`menu-group-source`、`native-menu-supporting`。新增槽族覆盖 Standard / Vibrant、选中/禁用、RTL、暗色、模板独立使用、窄宽度及动态移除/恢复。Baseline 夹具需提供外层菜单表面的 content color。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter 'FullyQualifiedName~MenuTests|FullyQualifiedName~MenuInteractionSourceTests|FullyQualifiedName~CommandMenuDensityTokenTests'
./tests/VisualParity/parity.ps1 self-test --family dropdown-menu
./tests/VisualParity/parity.ps1 self-test --family menu --require-match
./tests/VisualParity/parity.ps1 self-test --family dropdown-menu-item --require-match
./tests/VisualParity/parity.ps1 self-test --family menu-source
./tests/VisualParity/parity.ps1 self-test --family menu-group-source
./tests/VisualParity/parity.ps1 self-test --family native-menu-supporting --require-match
```

原生 `menu` 的 98 例执行错误为零且重复/额外截图稳定（`20261105-033821-8e21349e`）：
48 equal、50 withinTolerance。新增覆盖三套配色/主题的真实勾选向量、明暗/禁用/RTL、选中反向、
普通图标增删及自然宽度下的 spring 超调；几何、选择状态和圆角读数相等。未调整容差或参考基线。

独立项目族 `dropdown-menu-item` 的 54 例执行错误为零且重复/额外截图稳定（`20261105-034045-d03caad5`）：
50 equal、4 withinTolerance。新增四例为进入/退出途中切换 leading 槽 × LTR/RTL，几何和状态读数全部相等；
添加普通图标后的样本逐像素相等，动画中的少量截图最大单通道差为 1，沿用原有门禁。

辅助文字和尾部内容回归 `native-menu-supporting` 的 48 例全部通过现有门禁，重复稳定、执行错误为零
（`20261105-034217-d26d1c27`）。这些项目级场景不能替代原生 Popup 完整定位与开闭动画的验收。

原生交互源接入后，`native-menu-regression` 的原有 `menu` 98 例再次通过 `--require-match`，
全部重复稳定、无捕获错误。`native-menu-source-corrected` 的 `menu-source` 40 例（新增原生 12 例）
全部在原有像素容差内，几何、启用、选择和点击读数相等，捕获错误为零；仍有 Core 重复/额外截图不稳定，
故 self-test 保留失败。新增 native-baseline.disable 与 native-expressive.overlap 受此影响，
不是 Avalonia 反复捕获变化。重叠按压的 Core 图像差检查到最大单通道 1，未放宽稳定性门禁。
原生 Vertical 的参考使用既有映射 SelectableDropdownMenuItem(selected=false)，测量外层分配宽度，
按压坐标扣除对应的触摸内缩，不把原生 Margin 误当 Compose 内部 padding。
控件库全量 2202/2202、菜单相关 87/87、VisualParity 基础设施 124/124 和整仓 Release 构建通过。

## 分组路径形状（2026-11-08）

此前只开放圆角，原理由是组级动画与 Surface 仅共享圆角求值；本轮维护者明确授权扩展 API 及共享能力。
固定 AndroidX Menu.kt 的 `shapeByInteraction(MenuGroupShapes, hasBeenHovered, hovered)` 先判断两个 RoundedCornerShape，
再判断两个 CornerBasedShape，否则返回所选 Shape；Core 对此无不同实现。
现在 `Shape` / `InactiveShape` 各自覆盖原圆角，保留原 API、主题和调用方，无需迁移。

组仍按首次观察到 hover 的 composition 记录历史，替换 interactionSource 保留已记忆 Boolean，卸载清空历史。
配置变化重新创建形状状态，切角复用 AnimatedCornerRadius；百分比保留尺寸表达式，反向保留速度，混合类型
按固定 Interpolatable.lerp 的 0.5 阈值选择端点。任意路径使整个双状态配置直接切换。
Surface 接收已求值切角，尺寸完成安排后重新绘制；属性观察在卸载时解绑，模板替换释放旧 Surface 托管状态。
Catalog 提供切角分组；MenuGroupShapeTests 覆盖历史、反向、百分比尺寸变化、配置清除与模板替换。

分组双端运行 `menu-group-paths` 的 54 例全部通过，双端重复稳定、执行错误 0；包括切角、混合角形、
任意路径、RTL、首次/重叠 hover、换源、反向、尺寸变化、配置重建和卸载重挂。

## 独立容器与菜单项路径（同轮授权）

`DropdownMenu.Shape` 对应固定 DropdownMenu 的 shape 参数，直接交由 Surface 求值，开闭 Transition、
定位及滚动保留；ExposedDropdownMenu 继承入口。Catalog 可切换容器切角。`dropdown-container-paths`
54 例双端 self-test 全部通过，执行错误 0、重复稳定；含真实 overlay Popup、阴影、深色、RTL、
切角/百分比/凹路径、开闭与反向、运行时换形状和尺寸。GPU/实际桌面弹层后端未经本轮验证。

此前专用菜单项只将两个圆角交给 Surface 的 ButtonSurfaceState。固定 Menu.kt 由菜单项本身执行
shapeByInteraction 并将结果交给 Surface；为支持两个任意 Shape，本轮授权将该职责移至 MenuItemShapeMotion。
模板中的 selected radius / spec 改为绑定到菜单项内部属性，所有仓内调用已迁移；公开 Shapes 和主题
名称不变，业务调用方无需迁移。自定义模板若照搬原模板，改为使用新的 PART_ItemSurface，并避免同时
配置 ButtonSurfaceState.CheckedCornerRadius：形状播放器现由控件持有。

普通/选中 Shape 独立回退到原圆角；两端角形使用同一 AnimatedCornerRadius 的进度、反向速度与第三目标
语义，任一端为任意路径时直接切换。配置 key 比较不可变角值，替换为数值相同的 CutCornerShape 不重启动画；
真正改配置时重建 shape 和 indication，不重放旧 Press。Baseline 的 Shape 是本地便利入口，固定 Baseline
DropdownMenuItem 不公开该参数；已由本轮自主 API 授权采纳，Baseline 仍无选中形变。该阶段未迁移原生 MenuItem；本轮后续迁移见下文。

`menu-item-paths` 的 90 例双端 self-test 全部通过（86 equal、4 withinTolerance），执行错误 0、
重复稳定；包含原有 Baseline/Standard/Vibrant 场景以及新增切角、混合角形、任意路径、RTL、按压、
hover、选中与快速反向。已查看菜单分组反向与真实凹路径容器的双端截图。

显式状态形状现同样识别 `RoundedCornerShape`，保存逻辑 DIP/百分比的值快照，
复用既有状态 owner、配置重置与 CornerShape 混合族中点切换，不以 Generic path 近似圆角。
验证见 `rounded-shape-final`：菜单圆角与圆/切角状态组合的 12 例中 9 例通过原门禁，3 例仍保留状态切换差异。
固定来源与共享入口决定见 Surface INTERNALS / DECISIONS §35，不代表所有交互完全一致。


**横向内容排列（2026-11-09 本轮授权）**：独立 Expressive、Checkable、Selectable 的 HorizontalArrangement
映射固定 Menu.kt 的同名参数，复用 [共享排列决定 §36](../../../../docs/DECISIONS.md#36-组件横向排列能力)。
null 保留 MenuArrangement 的专用 Leading/TrailingSpacing；非 null 时按 RowColumnMeasurePolicy 顺序测量
真实存在的 leading、text、trailing 槽，保留每步间距截断与缺槽规则，不把它们补成 Chip 的固定三个槽。
选中图标存在但当前零宽时仍算一个槽；排列变更不重建图标动画。布局方向变化单独触发重新排列。
原模板图标 MinWidth 会在窄约束中强制溢出，与固定 WrappedLeadingIcon / trailing Box.defaultMinSize 不同；
本轮改用已有 PreferredMinWidth 来保留首选尺寸且服从实际父约束，普通尺寸和内容居中不变。

新 `menu-arrangement` 用例使用真正三种上游入口。透明 ordinary Surface 的环境内容色在两端显式指定为主题 OnSurface，
避免 Compose 默认黑色与 Avalonia 窗口环境色成为不同输入；非文字 marker 注册 Foreground 绘制失效，
不会用旧显示列表误判选中颜色。首次报告保留在 `menu-arrangement-first`，没有调整颜色算法或像素容差。
行为测试 MenuArrangementTests 覆盖缺槽、图标零宽、换排列/恢复默认、运行时 RTL、激活所有权与 Baseline 边界。

`menu-arrangement-verified` 121 例中 113 equal、8 withinTolerance，执行错误为零，重复和额外截图稳定；包含三种入口、
LTR/RTL、正负/小数间距、绝对/自定义排列、窄约束和缺槽、动态图标反向及真实点击。
已查看实际选中途中、普通按压与受限布局原图。相关行为测试 74 项通过，Catalog 构建通过。

### 2026-11-09 原生 MenuItem 的状态形状

固定 Menu.kt shapeByInteraction 对两端角形使用 rememberAnimatedShape；任一端为路径时直接切换。
原生主题此前使用 ButtonSurfaceState 的圆角适配，无法表达相同选中切角、百分比与路径行为。
本轮授权下新增 MenuAppearance.Shape / SelectedShape，并由独立项和原生项共用 MenuItemShapeMotion；
只有 owner 配置读取不同，动画、配置 key、可变形状订阅、indication 重置及清理仍是同一实现。
移除原生 Expressive Surface 上重复的 ButtonSurfaceState 选中圆角驱动，避免两套动画写同一 outline。
MenuItemChrome 负责挂载、配置变化和卸载连接，不接管原生勾选或命令。无破坏性调用迁移。

NativeMenuShapeTests 覆盖 LTR/RTL、反转、可变配置、默认回退、重挂载与订阅清理；
`native-menu-shapes` 双端矩阵包含三种形状族、两方向、静态选中/未选中和选中/反向/按压。


新增原生形状 `native-menu-shapes` 30 例为 21 equal / 9 withinTolerance，执行错误 0、重复稳定。
已查看混合切角/圆角选中动画的双端实际帧。CommandBar 比较测试改为读取原生项实际选中后的有效 outline，
保留布局/反馈/命令断言，不再依赖已移除的内部 ButtonSurfaceState 配置槽；相关 198 项测试通过。

原生水平排列同轮补齐：MenuAppearance.HorizontalArrangement 只绑定主 NativeMenuRow，测量读取排列 spacing、
安排读取物理像素位置，RTL 先抵消 Avalonia 镜像。真实缺槽由原生模板可见性决定，尾部组合不拆分。
null 保留原逻辑，独立项与原生项不互相套模板；不新增通用 Row 抽象。MenuArrangementTests 加入运行时
方向/排列切换与恢复，`native-menu-arrangement` 复用真实 Core SelectableDropdownMenuItem 作为参考。

`native-menu-arrangement` 63 例全部通过原门限（59 equal / 4 withinTolerance），执行错误 0、重复稳定。
包括正负/小数间距、Absolute、用户算法、80 DIP 窄约束、全部缺槽组合、动态换排列/槽、选中反转与真实按压。
原生形状接入后的原 menu family 98 例也已完整重采，48 equal / 50 withinTolerance、执行错误 0、重复稳定，报告为 `native-menu-shape-regression`。


本轮最终解决方案/Catalog 构建成功，VisualParity 自身 134 项测试通过。整库早期 2678 项通过；
新增原生项能力后最终 2682 项运行中 2681 通过、1 项 NavigationRail 键盘测试在 Headless
EnsureIsolatedApplication 初始化阶段遇到 Dispatcher 线程访问异常，复核该键盘测试通过。
同批复核的另一个既有 outgoing-label 用例遇到相同初始化异常，单独运行两种参数均通过。
这保留为测试环境不稳定记录，不能把多次运行拼成一次完整全绿；新菜单行为测试实际通过。
原始 TRX 保留在 tests/Material3Expressive.Tests/TestResults/porting-20261109-*.trx。
