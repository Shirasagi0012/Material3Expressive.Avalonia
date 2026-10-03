# SecureTextField

原生 `TextBox`、Filled / Outlined 安全主题和 `MaterialSecureTextField.IsSecure` 附加属性。
复用 TextField 的全部装饰、Token、标签位置、内容槽和动画，不新增编辑器、编辑状态副本或密码控件子类。

## 使用

先加载 Avalonia 基础主题，再加载 `MaterialTheme`，与 [TextField](../TextField/README.md) 相同。

```xml
<TextBox Theme="{StaticResource MaterialFilledSecureTextFieldTheme}"
         Text="{Binding Password}"
         m3:MaterialTextField.Label="密码"
         PlaceholderText="请输入密码"
         m3:MaterialTextField.SupportingText="至少 8 位" />

<TextBox Theme="{StaticResource MaterialOutlinedSecureTextFieldTheme}"
         Text="{Binding Password}"
         PasswordChar="*"
         RevealPassword="{Binding ShowPassword}"
         m3:MaterialTextField.Label="密码"
         m3:MaterialTextField.IsError="{Binding HasError}" />
```

```csharp
var field = new TextBox
{
    Theme = (ControlTheme)this.FindResource("MaterialOutlinedSecureTextFieldTheme")!,
    PasswordChar = '\u2022',
};
MaterialTextField.SetLabel(field, "密码");
field.SetCurrentValue(TextBox.RevealPasswordProperty, true); // Visible
field.SetCurrentValue(TextBox.RevealPasswordProperty, false); // Hidden
```

默认遮蔽符为上游的 `•`（U+2022），默认 Hidden。使用原生 `PasswordChar` 定义遮蔽符、
`RevealPassword` 切换 Hidden / Visible；没有公开不能实现的 RevealLastTyped 枚举值。
原生 TextBox 失焦时通常会重置 RevealPassword（原生菜单保持打开时例外）；
显式 `PasswordChar='\0'` 会关闭原生遮蔽。应用应使用非零遮蔽符。

两个主题自动启用 `IsSecure`，菜单只有 Paste / Select all。直接调用 TextBox.Copy / Cut、
IME 菜单调用及平台配置的复制/剪切快捷键都被拦截；Visible 时仍然拦截。
`CanCopy` / `CanCut` 仍是原生只读命令状态，Visible 时可能为 true，不能把它们当作本策略的执行结果。
Avalonia 12.1.2 TextBox 没有内建的文本拖出源；本实现不另建拖放流程。

`IsSecure` 只启用输入和导出限制，遮蔽由原生属性负责。自定义主题可以设置该属性并配置
PasswordChar / RevealPassword；单独设置 IsSecure 不替调用者选择显示模式或装饰主题。
限制启用期间强制 AcceptsReturn / AcceptsTab 为 false、NoWrap、MinLines / MaxLines 为 1，
并设置 Password、IsSensitive、无建议、无自动大写和单行 IME 提示。
限制以可释放的属性优先级值表达；撤去 IsSecure 或换回普通 TextField 主题后，
恢复调用者原有的属性和持续更新的绑定。普通明暗资源变化及控件 detach / reattach 不撤去策略。
这些属性是原生 IME 提示，尚未经真实系统输入法验证。

Label、Placeholder、SupportingText、Prefix、Suffix、前后图标、错误语义和标签位置等
继续使用 `MaterialTextField` 及原生 TextBox 属性；用法与布局边界见 TextField 文档。
只读状态保留原生 IsReadOnly。粘贴保留原生单行规则：只保留首行；编辑、选区、撤销/重做均为原生行为。

## 固定上游与适配

基线见 `docs/upstream-baseline.json`：AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，
Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
追踪 `material3/SecureTextField.kt` 的 SecureTextField / OutlinedSecureTextField，
`foundation/text/BasicSecureTextField.kt` 的 DisableCutCopy / PasswordInputTransformation /
SecureTextFieldController、`text/input/TextObfuscationMode.kt` 和 Core 的
`skikoMain/.../BasicSecureTextField.skiko.kt`；原生合约按 Avalonia `12.1.2` tag 阅读。
Material3 两入口复用 TextField 装饰，固定 Core 的 System 模式不揭示末字符，
显式 RevealLastTyped 仍强制揭示，持续 1500ms；Material3 默认是 RevealLastTyped。

2026-10-04 维护者在原生附加属性与 TextBox 子类方案中明确选择：
“仅用原生 TextBox 附加属性，接受暂缺 RevealLastTyped”。因此本轮采用 Hidden 默认值，
实现 Hidden / Visible 和原生编辑限制，不补造末字符揭示或编辑位置映射层。
TextField 既有装饰的连续禁用回落、排版/测量进度限制与原生编辑边界继续按其说明执行。

### 尚未实现

- RevealLastTyped 及其 1500ms 重置、单字符输入判定和失焦取消；本轮明确接受暂缺。
- Compose 的密码 accessibility semantics / 安全 automation peer：**Avalonia 原生
  TextBoxAutomationPeer 的 IValueProvider.Value 仍返回原文，并会发送原生值变化事件**。
  附加属性不能替换 peer；没有用隐藏 accessibility 树冒充已解决。
- 安全 IME SurroundingText：**原生客户端仍返回原文**。IsSensitive / Password 提示不改变
  客户端的读值合约。这和 automation 缺口都有 headless 复现，不能视为已接受或已对齐。
- TextFieldState、InputTransformation、Compose keyboard action / text layout 回调、完整
  InteractionSource、非圆角 Shape 等与原生 TextField 相同的未移植 API。

原生遮蔽按 UTF-16 单元生成遮蔽符，补充字符会产生两个符号；Compose 按码点转换。
原生 Visible 的失焦重置、粘贴首行规则、选区与 IME 等保留平台边界；不宣称这些行为与 Compose 相等。
真实 Windows 输入法、系统剪贴板、拖放、触控、屏幕阅读器及人工桌面验收尚未执行。

## 验证

```powershell
dotnet run --project src/Material3Expressive.Catalog -- --secure-text-fields
dotnet test tests/Material3Expressive.Tests --filter FullyQualifiedName~SecureTextFieldTests
./tests/VisualParity/parity.ps1 list --family secure-text-field
./tests/VisualParity/parity.ps1 self-test --family secure-text-field
# 普通 run 使用 prepare-reference 发布的参考，self-test 不发布缓存：
./tests/VisualParity/parity.ps1 prepare-reference --family secure-text-field
./tests/VisualParity/parity.ps1 run --family secure-text-field
```

独立双端入口：`tests/VisualParity/Material3Expressive.Parity/Cases/SecureTextFieldCases.cs`，
`tests/VisualParity/reference/src/main/kotlin/cases/SecureTextFieldCases.kt`。
参考端直接调用真实 SecureTextField / OutlinedSecureTextField，显式选择 Hidden；
不使用普通 TextField 冒充安全组件。按现行 AUTHORING 使用非文字槽位，
遮蔽文字、复制/剪切、IME 和 automation 边界在 `SecureTextFieldTests.cs` 验证。

2026-10-04：72 用例、238 采样点，27 equal、27 withinTolerance、18 different，
0 执行错误；72/72 两端重复捕获及额外被动截图稳定。本轮重新采集 Compose / Avalonia，
未复用参考或仅重比较；self-test 已提供比较证据，无额外重复 run。
产物：`D:/Temporary/m3e-secure-textfield/self-test-final-20261004/summary.md`，逐采样事实和 PNG 位于 `cases/`。

18 个 different 对应复用装饰的现有差异：

- `static.empty.*` / `static.narrow.*` 四例：空编辑行自动高度差 2.5 DIP。
- `dynamic.auto-blur.*` / `dynamic.auto-refocus.*` 八例：上述度量及已确认的标签尺寸限制。
- `dynamic.focus.*` 两例：160000µs 标签尺寸限制影响排布，Outlined 标签 Y 差 0.25 DIP。
- `dynamic.disable.*` 两例：已确认的连续回落与固定 Core 桌面装饰重建复位；部分槽位 notCompared。
- `dynamic.reverse.*` 两例：160000 / 240000µs 原始标签进度约 `7.45e-9` 差异，几何相同。

未调整夹具、遮罩、容差、参考或基础动画库以消除这些结果。
非文字 parity 不证明密码文字排版或完整安全语义一致。
控件库回归 593 项通过（其中 SecureTextField 12 项、TextField 25 项），VisualParity 框架 123 项通过，
Catalog Release 构建通过；原生状态截图已在 headless 渲染后检查。
控件回归与框架回归的 TRX、原生状态截图写入工作树的 `artifacts/secure-text-field/`。
