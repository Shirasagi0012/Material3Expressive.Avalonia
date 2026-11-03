# Reply

使用 Material3Expressive 和 Avalonia ContainerQuery 实现的 Reply 桌面示例。
布局、邮件数据、头像和浅／深色配色改编自 Android 官方 Compose Reply。

## 运行

在仓库根目录执行以下命令。

```powershell
dotnet run --project examples/Reply/Reply.csproj
```

依赖准备见仓库的 `docs/GETTING_STARTED.md`。

## 已实现

- 原始 12 封示例邮件、七条消息的阅读视图、头像和主题配色。
- 600 DIP 以下使用底部导航，600 DIP 起使用 NavigationRail，1200 DIP 起使用常驻抽屉。
- 840 DIP 起同时显示列表和详情，更窄时显示当前列表或选中的邮件详情。
- 主题头像菜单中的 Light、Dark、System 切换。
- 按主题或发件人前缀搜索，Ctrl+F 聚焦搜索，Escape 清除选择／搜索或返回列表。
- 收藏、点击头像多选、批量操作、单栏返回、侧栏抽屉。

## 暂缺及适配

- 库中尚无 DockedSearchBar。当前用 Material TextBox 组合搜索外观，结果直接过滤收件箱，未实现原版的展开结果面板与动画。
- ContainerQuery 负责窗口宽度适配。尚无 Adaptive 分栏过渡、折叠设备姿态和 Android 系统 Insets。
- Compose、Reply、Reply All、Direct Messages、Articles、Groups 保留入口并禁用。这些流程在参考应用中也包含 TODO 或占位页面。
- 邮件操作只修改本次运行的内存数据。Archive 和 Delete 都将邮件移出当前列表，尚无独立归档／垃圾箱、撤销、持久化或邮件服务。
- 阅读区沿用官方的演示会话数据，并使用固定顺序，便于重复比较布局。尚无附件预览和账户切换。
- 图标使用库中的 Material Symbols Rounded，字形与原版打包的向量图标有差异。
- DrawerSheet 模板把内容垂直对齐固定为 Top，未透传 VerticalContentAlignment。当前常驻侧栏显式按抽屉高度分配内容区域，以还原导航项的居中位置。
- 本阶段使用原始尺寸与当前组件默认值，Compact token 组合尚待设计。

## 来源

- [官方 Reply](https://github.com/android/compose-samples/tree/main/Reply)
- 邮件与账户数据来自 `data/local`，布局来自 `ui/components` 和 `ui/navigation`，颜色来自 `ui/theme/Color.kt`。
- 原始素材许可保留在 `ASSETS_LICENSE`，Apache 2.0 许可保留在 `LICENSE`，改编范围见 `NOTICE`。
