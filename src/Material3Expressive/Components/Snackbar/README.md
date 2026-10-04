# Snackbar

依据 `docs/upstream-baseline.json` 固定的 AndroidX/Core
`compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Snackbar.kt`、
`SnackbarHost.kt` 和 `tokens/SnackbarTokens.kt`。两个仓库的组件源码一致；固定版本
`ComposeMaterial3Flags.isSnackbarStylingFixEnabled` 为 false，因此移植 Legacy 布局，
不公开未默认启用的 styling-fix 开关。

## 使用

包含 MaterialTheme，并在所属 TopLevel 加载 XAML/设置 Content 前调用
`ComposeAnimation.VisualAnimationScope.Initialize(topLevel)`。

```xml
<Grid xmlns="https://github.com/avaloniaui"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <ContentControl Content="{Binding Page}" />
  <m3:SnackbarHost x:Name="Messages" VerticalAlignment="Bottom" />
</Grid>
```

```csharp
var result = await Messages.State.ShowSnackbarAsync(
    "已删除文件", actionLabel: "撤销", withDismissAction: true,
    cancellationToken: cancellationToken);
if (result == SnackbarResult.ActionPerformed) RestoreFile();
```

无 action 默认 Short（4 秒）；ActionLabel 非 null（包括空字符串）默认 Indefinite。
Long 为 10 秒。Core 桌面的 AccessibilityManager 原样返回时长，本实现沿用此规则。
自定义数据实现 `ISnackbarVisuals`，交给 `ShowSnackbarAsync(visuals, cancellationToken)`。

默认 Host 将 `ISnackbarData` 渲染为 Snackbar；可用 SnackbarTemplate 替换视觉内容，
或绑定自建 SnackbarHostState。默认关闭按钮使用 TooltipBox + PlainTooltip，
应用可通过 Snackbar.DismissActionText 本地化提示和可访问名称：

```xml
<m3:SnackbarHost>
  <m3:SnackbarHost.SnackbarTemplate>
    <DataTemplate><m3:Snackbar Data="{Binding}" DismissActionText="关闭" /></DataTemplate>
  </m3:SnackbarHost.SnackbarTemplate>
</m3:SnackbarHost>
```

Snackbar 也可独立使用，提供 Content/ContentTemplate、Action/ActionTemplate、
DismissAction/DismissActionTemplate、ActionOnNewLine、Background、Foreground、
ActionForeground、DismissActionForeground 和 CornerRadius。
Data 提供缺省文字和按钮；自定义槽优先，Data 入口仍保留外侧 12 DIP 留白。
DataActionForeground 对应上游数据重载的 actionColor，仅用于缺省动作按钮。
自定义槽中的 Click/Command 由应用处理，不会自动调用 Data.PerformAction()/Dismiss()。
槽颜色提供内容环境；自定义控件自身的主题或显式 Foreground 可以覆盖它。

```xml
<m3:Snackbar Content="连接失败" ActionOnNewLine="True">
  <m3:Snackbar.Action>
    <Button Content="重试" Command="{Binding Retry}"
            Theme="{StaticResource MaterialSnackbarActionButtonTheme}"
            Foreground="{Binding ActionForeground, RelativeSource={RelativeSource AncestorType=m3:Snackbar}}" />
  </m3:Snackbar.Action>
</m3:Snackbar>
```

## 布局与适配

保留 LegacyOneRowSnackbar/LegacyNewLineButtonSnackbar 的约束、测量顺序、整数像素
舍入、基线分支、RTL 相对放置及换行动作行的 Top 对齐。
OneRow 的 600 DIP 上限在 start/end 留白之后应用；NewLine 的上限在 start 留白之前应用。
普通单行默认高 48 DIP，多行文字至少 68 DIP；实际大小仍受上游布局关系和外部约束影响。
阴影 6 DIP、轮廓、InverseSurface/InverseOnSurface/InversePrimary 与字体从 Token 解析。
数据动作复用标准 Button，保留简单 TextButton 重载的 12/8 留白；关闭按钮使用 Core 内部
Filled.Close 路径，不替换为轮廓不同的 Material Symbols 字体图标。

Avalonia 没有 Compose 的 AlignmentLine 传播。局部布局读取实际 TextBlock 的首末文字基线、
ContentPresenter 和单子项 Grid，并读取已有标准 Button 的实际容器/文字度量。
默认字符串复用现有 ButtonContentPresenter 的段落测量适配；消息采用 BodyMedium，
动作槽采用 LabelLarge，各槽保留各自内容颜色环境。

**维护者决定（本次移植）：手动基线覆盖 API 留作 backlog。** 不加入
ContentFirstBaseline、ContentLastBaseline、ActionFirstBaseline，也不新增共享基线框架。
复杂自定义容器的基线传播尚未实现；无可读取基线时沿用 Legacy 缺少基线的分支。
该分支在“消息没有基线、动作却有基线”时本身可能产生异常放置，本实现保留其计算。
自定义复杂内容宜使用相应模板或不依赖该对齐关系的布局。

## 状态、动画和生命周期

ShowSnackbarAsync 公平 FIFO 排队。动作/关闭使任务立即完成，退出动画可能仍在运行；
调用方取消会移除当前项或排队项。没有 Host 时，请求等待明确结果或取消。
Host 分离只结束自身计时和动画，不结束应用持有的请求；重挂显示当前项并重新计时。
应用按所属页面/任务的生命周期取消请求，状态入口与其他 Avalonia 内容状态一样通常在 UI 线程使用。

State 立即变化；Host 在下一动画帧提交新呈现，保留固定 Compose 先采样旧动画、再重组的顺序。
Host 保留退出项，使用 FastEffects/FastSpatial spring 淡入淡出和缩放；透明度应用于完整子树
的合成结果，避免重叠内容改变效果。作用域由 ComposeAnimation 管理。

Avalonia 没有 Compose 的 dismiss 语义入口，当前项 Pane AutomationPeer 通过 Invoke 提供关闭，
设置 Polite live setting；退出项为 Off。实际屏幕阅读器播报尚未验证。

## 验证与剩余边界

- `SnackbarHostStateTests`：默认时长、FIFO、当前/排队取消、自定义数据和过期回调。
- `SnackbarHostTests`：保留退出项、状态/模板替换、真实 4/10 秒超时、分离/重挂和 AutomationPeer。
- `SnackbarButtonThemeTests`：标准动作度量/字体、Core Close 路径、48 DIP 交互布局和颜色跟随。
- `SnackbarTests`：真实文字首末基线、硬/软换行、同行/换行、RTL、Token 更新、Data 留白、
  内容/模板替换、默认动作/关闭的 Headless 鼠标点击与提示本地化。
- VisualParity `snackbar`：真实 Snackbar 的非文字槽、按钮、同行/换行、LTR/RTL、明暗主题、
  槽替换、受限宽高、颜色/轮廓、尺寸/主题变化及真实按钮点击过程。
- VisualParity `snackbar-host`：真实 Host 的中断、排队、取消、RTL、模板/状态替换、
  分离/重挂、重叠内容整体透明度，读取真实图层 alpha/scale。

两个 suite 使用固定依赖中的真实 Compose 组件，双端各自构建场景。
Host 分离通过实际移除 composition 内容执行；请求 coroutine 属于应用而非被移除的 Host。
同一帧前连续修改 mounted 两次会被重组合并，不能验证分离。
按现行 VisualParity 规范排除原生文字绘制；真实文字布局由 Headless 测试验证。
文字像素双端一致性、实体桌面交互、实际屏幕阅读器和其他操作系统尚未验证。

结果保存在 `artifacts/snackbar/` 与 `artifacts/visual-parity/runs/`；
Catalog 的 Snackbar demo 提供短/长提示、撤销/关闭、排队、取消和自定义槽示例。
Catalog Headless 检查使用实际 App 主题和字体，打开窗口并通过鼠标点击默认动作、等待退出；
其执行源码、日志和截图在 `artifacts/snackbar/catalog-smoke/`、`catalog-smoke.log`、`catalog-smoke.png`。
