# 控件 Template 消费新 Theme/Token 架构
这套新设计可以称为 Component Defaults Projection：

> Component Defaults 将 raw tokens 和 `ResolvedTheme` 解析成 semantic records；owner-local adapter 再通过 AvaloniaProperty 将这些 records 投影给 ControlTheme、template 和 presenter。


```mermaid
flowchart LR
    R["raw component tokens"] --> D["ButtonDefaults"]
    T["ResolvedTheme"] --> D
    A["Variant / Size / isDark"] --> D

    D --> C["DefaultColors"]
    D --> M["DefaultMetrics"]
    D --> S["DefaultShapes"]
    D --> E["DefaultElevation"]

    C --> CT["Library ControlTheme"]
    M --> CT
    CT --> P["Owner StyledProperties"]
    U["App Style / local values"] --> P
    P --> X["Passive Template"]

    S --> PR["Presenter"]
    E --> PR
    I["interaction / bounds / RTL"] --> PR
    PR --> O["DirectProperty outputs"]
    O --> X
```

## 1. Component Defaults 负责语义解析

`ButtonDefaults` 是无状态的 component policy owner：

```csharp
internal static class ButtonDefaults
{
    public static ButtonColors Colors(
        ResolvedTheme theme,
        ButtonVariant variant,
        bool isDark);

    public static ButtonMetrics Metrics(ButtonSize size);

    public static ButtonShapes Shapes(
        ResolvedTheme theme,
        ButtonSize size);

    public static ButtonElevation Elevation(
        ButtonVariant variant);

    public static SpringSpec ShapeMotion(
        ResolvedTheme theme);

    public static bool HasOutline(
        ButtonVariant variant);
}
```

它负责：

- variant/size 到 raw token table 的映射；
- system color、shape、typography、motion 的解析；
- Compose corrections；
- Avalonia platform adaptations；
- 输出组件语义对象。

它不订阅 theme，不持有控件，也不写 AvaloniaProperty。

## 2. Defaults Adapter 负责生命周期

每个组件拥有一个 owner-local adapter，例如：

```text
ButtonDefaultsAdapter
IconButtonDefaultsAdapter
SwitchDefaultsAdapter
```

Adapter 监听与 defaults 有关的输入：

```text
ResolvedTheme
ActualThemeVariant
Variant
Size
```

依赖变化后调用 `ButtonDefaults`，并发布解析结果：

```csharp
private void Refresh()
{
    var theme = MaterialThemeScope.GetResolvedTheme(_owner);
    var isDark = IsDark(_owner.ActualThemeVariant);
    var variant = ButtonAssist.GetVariant(_owner);
    var size = ButtonAssist.GetSize(_owner);

    var colors = ButtonDefaults.Colors(theme, variant, isDark);
    var metrics = ButtonDefaults.Metrics(size);

    ButtonAssist.SetDefaultColors(_owner, colors);
    ButtonAssist.SetDefaultMetrics(_owner, metrics);
    ButtonAssist.SetDefaultShapes(
        _owner,
        ButtonDefaults.Shapes(theme, size));
    ButtonAssist.SetDefaultElevation(
        _owner,
        ButtonDefaults.Elevation(variant));
    ButtonAssist.SetDefaultShapeMotion(
        _owner,
        ButtonDefaults.ShapeMotion(theme));

    ButtonAssist.SetDefaultOutlineThickness(
        _owner,
        new Thickness(
            ButtonDefaults.HasOutline(variant)
                ? metrics.OutlineWidth
                : 0));
}
```

Adapter 只发布 default outputs，不直接修改 `Background`、`Padding` 等用户属性。

## 3. 通过 AvaloniaProperty 发布 semantic records

对于需要支持原生 Avalonia `Button` 的组件，使用 `ButtonAssist` attached properties：

```csharp
internal static readonly AttachedProperty<ButtonColors>
    DefaultColorsProperty;

internal static readonly AttachedProperty<ButtonMetrics>
    DefaultMetricsProperty;

internal static readonly AttachedProperty<ButtonShapes>
    DefaultShapesProperty;

internal static readonly AttachedProperty<ButtonElevation>
    DefaultElevationProperty;

internal static readonly AttachedProperty<SpringSpec>
    DefaultShapeMotionProperty;

internal static readonly AttachedProperty<Thickness>
    DefaultOutlineThicknessProperty;
```

这些 properties：

- 是只读 default outputs；
- 由 adapter 写入；
- 不属于公共 override API；
- 不重复 Colors、Metrics 等字段；
- 可以被普通 Avalonia binding 观察。

对于 `MaterialFab` 等自有控件，也可以使用 DirectProperty 暴露同类输出，不一定需要 attached property。

## 4. 由 Material ControlTheme 激活 Adapter

原生 Button 只有在使用 Material ControlTheme 时才需要 defaults adapter。

核心主题设置内部 marker：

```xml
<Setter Property="m3:ButtonAssist.UseMaterialDefaults"
        Value="True" />
```

marker 激活 `ButtonDefaultsAdapter`。

因此：

- Material Button 会消费 Material defaults；
- 普通非 Material Button 不承担额外计算；
- adapter 生命周期跟随 owner；
- 不需要全局 component registry。

## 5. ControlTheme 将 defaults 映射到公共属性

Library ControlTheme 使用普通 self-rooted binding：

```xml
<!-- 示意语法 -->
<Setter Property="Background"
        Value="{Binding Path=(m3:ButtonAssist.DefaultColors).Container,
                        RelativeSource={RelativeSource Self}}" />

<Setter Property="Foreground"
        Value="{Binding Path=(m3:ButtonAssist.DefaultColors).Content,
                        RelativeSource={RelativeSource Self}}" />

<Setter Property="BorderBrush"
        Value="{Binding Path=(m3:ButtonAssist.DefaultColors).Outline,
                        RelativeSource={RelativeSource Self}}" />

<Setter Property="Padding"
        Value="{Binding Path=(m3:ButtonAssist.DefaultMetrics).ContentPadding,
                        RelativeSource={RelativeSource Self}}" />

<Setter Property="MinHeight"
        Value="{Binding Path=(m3:ButtonAssist.DefaultMetrics).ContainerHeight,
                        RelativeSource={RelativeSource Self}}" />

<Setter Property="BorderThickness"
        Value="{Binding Path=(m3:ButtonAssist.DefaultOutlineThickness),
                        RelativeSource={RelativeSource Self}}" />
```

状态默认值由 pseudo-style 选择：

```xml
<Style Selector="^:disabled">
    <Setter Property="Background"
            Value="{Binding Path=(m3:ButtonAssist.DefaultColors).DisabledContainer,
                            RelativeSource={RelativeSource Self}}" />

    <Setter Property="Foreground"
            Value="{Binding Path=(m3:ButtonAssist.DefaultColors).DisabledContent,
                            RelativeSource={RelativeSource Self}}" />
</Style>
```

## 6. Template 只消费最终属性

Template 不知道 `ButtonDefaults`、`ResolvedTheme` 或 raw tokens：

```xml
<ContentPresenter
    Padding="{TemplateBinding Padding}"
    TextElement.Foreground="{TemplateBinding Foreground}" />
```

对于 state layer 等 component-specific 能力，增加可覆盖的 StyledProperty：

```xml
<Setter Property="m3:ButtonAssist.StateLayerBrush"
        Value="{Binding Path=(m3:ButtonAssist.DefaultColors).StateLayer,
                        RelativeSource={RelativeSource Self}}" />
```

template 再使用：

```xml
<MaterialRipplePresenter
    StateLayerBrush="{TemplateBinding m3:ButtonAssist.StateLayerBrush}" />
```

## 7. Presenter 消费 profiles 和 runtime state

Presenter 直接读取默认 semantic properties：

```csharp
var shapes =
    ButtonAssist.GetShapesOverride(owner)
    ?? ButtonAssist.GetDefaultShapes(owner);

var elevation =
    ButtonAssist.GetElevationOverride(owner)
    ?? ButtonAssist.GetDefaultElevation(owner);
```

然后结合：

```text
enabled
pressed
hovered
IsSquare
ContainerShapeOverride
bounds
RTL
animation progress
```

计算当前展示结果。

需要 template 观察的运行时输出使用 DirectProperty，例如：

```text
CurrentCornerRadius
```

不创建统一的 `TemplateSettings` 对象。

## 8. Override 优先级

目标语义是：

```text
local value
    > app Style / ControlTheme
    > library Material ControlTheme bindings
    > DefaultColors / DefaultMetrics / ...
    > ButtonDefaults
    > raw tokens + ResolvedTheme
```

Default properties 和公共 override properties 是两套不同角色：

```text
DefaultColors       internal resolved fallback
Background          public effective property
ColorsOverride      optional public semantic override
```

Adapter 不能把 default 写成公共属性的 local value，否则会破坏 Avalonia 的样式优先级。

## 9. 最终删除的旧机制

目标架构不再需要：

```text
MdButtonTokenExtension
ButtonToken enum
ButtonTokenResolver
component-specific ThemeBinding/MultiBinding
TemplateSettings aggregate
generated component overrides
ResolvedTheme.Components
```

最终职责边界是：

```text
ButtonDefaults
    计算默认语义

ButtonDefaultsAdapter
    监听依赖并发布 default AvaloniaProperties

Library ControlTheme
    将 default properties 映射到标准/语义 StyledProperties

Template
    被动消费最终 properties

Presenter
    结合 interaction 和 geometry 计算运行时展示
```

仍需通过 Button 原型验证两点：ControlTheme 内 attached semantic property 的具体 binding 语法，以及 app Style 相对 library ControlTheme binding 的实际覆盖优先级。