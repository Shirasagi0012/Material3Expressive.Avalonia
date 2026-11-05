# Badge / BadgedBox

`Badge` 用圆点或短内容表达数量/状态；`BadgedBox` 把徽标放到图标等锚点的上方末端，
而不增加锚点的布局尺寸。它们都是库控件。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)；实现与已确认适配见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

以下示例无需 ViewModel；图标字体按公共接入注册：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:BadgedBox>
    <m3:BadgedBox.Badge>
      <m3:Badge Content="99+" AutomationProperties.Name="99 条未读通知" />
    </m3:BadgedBox.Badge>
    <m3:MaterialSymbol Symbol="Notifications" Width="24" Height="24" />
  </m3:BadgedBox>
</UserControl>
```

后续 XAML 沿用相同命名空间。BadgedBox 的 `Content` / `ContentTemplate` 是锚点槽，
`Badge` / `BadgeTemplate` 是徽标槽；都支持数据、模板和动态替换。

## 圆点、计数与导航

省略 Badge.Content 显示默认最小 6 DIP 圆点；内容或 ContentTemplate 存在时改为最小 16 DIP，
水平 padding 各 4 DIP，文字使用 LabelSmall。`Content=""` 仍是有内容分支，
不会回到圆点；设 Content=null 且无 ContentTemplate 才恢复。
尺寸是默认最小值，显式 Width/Height/MinWidth/MinHeight 可覆盖，父最大约束也可缩小。
数字不自动缩写或限制位数，`99+` 等格式由应用提供。

可将整个 BadgedBox 放进 NavigationRailItem、NavigationBarItem、ShortNavigationBarItem 或
WideNavigationRailItem 的 Icon 槽，使用该项提供的上方/末端徽标边界。
[TabRow](../TabRow/README.md) 的 Stacked 内容布局也提供该边界，Leading/Custom 不提供。Badge 本身可直接放入布局，
不要求必须有 BadgedBox。普通 BadgedBox 允许徽标溢出，其自身尺寸只由锚点决定；祖先裁剪仍可能遮住徽标。
RTL 改变相对位置，不镜像文字。

Stacked Tab 的图标可以直接组合徽标，标签栏仍按锚点尺寸布局：

```xml
<m3:Tab Content="收件箱" IsSelected="True">
  <m3:Tab.Icon>
    <m3:BadgedBox>
      <m3:BadgedBox.Badge><m3:Badge Content="99+" /></m3:BadgedBox.Badge>
      <m3:MaterialSymbol Symbol="Mail" Width="24" Height="24" />
    </m3:BadgedBox>
  </m3:Tab.Icon>
</m3:Tab>
```

切换到 Leading 时不再应用 Stacked 的徽标边界；内容可以超出 Tab，但仍受更外层容器的裁剪影响。
Catalog 的 TabRow 示例包含这一组合。

## 内容、外观与输入

用 `Background` / `Foreground` / `CornerRadius` / `Padding` 或内容模板定制单个 Badge。
组合内容可用水平 StackPanel；统一颜色、形状及排版见[Token 接入](../../../../docs/GETTING_STARTED.md#主题与定制)。
两种内容分支默认使用 Error 容器色；前景按容器匹配主题 On 色，无法匹配则继承外层内容色。

Badge 默认 `IsHitTestVisible="False"`，不挡住锚点输入。
若确实需要徽标子内容交互，显式允许整个子树参与命中：

```xml
<m3:Badge IsHitTestVisible="True">
  <Button Content="处理" />
</m3:Badge>
```

该片段只展示输入开启方式，业务操作仍需给按钮接入自己的 Click/Command。
Badge 没有专用动作、选中状态或动画。需要计数绑定时由 DataContext 提供内容属性；
启用编译绑定还需对应的 x:DataType。

## 文字基线与使用边界

BadgedBox 向 Snackbar、RichTooltip 的共享基线读取器转发锚点的首末文字基线，徽标本身的文字
不参与父布局对齐。锚点可为原生 TextBlock、内容模板或另一层 BadgedBox；动态替换和基线变化
会重新测量。无需额外属性，例如：

```xml
<m3:Snackbar Action="详情" ActionOnNewLine="True">
  <m3:BadgedBox>
    <m3:BadgedBox.Badge><m3:Badge Content="!" /></m3:BadgedBox.Badge>
    <TextBlock Text="同步完成&#10;有新的待处理项目" />
  </m3:BadgedBox>
</m3:Snackbar>
```

复杂自定义锚点通过现有 `MaterialTextBaseline.First/Last` 发布真实布局所得基线；不自动搜索任意
Grid/Panel 内的文字。BadgedBox 自身的显式基线优先，清除后恢复转发；只有 Last 时保留锚点首基线。
已有调用无需迁移。曾为弥补缺口在 BadgedBox 上固定基线的调用方，可以清除覆盖以跟随内容。
这不增加 Avalonia 任意父布局的 alignment-line 能力。Catalog 的 SnackbarDemo 包含组合示例。

默认不生成专属可访问性语义，应用应给徽标或锚点提供合适的 AutomationProperties.Name。
文字/MaterialSymbols 栅格、真实桌面操作与屏幕阅读器体验尚未经验证。
Catalog 的 [MainWindow](../../../Material3Expressive.Catalog/MainWindow.axaml) 包含圆点、计数、宽内容、
动态切换及 NavigationBar 集成；验证依据见 [INTERNALS](INTERNALS.md#一致性边界与验证)。
