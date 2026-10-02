# Typography

普通 Avalonia `TextBlock` 通过附加属性消费聚合 `TextStyle`，无需更换控件。
`MaterialTheme.axaml` 启用默认 BodyLarge；其余角色按需选择。所有示例中的 `m3` 命名空间是
`https://github.com/Shirasagi0012/Material3Expressive.Avalonia`。

## XAML

```xml
<TextBlock Text="标题" m3:MaterialText.Style="{m3:Typography TitleLarge}" />

<!-- BasedOn 和字段均可使用绑定；角色更新后，派生样式会重新合并。 -->
<TextBlock Text="强调标题"
           m3:MaterialText.Style="{m3:TextStyle BasedOn={m3:Typography TitleLarge}, FontWeight=Bold}" />

<!-- 原生属性是最终局部覆盖，不会被聚合样式的更新改写。 -->
<TextBlock Text="自定义字号" FontSize="26"
           m3:MaterialText.Style="{m3:Typography TitleLarge}" />

<StackPanel m3:MaterialText.DefaultStyle="{m3:Typography BodyMedium}">
  <TextBlock Text="继承 BodyMedium" />
  <StackPanel>
    <m3:MaterialText.DefaultStyle>
      <m3:TextStyle FontWeight="Bold" />
    </m3:MaterialText.DefaultStyle>
    <TextBlock Text="保留 BodyMedium 度量，只改变字重" />
  </StackPanel>
</StackPanel>
```

`TextStyle` 也可以直接声明为资源：

```xml
<UserControl.Resources>
  <m3:TextStyle x:Key="WarningText" FontSize="14" LineHeight="20"
                FontWeight="Bold" Foreground="DarkRed" />
</UserControl.Resources>

<TextBlock Text="提示" m3:MaterialText.Style="{StaticResource WarningText}" />
```

支持普通 Style Setter 中的角色/派生绑定。`{m3:TextStyle}` 中的字号等字段可写成
`FontSize={Binding PreferredFontSize}`，基础资源可写成 `BasedOn={DynamicResource WarningText}`。
共享资源对象使用不可变 record；更新时替换对象，使用 DynamicResource 的消费者随之更新。

## C#

```csharp
using Material3Expressive.Foundation.Typography;

// 消费位置必须已经能查找到 Material Token 资源。
var title = Typography.GetStyle(textBlock, TypographyRole.TitleLarge); // 当前快照
MaterialText.SetStyle(textBlock, title with { FontWeight = FontWeight.Bold });

// 持续跟随消费位置的资源、主题变化。
textBlock.Bind(MaterialText.StyleProperty,
    Typography.CreateBinding(TypographyRole.TitleLarge));

MaterialText.SetDefaultStyle(panel, new TextStyle { FontWeight = FontWeight.Bold });
```

`TypographyRole` 包含 15 个标准角色和对应的 15 个 Emphasized 角色。

## 选择、合并和优先级

- `DefaultStyle` 按字段合并父级默认值，对内容作用域生效，对应 Compose `ProvideTextStyle`。
  没有父级默认值时，采用消费位置的 BodyLarge。清空局部 DefaultStyle 恢复父级默认值。
- `Style` 非 null 时直接选择该对象，不自动补入环境默认值；为 null 时使用环境默认值。
  `TextStyle.Empty` 可以退出聚合默认值，但不取消 Avalonia 自身的属性继承。
- 字段的 null 表示未指定；`Merge` 仅替换非 null 字段。`FontWeight.Normal`、0 字距、
  `LineHeight = double.NaN` 都是显式值。`with { Foreground = null }` 可以清空字段；Merge 的 null 不负责清空。
- 已指定字段投影为控件自身 `Styles` 集合开头的普通样式，未指定字段没有 Setter。
  本地值/绑定、模板值、类或伪类触发样式遵循 Avalonia 优先级；同一控件后续普通样式可以覆盖。
  祖先处普通类型样式会输给更近的控件自身样式，不能假定所有原生 Setter 都优先。
  全局更改角色建议覆盖角色 Token，局部改动可直接设置 FontSize 等属性或使用类选择器。
- Foreground 未指定时保留原生颜色继承，因此 Button/Surface 的内容颜色仍生效。
  TextStyle.Foreground 指定颜色时，参与同样的原生属性优先级。画刷对象本身仍可变，应按 Avalonia 的共享资源规则使用。

默认值沿现有逻辑/继承关系传递，模板生成内容通过模板父级和视觉父级接入。
重新挂载会重连当前父级，分离时取消对旧父级的订阅。Popup 等独立树需要显式提供默认样式，
不额外建立跨树隐式查找规则。

## Token 层换算

每个 `MaterialTokens.Sys.TypeScale.<Role>` 增加 `Style` 键；基线通过 `TextStyleRef` 在消费位置
组合 Font、Size、Weight、LetterSpacing、LineHeight 五个键。原有 System 标量覆盖继续生效。
直接覆盖 `.Style` 则替换整个角色，该对象不再自动读取原有标量键。

`TextStyle` 和标量 Token **解析结果**的长度统一为 DIP。Token 来源可返回：

```csharp
// 原始 double 表示已经换算好的 DIP，不再应用 FontScale。
host.Resources[MaterialTokens.Sys.TypeScale.TitleLarge.Size] = 24d;

// TextUnit 是 Token 表达式，在资源解析阶段换算。
host.Resources[MaterialTokens.Sys.TypeScale.TitleLarge.Size] = TextUnit.Sp(22);
host.Resources[MaterialTokens.Sys.TypeScale.TitleLarge.LetterSpacing] =
    TextUnit.Em(0.1, MaterialTokens.Sys.TypeScale.TitleLarge.Size);
host.Resources[MaterialTokens.Sys.TypeScale.FontScale] = 1.25d;
// 上例得到字号 27.5 DIP、字距 2.75 DIP。
```

`TextUnit` 位于 `Foundation.Tokens`。sp 乘字体缩放；em 乘指定字号键已经解析出的 DIP 值，
不再次缩放。Avalonia 自身处理显示 DPI，不在这里乘 RenderScaling。
FontScale 是本项目的转换输入键，默认 1，要求有限且大于零；应用负责接入用户字体偏好，
并未自动读取操作系统设置，也未实现 Android 非线性字体缩放。

em 的参考是明确的字号 Token，不是稍后 TextBlock.FontSize 的局部覆盖。
如果字号直接来自 VM 或局部属性，需同步提供所需的 DIP 字距/行高。
循环引用、非法缩放与非有限计算结果沿用 TokenResolver 的解析失败行为。

## 组件接入及迁移

Button 的五种尺寸改为消费 `MaterialTokens.Comp.Button<Size>.LabelTextStyle`，分别引用
LabelLarge、LabelLarge、TitleMedium、HeadlineSmall、HeadlineLarge。Button 作为 DefaultStyle
提供者，将有效原生字体属性传给其字符串、显式控件与模板内容；因此 `Button.FontSize` 仍然有效。
NavigationRail 的标签 Presenter 提供 LabelMedium，图标分支不设置标签的排版默认值。

新增 API：TextStyle、MaterialText.Style/DefaultStyle、TypographyRole、Typography.GetStyle/CreateBinding、
两个 XAML 扩展、30 个 System Style 键、5 个 Button LabelTextStyle 键、TextStyleRef、TextUnit 和 FontScale。
没有删除公开成员，也没有新增公开 MaterialTextBlock。

**行为迁移：** stock Button 主题不再消费旧 Component `LabelTextFont/Size/Weight/Tracking/LineHeight`
槽。旧键和来源保留供现有自定义主题直接消费；曾通过这些键定制 stock Button 的调用方需改为
覆盖该尺寸的 LabelTextStyle，或设置 Button 的原生字体属性。System 标量键的定制无需迁移。
直接调用 TypographyTokenSource.TryGetToken 得到的是来源表达式；需要数值或聚合对象时通过消费位置解析。

## 实现与验证边界

本层提供 TextStyle 的选择、合并、角色查找和 Avalonia 属性投影，不替换 Avalonia 文本排版器。
Compose 的 LineHeightStyle 对齐/首尾裁剪、PlatformTextStyle、字体合成、阴影及文本变换等未实现；
TextWrapping、TextTrimming、MaxLines 继续使用控件自身 API。Rich text 的段内样式仍使用 Avalonia Inline。
现有 ButtonContentPresenter 保留自动标签的受约束段落宽度与裁剪布局职责。

测试覆盖聚合 API、XAML、资源/主题变化、原生覆盖/恢复、作用域生命周期及 Button/Rail 标签集成。
当前 VisualParity 组件用例按规范排除文字；非文字组件回归不证明文本像素或完整 Compose TextStyle 一致性。
