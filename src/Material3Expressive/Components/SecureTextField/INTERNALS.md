# SecureTextField 内部机制与维护说明

## 1. 上游基线与职责映射

固定 AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与 Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。

| 上游文件 / 职责 | 本地入口 |
| --- | --- |
| Material3 `SecureTextField.kt`：Filled/Outlined 安全字段、默认 RevealLastTyped | `SecureTextField`、两个既有安全主题 |
| Foundation `text/BasicSecureTextField.kt`：`PasswordInputTransformation` / `SecureTextFieldController` | `SecureTextField.ApplyUserChange`、1500ms scope delay、失焦清理 |
| Foundation `text/input/TextObfuscationMode.kt` | `TextObfuscationMode`；内部 `SecureTextPresenter` 负责呈现 |
| Core `skikoMain/.../BasicSecureTextField.skiko.kt` 的 `SplitVisibilitySettings(false,false)` | System 新输入不揭示，RevealLastTyped 仍允许 |
| DisableCutCopy、password semantics、secure IME | 既有附加限制 + 专用 automation peer + `SecureTextInputMethodClient` |
| TextField 装饰、布局与动画 | 继续复用 [TextField](../TextField/INTERNALS.md)，仅共享模板的 TextPresenter 改为兼容派生类型 |

## 2. 关键机制与架构契约

TextBox 始终拥有 Text、选区、Caret、撤销与原生输入；内部 presenter 不改写 Text。
普通 TextBox 直接使用 base.CreateTextLayout，不改变原模板契约、布局和选择行为。
新控件保留 native layout 的约束、选择颜色和预编辑下划线。先按代码点构造显示字符串并真实 shaping，
补充字符压缩为一个 mask 后，SecureTextSource 将实际 glyph clusters 投影回原 UTF-16 索引，保留 glyph、
fallback 字体、bidi、advance 和 offset。最终 TextRun 只持有安全占位文字，不含隐藏原文；输入状态不复制。
原生 Backspace 还会检查占位串的 Unicode 边界，故新字段在此前把真实 surrogate pair 设为原生删除选区，
避免其把同一 mask cluster 再细分。原生编辑、命中和撤销仍执行真实修改；普通 TextBox 不采用此接入。

用户输入判定保留固定 `changes.getRange(0).length == 1` 和无选区条件；同一插入位置不重置 1500ms，
不同位置通过取消前次等待再启动共享 scope.DelayAsync。该 delay 不受 DurationScale 影响，不请求动画帧。
失焦、Hidden/Visible、卸载取消并隐藏；System 与 RevealLastTyped 互换保持已揭示位置，下一次 System 用户编辑隐藏。
程序 Text 更新及 undo/redo 不走 input transformation，保留位置直至截止。键盘删除隐藏；导航与选区变化本身不隐藏。
粘贴拦截后重新进入同一原生 OnTextInput，保留首行、最大长度和选区替换，并让单字符粘贴采用相同显示规则。
拦截位于公开 PastingFromClipboardEvent.RouteFinished，先允许控件及祖先处理器取消，再接管原生 Paste 后续动作；
进程级订阅仅根据事件 Source 分派，不持有控件实例。
原生 Paste 的独立 undo 快照通过固定 Avalonia 12.1.5 SnapshotUndoRedo(bool) 的单一 UnsafeAccessor 保留；
不能删为计数式输入快照。剪贴板仅捕获原生同样四类预期错误，其他错误仍暴露。该内部签名需随 Avalonia 升级核验。

新控件保持 IsSecure，不能以换主题撤去输入限制。原附加属性兼容入口仍可释放自己的限制并恢复绑定。
新 automation peer 返回空 Value、不发布密码值变化；可用且非只读时沿用户输入规则接收替换（多字符隐藏，单字符揭示）。
IME 包装真实客户端，转发光标、预编辑、重置和激活通知；明确 SupportsSurroundingText=false，并返回空周边文本/选区。
不隐藏 accessibility 树，不在视觉失焦时把限制解除。包装随卸载解除订阅。
原生预编辑以单独的显示插入呈现；遮蔽路径把已提交的 reveal 索引映射过预编辑插入区，
避免移动光标后启动组合输入意外揭示另一个已提交字符。组合文字在提交前保持遮蔽，Visible 模式照常显示。

## 3. 已确认决定与已知差异

2026-10-04 原决定选择“仅用原生 TextBox 附加属性，接受暂缺 RevealLastTyped”，原因是保留唯一原生编辑 owner、
避免替换编辑状态。这没有批准 automation/IME 泄露。2026-11-09 本轮维护者明确授权新增 API 与修订历史决定：
新增派生 SecureTextField 仍保留相同编辑 owner，通过可重写的 presenter/automation 和可替换的 IME 请求补齐相应职责。
旧附加入口保留兼容，不追认其安全缺口。新类型默认行为不同；迁移见 README，Catalog 已迁移。
安全控件的自动化不向客户端导出原文，统一以空 Value 表示；这是 UIA 适配，不声明与 Compose 语义树字段逐项相同。

**尚未实现**：TextFieldState/InputTransformation、输出转换及完整 keyboard action/layout 公共 API；
IME 组合输入的完整上游 transaction 分类尚不等价：原生 SetPreeditText(null) 无法区分取消组合与提交前清除，
因此尚未对组合中的单字符执行上游输入变换和重新计时；现有已提交字符的揭示位置与截止时间保持不变。
固定上游 revealCodepointIndex 从 UTF-16 变更范围取得，
显示却以代码点索引匹配；补充字符后的单字符输入沿用该原有行为，不自行修正目标源码。
旧 TextBox 路径仍无末字符揭示，Visible automation 与 IME 周边文本原文缺口仍存在；需使用新类型。
**未经验证**：真实系统 IME、屏幕阅读器、触摸、平台剪贴板失败和跨平台；原生 UTF-16 编辑、Visible 失焦行为与目标不声明一致。
非常规遮蔽字符的连字、复杂双向文字与字体回退组合仍未经验证；不能从默认 bullet 的通过推断任意 glyph cluster 分组。
TextField 标签尺寸插值、空编辑行高度和禁用状态回落的既有差异继续按其文档报告。

## 4. 验证入口

`SecureTextFieldTests` 保留旧路径的模板、输入限制、绑定、原生 automation/IME 和 UTF-16 边界回归。
`SecureTextObfuscationTests` 验证精确截止时间、重置/同位置替换、程序修改、模式、失焦/卸载、粘贴、导出与预编辑保护。
与普通 TextField 回归合计已运行 56 项通过，新增代码点映射覆盖 LTR/RTL glyph 数、caret、命中、选区和删除/替换。
补充用例覆盖组合插入不得移动旧揭示字符、System 模式与 native undo/redo 保留截止时间、控件/祖先可取消粘贴；
每个新场景卸载后检查计时器归零且 scope 无错误。

`SecureTextFieldObfuscationCases.cs` / `.kt` 的 `secure-obfuscation` 实际操作两端安全字段，覆盖 Filled/Outlined、
四模式、连续输入、替换、多字符输入、程序修改、失焦、模式切换、禁用与补充字符，使用相同计时及真实显示字符串。
公开 cursor color 在两端均透明，隔离未虚拟化的原生光标时钟，其他文字和装饰保留。
显示读数来自实际 shaping 后的显示 TextRuns（投影前）；不把用于原生 UTF-16 索引的安全占位串误报为显示文本。
必要文字例外及原理由见 [VisualParity 决定](../../../../tests/VisualParity/DECISIONS.md#securetextfield-遮蔽呈现的必要文字覆盖)。
命令：`./tests/VisualParity/parity.ps1 self-test -NoRestore --family secure-obfuscation`。
`artifacts/visual-parity/runs/secure-obfuscation-final`：22 例全部重复/额外截图稳定，零执行错误；
全部 154 个采样的实际显示文本、UTF-16 长度和焦点相等，包括精确 1500ms 截止、同位置替换、程序更新及补充字符一 mask。
20 例图像在既有容差内；2 例保留真实差异。Filled 禁用后的 516ms 样本仍保留紫色粗指示线，Core 已即时变灰细线，
下一采样恢复在容差内，属于 TextField 原有禁用提交边界。Outlined Visible 的 `ab` 字形边缘 1× 最大通道差为 21，
超过既有 20 门禁；未调整文字、采样或容差。已检查实际显示和差分图，不将这些差异记为已接受。
首个 `secure-obfuscation-probe` 记录了参考端普通 codePoint KeyDown 不产生文字的夹具错误；
固定 Core desktop Foundation 明确只识别 AWT KEY_TYPED，修正为真实 typed 事件后再完整重跑，未改参考控件。
旧 `secure-text-field` 装饰套件的其他差异不因新增行为测试而记为一致。

`SecureTextFieldImeCases.cs` / `.kt` 的 `secure-ime` 补充 Filled/Outlined 的组合更新、取消与提交共 4 例。
Core 平台会话接入固定 DesktopTextInputService2 并发送真实 AWT InputMethodEvent，使用其上游 Empty 平台组件替代 OS 窗口；
Avalonia 使用真实 TextInputMethodClient.SetPreeditText，提交时清除预编辑再发送 native TextInput。
不通过程序 state.edit 模拟输入事务。相同合成初始值 `abc`，输入 `d` 后按真实 Left 键回到首位，
依次组合 `x`、补充字符和 `yz`，再取消或提交 `z`。
`artifacts/visual-parity/runs/secure-ime-final`：4 例全部重复/额外截图稳定，零执行错误；
36 个采样的已提交 UTF-16 长度、组合 UTF-16 长度与焦点全部相等。20 个显示及图像采样一致/在容差内，
16 个保留不同：Core 在单字符组合时已显示 `x••••`，本地保持旧揭示位置 `••••d`；
后续多单元组合时 Core 隐藏旧字符，本地保留它直到旧截止时间。Core 未绘制本地原生预编辑下划线。
取消后 Core 已全遮蔽，本地等到原 1500ms 截止；提交 `z` 后两端显示及新 1500ms 截止一致。
已检查实际配对图，不将组合差异归为栅格容差，也不以调度猜测补写原生接口没有提供的事务边界。
这验证真实平台 adapter 的输入协议，尚非真实 OS 输入法窗口验收；首次 `secure-ime-probe` 结果保留。
