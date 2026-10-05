# SharedTransition Demo 回归计划

## 目标

验证翻新后的 Demo 能稳定启动，并在不依赖人工盯帧或断点的前提下覆盖 SharedElement、SharedBounds、往返导航、连续点击和异常收尾。回归以可重复日志、计数和构建结果为主。

## 阶段一：入口与基线

1. 确认解决方案包含翻新后的 Demo 项目，清理旧的 `bin/obj` 影响后分别执行 Debug/Release 构建。
2. 运行现有 SharedTransition 单元测试及 Host/Coordinator/Session 测试，记录通过数。
3. 检查 Demo 启动、关闭和未选中项状态，确保没有未处理异常或后台进程残留。

## 阶段二：自动化导航回归

为 Demo 保留一个可关闭的自动探针（命令行参数或环境变量启用），执行固定序列：

`列表 → Grove → 返回 → Grove → 返回 → Dusk → 在动画中反向 → 返回`。

每个动作记录：页面状态、Transition 状态、Host generation、端点数、session 数、preparing/running、overlay 状态、各 key 的 endpoint 状态、诊断和异常。每次进入稳定态后断言：

- 配对 key 至少建立一次 session；
- 动画结束后 session、overlay 和临时端点归零；
- 列表项仍可见且矩形没有被抑制状态永久隐藏；
- 没有 `MissingTarget`、`TargetLayoutUnavailable` 或未处理异常；
- 重复运行的 generation 单调增加，资源计数不累积。

## 阶段三：内容类型覆盖

1. SharedElement：验证源端隐藏、目标端捕获、目标布局接管和往返复用。
2. SharedBounds：验证源/目标内容交叉淡变、矩形动画和目标内容变化。
3. 同一 Host 多 key：至少覆盖三项同时注册、缺失目标回退和嵌套去重。
4. 若翻新 Demo 提供参数化数据，使用 12 项列表检查滚动或重排后 key 仍能配对。

## 阶段四：轻量质量检查

- 采集一次 Release 自动探针日志，比较导航前后端点、session、overlay 和托管对象的稳定性。
- 对关键状态输出结构化 JSON，便于后续 CI 断言；保留现有可读文本日志供人工排查。
- 更新 Demo README，写明启动命令、探针开关、日志位置和已知限制。

## 明确跳过项

本轮不做依赖人工观察的逐帧平滑度、像素级截图比对、GPU 合成性能、Rider 断点栈验证，以及复杂滚动/RTL/窗口缩放场景。若自动日志无法判断某项，将标记为“未验证”，不会把它误报为通过。

## 交付物

- Demo Debug/Release 构建结果；
- SharedTransition 测试结果；
- 一份完整自动探针日志和摘要；
- 更新后的 Demo 使用说明；
- 本计划中每项的通过、失败或跳过状态。
