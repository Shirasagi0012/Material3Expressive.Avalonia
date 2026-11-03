# DragHandle 内部机制与维护说明

## 1. 上游基线与职责映射

固定基线见 [upstream-baseline.json](../../../../docs/upstream-baseline.json)，AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 与 Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e`。
两端 `compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/DragHandle.kt` 实现相同。

| 上游符号 / 职责 | 本地类型 / 成员 |
| --- | --- |
| `VerticalDragHandle`、`collectIsDraggedAsState` | `VerticalDragHandle` 的交互收集与记忆状态 |
| minimumInteractiveComponentSize、layout | `DragHandleLayout` 的外层留白与居中溢出 |
| hoverable、pressable、indication | `DragHandlePresenter` 与共享交互控制器 |
| `tokens/DragHandleTokens.kt` | `DragHandleTokenSource`、`Comp.DragHandle` 与主题绑定 |

## 2. 关键机制与架构契约

- owner 按对象身份收集 DragStart，Stop/Cancel 配对移除。替换 source 清空 collector 列表，记忆布尔值保持到新流的下一条事件。模板替换保留 owner 状态，卸载清空状态和订阅。
- 内部测量按手柄目标尺寸进行，外层处理最小交互尺寸与父约束造成的居中溢出。位置和对齐留白按物理像素舍入，布局绕过自动 RTL 镜像。
- presenter 在 Tunnel 观察局部按压，释放、移出、失去捕获或宿主消费移动时清理。状态优先级为拖动、按下、正常，尺寸、颜色和圆角直接切换。
- presenter 设置内部 `ObservePointerPresses=false`，局部按压由自身维护。共享控制器继续处理 Hover 和外部源反馈，外部 Press 仅驱动 indication。
- source 替换调用 `ResetSourceIndication` 重建反馈，独立于 owner 的记忆状态。卸载释放 TopLevel 处理器和动画等待。
- 尺寸及最小交互留白均经 `Comp.DragHandle` → 主题 → 配置属性进入布局。上游未消费的 ContainerWidth Token 同样保留为未使用。

## 3. 已确认决定与已知差异

- 新组件与共享交互源入口依据剩余组件移植任务授权，记录于引入提交 `e89f5fc8`。通用输入和最小布局环境遵循[共享决定](../../../../docs/DECISIONS.md)。
- 尚未实现任意路径 Shape，当前配置采用圆角。扩展形状需要全库形状与裁剪契约。
- 双端用例覆盖明暗、RTL、分数尺寸、约束、配置切换及重叠交互。Windows Catalog 已检查鼠标拖动和共享源联动，触屏、笔及真实辅助技术未经验证。

## 4. 验证入口

- `tests/Material3Expressive.Tests/DragHandleTests.cs` 检查布局、合成鼠标输入、配对、替换、卸载及 Token 更新。
- 双端用例为 `DragHandleCases.cs` / `DragHandleCases.kt`，族名 `drag-handle`。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~DragHandleTests
./tests/VisualParity/parity.ps1 self-test --family drag-handle --require-match
```
