# Dialog 与模态弹层

`Dialog` 是窗口客户区内的基础模态容器，提供居中内容、遮罩、焦点约束和进出场动画。
它不包含 Material AlertDialog 的标题、按钮排列、尺寸 Token 或 Surface；这些属于组件层。
TextField 使用现阶段 Avalonia 的编辑能力，由独立组件负责。

## 接入

引入 `MaterialTheme`，在可视树中声明 Dialog；声明本身不占布局空间。
宿主使用标准 Window 模板，或包含名为 `PART_VisualLayerManager` 的 VisualLayerManager 的 Window 模板。
Window 在应用该模板时启用内部 PopupOverlayLayer。仅在 Content 内放置启用普通 OverlayLayer 的
VisualLayerManager 不满足此条件。主题提供 OverlayPopupHost 的内容模板。

```xml
<m3:Dialog IsOpen="{Binding IsDialogOpen}"
           Width="360" UsePlatformDefaultWidth="False" DismissRequested="OnDismissRequested">
    <m3:MaterialSurface Padding="24" CornerRadius="28">
        <TextBlock Text="Dialog content" />
    </m3:MaterialSurface>
</m3:Dialog>
```

```csharp
private void OnDismissRequested(object? sender, DismissRequestedEventArgs e)
{
    // Application state decides whether to accept the request.
    ViewModel.IsDialogOpen = false;
}
```

| API | 语义 |
| --- | --- |
| Content / ContentTemplate | 普通 Avalonia 内容及模板，保留本地资源和 DataContext 继承 |
| IsOpen | 调用方的目标状态，默认双向绑定 |
| IsPresented（只读） | Popup 仍在显示；退出动画中可为 true，而 IsOpen 已为 false |
| DismissRequested | Escape、Back、PointerOutside 请求；不自行改写 IsOpen |
| DismissOnEscape / DismissOnBack / DismissOnPointerOutside | 分别控制上述请求，默认 true |
| AnimateTransition | 默认 true；false 在下一次状态切换直接到终点 |
| UsePlatformDefaultWidth | 默认 true；客户区短边 <480 / 480–599 / ≥600 DIP 时，上限分别为 320 / 440 / 580 DIP，仍受客户区宽度限制 |
| ScrimBrush | 默认黑色、alpha 0.6；动画 alpha 另行叠加 |
| Width / Height / MinWidth / MaxWidth / MinHeight / MaxHeight | 约束居中的内容区域 |

普通非模态浮层继续由组件直接持有 Avalonia `Popup`，设置 `ShouldUseOverlayLayer = true`、
`PlacementTarget`、`Child`，使用 `Open()` / `Close()`；没有新增公开 Popup 包装或中央服务。
Popup 声明须接入所属控件的逻辑树与可视树，以保留资源继承、键盘路由和卸载生命周期。
原生 ContextMenu/ComboBox 的 Popup 是否采用 overlay，仍由它们自身及 Avalonia 平台设置决定。

## 共享职责与适配

每个组件拥有自己的 Popup。内部 `ModalPresenter` 覆盖客户区，处理焦点、Tab、Escape/Back 和关闭清理，
不持有组件状态或动画。Dialog 与 ModalWideNavigationRail 复用它；Rail 仍拥有原有 State、拖动及动画。
Presenter 按实际 PopupOverlayLayer 子项顺序判定最上层模态，不建立全局注册表。
焦点归属沿子 Popup 的 PlacementTarget 链判断，因此 Dialog 中的 overlay 菜单不会被当作背景夺回焦点。
Tab 仍委托 Avalonia 的焦点导航；关闭恢复打开前的有效焦点，关闭较早的兄弟弹层不抢占后开的弹层。
退出期间保留遮罩和模态输入作用域；卸载立即清理。普通 OverlayLayer 留给 ConnectedAnimation 等视觉覆盖。

2026-10-04，维护者确认独立 Popup、PopupOverlayLayer、无独立窗口、无中央管理器的方向，
并授权最小 Dialog 与 Modal Rail 迁移。共享决定见 [DECISIONS](../../../../docs/DECISIONS.md)。

动画与桌面关闭语义依据 `docs/upstream-baseline.json` 固定 Core 的
`compose/ui/ui/src/skikoMain/kotlin/androidx/compose/ui/window/Dialog.skiko.kt` 及
`compose/ui/ui/src/skikoMain/kotlin/androidx/compose/ui/animation/Animation.skiko.kt`：
进入 200ms，退出当前 progress × 100ms，ease-out 为 `-p * (p - 2)`；
alpha 为 `0.2 + 0.8p`、scale 为 `0.95 + 0.05p`、Y 位移为 `10(1-p)` DIP。
外部指针主键在遮罩释放时请求关闭；从内容区开始的手势不算外部点击。
固定 Core 的 Desktop `ComposeSceneMediator` 将未消费的键传给 `BackNavigationEventInput`，
Escape 在 KeyDown 请求关闭。2026-10-04 维护者批准 Dialog、BasicAlertDialog 和 AlertDialog 对齐此时机；
ModalWideNavigationRail 保留其现有 KeyDown/KeyUp 配对策略。

平台宽度依据固定 Core `ComposeSceneLayerMeasurePolicy.kt`，由内部 DialogLayout 在显式尺寸之前限制可用宽度。
2026-10-04 维护者批准新增默认启用的 UsePlatformDefaultWidth。已有宽于平台上限的 Dialog 若需保留原宽度，
显式设置 UsePlatformDefaultWidth="False"；Material BasicAlertDialog 的 280–560 DIP 默认范围由组件继续应用。

上游退出时冻结 GraphicsLayer；本地使用实际 RenderScaling 下的 RenderTargetBitmap 冻结内容。
快照源矩形以物理像素表示、目标布局以 DIP 表示，避免高 DPI 下退出内容放大、裁切。
DialogPresenter 局部提供整层透明度合成选项：Avalonia 在子视觉层自己的 RenderOptions 生效前已压入其 opacity，
所以选项设置在父 presenter，保持 GraphicsLayer 的背景与内容一次性合成语义。
纯色遮罩先将动画 alpha 乘入颜色再量化，与 Core scrimColor 更新顺序一致。
动画效果在呈现首帧安装，再由下一帧建立起始时间戳；入场中关闭仍消费关闭重组帧再冻结进度。
重开创建新的入场作用域，旧冻结层和遮罩继续独立退出。卸载或替换模板立即释放全部任务和位图。
2026-10-04 维护者批准上述首帧与重开行为对齐，并要求修复退出缩放问题。
不要把基础 Dialog 的桌面动画套到 Rail：Rail 的遮罩、水平运动和拖动仍使用自己的上游逻辑。

## 当前边界

- 尚未实现：系统/IME inset 策略、predictive back、平台安全窗口选项。
  Material [BasicAlertDialog / AlertDialog](../../Components/AlertDialog/README.md) 已接入此宿主。
- 已接受的宿主适配：限定当前客户区的 PopupOverlayLayer，不创建独立 Window，不设中央管理器。
- 未经验证：真实 OS 输入法组合、原生 popup 窗口混用、屏幕阅读器的模态隔离、触屏、
  多显示器 DPI 切换与跨平台冻结一致性。Headless 的中文文本输入不代表 OS IME 验收。

`DialogTests` 覆盖嵌套、焦点还原、真实 TextBox/ContextMenu、Tab、关闭请求、内容模板与资源更新、
窗口尺寸改变、退出/重开/卸载及与 Modal Rail 叠放。Catalog 提供嵌套 Dialog、编辑框与下拉框供桌面验证。
退出快照另覆盖 1×、1.25×、1.5×、2×、4× 的完整四象限内容，整层透明度与多次重开卸载均有回归。
本轮双端验证结果见 [AlertDialog 说明](../../Components/AlertDialog/README.md#验证与边界)。

本次合并后控件库 670/670、Parity 框架 123/123、Catalog Release 构建通过，包含 Dialog / AlertDialog / Modal Rail 共 51 项回归。
此前 PopupOverlayLayer 迁移时，Rail 的 94 配置双端重复性稳定，迁移前后 308 张采样图及
3,898 项结构化值完全一致；Compose 的既有差异保留。这是先前迁移的证据，
本轮未重新采集 Rail 双端。[先前迁移产物](../../../../artifacts/dialog-foundation/README.md)。
