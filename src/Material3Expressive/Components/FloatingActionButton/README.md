# Floating action buttons

依据 `docs/upstream-baseline.json` 固定的 AndroidX `FloatingActionButton.kt`、
`FloatingActionButtonMenu.kt` 及相关 Fab / ExtendedFab / FabMenuBaseline tokens 移植；
桌面参考使用同一 JSON 固定的 Compose Multiplatform Core。两个基线的组件实现一致。

## 入口与尺寸

| Compose 入口 | Avalonia 入口 |
| --- | --- |
| Floating / Small / Medium / LargeFloatingActionButton | `FloatingActionButton.Size = Default / Small / Medium / Large` |
| 四种 Extended FAB 的内容槽重载 | `ExtendedFloatingActionButton.Content`，`Icon = null` |
| 四种 Extended FAB 的 text + icon 重载 | `ExtendedFloatingActionButton.Content` + `Icon` + `Expanded` |
| FloatingActionButtonMenu / MenuItem | `FloatingActionButtonMenu.Button` + `Items` / `FloatingActionButtonMenuItem` |
| ToggleFloatingActionButton / checkedProgress | `ToggleFloatingActionButton.IsChecked` / `CheckedProgress` |
| Modifier.animateFloatingActionButton | `FloatingActionButton.AnimatedVisible`，同样适用于 Extended FAB |

图标 FAB 的容器尺寸为 56 / 40 / 80 / 96 DIP，小 FAB 另有 48 DIP 最小交互分配。
Extended FAB 的高度为 56 / 56 / 80 / 96 DIP。内容槽不强制图标大小；建议普通/小尺寸使用 24，
中尺寸使用 `FloatingActionButtonDefaults.MediumIconSize`（28），大尺寸使用 `LargeIconSize`（36）。
Large 的 36 及 Medium/Large Extended 的 12/16 图标文字间距保留上游 composable 的明确修正，
原始 token 的 32 及 16/20 不被改写。

`Background`、`Foreground`、`CornerRadius`、`ContentTemplate`、`Command`、`Click` 等复用 Button 的入口。
`IconTemplate`、`ButtonTemplate` 支持数据槽。字符串内容使用共享的 Button 文字呈现者与对应 typography。

## 使用

加载 `MaterialTheme.axaml` 后可以直接使用：
示例的 `converters` 对应 `xmlns:converters="using:Avalonia.Markup.Xaml.Converters"`。

```xml
<Window.Resources>
  <converters:ColorToBrushConverter x:Key="FabIconBrushConverter" />
</Window.Resources>

<m3:FloatingActionButton Size="Large" Click="Create">
  <m3:MaterialSymbol Symbol="Add" Width="36" Height="36" />
</m3:FloatingActionButton>

<m3:ExtendedFloatingActionButton Content="Create"
    Expanded="{Binding Expanded}" AnimatedVisible="{Binding ShowAction}">
  <m3:ExtendedFloatingActionButton.Icon>
    <m3:MaterialSymbol Symbol="Add" Width="24" Height="24" />
  </m3:ExtendedFloatingActionButton.Icon>
</m3:ExtendedFloatingActionButton>

<m3:FloatingActionButtonMenu x:Name="Actions" Expanded="{Binding Expanded}"
    MenuAlignment="End" MaxHeight="320">
  <m3:FloatingActionButtonMenu.Button>
    <m3:ToggleFloatingActionButton x:Name="Toggle" IsChecked="{Binding Expanded}"
        CheckedChangeRequested="RequestExpanded">
      <m3:MaterialSymbol Symbol="Add"
          Width="{Binding IconSize, ElementName=Toggle}"
          Height="{Binding IconSize, ElementName=Toggle}"
          Foreground="{Binding IconColor, ElementName=Toggle, Converter={StaticResource FabIconBrushConverter}}" />
    </m3:ToggleFloatingActionButton>
  </m3:FloatingActionButtonMenu.Button>
  <m3:FloatingActionButtonMenuItem Content="Edit" Command="{Binding EditCommand}">
    <m3:FloatingActionButtonMenuItem.Icon>
      <m3:MaterialSymbol Symbol="Edit" Width="24" Height="24" />
    </m3:FloatingActionButtonMenuItem.Icon>
  </m3:FloatingActionButtonMenuItem>
</m3:FloatingActionButtonMenu>
```

Toggle 是受控状态：激活发出反转请求，不自行写入 `IsChecked`。调用方接受请求后更新状态，
同时决定菜单的展开状态及菜单操作后的收起策略。C# 示例：

```csharp
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using MaterialSymbols;

var toggle = new ToggleFloatingActionButton { Size = FloatingActionButtonSize.Medium };
var menu = new FloatingActionButtonMenu { Button = toggle };
toggle.CheckedChangeRequested += (_, e) =>
{
    toggle.IsChecked = e.IsChecked;
    menu.Expanded = e.IsChecked;
};
menu.Items.Add(new FloatingActionButtonMenuItem { Content = "Edit", Command = editCommand });

var fab = new FloatingActionButton
{
    Content = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Add, Width = 24, Height = 24 },
    Elevation = FloatingActionButtonDefaults.LoweredElevation()
};
fab.AnimatedVisible = false;
```

`MaterialSymbolCodepoint` 来自 `MaterialSymbols` 命名空间；Catalog 给出可编译的 XAML 示例。
Toggle 的 `ContainerColor`、`ContainerSize`、`ContainerCornerRadius` 可接收进度函数，
例如 `ContainerSize = ToggleFloatingActionButtonDefaults.ContainerSize(80, 56)`。
图标可绑定 `IconSize`/`IconColor`；自定义图标变化可以直接消费 `CheckedProgress` 及 defaults 提供的映射函数。
`IconColor` 是 `Color`，绑定到 `Foreground` 等 `IBrush` 属性时使用 Avalonia 的 `ColorToBrushConverter`。

## 行为与适配

2026-10-03，维护者确认两个尺寸化按钮类型、菜单 Button/Items 槽、受控 Toggle 的
IsChecked/CheckedProgress/CheckedChangeRequested，以及 AnimatedVisible 入口。
此 API 合并 Compose 的尺寸入口与重载，不合并其不同的布局和动画分支。

- Default Extended 保留 AnimatedVisibility 的整数宽度、展开/收起不同 spring、alpha 规格和中断回到 Visible 的默认 spring。
  Expressive Extended 保留浮点宽度进度、FastSpatial / FastEffects，以及全部收起后才移除文字槽。
- FAB elevation 保留最后一个实际 press/focus/hover 交互，避免把相同高度误认成同一状态；
  tonal elevation 固定取配置的 Default。修改配置先 snap 当前目标，再处理交互目标。动画优先级绑定随作用域清理。
- Toggle 保留外层初始尺寸、内层向逻辑 TopEnd 收缩、Oklab 颜色插值、动态圆角、固定阴影和显式 ripple 半径。
  内容按上游整数除法居中，图标尺寸按物理像素取整；checked 反向保留 AnimateAsState 的取消与重启规则。
  任意内容默认继承环境 Foreground，图标动画为显式绑定。`IconColor` 的绑定适用于可设置颜色的图标，
  2026-10-04，维护者确认本次使用 IconSize / IconColor 绑定入口；Compose animateIcon 对任意位图或复合内容的
  整层 SrcIn tint 留作独立基础能力任务，不计入本次已确认交付范围。
- 菜单保留从底部开始的整数 stagger、每项独立宽度/alpha、自然内容宽度、按钮底部留白及逻辑对齐。
  动态增项在放置阶段初始化动画值，透明度独立更新条目图层，避免菜单总尺寸不变时冻结旧值。
  高度受限时使用内部 ScrollViewer / ScrollContentPresenter，提供原生滚轮、触摸惯性和焦点带入视口，
  收起期间关闭滚动输入；按钮不随列表滚动。滚轮距离与触摸手势由 Avalonia 平台实现决定。
- 隐藏动画 alpha 为零才移除布局，默认缩放到 0.2、逻辑 BottomEnd 枢轴内缩一个物理像素。
  RTL 使用 Avalonia 的视觉树镜像；内部布局保持逻辑坐标。部分透明时启用整组 opacity，阴影与填充一起淡出。
- Button 的命令、焦点和无障碍复用框架实现。Toggle 提供受控 IToggleProvider，禁用时拒绝切换。
  Enter 的原生 keydown 激活沿用共享的已接受决定，见 `docs/DECISIONS.md`。

动画依赖 `ComposeAnimation 0.2.2-alpha`（修复提交 `307e4b8`）：spring 预计时长按 Compose 的整数毫秒截断，
避免 threshold=1 的菜单 stagger 在帧边界多延后一帧。通过本地 NuGet 包接入，没有跨仓库 ProjectReference。

## 验证入口与边界

`tests/Material3Expressive.Tests/FloatingActionButtonTests.cs` 检查尺寸、可见性、状态请求、无障碍、主题角色、
图标绑定、Token 覆盖、卸载重挂、整数放置、菜单焦点与受限滚动。独立双端场景位于
`FloatingActionButtonCases.cs` / `FloatingActionButtonCases.kt`，
suite 为 `floating-action-button`，包含四尺寸、内容重载、主题、约束、RTL、elevation、输入、展开/反向、
Toggle 图标、菜单增项及显隐场景。参考端调用真实 Compose API，并提供真实窗口像素尺寸与桌面环境。

```powershell
./tests/VisualParity/parity.ps1 self-test --family floating-action-button --require-match
./tests/VisualParity/parity.ps1 prepare-reference --family floating-action-button
./tests/VisualParity/parity.ps1 run --family floating-action-button --require-match
```

实际结果与剩余差异以本次交付的 run 路径为准。Raster 的 withinTolerance 不表示逐像素相等；
当前 parity 使用几何色块覆盖内容槽，不评价文字栅格。自动化 headless 和参考回归不代替真人桌面验收。

2026-10-04 的最终自检覆盖 111 个 FAB 用例：双端新捕获、110 个 withinTolerance / 1 个 equal、重复捕获稳定，
执行错误和 notCompared 均为 0。控件库 420 项、阴影模块 7 项、ComposeAnimation 774 项回归通过，
Release 与 Catalog 构建通过。附加的 `button.dynamic.hover.elevated` 回归截图均在容差内，
但既有 Double tween 与 Compose Float 的 elevation 精确读数仍有差异，不计为该 Button 用例通过。
