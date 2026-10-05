# TextField 维护说明

使用方法见 [README.md](README.md)。本文记录上游对应、实现约束、已确认适配与验证边界；
通用移植要求见 [PORTING.md](../../../../docs/PORTING.md)，共享能力见
[ARCHITECTURE.md](../../../../docs/ARCHITECTURE.md)及[共享决定](../../../../docs/DECISIONS.md)。

## 上游与源码入口

固定版本以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`；Core
`a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。更新基线后需要重新核对，不能用浮动 HEAD 代替。
下列 Material3 文件位于 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/`：

| 上游文件 / 关键符号 | 本地入口与职责 |
| --- | --- |
| [TextField.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/TextField.kt) / `TextField` | [TextField.axaml](TextField.axaml) 的 Filled 主题与原生编辑器模板 |
| [OutlinedTextField.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/OutlinedTextField.kt) / `OutlinedTextField` | 同一模板的 Outlined 主题与缺口布局 |
| [TextFieldDefaults.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/TextFieldDefaults.kt) / `TextFieldDefaults`、`OutlinedTextFieldDefaults` | [MaterialTextField.cs](MaterialTextField.cs) 的装饰配置，以及 [TextFieldTokenSource.cs](TextFieldTokenSource.cs) 的颜色来源 |
| [internal/TextFieldImpl.kt](https://github.com/androidx/androidx/blob/f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea/compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/internal/TextFieldImpl.kt) / `CommonDecorationBox` | [TextFieldLayout.cs](TextFieldLayout.cs) 的三态、槽位动画、测量排列与绘制；[TextFieldLabelPresenter.cs](TextFieldLabelPresenter.cs) 的标签呈现 |
| `tokens/FilledTextFieldTokens.kt`、`tokens/OutlinedTextFieldTokens.kt` | [TextFieldTokenSource.cs](TextFieldTokenSource.cs)；排版/运动继续消费系统 Token |

上述入口及公共装饰在两个固定仓库的对应关系由原移植核对记录保留；平台差异需另查 Core。
与禁用行为有关的 Core 文件是
[foundation/text/ContextMenu.desktop.kt](https://github.com/JetBrains/compose-multiplatform-core/blob/a9e47e8aa04c95cfe572ab4297c770f3cbfb234e/compose/foundation/foundation/src/desktopMain/kotlin/androidx/compose/foundation/text/ContextMenu.desktop.kt)。
原生模板合约按仓库固定的 Avalonia 12.1.2 `TextBox.cs` / Fluent `TextBox.xaml` 核对。

## 实现职责与约束

- 公开入口是原生 `TextBox`、主题和 `MaterialTextField` 附加属性；没有公开的基础编辑控件、TextBox 子类、编辑状态副本或输入事务层
- `TextField.axaml` 保留 `PART_TextPresenter` / `PART_ScrollViewer`，原生控件继续管理选区、IME、撤销重做、剪贴板和滚动；修改模板时必须保留这一合约
- `TextFieldLayout` 在 attach 时连接 owner、presenter 与内容槽，并创建动画作用域；detach 时释放作用域、订阅和无障碍绑定
- `LeadingIcon` / `TrailingIcon` 及模板接入 Compose 的前后图标槽；任一新值非 null 时使用新槽，
  两者均 null 时回退到原生 `InnerLeftContent` / `InnerRightContent`，SecureTextField 共享同一规则
- 图标 presenter 外部保留 48 DIP 槽，内部按子控件测得的 DesiredSize 居中排列，避免 ContentPresenter
  将默认 24 DIP MaterialSymbol 拉伸到整槽；按钮等本身首选尺寸为 48 DIP 的内容继续占满交互空间
- `Phase` 区分聚焦、未聚焦空值、未聚焦非空值。label / placeholder / affix 独立动画，目标在下一共享帧合并；不要用一个 opacity 或仅一张静态截图替代状态转换
- 隐藏槽位不参与布局及辅助技术遍历。placeholder 同时检查原生 Text 与 PreeditText，不能遮住 IME 预编辑文本
- Inside / Cutout / Above 的测量与排列保留在内部布局；leading/trailing 的 48 DIP 槽、12 DIP Padding 补偿、affix 的 2 DIP 内距与支持区由这里消费
- 缺口通过真实几何裁剪形成，额外水平留白为 4 DIP，透明容器也适用；`TextFieldDecoration` 只是内部绘制部件，不是公开替代控件
- 无障碍名称/帮助文字以 Style 优先级投影到原生 Automation 属性，应用的显式值优先；owner 保留原生 Edit / IValueProvider peer，不声称构建了 Compose 的完整 semantics 树
- 共用排版通过 BodyLarge / BodySmall 聚合 Token；运动使用 FastSpatial / FastEffects / SlowEffects。共享能力的原因和限制引用相应文档，不在此复制决定

## 默认配置的数据流

TextFieldTokenSource 保存 Filled / Outlined 的颜色与形状基线，以及共享 TextField 的软最小尺寸、
两种 Padding 和运动引用。主题将它们绑定到原生 TextBox 或内部 TextFieldLayout 的配置属性；
布局与动画只消费这些有效属性，不重新查询 motion Token 或使用 Expressive 备用规格。
运行中的动画值仍由模板局部 Animatable 持有，不写回公开颜色、Padding 或运动配置。

聚焦、错误与禁用样式只选择内部 TextFieldThemeDefaults 的颜色默认值，基础主题再以普通主题
优先级绑定 Background、Foreground、BorderBrush、PlaceholderForeground 和 CaretBrush。
这样普通应用 Style、Binding 与 LocalValue 均可覆盖，清除后恢复当前状态的默认值。
内部标签和其余槽位的颜色仍由主题直接提供给模板部件。

Padding 的主题 MultiBinding 按 Label 非 null 且位置为 Inside 选择 WithLabelContentPadding，
其他情况使用 ContentPadding；布局不再用 IsSet 自建默认值。软最小宽高仍是模板部件配置，
不是原生 MinWidth / MinHeight 的强约束；显式原生约束与受限父空间的原有处理不变。
48 DIP 图标交互槽、12 DIP 图标 Padding 补偿、2 DIP affix 留白等保留在对应布局算法中。

TextStyle 的字段 null 表示未指定，符合共享排版协议；本次没有改变稀疏样式参与标签插值时
现有的字号、行高、字距和字重补值语义。该插值补值仍在组件内，不能据此声称已消除所有排版默认值。
SecureTextField 通过 BasedOn 继承同一数据流，其编辑安全策略不参与外观配置迁移。

## 已确认的原生编辑边界

以下依据来自[拆分前组件移植记录](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/df8656750b2ac03d29ecfd3d81949bfadedcffdf/src/Material3Expressive/Components/TextField/README.md)，确认范围限于该方案；并非仅因当前代码如此实现而追认为决定。
维护者在首次移植前选择附加属性及内部模板部件，并接受：

1. 辅助文字留在 TextBox 模板内，点击可能聚焦编辑器并移动光标，不另建输入边界
2. 替换 Theme 重新套用原生模板，保留文本和选区但丢失撤销历史；Fluent 对照也复现。普通明暗资源更新保持编辑器/模板身份
3. 编辑状态、选区、IME、剪贴板和撤销重做交给原生 TextBox；本阶段不引入 TextFieldState、输入事务、InputTransformation / OutputTransformation 或显示文本到编辑文本的位置映射
4. 禁用时保留 AndroidX 公共装饰的连续回落动画。固定 Core 桌面旧 context-menu 分支在 `enabled=false` 时直接调用 content，重建装饰并使标签、placeholder、affix 复位；维护者在双端比较后明确选择连续动画。原生编辑器不因此重新应用模板

相应可重复检查见 [TextFieldTests.cs](../../../../tests/Material3Expressive.Tests/TextFieldTests.cs)：
`SupportingTextInsideTemplateParticipatesInNativeEditorHitTesting`、
`DecorationAnimatesAndThemeReplacementPreservesTextButLosesNativeUndoHistory`、
`PlainNativeTextBoxAlsoLosesUndoHistoryWhenReplacingTheme`、
`DisableKeepsContinuousDecorationAndDynamicSlotsTrackNativeContent`。

## 已确认的标签过冲与自动高度适配

2026-10-04 维护者确认：保留标签坐标的弹簧过冲，将排版、测量与 outline 缺口尺寸的
插值进度限制在 `[0, 1]`。原始动画值及速度不变；坐标继续消费原始进度，字号、行高、字距等不外推。
此项改变上游过冲时的尺寸行为，属于有意适配，不是与未修改 Compose 完全一致的声明。

原因来自真实自动高度失焦用例：原动态用例把外部高度固定为 96 DIP，未覆盖自动测量。
固定 Core 在 4×、几何标签、无额外槽位时从 66.5 增至 67 DIP 后回落；修复前 Avalonia 从
64 增至 67 DIP，修复后同一用例保持 64 DIP。端点 2.5 DIP 差异来自原生空编辑行度量，未补偿。
这些是旧验证环境的观测值，不是所有字体/平台的固定保证。

同一限制也影响正向过冲：`dynamic.focus.*` 在 160000 µs 的标签/内容几何会与未限制尺寸的
Compose 不同。内容真正变化时仍重新测量，不冻结布局，不保证任意自定义内容恒高。
回归入口为 `EmptyBlurKeepsAutoHeightWhileLabelPositionOvershoots` 和
`BlurRefocusPreservesContentDrivenSizeAndTypographyBounds`。

## 尚未实现与未经验证

**尚未实现：**

- SearchBar 展开、建议列表和键盘/IME 搜索动作流程；Catalog 的搜索输入仅演示内容槽
- Compose keyboard options/action、InteractionSource 对外 API、任意非圆角 Shape、完整 Compose semantics
- 本阶段没有实现上述编辑状态/转换模型；接受原生编辑边界不代表这些 Compose API 已经存在
- SecureTextField 的 RevealLastTyped、密码 automation / IME 缺口由[该组件](../SecureTextField/README.md)维护；PasswordChar / RevealPassword 透传不启用安全策略

**已观察到但未消除的差异：**

- 原生编辑行度量、换行、字体排版及鼠标光标定位沿用平台语义。原 4× 固定字体环境中，Core 空输入行为 26.5 DIP、Avalonia 最小编辑行为 24 DIP；自动尺寸不同不能用强制高度或测试偏移掩盖
- `dynamic.reverse.*` 旧验证在 160000 / 240000 µs 观察到约 `7.45e-9` 的标签进度差异，即使所测槽位几何一致，精确数值比较仍可失败；没有为此取整、改容差或改共享运行时
- 禁用连续回落与尺寸插值限制按上文已确认适配解释，其余差异不能因测试执行完成而一并视为接受

**未经验证：**

- 真实 Windows IME 候选窗、系统剪贴板、触控与屏幕阅读器桌面验收；headless IME 测试仅覆盖客户端合约
- 几何内容的双端比较不证明真实文字排版一致；headless 与编译检查也不替代人工桌面验收
- 如需替换 TextBox automation peer 或 override protected 编辑策略，需重新决定派生/组合方案，当前实现不依赖这些能力

## 验证入口

从仓库根目录执行；双端环境与报告阅读方式见 [VisualParity](../../../../tests/VisualParity/README.md)。

```powershell
dotnet run --project src/Material3Expressive.Catalog -- --text-fields
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~TextFieldTests
./tests/VisualParity/parity.ps1 prepare-reference --family text-field
./tests/VisualParity/parity.ps1 run --family text-field
./tests/VisualParity/parity.ps1 self-test --family text-field
```

- [原生编辑与生命周期测试](../../../../tests/Material3Expressive.Tests/TextFieldTests.cs)：主题、错误/禁用优先级、内容槽、IME 客户端、选区/撤销、主题替换、自动高度、反向与 detach
- [Avalonia 双端用例](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/TextFieldCases.cs)与 [Compose 用例](../../../../tests/VisualParity/reference/src/main/kotlin/cases/TextFieldCases.kt)：Filled / Outlined、三种标签位置、最小化、RTL、错误/禁用/明暗/约束和动态中断
- 自动尺寸用例保留真实空编辑行；几何内容通过正式槽位传入，窄宽度采用与 Compose `Modifier.size` 同义的受约束首选尺寸
- 聚焦动态主采样读取槽位几何与标签进度，不对未虚拟化的原生 caret blink 做常规截图；其他适用场景保留原图

旧 README 中测试数量与本机临时报告路径不作为当前验证结果。历史结果可查 Git 历史，复核应以
上述已跟踪测试和新生成报告为准。`run` 成功只说明执行完成，`self-test` 稳定不等于两端一致；
报告应分别记录执行错误、比较差异、稳定性和未覆盖项。
