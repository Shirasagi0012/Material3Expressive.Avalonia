# Snackbar

`Snackbar` 显示简短消息和可选动作；`SnackbarHost` 结合 `SnackbarHostState` 排队、计时并呈现消息。
它们是库控件，不是原生通知服务。先完成[公共接入](../../../../docs/GETTING_STARTED.md)，
在 Window 加载内容前初始化动画作用域。维护说明见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

将 Host 放在页面底部；下面无需 ViewModel，空队列不会显示消息：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <Grid RowDefinitions="*,Auto">
    <TextBlock Text="页面内容" Margin="24" />
    <m3:SnackbarHost x:Name="Messages" Grid.Row="1" HorizontalAlignment="Stretch" />
  </Grid>
</UserControl>
```

在该视图的业务处理器中调用以下片段；`Messages` 是上面的 Host，`cancellationToken`
由页面/操作生命周期提供，`RestoreFile()` 是应用的撤销方法：

```csharp
using Material3Expressive.Components;

var result = await Messages.State.ShowSnackbarAsync(
    "已删除文件", actionLabel: "撤销", withDismissAction: true,
    cancellationToken: cancellationToken);
if (result == SnackbarResult.ActionPerformed)
    RestoreFile();
```

## 排队、结果与生命周期

不指定时长时，无 action 使用 `Short`（4 秒）；`ActionLabel` 非 null，包括空字符串，
使用 `Indefinite`。`Long` 为 10 秒；可通过 `duration: SnackbarDuration.Long` 显式选择。
自定义数据实现 `ISnackbarVisuals`，传入 `ShowSnackbarAsync(visuals, cancellationToken)`。

请求按 FIFO 排队，动作返回 `ActionPerformed`，关闭或超时返回 `Dismissed`。
任务完成时退出动画可能仍在运行。取消 token 会取消当前或排队请求，调用方应处理
`OperationCanceledException`。没有 Host 的请求会一直等明确结果或取消；Host 卸载只清理自身
计时和动画，不取消应用请求，重挂后显示当前项并重新计时。
通常在 UI 线程使用状态入口，并随页面生命周期取消不再需要的请求。

可以将自己的 `SnackbarHostState` 绑定到 `State`；此时 DataContext 必须提供该实例，
启用编译绑定的视图需声明对应 `x:DataType`。默认 Host 自动创建一个独立 State。

## 默认模板与自定义内容

默认 Host 用 `Snackbar` 呈现 `ISnackbarData`，自动将标准动作/关闭按钮连接到数据结果。
`DismissActionText` 同时提供关闭按钮的提示与可访问名称，可用模板本地化：

```xml
<m3:SnackbarHost>
  <m3:SnackbarHost.SnackbarTemplate>
    <DataTemplate>
      <m3:Snackbar Data="{Binding}" DismissActionText="关闭" />
    </DataTemplate>
  </m3:SnackbarHost.SnackbarTemplate>
</m3:SnackbarHost>
```

片段沿用最小示例命名空间；模板的 DataContext 是 Host 提供的 `ISnackbarData`。
项目默认启用编译绑定时，可为该模板设 `x:DataType="m3:ISnackbarData"` 或明确使用反射绑定。

Snackbar 也可独立使用，其 `Content`、`Action`、`DismissAction` 各有对应模板。
自定义槽优先于 `Data` 的默认内容；自定义动作的事件/命令不会自动调用
`Data.PerformAction()` / `Data.Dismiss()`，由应用负责：

```xml
<m3:Snackbar Content="连接失败" ActionOnNewLine="True">
  <m3:Snackbar.Action>
    <Button Content="重试" Theme="{StaticResource MaterialSnackbarActionButtonTheme}"
            Foreground="{Binding ActionForeground, RelativeSource={RelativeSource AncestorType=m3:Snackbar}}" />
  </m3:Snackbar.Action>
</m3:Snackbar>
```

该片段只展示布局；要执行重试，为按钮添加应用自己的 `Command` 或 `Click` 处理器。
`ActionOnNewLine` 将动作移到下一行。使用 Data 时保留外侧 12 DIP 留白；直接槽入口无此默认外边距。

## 外观与使用边界

`Background` / `Foreground`、`ActionForeground`、`DismissActionForeground`、`CornerRadius`
用于局部定制；`DataActionForeground` 只控制 Data 创建的默认动作按钮。
各槽提供文字/颜色环境，子控件的主题或局部前景可覆盖它；上例显式绑定动作颜色。
默认消息为 BodyMedium、动作为 LabelLarge，标准关闭图标使用固定 Core 的 Close 路径。
全局调整见[主题与 Token 接入](../../../../docs/GETTING_STARTED.md#主题与定制)。

普通单行默认高 48 DIP，多行文字至少 68 DIP，最终尺寸仍受内容与约束影响。
默认使用固定基线的 Legacy 布局；复杂自定义容器没有通用文字基线传播，也没有手动基线覆盖 API。
尤其消息读不到基线而动作有基线时可能产生异常对齐，详见 [INTERNALS](INTERNALS.md#布局与内容不变量)。

当前项提供 Pane automation、Invoke 关闭与 Polite live setting，实际屏幕阅读器播报尚未经验证。
文字像素一致性、实体桌面与其他操作系统验收也未完成。
[SnackbarDemo](../../../Material3Expressive.Catalog/SnackbarDemo.axaml) 与其
[code-behind](../../../Material3Expressive.Catalog/SnackbarDemo.axaml.cs) 展示时长、排队、取消及自定义动作。
