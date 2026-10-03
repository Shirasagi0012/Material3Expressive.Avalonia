# 共享能力的已确认决定

本文件只记录跨组件的共享能力决定。组件行为、适配与确认依据随各组件 README 保存；
通用职责与接入规则见 [ARCHITECTURE.md](ARCHITECTURE.md)，VisualParity 决定见其[测试目录文档](../tests/VisualParity/DECISIONS.md)。

## 聚合 TextStyle 与 Token 层文字单位换算

决定：通过不可变 TextStyle、MaterialText.Style/DefaultStyle、Typography 角色绑定及派生 XAML 扩展，
让普通 TextBlock 消费聚合排版；不要求公开 MaterialTextBlock。DefaultStyle 合并环境，Style 显式选择。
角色保留标量来源并增加聚合 Style Token；Button/Rail 的标签接入同一默认值机制。
sp/em 在 Token 消费位置换算为 DIP，保持资源覆盖与字体缩放的动态性，TextStyle 本身只保存 DIP。
原因：保持 Compose 的样式选择、合并与提供默认值语义，同时通过 Avalonia 属性优先级保留原生覆盖。
范围：聚合 API 与当前可投影字段；未授权用近似行为补齐 LineHeightStyle 等底层排版缺口。
具体使用方式、原生样式就近规则、旧 Button Component 标量槽迁移及未实现项见
[Typography 说明](../src/Material3Expressive/Foundation/Typography/README.md)。
确认：2026-10-03，维护者认可 API 方案并要求落地，同时提出在 Token 层完成 sp/em → DIP 换算。

## ComposeAnimation 颜色转换与原始动画终值

决定：在相邻 ComposeAnimation 修正现有 ColorConverter，保留公开 API 与 double 向量接口，
按固定 Compose 的 sRGB / XYZ / Oklab 路径保留矩阵、Float 边界、两次 Half / 10 位 alpha 存储及舍入。
TargetBasedAnimation 保留原始起点和目标，结束时返回原目标，避免有损颜色往返改变终值。
原因：固定上游 Color.VectorConverter 调用 ui-graphics Color.convert，其中间存储会改变动画颜色；
Rail 反向 65ms 的 R=46/47 已由独立数值复现定位。范围为现有 byte sRGB 颜色 API，未决定扩展 ColorSpace API，
也未授权全局改变 core 的 double 精度或亚毫秒处理；若仍有相关差异，另行提出具体方案。
接入：依照现有本地 NuGet 流程打包 Runtime / Avalonia / TestSupport，版本统一为 `0.2.1-alpha`。
确认：2026-10-03，维护者批准修复方案，并指定“打包版本用0.2.1-alpha”。

## 共享交互反馈与键盘激活职责

决定：`MaterialInteractionController` 的键盘输入处理只维护交互反馈，激活由控件自身负责。
共享控制器不拦截 Enter，不通过 automation Invoke 或其他路径代为点击，也不维护组件类型排除名单。
原因：反馈接入不应改变应用命令的执行时机；接管 Enter 会与原生 Space 和导航项的键盘处理形成多套激活路径。
做法：移除专用 Enter 激活状态和 KeyUp 点击分支，保留 Space / Enter 的按压反馈，
并在释放、失焦或禁用时清理反馈。真实 Compose 参考、采样与比较规则保留，激活时机差异如实报告。
影响与范围：共享控制器的键盘反馈接入遵循上述职责；普通 Button / IconButton 保留 Avalonia 原生激活，
Enter 按下激活与固定 Compose 的松开激活不同，已作为框架适配接受，Space 遵循原生 ClickMode。
导航项已有的释放激活保留；该决定不统一各组件的键盘行为。具体行为见
[Button](../src/Material3Expressive/Components/Button/README.md#默认值与接入)与
[IconButton](../src/Material3Expressive/Components/IconButton/README.md#形状与接入)说明。
确认：2026-10-03，维护者要求“不需要Enter拦截了。然后将决策记录在案”，
并进一步要求移除共享控制器中非必要的 Enter 特殊处理；此决定替代此前在 KeyUp 经 automation Invoke 激活的决定。
