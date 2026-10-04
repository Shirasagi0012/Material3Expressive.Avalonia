# SecureTextField 维护说明

使用方法及重要安全限制见 [README](README.md)。这层在原生 TextBox 上附加限制，复用 TextField
模板、装饰和动画，不持有编辑状态副本，也不替换原生 automation peer 或 IME client。

## 固定上游与职责

基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)：AndroidX
`f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`；Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。

| 上游文件 / 符号 | 本地入口 |
| --- | --- |
| [SecureTextField.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SecureTextField.kt)：`SecureTextField` / `OutlinedSecureTextField` | [SecureTextField.axaml](SecureTextField.axaml)：基于 Filled / Outlined TextField 的主题、遮蔽默认值及限制菜单 |
| `compose/foundation/foundation/src/commonMain/kotlin/androidx/compose/foundation/text/BasicSecureTextField.kt`：`DisableCutCopy`、`PasswordInputTransformation`、`SecureTextFieldController` | [MaterialSecureTextField.cs](MaterialSecureTextField.cs)：复制/剪切与单行/IME 提示限制，只覆盖能通过附加属性实现的部分 |
| Foundation `text/input/TextObfuscationMode.kt`；Core `skikoMain/.../BasicSecureTextField.skiko.kt` | 原生 PasswordChar / RevealPassword；未实现 RevealLastTyped 控制器 |
| TextField 的装饰、布局与状态机 | [TextField 维护说明](../TextField/INTERNALS.md)，不在安全组件复制实现 |

原生合约按 Avalonia 12.1.2 `TextBox`、`TextBoxAutomationPeer` 与 IME client 核对。
固定 Core 的 System 模式不揭示末字符，显式 RevealLastTyped 仍揭示 1500ms；
Material3 默认是 RevealLastTyped，因此当前 Hidden 默认值不是上游默认行为的等价实现。

## 已确认方案与实现约束

依据[拆分前确认记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/cbbd37e1f85ea992287108235a707b8d87bf30eb/src/Material3Expressive/Components/SecureTextField/README.md)，
维护者于 2026-10-04 在附加属性和派生 TextBox 方案中选择“仅用原生 TextBox 附加属性，接受暂缺
RevealLastTyped”。这项决定允许 Hidden / Visible 与原生编辑限制，不批准 automation / IME 泄露缺口。

`IsSecure` 变为 true 时，以可释放的 Animation 优先级值覆盖单行与 IME 提示；变为 false 时释放，
保留调用者仍在更新的绑定。复制/剪切 routed event 拦截保护直接方法及菜单/IME 调用，
隧道路由的快捷键拦截按平台 HotkeyConfiguration 匹配。不能只隐藏菜单或检查 CanCopy/CanCut。

策略不因 detach / reattach 失效，不能把视觉生命周期当安全限制生命周期；普通主题变化也不关闭策略。
Visible 只影响呈现，不放开导出。应用显式 PasswordChar 为零仍能取消原生遮蔽，IsSecure 不代替显示配置。
TextField 的原生编辑边界、禁用连续回落和标签尺寸插值限制按其已确认范围执行。

## 尚未实现与未解决缺口

- 已接受暂缺：RevealLastTyped 的 1500ms 重置、单字符判定与失焦取消；没有公开不能实现的模式值
- 未解决安全缺口：原生 IValueProvider.Value 仍返回原文并发送值变化事件；附加属性不能替换 peer
- 未解决安全缺口：原生 IME SurroundingText 仍返回原文；IsSensitive / Password 提示不改变读取合约
- 未移植：TextFieldState、InputTransformation、keyboard action / text layout 回调、完整 InteractionSource 和非圆角 Shape，范围同普通 TextField

不能通过隐藏 accessibility 树或减少验证读取掩盖安全缺口。若要修复，需要重新决定派生/组合方案。
原生 UTF-16 遮蔽、Visible 失焦重置、粘贴首行、选区与 IME 行为不声明与 Compose 等价。
真实系统输入法、剪贴板、拖放、触控及屏幕阅读器尚需桌面验收。

## 验证入口与已知差异

[SecureTextFieldTests](../../../../tests/Material3Expressive.Tests/SecureTextFieldTests.cs) 覆盖呈现、选区/撤销、
直接导出/快捷键、粘贴、限制释放、绑定保持，以及原生 automation / IME 原文和补充字符遮蔽的明确复现。
双端场景见 [Avalonia SecureTextFieldCases](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/SecureTextFieldCases.cs)
与 [Compose SecureTextFieldCases](../../../../tests/VisualParity/reference/src/main/kotlin/cases/SecureTextFieldCases.kt)。
参考调用真正安全控件并显式选择 Hidden；非文字内容只验证装饰，不能证明安全语义。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~SecureTextFieldTests
./tests/VisualParity/parity.ps1 self-test --family secure-text-field
./tests/VisualParity/parity.ps1 prepare-reference --family secure-text-field
./tests/VisualParity/parity.ps1 run --family secure-text-field
```

拆分前记录的 different 分别来自以下场景，保留为复查入口，不把历史测试数量当当前结果：

- `static.empty.*` / `static.narrow.*`：原生空编辑行自动高度差 2.5 DIP
- `dynamic.auto-blur.*` / `dynamic.auto-refocus.*`：上述度量与已确认标签尺寸限制
- `dynamic.focus.*`：160000µs 尺寸限制影响布局，Outlined 标签 Y 曾差 0.25 DIP
- `dynamic.disable.*`：连续回落与固定 Core 装饰重建复位，部分槽位 notCompared
- `dynamic.reverse.*`：160000 / 240000µs 标签进度约 `7.45e-9` 差异，所测几何相同

这些是原固定环境中的观测值，不是所有字体的常量；未调整夹具、容差或参考来消除差异。
报告阅读与稳定性/一致性区别见 [VisualParity](../../../../tests/VisualParity/README.md)。
