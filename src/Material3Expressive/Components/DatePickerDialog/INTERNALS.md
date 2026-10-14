# DatePickerDialog 实现说明

使用入口见 [README](README.md)。固定版本以 [upstream-baseline.json](../../../../docs/upstream-baseline.json) 为准：
AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea`，Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
以下 Material3 路径相对 `compose/material3/material3/src/`。

## 布局与来源

- 桌面入口为 Core `skikoMain/kotlin/androidx/compose/material3/DatePickerDialog.skiko.kt`；
  默认参数来自 `commonMain/.../DatePickerDialog.kt` 与 DatePickerDefaults。
- DatePickerDialogContent 将 requiredWidth(360) 与 heightIn(max=568) 放在 BasicAlertDialog 约束内部；
  测量 Surface 时允许超过外层宽度，排列时居中。不能用普通受限 Width 替代。
  wrapContentHeight 清除内部最小高度；显式较大 Height 的分配内仍垂直居中包裹 Surface。
- DatePickerDialogColumn 先测动作，再把剩余高度分配给内容，保留 weight(fill=false) 与 SpaceBetween。
- AlertDialogButtons 对应共享 AlertDialogFlowRow；内部 CrossAxisSpacing 默认 0 保留 AlertDialog 行为，
  日期模板传入 12。确认先测量，横排在 end，换行在上。
- Surface、内容环境和动作文字角色由 XAML 提供。颜色、圆角和尺寸使用 DatePickerModalTokenSource。
  DatePickerDefaults.TonalElevation 是 Level0，不是 DatePickerModalTokens.ContainerElevation 的 Level3。

## 已确认决定

2026-10-11，维护者确认 DatePickerDialog : BasicAlertDialog，Content/Template、
ConfirmButton/DismissButton 及对应 Template、公开 TonalElevation；containerColor 映射 Background，
不提前建立完整 DatePickerColors。动作和关闭请求保持调用方所有。

固定 AndroidX `androidMain/.../DatePickerDialog.android.kt` 的按钮跨行基准为 8 DIP，
扣除默认最小交互尺寸与按钮高度之差后为 0，另有专门 Tab 焦点重排。
Core 桌面入口直接使用 12 DIP 且没有该重排。维护者明确选择整个固定 Core 桌面分支，
不混入 Android 的间距或焦点算法。

宿主复用 [Foundation Dialog](../../Foundation/Overlays/README.md) 的 PopupOverlayLayer、模态焦点、
关闭请求、首帧和冻结退出机制；未新增窗口、中央管理器或动画机制。
模板保持 PART_Root / PART_Modal / PART_Scrim / PART_Surface / PART_DialogContent 合约。
Content 由实际 presenter 持有逻辑关系，使其继承 Surface 环境。
共享决定见 [DECISIONS](../../../../docs/DECISIONS.md)。

## 验证与范围

维护者明确限定为两个同名 Dialog 容器入口。DatePicker、TimePicker、TimeInput 本体，
VibrantTimePickerDialog、Defaults 标题/带 Tooltip 模式按钮辅助组件不在本次范围，也未声称已实现。

- [PickerDialogTests](../../../../tests/Material3Expressive.Tests/PickerDialogTests.cs)：内容分配、按钮间距、
  槽模板、Token 更新、文字环境、真实点击、关闭请求、重开和卸载。
- [DatePickerDialogCases.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/DatePickerDialogCases.cs)：配置展开。
- [PickerDialogScene.cs](../../../../tests/VisualParity/Material3Expressive.Parity/Cases/PickerDialogScene.cs) 与
  [PickerDialogCases.kt](../../../../tests/VisualParity/reference/src/main/kotlin/cases/PickerDialogCases.kt)：两端独立调用真实组件。

非文字矩形通过正常槽测量，读取实际坐标、继承颜色和点击/请求计数；覆盖可选槽、主题、RTL、
约束、内容高度切换、动作宽度切换、输入、进出场和中断。执行命令：

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj -c Release --filter FullyQualifiedName~PickerDialogTests
./tests/VisualParity/parity.ps1 self-test --family date-picker-dialog --require-match
```

self-test 新采集真实双端、重复捕获与额外被动截图，按命令输出路径读取 summary/overview/sample。
withinTolerance 不是严格像素相同。文字排版/像素、真实 OS IME、屏幕阅读器模态隔离、触屏、
多显示器 DPI 和跨平台桌面效果未经验证；Foundation 的系统/IME inset、平台安全窗口缺口继续存在。
