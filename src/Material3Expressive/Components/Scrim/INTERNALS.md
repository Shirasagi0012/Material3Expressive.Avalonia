# Scrim 内部机制与维护说明

## 1. 上游基线与职责映射

AndroidX `f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea` 的 Material3 `Scrim.kt`、`tokens/ScrimTokens.kt`；
Core `a9e47e8aa04c95cfe572ab4297c770f3cbfb234e` 的同名文件与 Foundation `gestures/TapGestureDetector.kt`。
固定 Material3 两端逻辑相同。

| 上游职责 | 本地对应 |
| --- | --- |
| Scrim Canvas、alpha lambda、Color.Unspecified | `Scrim.Render`、`Alpha`、nullable `Color` |
| onClick / null、contentDescription | `Click` / `IsDismissEnabled`、`AutomationProperties.Name` |
| ScrimDefaults.color | `ScrimTokenSource`，Sys.Color.Scrim × .32 |

## 2. 关键机制与架构契约

没有动画时钟、模态宿主或共享状态。颜色透明度与 Alpha 相乘后绘制矩形；Alpha 只在绘制时钳制。
null 与透明颜色不同：前者省略自然尺寸、绘制和点击，后者保留点击。
装饰分支以 Style 优先级关闭命中，应用显式配置仍保留 Avalonia 属性优先级。
首个未消费主键按下开始手势，消耗 down 与最终 up；区域内移动不应用点击距离阈值。
越界或已处理的移动取消本次点击，不能移回恢复；捕获丢失、配置变更与卸载取消活动指针。
多指等待所有已接入指针释放。触屏边界留白按 48 DIP 最小目标计算，鼠标不增加留白。
自动化以 Custom + Invoke 投影上游未指定 Role 的 click 节点；装饰/无颜色分支不进入 control/content view。

## 3. 已确认决定与已知差异

本次剩余移植任务授权非破坏性组件 API 和内部适配，不据此批准其他组件改变手势。
只接入新 ModalBottomSheet，不重构既有 Dialog、Drawer、Rail 的遮罩职责。
尚未实现通用 Compose semantics traversalIndex 投影和布局外初始触摸命中扩张。
后者应复用共享输入层的触摸目标搜索，而不是在 Scrim 中补发指针事件；例如维持
`MaterialInteractiveSize.Minimum="48"` 的使用入口并保留鼠标视觉边界、重叠候选距离和绘制顺序。
这涉及共享输入协议的能力扩展，待维护者决定，不能由本组件的 48 DIP 手势取消边界推断初始命中也已实现。
未经验证：多指、真实触屏/笔、祖先在事件冒泡后消费移动的仲裁、OS 屏幕阅读器以及非 Windows 平台。

## 4. 验证入口

`ScrimTests` 覆盖 null/透明、区域内移动、越界后返回、按压中禁用、装饰点击穿透和 Invoke。
双端 `scrim` 用例覆盖默认/自定义/透明/未指定色、Alpha 边界、无回调、明暗主题及属性和输入变化。

```powershell
dotnet test tests/Material3Expressive.Tests/Material3Expressive.Tests.csproj --filter FullyQualifiedName~ScrimTests
./tests/VisualParity/parity.ps1 prepare-reference --family scrim
./tests/VisualParity/parity.ps1 run --family scrim --require-equal
```

当前 22 个双端场景、39 个采样全部精确相等，无执行错误；自检 `20261104-153253-8fc5a483` 重复及额外截图稳定。
5 项组件行为测试通过。这不包含真实文字或系统辅助技术验证。
