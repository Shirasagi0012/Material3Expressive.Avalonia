# Button

这里使用 Avalonia 原生 `Button`，由 Material 主题提供五种外观、五档尺寸和交互反馈；
不需要改成另一个按钮控件。命令、事件和内容仍使用 Avalonia 的公开 API。
首次使用请先完成[项目接入](../../../../docs/GETTING_STARTED.md)。

实现映射、设计依据和一致性边界见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

在已加载 Material 主题的应用中，下面就是默认 Filled / Small 按钮：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <Button Content="保存" />
</UserControl>
```

后续 XAML 片段使用相同的命名空间。

## 选择外观与尺寸

通过 `Theme` 选择外观；五种主题共享尺寸 class 和内容布局：

| 外观 | `Theme` 资源名 | 默认表现 |
| --- | --- | --- |
| Filled | `MaterialFilledButtonTheme` | 主色填充；也是未指定 Theme 时的默认值 |
| Elevated | `MaterialElevatedButtonTheme` | 低层级表面色与阴影 |
| Tonal | `MaterialTonalButtonTheme` | 次要容器色填充 |
| Outlined | `MaterialOutlinedButtonTheme` | 透明背景与描边 |
| Text | `MaterialTextButtonTheme` | 透明背景、无描边 |

```xml
<StackPanel Spacing="12">
  <Button Content="保存" />
  <Button Content="新建" Theme="{StaticResource MaterialElevatedButtonTheme}" />
  <Button Content="完成" Theme="{StaticResource MaterialTonalButtonTheme}" />
  <Button Content="取消" Theme="{StaticResource MaterialOutlinedButtonTheme}" />
  <Button Content="了解更多" Theme="{StaticResource MaterialTextButtonTheme}" />
</StackPanel>
```

每个按钮只选一档尺寸 class。Small 不需要 class；没有额外的 `small` 或 `square` 尺寸/形状开关。

| 尺寸 | `Classes` | 默认容器最小高度（DIP） |
| --- | --- | --- |
| Extra small | `xsmall` | 32 |
| Small | 不设置 | 40 |
| Medium | `medium` | 56 |
| Large | `large` | 96 |
| Extra large | `xlarge` | 136 |

```xml
<Button Content="继续" Classes="medium"
        Theme="{StaticResource MaterialTonalButtonTheme}" />
```

尺寸 class 一起选择默认圆角、Padding 和标签排版。表中的高度不是固定 `Height`：内容可以把按钮撑高，
父布局约束也会影响结果。默认容器最小宽度为 58 DIP；未显式约束时外层预留至少 48 DIP 的布局空间，
所以空的 Small 按钮通常占高 48 DIP，背景高 40 DIP；Extra small 的背景为 32 DIP，外层仍为 48 DIP。
这层留白不会扩大鼠标可点击区域。显式 `Width` / `Height`、`MinWidth` / `MinHeight` 可覆盖相应默认约束。
要调整一处作用域的共享布局默认，可覆盖 `Comp.Button.MinWidth` / `MinimumInteractiveSize` Token；
它们也影响同一作用域中消费这些默认值的其他按钮布局。

默认水平靠左、垂直居中；需要铺满父布局时设置 `HorizontalAlignment="Stretch"`。

## 命令、事件与状态

绑定现有 ViewModel 的 `ICommand`，参数用原生 `CommandParameter`：

```xml
<Button Content="保存"
        Command="{Binding SaveCommand}"
        CommandParameter="{Binding SelectedItem}" />
```

这里假设 `DataContext` 提供这些成员；启用编译绑定的视图还需声明对应的 `x:DataType`。
也可使用原生 `Click` 事件，例如 `<Button Content="保存" Click="OnSaveClick" />`，
并在该视图的 code-behind 中实现处理器。两种方式都不需要 Material 专用事件。

- `IsEnabled="False"` 显示禁用外观并禁止激活；`Command.CanExecute` 继续由原生 Button 处理
- 悬停、键盘焦点和按压自动显示状态层或 ripple，无需手动设置伪类
- 按下后将指针移出交互范围会取消这次手势，移回再释放也不会点击；下一次按下可正常激活
- Enter 按下时激活，重复 KeyDown 沿用原生处理；Space 遵循原生 `ClickMode`。已执行的命令不会因随后失焦或禁用而撤回，按压反馈则会清理
- 程序化 `Focus()` 的视觉焦点反馈取决于当前输入模式；需要表达键盘导航焦点时，可在 C# 中调用 `button.Focus(NavigationMethod.Tab)`（`Avalonia.Input`）

## RepeatButton

原生 `RepeatButton` 派生自 `Button`，未指定 `Theme` 时同样是 Filled 按钮；上面的五个 `Theme` 资源、
尺寸 class、`MaterialButton.Icon` 与交互反馈都可直接用于它：

```xml
<RepeatButton Content="增加" Click="OnIncrease" Delay="400" Interval="80"
              Theme="{StaticResource MaterialTonalButtonTheme}" Classes="medium" />
```

- 重复行为沿用原生：按住鼠标左键或 Space 时先等待 `Delay`，再每隔 `Interval` 触发一次 `Click`；Enter 每次 KeyDown 点击一次（系统按键自动重复时随之重复）
- 按住期间状态层与 ripple 保持按压，释放、失去捕获、失焦或禁用时清理
- 与普通 Button 相同，按住时把指针移出交互范围会取消这次手势：按压反馈结束，重复也随 `IsPressed` 清除而停止，移回不会恢复；原生 Fluent 下指针移出后仍会继续重复，这是共享交互带来的行为差异
- ScrollBar、NumericUpDown / ButtonSpinner 以及 Fluent 日期/时间选择器中显式指定主题的 RepeatButton 不受影响

## HyperlinkButton

M3 没有链接按钮。原生 `HyperlinkButton` 未指定 `Theme` 时是 Text 按钮加下划线标签：主色标签、Text 按钮的
状态层、形状、高度与 Padding，因此保留 48 DIP 布局空间和焦点反馈；尺寸 class 与 `MaterialButton.Icon` 同样可用。

```xml
<HyperlinkButton Content="查看文档" NavigateUri="https://github.com/Shirasagi0012/Material3Expressive.Avalonia" />
<HyperlinkButton Content="在新窗口打开" m3:MaterialButton.Icon="{m3:Symbol OpenInNew}" Click="OnOpen" />
```

- 下划线通过标签槽继承的 `TextBlock.TextDecorations` 施加：字符串标签和放入标签槽的自定义文字都会带下划线，图标槽不会。需要去掉时在自己的样式中把标签槽的 `TextBlock.TextDecorations` 设为 `{x:Null}`
- `NavigateUri` 的启动、成功后设置 `IsVisited`、Click / Command 与自动化 peer 均沿用原生
- `:visited` 伪类照常切换，但外观不变：M3 没有“已访问”颜色 Token，主题不自造颜色；需要区分时用自己的 `:visited` 样式
- 禁用使用 Text 按钮的禁用颜色，下划线保留并随标签变为禁用色；RTL 与明暗主题同 Text 按钮

## DropDownButton

M3 没有下拉按钮。原生 `DropDownButton` 未指定 `Theme` 时是 Filled 按钮加尾部 `keyboard_arrow_down` 箭头，
点击、Enter 或 Space 打开其 `Flyout`（原生行为）。五个 `Theme` 资源、尺寸 class 与 `MaterialButton.Icon` 都可直接使用：

```xml
<DropDownButton Content="排序" Theme="{StaticResource MaterialTonalButtonTheme}">
  <DropDownButton.Flyout>
    <MenuFlyout>
      <MenuItem Header="名称" />
      <MenuItem Header="修改日期" />
    </MenuFlyout>
  </DropDownButton.Flyout>
</DropDownButton>
```

- 箭头尺寸、颜色与标签间距同当前尺寸档的前导图标（XS/S/M/L/XL 20/20/24/32/40 DIP），外侧 Padding 不变；RTL 下位于左侧
- Flyout 打开期间（`:flyout-open`）箭头以弹簧旋转 180°，关闭后转回；按压、悬停与焦点反馈与普通 Button 相同
- 箭头由库的全局 `DropDownButton` 样式提供，任何 Button 主题下都会出现；不能通过属性替换或隐藏，也没有公开的尾部图标属性。
  自定义模板若不含 `PART_TrailingIconPresenter` 则没有箭头
- `MenuFlyout` 使用 Material 菜单外观；普通 `Flyout` 的 `FlyoutPresenter` 仍是 Fluent 外观，本库未为其提供主题
- 自动化 peer、`Flyout` 的打开与关闭均沿用原生
- Avalonia 自带的 ColorPicker（`Avalonia.Controls.ColorPicker` 包，Fluent 与 Simple 模板）内部使用未指定主题的 DropDownButton，
  加载本主题后它也会变成 Filled 按钮加箭头；本库未引用该包，不做局部恢复

## 内容与排版

`Content` 可以是字符串或任意控件，也支持原生 `ContentTemplate` 和匹配的 `DataTemplate`。
普通字符串自动应用当前尺寸的标签排版、换行和有限边界内裁剪；自定义内容控件与模板优先。
换行通过原生附加属性 `TextBlock.TextWrapping` 控制，默认为 `Wrap`；主题、绑定和运行时修改均会传给标签。

```xml
<Button Content="保持单行的操作" TextBlock.TextWrapping="NoWrap" />
```

通过 `MaterialButton.Icon` / `IconTemplate` 设置可选的前导图标槽，模板-only 也会占用图标槽：

```xml
<Button Content="新建">
  <m3:MaterialButton.Icon>
    <m3:MaterialSymbol Symbol="Add" />
  </m3:MaterialButton.Icon>
</Button>
```

图标默认尺寸随按钮档位为 20/20/24/32/40 DIP，XS 与 Small 的标签间距为 4 DIP，其余档位沿用对应 Button Token。
`MaterialIcon.DefaultSize` 是可由图标实现消费的尺寸提示；显式 `Width` / `Height` 优先，任意控件内容不会被强制缩放。
只设置图标时不会额外加入标签间距。图标槽不改变默认 `Padding`，也不从任意 `Content` 推断图标。
需要完全自定义省略号或排列时，仍可把图标和文字组合为普通 `Content`。文字默认值通过
`MaterialText.DefaultStyle` 传给内容，显式文字设置仍可覆盖；
更多排版用法见 [Typography](../../Foundation/Typography/README.md)。

## 默认值与接入

默认按压保持 `CornerRadius`，但仍有 ripple。需要形状变化时显式设置可空附加属性
`MaterialButton.PressedCornerRadius`：

```xml
<!-- 固定圆角：按压时仍为 20 DIP -->
<Button Content="保存" CornerRadius="20" />

<!-- 显式启用变形：按压至 8 DIP，释放后恢复 -->
<Button Content="保存" CornerRadius="20"
        m3:MaterialButton.PressedCornerRadius="8" />

<!-- 选择 Small 的按压形状 Token -->
<Button Content="保存"
        m3:MaterialButton.PressedCornerRadius="{m3:Token {x:Static m3:MaterialTokens+Comp+ButtonSmall.PressedContainerShape}}" />
```

C# 使用 `MaterialButton.SetPressedCornerRadius(button, new CornerRadius(8))`
（`Material3Expressive.Components`、`Avalonia`）；传 `null` 恢复固定形状。
属性支持绑定、样式和非对称圆角。普通用户 Style、局部值或绑定设置的 `CornerRadius` 和按压目标不会被尺寸 class 覆盖，
切换尺寸时如需不同的按压目标，应通过自己的样式或绑定更新。
从旧的自动按压变形行为迁移时，需要显式设置 `PressedCornerRadius`；主题不再自动消费按压形状 Token。

常用自定义入口：

- `Background` / `Foreground`：容器色和内容色
- `Padding`、内容对齐、`ContentTemplate`：内容布局
- `CornerRadius`：静止圆角；圆角方形也通过此属性或对应形状 Token 表达
- `BorderBrush` / `BorderThickness`：描边；Outlined 默认在所有尺寸均为 1 DIP
- `m3:MaterialButton.Elevation`：各交互状态的阴影高度；Filled / Tonal 默认悬停升到 1 DIP，Outlined / Text 默认无 elevation 配置

例如自定义一组阴影高度：

```xml
<Button Content="新建">
  <m3:MaterialButton.Elevation>
    <m3:ButtonElevation Default="1" Pressed="1" Focused="1"
                        Hovered="3" Disabled="0" />
  </m3:MaterialButton.Elevation>
</Button>
```

设 `m3:MaterialButton.Elevation="{x:Null}"` 会移除按钮的交互高度策略，恢复模板本身的高度值。
普通用户 Style、局部值和绑定都会覆盖相应的主题默认配置，包括尺寸 class 与禁用状态选择的默认值。
因此固定设置颜色或描边后，禁用时也保留该设置；需要禁用差异时再用自己的状态样式或 Token 表达。
清除局部值（例如 `button.ClearValue(Button.PaddingProperty)`，`button` 为按钮实例）后，
恢复剩余用户样式或当前尺寸的主题默认值，而非创建时的旧值。
默认配置中的 Token 会随资源、主题和所在作用域更新；显式覆盖保持生效，移除后重新使用实时默认值。
状态层和 ripple 的墨色来自主题 Token，不能假定修改 `Foreground` 会同时重配它们。
需要保持各状态联动时，优先按[共享接入说明](../../../../docs/GETTING_STARTED.md)使用 Token 覆盖。

## 使用边界

- Material 焦点环尚未实现；当前键盘焦点反馈来自状态层，主题关闭了原生 `FocusAdorner`
- 普通 Button 不提供选中/切换状态，需要此行为时见 [ToggleButton](../ToggleButton/README.md)
- 默认尺寸与 Padding 未实现上游 precision-pointer 紧凑分支；需要更紧凑布局时可显式覆盖尺寸与 `Padding`
- 替换整个 `Template` 后，不保证保留 Material 的布局、形状动画或交互反馈接入
- 当前实现不是完整视觉一致性承诺；未决差异、验证入口及已接受适配见 [INTERNALS.md](INTERNALS.md#一致性边界与验证)
