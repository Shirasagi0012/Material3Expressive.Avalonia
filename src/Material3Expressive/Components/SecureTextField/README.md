# SecureTextField

使用原生 `TextBox`，通过 Filled / Outlined 安全主题与 `MaterialSecureTextField.IsSecure`
提供密码遮蔽及复制/剪切限制。标签、错误、前后图标等继续使用 [TextField](../TextField/README.md)
的装饰入口。先完成[公共接入](../../../../docs/GETTING_STARTED.md)；维护依据见 [INTERNALS.md](INTERNALS.md)。

**当前安全边界：原生 automation 的 `IValueProvider.Value` 和 IME 客户端的 SurroundingText
仍可能读取原文，原生值变化事件也未替换。隐藏显示与复制限制不等于已经具备完整的密码安全语义。**
这两项是未解决缺口，不能当作已接受或已对齐行为。

## 最小示例

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <TextBox Theme="{StaticResource MaterialFilledSecureTextFieldTheme}"
           m3:MaterialTextField.Label="密码"
           PlaceholderText="请输入密码"
           m3:MaterialTextField.SupportingText="至少 8 位" />
</UserControl>
```

后续片段沿用同一命名空间。切换 Outlined 外观只需更换 Theme；没有密码控件子类。

## 绑定与显示模式

```xml
<TextBox Theme="{StaticResource MaterialOutlinedSecureTextFieldTheme}"
         Text="{Binding Password, Mode=TwoWay}"
         PasswordChar="*"
         RevealPassword="{Binding ShowPassword, Mode=TwoWay}"
         m3:MaterialTextField.Label="密码"
         m3:MaterialTextField.IsError="{Binding HasError}" />
```

假设 DataContext 提供 `Password`、`ShowPassword` 和 `HasError`；启用编译绑定时补相应
`x:DataType`。Text、选区、只读、撤销/重做和 TextChanged 等事件仍是原生 TextBox API。
不要把密码写入日志、演示用固定文本或验证报告。

两个安全主题默认使用 `•`（U+2022）并隐藏文本；`PasswordChar` 定义遮蔽符，
`RevealPassword` 切换 Hidden / Visible。没有 RevealLastTyped 入口。
原生 TextBox 通常在失焦时重置 RevealPassword，原生菜单保持打开时例外；
若使用绑定，应允许应用状态接收这一更新。显式将 PasswordChar 设为零字符会关闭原生遮蔽，
应用应保持非零值。遮蔽按 UTF-16 单元生成，补充字符会显示两个遮蔽符。

## 输入限制与定制

两个安全主题自动设置 `IsSecure=True`，菜单只含 Paste / Select all。
直接 `Copy()` / `Cut()`、IME 菜单调用与平台复制/剪切快捷键都会被拦截，Visible 模式仍一样。
`CanCopy` / `CanCut` 是原生只读命令状态，Visible 时可能为 true，不能用来判断本策略是否允许导出。

限制启用期间固定为单行、不换行、不接受 Return/Tab；IME 提示设置 Password、IsSensitive、
无建议、无自动大写。粘贴保留原生首行规则，只读继续用 `IsReadOnly`。
普通明暗切换、detach / reattach 不撤去限制；换回普通 TextField 主题或关闭 IsSecure 后，
释放临时属性值并恢复调用者原来的属性/绑定。

自定义主题也可设置 `m3:MaterialSecureTextField.IsSecure="True"`，但该属性只启用输入与导出限制，
不会自行选择遮蔽符、显示模式或装饰 Theme。安全主题使用同一组 `MaterialTextField.LeadingIcon` /
`TrailingIcon` 及对应模板；两者均为 null 时仍回退到原生内容槽。前后图标、Label、Placeholder、Prefix/Suffix、
SupportingText、错误与标签位置的用法见 [TextField](../TextField/README.md)。

## 使用边界

- 暂无最后输入字符的 1500ms 揭示；当前默认 Hidden 与 Compose Material3 默认值不同
- automation / IME 原文读取缺口尚未解决，Password / IsSensitive 只是原生提示
- 没有额外文本拖出流程；当前 Avalonia TextBox 本身没有内建文本拖出源
- 真实系统输入法、剪贴板、拖放、触控和屏幕阅读器未经完整桌面验收
- TextField 的原生编辑、主题替换及自动高度边界同样适用；几何 parity 不证明密码文字或安全语义一致

可运行 `dotnet run --project src/Material3Expressive.Catalog -- --secure-text-fields`，
源码见 [SecureTextFieldDemo](../../../Material3Expressive.Catalog/SecureTextFieldDemo.axaml)。
