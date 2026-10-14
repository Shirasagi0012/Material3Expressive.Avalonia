# CommandBar 实现说明

这是维护者确认的原生 CommandBar 主题设计，借用固定 NavigationBar 的胶囊与选择动效，
不是 Compose 同名组件的直接移植。原生逻辑依据当前 Avalonia 12.1.2 的 CommandBar 源码。

## 模板与布局

三套父主题共享 CommandBar 模板，分别设 Bottom / Right / Collapsed 和最小高度。
模板保留 PART_OverflowButton、PART_OverflowPopup、PART_OverflowPresenter、PART_ContentPresenter，
主列表绑定 VisiblePrimaryCommands，弹出列表绑定 OverflowItems。点击、键盘、切换和溢出由原生控件处理。
更多按钮使用原生 CommandBarButton（同时是 Button），宽度匹配原生固定预留的 48 DIP。
溢出 Popup 接入窗口内 PopupOverlayLayer，关闭平台阴影提示；局部取消 OverlayPopupHost
及其 LayoutTransformControl 的裁剪，以保留 MaterialSurface 绘制的阴影，沿用已有菜单与 ComboBox 接入方式。

两个原生命令类型各有一个明确 TargetType 的 ControlTemplate，公共样式、布局和动画仍共享。
原生属性通过 TemplateBinding 读取，具名布局的动画采样通过 CompiledBinding 读取。
内容颜色使用 CommandBarItemLayout.ContentBrushConverter 显式将 Color 转为 IBrush。
CommandBarItemLayout 是模板中的非交互 Panel，负责四个独立子元素的尺寸和排列。
它不创建嵌套按钮，不替换原生点击或自动化语义。

上下图文的胶囊只覆盖图标区域，横排与纯图标的胶囊覆盖内容区域；标签一直显示时不位移。
有色指示器从中心改变宽度，水波纹 Surface 始终保持完整尺寸。
普通命令的 hover / focus 状态层填充为零，仍消费现有 MaterialInteractionController 的 ripple。
PressedStateLayerOpacity 也控制 DrawRipples 的透明度，不可为了关闭固定填充而设为零；
共享控制器的 UpdateStateLayer 已排除 Pressed，模板保留默认 pressed opacity 即可只显示波纹。
横排图文主命令使用 InlineContentPadding（左右 12、上下 6 DIP），
根据实际图标/标签尺寸加内边距测量完整胶囊，再在外侧留出每边 4 DIP 的间隔。
输入原点使用共享控制器的实际 Surface 坐标转换，不复制导航专用的固定偏移。

溢出项对齐当前 MaterialVerticalMenuItemTheme 的 Standard 排版：LabelLarge、默认 20 DIP
leading icon、8 DIP 图文间距、VerticalItemPadding、ItemMargin、VerticalItemHeight 与
MinimumInteractiveSize。最低/最高 intrinsic 宽度也消费 Menu 的 MinItemWidth / MaxItemWidth，
弹出容器使用 StandardContainer、VerticalShape、VerticalPadding 和已有 MenuIntrinsicWidth。
没有图标时隐藏槽，不保留图文间距。实际图标尺寸仍参与布局，不强制缩放调用方的显式图标。
指示器继续按现有尺寸进度从中心展开、按原有透明度淡入；仅将最终圆角从 Full 改为
Menu.SelectedItemShape。溢出切换项的 ripple 始终使用同一 SelectedItemShape，
不随 IsChecked 切换圆角，避免水波纹与正在展开/收缩的选中背景不匹配；普通命令使用 ItemShape。
这是维护者确认的视觉适配：保留 CommandBar 的选择动效，因此不沿用 Menu 的状态圆角切换规则。
选择和内容颜色仍由 CommandBar Token 提供，不使用菜单的 StandardSelectedContainer 配色。

## 动画与生命周期

仅 CommandBarToggleButton 创建位于 NavigationBar 目录的 SelectionIndicatorAnimation。
四条 AnimatedState 输出通过 Bind 写入布局内属性，具有 Animation 优先级；
布局采样色通过 XAML 绑定投影为内容画刷，不用动画回调覆盖原生颜色来源。
运动参数未就绪时不提交目标，配置恢复后重新提交当前状态。
普通命令没有选择轨迹，直接更新内部禁用/普通内容色。

模板布局 detach 时释放订阅和动画身份，重新 attach 按原生当前状态初始化。
这与本轮 Bar / Rail 的生命周期一致。采用 AnimatedState 当前默认目标替换和采样行为，
不加 Stop 清零速度或额外等帧。固定 Compose 的逐帧一致性仍需要独立研究。

## 确认与边界

2026-10-15：维护者确认三套主题、铺满零圆角容器、原生溢出估算，以及切换项沿用导航项动效、
普通项只播放胶囊水波纹。高度和估算宽度为本地主题初始值，不作为 Compose Token 宣称。
保留原生菜单打开与命令激活行为，不补充另一套 Popup 管理或选择模型。
同日维护者确认：溢出项参考 Standard MenuFlyout 排版和选中圆角矩形，直接复用 Menu 组件 Token；
切换项保留已有 CommandBar 的选择动效和颜色，只适配背景圆角。

CommandBarThemeTests 检查三种标签布局、容器尺寸、进入溢出后的标签与状态、
原生键盘激活/禁用、普通项波纹、切换动画与重挂载。
鼠标输入回归同时检查波纹计数和实际 Skia 像素变化，覆盖三种布局、普通/切换按钮及明暗主题；
按住时的像素变化只能位于胶囊分配区域，普通按钮快速释放后仍有波纹，结束后恢复静止画面。
内容颜色回归使用与 Catalog 相同的 Fluent + Material 主题组合，覆盖三种布局的明暗主题、
运行时主题切换，以及普通、选中、未选中、禁用和溢出状态；同时核对动画采样、Presenter 画刷、
实际 MaterialSymbol 和生成 TextBlock 的内容色，避免仅检查“颜色未变化”而漏掉转换失败。
可设 M3E_COMMAND_BAR_CAPTURE 导出真实 Skia 截图。
溢出样式回归直接对照真实 Standard MenuItem 的图标、文字、间距及容器尺寸，
对照主栏与溢出切换项的四条动画采样，检查 Menu 圆角 Token 的动态覆盖与真实鼠标按压像素变化。
NavigationSelectionAnimationTests 对照 Bar / Rail 的四条轨迹。
自动化通过不证明原生桌面焦点轮廓、屏幕阅读器或所有长文本的人工体验已经验收。
