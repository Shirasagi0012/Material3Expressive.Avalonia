# Navigation drawer

实现 `ModalNavigationDrawer`、`DismissibleNavigationDrawer`、`PermanentNavigationDrawer`、
三种 `DrawerSheet`、`NavigationDrawerItem` 和 hoisted `DrawerState`。
依据 [固定基线](../../../../docs/upstream-baseline.json) 的
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/NavigationDrawer.kt`
及 `tokens/NavigationDrawerTokens.kt`；该文件的 AndroidX 与 Core 版本一致。
拖动、速度估算和桌面键盘分支另外对照固定 Core Foundation/UI 源码。

## 使用

应用在加载内容前调用 `VisualAnimationScope.Initialize(window)`，并接入 `MaterialTheme`。
容器的 `Content` / `ContentTemplate` 承载页面，`DrawerContent` / `DrawerContentTemplate` 承载抽屉。
三种 Sheet 的 `Content` / `ContentTemplate` 是普通内容槽，由调用方用 StackPanel 等组织目的地。

```xml
<m3:ModalNavigationDrawer x:Name="Drawer">
  <m3:ModalNavigationDrawer.DrawerContent>
    <m3:ModalDrawerSheet State="{Binding State, ElementName=Drawer}">
      <StackPanel Margin="12,16">
        <m3:NavigationDrawerItem Content="Inbox" IsSelected="{Binding InboxSelected}"
                                Command="{Binding OpenInboxCommand}">
          <m3:NavigationDrawerItem.Icon>
            <m3:MaterialSymbol Symbol="Inbox" Width="24" Height="24" />
          </m3:NavigationDrawerItem.Icon>
          <m3:NavigationDrawerItem.Badge><TextBlock Text="24" /></m3:NavigationDrawerItem.Badge>
        </m3:NavigationDrawerItem>
      </StackPanel>
    </m3:ModalDrawerSheet>
  </m3:ModalNavigationDrawer.DrawerContent>
  <TextBlock Text="Page content" />
</m3:ModalNavigationDrawer>
```

```csharp
var state = new DrawerState(DrawerValue.Closed, target => CanNavigateAway(target));
var drawer = new DismissibleNavigationDrawer {
    State = state,
    DrawerContent = new DismissibleDrawerSheet { State = state, Content = destinations },
    Content = page
};
// 在已挂载的控件上调用；新的操作、外部取消或卸载会取消先前任务。
await state.OpenAsync(cancellationToken);
await state.CloseAsync(cancellationToken);
await state.SnapToAsync(DrawerValue.Open, cancellationToken);
```

Permanent 使用 `PermanentNavigationDrawer` + `PermanentDrawerSheet`，不需要 State。
Modal/Dismissible 容器未指定 State 时各自创建独立 Closed 实例。
Sheet 的可选 State 对应上游有状态重载：绑定同一个 State 后启用打开过冲的表面放大、
内容逆缩放及 BackRequested；不绑定时对应无状态重载。
Item 的 `Icon`、`Badge` 均有对应 Template；`IsSelected` 由应用维护，Click、Command、
Enter/Space 松开和自动化 Select 不隐式改变选中值。

## 布局、状态与交互

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

## 适配与验证边界

维护者于 2026-10-04 确认上述槽、State、Sheet 和外部选中 API，以及平台适配：
Avalonia 的 BackRequested 不提供 predictive-back 连续进度流，本组件实现离散 BackRequested
与抽屉内 Escape KeyUp，尚未实现上游 predictive-back 的进度变形/取消恢复。
其余普通开关、拖动、过冲、scrim 和状态行为仍按固定源码移植。
WindowInsets 默认零，由应用传入平台安全区域；不创建第二套 inset 或 overlay 机制。

维护者于同日确认保留以下两类原始数值差异，不升级 ComposeAnimation、不调整比较容差：
ComposeAnimation 0.2.2-alpha 的 double tween/Bezier 运算相对于 Compose Float 运算，
会在部分手势收尾采样中产生约 0.000031 DIP 的 offset 差异；Avalonia double 变换矩阵
相对于 Compose Float 矩阵，会在打开过冲时产生约 0.000032 DIP 的原始布局框差异。
报告仍将超出既定精度的字段标为 `different`；这两类差异不改变已核验采样的状态、回调和
整数像素摆放，Raster 比较通过。不能据此将整轮 SourceProbe 声称为严格一致。

headless 回归位于 [NavigationDrawerTests.cs](../../../../tests/Material3Expressive.Tests/NavigationDrawerTests.cs)，
Catalog 展示位于 [NavigationDrawerDemo.axaml](../../../Material3Expressive.Catalog/NavigationDrawerDemo.axaml)。
独立双端用例为
[NavigationDrawerCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/NavigationDrawerCases.cs) 与
[NavigationDrawerCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/NavigationDrawerCases.kt)，suite 为 `navigation-drawer`。
覆盖三容器、五个 Sheet 入口/重载、槽与选中组合、主题、RTL、insets、宽度边界，以及开关、反向、重开、
snap、拖动/速度/禁用、veto、scrim、键盘、按压、取消、hover 和动态配置变更。
配置变更在下一重组帧观察已提交的布局/绘制，保留变更事件与相同送帧历史。
2026-10-04 合并验证：Release 整体构建（含 Catalog）与 Compose 参考构建通过；
控件库 698 项、Parity 框架 123 项测试通过，其中 Drawer 新增 28 项控件测试。
110 个双端用例
（61 静态、49 动态）的最终比较为 8 `equal`、88 `withinTolerance`、14 `different`，
执行错误及 `notCompared` 均为零。499 份 Raster 中 103 严格相等、396 在既定容差内，
SourceProbe 的其余差异仅为上文已确认的两类数值精度差异。
本次完整 110 用例自检重新采集双端，重复捕获及额外被动截图均稳定，详见
[合并自检报告](../../../../artifacts/visual-parity/runs/navigation-drawer-merge-20261004/summary.md)。
分支阶段的 Catalog headless 集成覆盖 XAML 绑定、开关、外部选中、主题和 RTL，并生成 8 张截图。
文字以稳定几何标记替代，真实字体排版、真实 OS 触摸和屏幕阅读器仍需专项验证。
