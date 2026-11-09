# SecureTextField

`SecureTextField` 在原生 TextBox 编辑能力上提供末字符短暂显示、密码显示模式和导出保护。
标签、错误、前后图标等使用 [TextField](../TextField/README.md) 的装饰入口。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)。

## 基础用法

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <m3:SecureTextField Text="{Binding Password, Mode=TwoWay}"
                      m3:MaterialTextField.Label="密码"
                      PlaceholderText="请输入密码" />
</UserControl>
```

假设 DataContext 提供可写 Password；编译绑定需提供对应 `x:DataType`。
默认 Filled 外观，最后输入的单个字符显示 1500ms，其他字符以 `•` 遮蔽。

## 变体与属性

```xml
<m3:SecureTextField Theme="{StaticResource MaterialOutlinedSecureTextFieldTheme}"
                    TextObfuscationMode="Hidden" PasswordChar="*"
                    m3:MaterialTextField.Label="PIN" />
```

`TextObfuscationMode` 可选 `RevealLastTyped`（默认）、`Hidden`、`Visible`、`System`。
当前桌面 System 对新输入保持遮蔽。Hidden 总是遮蔽；Visible 显示全部内容；显示模式不开放复制、剪切或自动化读取。
`PasswordChar` 定义遮蔽字符；新控件以 `TextObfuscationMode` 选择显示方式，不使用继承的 `RevealPassword`。

最后输入只揭示一次增加一个 UTF-16 单元且结束后无选区的用户编辑；一次粘贴多个字符会隐藏正在揭示的字符。
同一位置的单字符替换沿用原截止时间；新位置输入重新计时。失焦、卸载或切至 Hidden/Visible 清除正在揭示的位置。
程序直接更新 Text 不启动用户输入计时，也不清除已有位置；正在揭示的位置可能显示程序更新后的字符，直至原截止时间。
Text、选区、最大长度、只读、撤销/重做仍由原生编辑器拥有。

## 内容槽与定制

使用 `MaterialTextField.Label`、`LeadingIcon`、`TrailingIcon`、Prefix/Suffix、SupportingText 和各自模板，
两类外观可继续覆盖 TextField Token 和 Shape。示例中的显示按钮可切换 `TextObfuscationMode.Visible` 与 `RevealLastTyped`。
新控件始终保留单行、不换行、复制/剪切限制及密码 IME 提示；输入法仍可提交和显示组合输入，无法读取 surrounding text。
遮蔽模式中的组合文字在提交前保持遮蔽；完整的组合输入末字符揭示协议尚未实现。
自动化 Value 读取为空字符串，值变化不发布原文；只读和禁用时拒绝自动化写入。

旧 `<TextBox Theme="{StaticResource MaterialFilledSecureTextFieldTheme}">` 仍兼容，但只提供旧的 Hidden/Visible 原生行为，
不能得到末字符显示或替换原生 automation/IME。旧路径在 Visible 时 automation 可读取原文，IME 周边文本也可读取原文。
迁移为 `<m3:SecureTextField>`，保留 Theme、Text 绑定和装饰；将 `RevealPassword` 布尔切换改为显示模式。
若要保持旧默认外观，显式指定 `TextObfuscationMode="Hidden"`。仓内 Catalog 已迁移。

## 使用限制与注意事项

新控件按 Unicode 代码点遮蔽，补充字符显示一个遮蔽符；CaretIndex 和选区仍使用原生 UTF-16 索引。
完整输入转换协议尚未提供。真实系统输入法、剪贴板、触控及屏幕阅读器客户端未经完整桌面验收。
TextField 原有的原生编辑、自动高度和标签动画边界同样适用，详见 [INTERNALS.md](INTERNALS.md)。
不要把实际密码写入日志、示例或验证报告。

运行 `dotnet run --project src/Material3Expressive.Catalog -- --secure-text-fields` 查看
[Catalog 示例](../../../Material3Expressive.Catalog/SecureTextFieldDemo.axaml)。
