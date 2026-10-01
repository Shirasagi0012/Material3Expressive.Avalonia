# 编写视觉一致性用例

本文说明当前框架的接入方式与执行边界。用例粒度、采样密度和覆盖策略仍待讨论，
现有例子不代表组件覆盖完整，也不是新用例必须照搬的模板。
运行命令见 [README](README.md)，事实定义与输出格式见 [MEASUREMENTS](MEASUREMENTS.md)。
涉及组件时，先按[移植指南](../../docs/PORTING.md)读取固定上游；测试场景不能替代上游依据。

## 选择参考例子

两端入口分别位于 [Cases.cs](Material3Expressive.Parity/Cases.cs) 和
[Cases.kt](reference/src/main/kotlin/Cases.kt)。

| 用例 | 可参考的内容 | 当前范围 |
| --- | --- | --- |
| `surface.static` | 显式尺寸、颜色、几何与截图 | 单个静态容器 |
| `button.filled.enabled` / `disabled` | 真实主题、具名部件、文字与容器读数 | 初始状态；disabled 没有施加点击 |
| `button.filled.press` | 鼠标按压/释放、多个采样点、部分点仅测量 | 单次按压；分别点击各端自身中心，不验证共同坐标的命中范围 |
| `text.wrapped` / `text.paragraph-breaks` | 文字布局、UTF-16 范围及段落边界 | 纯色文本与指定字体输入；不证明真实字体解析或 glyph ink 相同 |
| `motion.interrupted` | 目标中断、value/velocity/running、显式帧序列 | 合成线性动画，不代表组件的全部动效 |
| `protocol.direct` | 不使用宿主，直接提交事实与透明像素夹具 | 验证协议，不验证组件 |

## 接入一个用例

1. 在 `cases/` 新增 JSON 元数据。它只记录身份、标题、family 和两端入口，不定义场景参数或事件语言。
2. 在 `Cases.cs` 和 `Cases.kt` 实现两端入口，并分别加入 `Cases.Entries` 和 `Cases.entries`。
   C# 入口接收 `CaptureWriter`、返回 `Task`；Kotlin 入口为接收 `CaptureWriter` 的 suspend 函数。
3. 在入口中构建真实场景、施加输入、推进所需帧并提交 `Sample`。使用宿主时沿用例子的
   `await using` / `use` 生命周期；成功入口至少提交一个样本。
4. 运行所选用例，读取概览与样本详情；用所选用例的 `self-test` 检查重复捕获和额外截图是否改变事实。
   具体返回码见 README。双端一致、采集稳定与覆盖充分是不同结论。

以下是已有按钮用例的元数据；新增时替换 ID、标题与入口名：

```json
{
  "schemaVersion": 2,
  "id": "button.filled.press",
  "title": "Press and release timeline",
  "family": "button",
  "composeEntry": "buttonPressed",
  "avaloniaEntry": "ButtonPressed"
}
```

ID 在用例集合内唯一；样本 ID 在单次捕获内唯一，时间不能倒退。同一时间可以有不同 ID 的样本。
Point ID 在样本内唯一，Field 名在 Point 内唯一。两端按这些身份配对，不按提交数组的下标配对。
Point 对应领域对象，role/parent 不要求两端内部视图树一致；具体语义检查见测量文档。

```powershell
# 替换为新用例 ID；run 会先构建双端。
./tests/VisualParity/parity.ps1 run --case button.filled.press
./tests/VisualParity/parity.ps1 self-test --case button.filled.press

# 修改比较器、协议或读数器时，可运行框架自身测试。
./tests/VisualParity/parity.ps1 test
```

用例直接提交事实，不手写结果 YAML、manifest 或差分。采集与结果生成由现有流程完成。
没有新增测量语义时可以复用读数器；需要新读数时核对双方实际 API，
不能仅凭同名字段就认定定义相同，也不能用输入参数替代运行时结果。

## 执行与采集边界

- Frame 只投递一次动画帧，然后 Draw。AdvanceTo 只推进事件逻辑时间，不投递帧。
- Compose 的 AdvanceTo 同时推进 TestCoroutineScheduler，其 delay 精度为整毫秒；Avalonia 的任意 Task.Delay 和框架计时器未虚拟化。
- Draw 处理布局/渲染而不投递 ComposeAnimation 帧；Snapshot 被动读取已有结果，不 Draw、不 Frame。
- Sample 同时保存 timeUs 和 frameTimeUs。时间相同不保证输入、注册与动画开始顺序相同，trace 保留操作次序。
- Avalonia 用 Skia headless、ManualAnimationFrameClock 和 GetLastRenderedFrame；绘制最多处理 20 轮工作，不是全局异步静止证明。
- ExtraImages 只在作者已定义的非截图采样点增加被动截图，不增加样本、帧或绘制。
- 关闭窗口后等待动画根清理并检查残留任务；用例/动画/清理异常保留为执行错误。

目前读数覆盖布局框、纯色文字样式与行布局、Avalonia 实际字体家族、按钮容器和单次 press 的 ripple，
以及示例动画的 value/velocity/running。Compose 实际字体、文本真实 drawOrigin、glyphInk、完整 hit region
尚未实现；两端行宽原值定义不同，暂不直接比较。具体语义见测量文档。
没有宣称所有控件变体、键盘/焦点、Popup、原生桌面或跨平台已经覆盖。

### 读懂现有按钮时间线

| 操作 | 当前例子的意义 |
| --- | --- |
| mount → frame(-2000) → frame(-1000) → frame(0) | 显式初始化帧；不是“所有组件两帧即可稳定”的保证 |
| initial → move → down | 先记录初始状态，再在逻辑时间 0 施加输入 |
| frame(16000/32000/80000) → sample | 观察按压过程；32000 点只测量 |
| advanceTo(88000) → up | 在两帧之间释放，不额外投递动画帧 |
| frame(96000) → released | 释放之后一次实际帧的结果 |
| frame(300000) → late-300000 | 固定的较晚观察点，名称不宣称动画已经结束 |

每次 Frame 只发送一个帧；从 96000 跳到 300000 不会补齐中间帧。
两端有相同的时间戳仍需核对输入顺序、动画注册、调度器与最后绘制的关系。
若观察输入后但绘制前的状态，当前属性与截图可能属于不同阶段，不能默认截图已反映该输入。
Compose probe 使用普通 holder，不能通过采集回调触发重组。
提交器复制集合，后续报告不会再读取活的控件或布局对象。

## 实现入口

| 文件 | 职责 |
| --- | --- |
| `cases/*.json` | 用例身份、描述、分类和双端入口，不定义场景参数或事件语言 |
| `Cases.cs` / `Cases.kt` | 用例作者的真实场景、输入、采样步骤和共享函数 |
| `Contracts.cs` / `Contracts.kt` | 双端采集协议与事实描述 |
| `AvaloniaCapture.cs` / `Host.kt` | 可选宿主和快照提交器 |
| `Measurements.cs` / `Measurements.kt` | 可选几何、文字与像素读数 |
| `ButtonMeasurements.cs` / `ButtonMeasurements.kt` | 当前按钮例子的固定版本内部读数适配器 |
| `Comparison.cs` | 样本与字段配对、精确数值和像素差分 |
| `ResultFiles.cs` / `ResultYaml.cs` | 公开文件布局、双端公共数据合并、回读及概览 |
| `Report.cs` / `Report*.html` | 从公开文件生成开发者界面 |
| `ReferenceProducer.cs` | 参考进程与内容寻址缓存 |

场景、输入、取样时间及测量内容均由作者决定。框架不自动发现控件属性，也不声明覆盖完整。
按钮读数器的固定版本内部访问和已知限制见 [MEASUREMENTS](MEASUREMENTS.md#几何与按钮例子)。
