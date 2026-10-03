# BasicAlertDialog / AlertDialog

依据 `docs/upstream-baseline.json` 固定的 AndroidX `AlertDialog.kt` /
`tokens/DialogTokens.kt` 和 Core `AlertDialog.skiko.kt`、`Dialog.skiko.kt`、
`ComposeSceneLayerMeasurePolicy.kt`。两个基线的 Material 内容逻辑相同；桌面宿主与动画取 Core。
不新增上游已废弃的任意内容 AlertDialog 重载，用 BasicAlertDialog 表达此用途。

## 使用

引入 MaterialTheme，并在 Window 可视树中声明。声明不占父布局空间。
Window 模板要求、模态生命周期、宿主与焦点规则见 [Foundation Overlays](../../Foundation/Overlays/README.md)。

```xml
<m3:AlertDialog IsOpen="{Binding IsDialogOpen}" Title="删除记录？"
                DismissRequested="OnDismissRequested">
  <TextBlock Text="选中的记录将被移除。" TextWrapping="Wrap" />
  <m3:AlertDialog.ConfirmButton>
    <Button Content="删除" Click="OnConfirm"
            Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:AlertDialog.ConfirmButton>
  <m3:AlertDialog.DismissButton>
    <Button Content="取消" Click="OnCancel"
            Theme="{StaticResource MaterialTextButtonTheme}" />
  </m3:AlertDialog.DismissButton>
</m3:AlertDialog>

<m3:BasicAlertDialog IsOpen="{Binding IsCustomDialogOpen}" Width="380"
                     UsePlatformDefaultWidth="False" DismissRequested="OnDismissRequested">
  <m3:MaterialSurface CornerRadius="28" Padding="24">
    <TextBox PlaceholderText="自定义内容" />
  </m3:MaterialSurface>
</m3:BasicAlertDialog>
```

```csharp
var dialog = new AlertDialog
{
    Title = "删除记录？",
    Content = new TextBlock { Text = "选中的记录将被移除。" },
    ConfirmButton = new Button { Content = "删除" },
    DismissButton = new Button { Content = "取消" }
};
((Button)dialog.ConfirmButton).Click += (_, _) => dialog.IsOpen = false;
dialog.DismissRequested += (_, _) => dialog.IsOpen = false;
```

调用方必须提供 ConfirmButton 内容或模板。组件不创建按钮、不连接按钮事件、不自动关闭；
DismissRequested 同样只请求关闭。Button 的 Command、Click 和颜色仍由按钮自己处理。
Icon / Title / Content / ConfirmButton / DismissButton 均支持普通数据与模板；
非 null 模板即表示槽存在，即使模板输出为空。

## API 与布局

| Compose 输入 | Avalonia |
| --- | --- |
| BasicAlertDialog content | Content / ContentTemplate，调用方自行提供外观 |
| AlertDialog text | Content / ContentTemplate |
| icon / title / confirmButton / dismissButton | 同名 PascalCase 内容槽及对应 Template 属性 |
| shape / containerColor | CornerRadius / Background |
| iconContentColor / titleContentColor / textContentColor | IconForeground / TitleForeground / TextForeground |
| tonalElevation | TonalElevation，默认 0 |
| onDismissRequest / properties | 继承 Foundation Dialog 的 IsOpen、DismissRequested 和各行为属性 |
| modifier 尺寸 | Width / Height / MinWidth / MaxWidth / MinHeight / MaxHeight |

BasicAlertDialog 默认宽度为 280–560 DIP，没有默认高度，也不加 Surface、padding、阴影或内容颜色。
默认宽度在显式尺寸约束内部应用；例如 Width=100 可以小于默认最小宽度。
Foundation 平台最大宽度先根据客户区短边计算 320 / 440 / 580 DIP，默认启用。
UsePlatformDefaultWidth=false 退出平台上限，保留 Material 默认约束。

AlertDialog 使用 24 DIP 容器 padding。图标和标题各有 16 DIP 底部 padding；
正文底部 24 DIP。图标居中；有图标时标题居中，否则位于 start；正文位于 start，按钮区域位于 end。
正文在测量中使用剩余高度，等价于 Column 的 weight(1, fill=false)，不自动增加滚动容器。
请在需要滚动时自行提供 ScrollViewer；字符串正文可用 ContentTemplate 控制 TextWrapping。

按钮以 confirm、dismiss 的顺序测量：横排 LTR 下 dismiss 在左，confirm 在右；
空间不足时 confirm 位于上一行；RTL 反转水平位置，内容自身保留原方向。
默认 8 DIP 水平间距；上游默认最小交互尺寸 48 减 ButtonDefaults.MinHeight=40 抵消 8 DIP 竖向间距。
没有强制按钮种类或固定按钮宽度。两个槽可以各自提供多个控件的组合，组合本身作为一个布局项。

Surface 使用 SurfaceContainerHigh、ExtraLarge 圆角和三个内容色 Token。
标题、正文、按钮分别提供 HeadlineSmall、BodyMedium、LabelLarge 的聚合文字环境。
IconSize=24 作为 Token 提供给调用方；上游不强制把任意图标内容缩放到此尺寸。
DialogTokens.ContainerElevation=Level3 保留，但实际 AlertDialogContent 的 Surface 没有传 shadowElevation；
默认 tonalElevation=0，不擅自增加阴影。

## 适配与决定

2026-10-04，维护者批准 BasicAlertDialog : Dialog、AlertDialog : BasicAlertDialog 的继承与上述槽 API，
用 Content 表达正文；同时批准 Foundation 默认启用 UsePlatformDefaultWidth。
后续源码核查发现桌面 Escape 在 BackNavigationEventInput 的 KeyDown 路径触发，
维护者批准仅将 Dialog 系列修正为未处理的 Escape KeyDown 请求，Rail 保留现有策略。

模板与内容组合用 XAML；特殊测量和按钮排列用组件内部 Panel。
这些布局已按 start/end 解析物理位置，内部布局部件绕过 Avalonia 的整层 RTL 镜像，
避免位置被再次反转；槽内容仍保留原生 FlowDirection。
退出冻结复用 Foundation：PART_DialogContent 是实际 Surface 内容，整个 AlertDialog 被保留到退出结束。
2026-10-04 维护者另行批准首帧与退出中重开对齐：旧冻结层继续退出，新内容重新入场。
高 DPI 冻结按像素源矩形绘制，动画保持 DIP；背景与内容使用局部整层透明度合成。
正文内容由对应 presenter 建立逻辑/继承关系，以便消费该槽的独立 MaterialText 环境，
而非直接继承 Dialog 声明处的默认文字角色。原生局部字体和颜色覆盖仍遵循 Avalonia 优先级。

## 验证与边界

双端入口为 `tests/VisualParity/Material3Expressive.Parity/Cases/AlertDialogCases.cs` 和
`tests/VisualParity/reference/src/main/kotlin/cases/AlertDialogCases.kt`，包含独立的
`alert-dialog` 与 `basic-alert-dialog` suite。用例通过真实公开组件构造布局与输入，
覆盖可选槽全组合、明暗主题、自动尺寸、显式尺寸、平台宽度、受限高度、横排/竖排、RTL、
自定义形状/颜色/tonal elevation、槽与主题更新、按钮与外部点击、Escape 开关和进出场/重开，
包括入场中关闭、退出中重开、首个动画帧前关闭，以及明暗/RTL 动画。
使用非文字几何夹具，颜色从组件的实际内容环境取得，变化场景显式送重组帧。
桌面键盘参考使用固定 Core 的真实 BackNavigationEventInput，挂载和清理其 dispatcher input。

Headless 的 AlertDialogTests 覆盖真实 XAML、数据模板、原生文字角色/局部覆盖、Token 更新、
尺寸覆盖、按钮应用所有权和 Foundation 平台宽度切换。
Catalog 的 AlertDialogDemo 提供标准对话框和自定义 Basic 对话框。

- 尚未实现：系统/IME inset 与平台安全窗口选项（Foundation 既有缺口）；
  Android 精确指针监听及紧凑尺寸分支；LocalMinimumInteractiveComponentSize 的自定义环境覆盖。
  固定 Core 的精确指针监听是 no-op，此次按维护者确认覆盖其默认 false 分支。
- 已接受的适配：当前客户区内 PopupOverlayLayer 宿主、原生尺寸/模板 API、正文 Content 槽和上述继承结构。
- 未经验证：文字像素一致性、OS 输入法与屏幕阅读器隔离、触屏、多显示器 DPI 切换与跨平台冻结一致性。
  Headless 和双端离屏测试不构成真实桌面交互验收。

2026-10-04 合并后验证：控件库 670/670（包含 AlertDialog 12 项）、Parity 框架 123/123，
Catalog Release 构建与 Compose strict installDist 通过。Foundation 另有 1×、1.25×、1.5×、2×、4× 退出快照、
整层透明度、多次重开及卸载清理回归。
最终双端 self-test 共 75 用例、246 个采样点：BasicAlertDialog 16 个严格相等；AlertDialog 1 个严格相等、
58 个在既定 raster 容差内。结构化读数全部相等；没有不同、未比较项或执行错误。
两端本轮重新采集，各端重复捕获和额外被动截图检查全部稳定，没有复用缓存或仅重比较。
本轮使用 `self-test --family basic-alert-dialog --require-match` 和
`self-test --family alert-dialog --require-match`；结果见
[BasicAlertDialog 回归](../../../../artifacts/visual-parity/runs/basic-alert-dialog-merge-20261004/summary.md) 与
[AlertDialog 回归](../../../../artifacts/visual-parity/runs/alert-dialog-merge-20261004/summary.md)。
完整 PNG、原始差分、结构化读数及重复性结果均保留。
