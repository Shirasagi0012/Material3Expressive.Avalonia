# TextField

原生 `TextBox` + Material `ControlTheme` + `MaterialTextField` 附加属性。
没有公开的基础编辑控件、TextBox 子类、编辑状态副本或输入事务层。
`TextFieldLayout` / `TextFieldDecoration` 是模板内部的测量、绘制和动画部件。

## 使用

应用先加载 Avalonia 基础主题（Catalog 使用 Fluent），再加载 `MaterialTheme`。
基础主题继续为原生 ScrollViewer、滚动条和编辑菜单提供模板。
未指定 `Theme` 的 `TextBox` 使用隐式主题，`BasedOn` `MaterialFilledTextFieldTheme`。

```xml
<TextBox Text="{Binding Name}"
         m3:MaterialTextField.Label="姓名"
         PlaceholderText="请输入姓名" />

<TextBox Theme="{StaticResource MaterialFilledTextFieldTheme}"
         Text="{Binding Name}"
         m3:MaterialTextField.Label="姓名"
         PlaceholderText="请输入姓名"
         m3:MaterialTextField.SupportingText="用于显示在个人资料中" />

<TextBox Theme="{StaticResource MaterialOutlinedTextFieldTheme}"
         Text="{Binding Amount}"
         m3:MaterialTextField.Label="金额"
         m3:MaterialTextField.Prefix="¥"
         m3:MaterialTextField.Suffix="CNY"
         m3:MaterialTextField.IsError="{Binding HasError}"
         m3:MaterialTextField.ErrorMessage="请输入有效金额" />
```

```csharp
var field = new TextBox
{
    Theme = (ControlTheme)this.FindResource("MaterialOutlinedTextFieldTheme")!,
    AcceptsReturn = true,
    TextWrapping = TextWrapping.Wrap,
    MinLines = 3,
    MaxLines = 5,
};
MaterialTextField.SetLabel(field, "说明");
MaterialTextField.SetLabelPosition(field, TextFieldLabelPosition.Above);
MaterialTextField.SetMinimizedLabelAlignment(field, TextFieldLabelAlignment.Start);
```

Label、Placeholder、SupportingText、Prefix、Suffix 接受任意内容及对应 `*Template`。
附加 Placeholder 非 null 时优先于原生 `PlaceholderText`。前后图标使用原生
`InnerLeftContent` / `InnerRightContent`；在装饰布局中分别代表逻辑 leading / trailing，跟随 RTL。
交互图标由调用方提供，例如 `IconButton`；是否可聚焦由该内容自行决定。

Filled 默认 Inside 标签；Outlined 默认 Cutout。两种容器都支持 Inside / Cutout / Above，
以及 `AlwaysMinimizeLabel`、展开/最小化时独立的 Start / Center / End 对齐。
默认宽高约束为 280 / 56 DIP，实际高度包含标签、原生输入行与辅助内容；Cutout 另预留半个最小化标签行高。
显式 Width、Height、MinWidth、MinHeight、Padding 和 CornerRadius 参与本地布局。
默认 Padding 是 16；Inside 且有标签时上下为 8。原生 AcceptsReturn / TextWrapping / MinLines / MaxLines 控制编辑区。

颜色通过 `MaterialTokens.Comp.FilledTextField` / `OutlinedTextField` 接入，状态优先级
disabled > error > focused > unfocused；绑定验证的 `:error` 与显式 IsError 都可触发错误外观。
排版来自 BodyLarge / BodySmall 聚合 Token；动画来自 FastSpatial / FastEffects / SlowEffects。
图标/内容可通过普通前景色继承获取槽位色，显式内容自己的本地值继续优先。

## 上游与适配

固定版本见 `docs/upstream-baseline.json`：AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，
Compose Multiplatform Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
参考 `TextField.kt`、`OutlinedTextField.kt`、`TextFieldDefaults.kt`、`internal/TextFieldImpl.kt`、
Filled / Outlined TextField Tokens；前两个入口和内部装饰实现在两个固定仓库中一致。
原生模板合约按 Avalonia 12.1.2 的 `TextBox.cs` / Fluent `TextBox.xaml` 核对。

- `CommonDecorationBox` 三态、label / placeholder / affix 分别动画；隐藏槽位不参与布局及辅助技术遍历。
- Inside / Cutout 的测量与排列、标签最小行高、Above 外置标签、48 DIP 图标槽和 12 DIP 补偿、2 DIP affix 内距、支持区及 outline 缺口保留在内部布局部件。
- 缺口是真实几何裁剪，额外水平内距 4 DIP；透明容器也可使用。
- 原生 `PART_TextPresenter` / `PART_ScrollViewer` 保留 TextBox 的选区、IME、撤销重做、剪贴板及滚动。
- 字符串 Label / SupportingText 映射到原生 Automation Name / HelpText；错误时使用 ErrorMessage。
  应用显式 AutomationProperties 值优先；复杂 Label 内容请显式提供可访问名称。
  owner 保留原生 Edit / IValueProvider peer，不声称实现 Compose 的完整 semantics 树。

### 已接受的边界

维护者在本轮实现前明确选择了附加属性和内部模板部件，并接受以下原生编辑边界：

1. 辅助文字位于 TextBox 模板内，点击可能聚焦编辑器并移动光标。没有建立独立输入边界。
2. 替换整个 Theme 会触发原生重新套用模板，文本和选区保留，但撤销历史丢失；Fluent 对照也复现。
   普通明暗资源更新保持编辑器和模板身份，已验证不会清空撤销历史。
3. 编辑状态、选区、IME、剪贴板、撤销重做采用原生 TextBox；本阶段不引入 TextFieldState、输入事务、
   InputTransformation / OutputTransformation 或显示文本与编辑文本的位置映射。
4. 禁用时保留 AndroidX 公共装饰的连续回落动画，不模拟 Core 桌面的装饰重建。
   固定 Core `foundation/text/ContextMenu.desktop.kt` 旧菜单分支在 `enabled=false` 时改为直接调用 content，
   重建装饰使标签、placeholder 和 affix 立即回到初态；这是双端禁用中断用例保留的已接受差异。
   维护者在本轮比较后明确选择连续动画；原生编辑器不因该选择重新套用模板。

### 标签过冲与自动高度（2026-10-04）

维护者确认：保留标签坐标的弹簧过冲，但将排版、测量和 outline 缺口尺寸的插值进度限制在 `[0, 1]`。
原始动画值及其速度不变，标签坐标仍用原始进度；字号、行高、字距等排版插值不再超出两个端点。
这是有意采用的上游行为适配，不改变 TextBox 的编辑状态、公开附加属性或模板用法。

固定 Core 的真实自动高度失焦用例同样复现了撑高：4× 渲染、几何标签、无额外槽位时，
Outlined 从 66.5 增至 67 DIP 后回落；修复前 Avalonia 从 64 增至 67 DIP。
端点的 2.5 DIP 差异来自既有的原生空编辑行度量。修复后同一 Avalonia 用例的采样高度保持 64 DIP。
此前动态用例固定外部高度 96，未覆盖这项自动测量行为。

尺寸限制同时适用于聚焦方向的正向过冲，因此原 `dynamic.focus.*` 在 160000 µs 的标签/内容几何
也会与未限制尺寸的 Compose 不同；这属于同一适配。真实内容变化仍可改变高度，并不冻结布局，
也不保证任意自定义内容在整个焦点动画中恒高。

### 尚未实现与未经验证

- SearchBar 展开、建议列表和键盘/IME 搜索动作流程未实现。Catalog 搜索示例只展示可复用输入槽。
- [SecureTextField](../SecureTextField/README.md) 已通过原生主题和附加策略实现 Hidden / Visible、
  单行与复制/剪切限制；RevealLastTyped 暂缺，密码 automation / IME 语义仍有明确缺口。
  单独的 PasswordChar / RevealPassword 透传不启用安全策略。
- Compose keyboard options/action、InteractionSource 对外 API、任意非圆角 Shape、完整 Compose semantics 未移植。
- 原生编辑行度量、换行、字体排版和鼠标光标定位保留平台语义；自动尺寸可能因此不同。
  不把几何槽位 parity 当作文字排版一致性证明。
- 未进行真实 Windows IME 候选窗、系统剪贴板、触控或屏幕阅读器桌面验收；headless IME 测试只覆盖客户端合约。
- 如未来需要替换 TextBox 的 automation peer 或 override 其 protected 编辑策略，需重新决定派生或组合；当前实现不依赖这些能力。

## 验证入口

```powershell
dotnet run --project src/Material3Expressive.Catalog -- --text-fields
dotnet test tests/Material3Expressive.Tests --filter FullyQualifiedName~TextFieldTests
./tests/VisualParity/parity.ps1 prepare-reference --family text-field
./tests/VisualParity/parity.ps1 run --family text-field
./tests/VisualParity/parity.ps1 self-test --family text-field
```

原生编辑与生命周期测试：`tests/Material3Expressive.Tests/TextFieldTests.cs`，截图写入 `artifacts/text-field/`。
独立双端用例：`tests/VisualParity/Material3Expressive.Parity/Cases/TextFieldCases.cs` 和参考端 `cases/TextFieldCases.kt`。
参考直接调用固定 Core 的真实 TextField / OutlinedTextField；无文字几何内容通过正式槽位传入。
固定尺寸矩阵验证两种容器 × 三种标签位置 × 最小化状态 × 内容槽，补充 disabled/error/dark/RTL/对齐和约束。
自动尺寸用例保留真实空编辑行度量；没有给原生编辑器加入测试补偿。
动态用例覆盖 focus、反向、禁用中断、错误和明暗更新。
聚焦动态用例提交真实槽位几何和标签进度，常规采样不截图未虚拟化的原生 caret blink；其余场景保留完整原图。
具体本轮结果和路径在交付说明中记录，run 成功只表示执行完成，self-test 稳定不等于双端一致。

### 首次移植验证结果（2026-10-04）

- 控件库测试 423 通过，其中 TextField 15 项；VisualParity 框架测试 123 通过；Catalog 构建成功。
- 最终普通比较：64 用例、158 采样点、0 执行错误；29 equal、27 withinTolerance、8 different。
  全部固定尺寸外观矩阵、RTL、错误/禁用静态外观、明暗变化与聚焦过程已通过所提交测量。
- `dynamic.disable.*` 两例：已确认的连续回落与 Core 桌面复位差异，动态槽位有无也如实产生 notCompared；没有修改参考或掩盖缺失点。
- `static.empty.*` / `static.narrow.*` 四例：当前固定字体环境和 4× 渲染下，Core 原生空输入行高为 26.5 DIP，
  Avalonia 编辑区最小行为 24 DIP，导致自动总高度差 2.5 DIP 及居中位置差。未通过强制编辑区高度或测试偏移补偿。
  窄宽度几何内容使用受约束的首选尺寸，与 Compose `Modifier.size` 同义；不使用 Avalonia 强制 Width 冒充该语义。
- `dynamic.reverse.*` 两例：160000 / 240000 µs 的标签进度相差约 `7.45e-9`；所测槽位几何一致，精确数值比较仍失败。
  未对读数取整、调整比较容差或修改共享动画运行时。
- 普通比较产物：`D:/Temporary/m3e-textfield/run-final-3/summary.md`；原图、差分和逐字段事实在同目录 `cases/`。
  工作树内早期发布曾遇 Windows 目录重命名拒绝，最终完整产物改在该独立目录生成。
- 最终 self-test：64/64 用例两端重复采集及额外被动截图检查均稳定，0 执行错误；比较分类与普通运行一致。
  产物：`D:/Temporary/m3e-textfield/self-test-final-2/summary.md`。

这些结果不把已接受的禁用差异、原生编辑测量或剩余浮点误差概称为“全部 parity 通过”。

### 自动高度修复验证（2026-10-04）

- 控件库测试 433 项通过，其中 TextField 25 项；新增 10 项覆盖空文本失焦、辅助文字与后续行位置、
  有无前后缀、长标签、多行、自定义排版及过冲时重新聚焦。默认单行在逐帧检查中保持布局高度，
  原始标签动画仍越过端点。检查在 headless 1× 下执行，不代表人工桌面验收。
- Catalog 构建成功。完整 TextField self-test 72 用例、238 采样点：27 equal、27 withinTolerance、
  18 different；0 执行错误，72/72 双端重复采集及额外被动截图检查稳定。
- 本轮重新采集真实 Compose 与 Avalonia，非复用参考或仅重比较。新增 8 个自动高度 blur/refocus
  用例保留真实差异；原有两个 focus 用例新增上述尺寸限制差异，其余原有用例分类保持不变。
  文字场景在控件测试覆盖，双端 parity 使用非文字槽位；焦点动态主采样只读几何和进度。
- 修复前双端证据：`D:/Temporary/m3e-textfield/blur-before/summary.md`。
  修复后完整比较及重复性产物：`D:/Temporary/m3e-textfield/blur-fix-parity/summary.md`。
