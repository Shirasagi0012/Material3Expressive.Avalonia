# 测量语义与文件读取

本系统呈现提交的事实与差异，不推断来源、不修复数据；Raster 的容差接受与原始事实完全相等分开记录。
先读取 `overview.md`，再读取 `samples/<序号>-<ID>.yaml`。完整数据在文件系统中，
运行与目录结构见 [README](README.md)，编写入口和时钟边界见 [AUTHORING](AUTHORING.md)。
本文描述结果 schema 8；版本迁移统一见 README。

## 采样文件

顶层包含 case、sample、status、present、timeUs、frameTimeUs、stage、points、rasters、
rasterCollection 和 trace。序号只用于排序，sample ID 才是身份。

- 相同的时间、stage、角色、父关系和字段描述只写一次；不同时写 `{compose: ..., avalonia: ...}`。
- points 按 ID 建立映射，其下按 geometry/text/properties/motion 分组，再按字段名建立映射。
  两端 category 不同的字段放入 mixed，并显式保留 category 的双端值。
- 字段保留 status、kind、unit、space、definition、source；数值字段的 components 显式命名每个分量。
- 共同读数写 value；不同读数分别写 compose / avalonia。标量 number/string/boolean 使用原生 YAML 类型，
  向量及字符串列表使用数组。精确数字不截断或舍入。equal 的全零 delta 不重复保存；不同数值保留有符号 delta。
- 普通读数表示 measured；无法读取时使用 `{status: unsupported|missing|error, reason: ...}`。
  一侧没有该字段时使用 `{status: absent}`。相同的不可用状态可以合并为 value，但比较状态仍为 notCompared。
- null 的 frameTimeUs 表示没有最后帧证据，null 的 parent 表示根节点；缺失端通过 present、role 或 absent 状态明确区分。
- issue 和 reason 说明无法比较的具体原因；source 描述读数入口，不作为两个字段语义一致的自动证明。
- rasterCollection 保留采集选择；null 表示未记录遗漏意图，不能解释成 notRequested。
- trace 是各端从前一次 Sample 提交之后到当前提交（含）的完整操作片段，保留全局 sequence。
  case.yaml 的 traceTail 保存最后一次提交之后的操作，包括清理；sampleOrder 保存原始提交顺序。
- rasters 按 ID 分组，共同图像描述合并，双端 image 路径分别保留。路径均相对用例目录，
  不是相对 samples 子目录。像素、差分和 ROI 的统计含义不变。

每个采样文件自包含，没有跨文件描述符字典、锚点或字段编号。
case.yaml 的 metadata 保存用例身份、suite/scenario 与实际 config；它们来自运行前冻结的 schema 4 元数据。
同一文件还保存样本索引、采集身份/错误、重复性和清理信息；environment.yaml 保存环境。
case.yaml 的 sampleSummaries 提供全部采样点的分类计数及图像采集选择，不复制字段详情。
manifest.json 验证全部权威文件；overview.md 是派生概览，交互报告由独立应用按需读取这些文件。

## 事实身份与分量

字段由 Sample → Point → Field 定位。配对检查样本逻辑时间、最后帧时间、stage、
点 role/parent，以及字段 category/kind/unit/space/definition/components。Source 可以不同，
但读数器作者仍需确认两端定义相同；相同的 definition 字符串本身不能证明两个 API 等价。

| 数字 kind | 默认分量 / 约定 |
| --- | --- |
| number | value |
| point | x, y；`layout.box.local-size` 明确定义为 width, height |
| rect | x, y, width, height |
| range | start, endExclusive；半开区间 |
| color | r, g, b, a；unit 区分 srgb 浮点与 srgba8 整数 |
| quad | p0.x, p0.y … p3.x, p3.y；布局四角按左上、右上、右下、左下 |
| matrix | m11, m12, m21, m22, m31, m32；行向量仿射变换，平移在 m31/m32 |

其他数值用途应显式给出 components，长度必须与 kind 的数字分量数相同，名称唯一。
例如圆角读数使用 topLeft.x/y、topRight.x/y、bottomRight.x/y、bottomLeft.x/y。
精确差分保留 Float/Double 以及规范像素中的细小差异，不按显示精度取整。

Source 说明实际读数入口。`declared` 表示用例输入；`derived: ...` 表示确定性转换，
应说明转换规则并保留原生读数。样式属性、布局结果、上次绘制缓存与最终像素是不同证据，
不能用其中一种冒充另一种。stage 和样本名称只是作者标签。

## 通用读数与组件专用读数

`layout.box.*` 表示未裁剪布局框，不是像素轮廓或命中范围。
共享几何和文字读数器分别位于 C# `Capture/AvaloniaMeasurements.cs` 与 Kotlin `capture/Measurements.kt`。
Compose 的 onTextLayout 结果可能保留颜色更新前的 style；颜色读数从固定 Core 的
TextAnnotatedStringNode 当前绘制状态取得，按 brush、overrideColor、style.color 的实际绘制分支读取。
使用 brush 绘制、非 sRGB 纯色或不支持的文字节点明确记为 unsupported；固定内部字段变化时报错，不回退到旧布局颜色。
组件特有的形状、绘制和交互读数由用例作者按需实现，框架不提供统一组件属性采集清单。

上次绘制缓存可能与动画当前值处于不同阶段；报告按各自来源陈述数据，不猜测或重算“应当画成什么”。
需要访问固定版本内部成员时，应明确读数来源与适用范围；无法采集时保留错误或不可用原因，
不能通过猜测节点、近似计算或改写组件行为来补齐事实。
当前通用读数没有完整命中区域、Compose 实际字体、真实文本绘制原点和 glyph ink。

## 文字行范围

| 字段 | definition / 精确含义 |
| --- | --- |
| nativeRange | 框架原值；Compose 与 Avalonia 保持不同 definition，不直接比较 |
| range | `text.line.content-range.utf16.no-paragraph-break.v1`：原生范围裁到输入 UTF-16 索引域，再去掉区间末尾的 CR/LF/U+2028/U+2029；保留尾随空格 |
| consumedRange | `text.line.consumed-range.utf16.includes-paragraph-break.v1`：当前行起点到下一行起点，末行到输入末尾；包含段落分隔符 |

这两类规范范围描述不同事实。空行允许空内容范围；代理对仍使用 UTF-16 索引。
范围规范化不会修改原生数据或行起点。文字读数器的框架自测覆盖 CRLF、空行、emoji、
尾随空格、U+2028 和 U+2029，仍保留框架真实行归属差异。
旧 `text.line.source-range` 不被自动重新解释为新定义；跨新旧定义比较会标为 notCompared。
行宽的尾随空白语义仍未统一，原值保留、定义分开。

## 采集选择、未比较原因与统计

可选 Sample.rasterCollection 记录 captured / notRequested / unsupported / missing / error。
captured 必须与实际 rasters 一一对应，其他状态必须附原因。宿主测量-only 样本明确写 notRequested；
ExtraImages 可以将它改为 captured。未记录选择时，遗漏意图标为未知；任意未提交字段也仍为未知。

issue.code 是配对/比较操作的结果，不是组件故障诊断。members 中保留比较协议的 expected/actual 命名，分别对应 YAML 的 compose/avalonia：

| code | members |
| --- | --- |
| measurementAbsent / rasterAbsent | 缺失的 expected 或 actual |
| measurementUnavailable | 非 measured 的 expected.status / actual.status；具体状态在原字段中 |
| descriptorMismatch / rasterDescriptorMismatch | 实际冲突的描述符名字 |
| sampleMismatch | 缺失端或 timeUs/frameTimeUs/stage/captureIdentity/executionError |
| pointMismatch | 缺失端或 role/parentId |

概览按采样点统计各类别的 different / notCompared，图像单独计数；空点/样本另行提示。
重复时间点、box/corners/size 与重叠 ROI 不是独立故障，计数也不是覆盖完整度或严重程度。
“首次观测到差异”只定位已提交样本，不推断两次采样之间的起始时刻。

## 图像与区域

双端采集时将 PNG 规范化为 **sRGB、8-bit、straight alpha**，持久化比较实际使用的像素。
只保存 PNG，比较时按 **straight sRGBA8、行优先、RGBA 通道顺序**解码到内存。
逐通道比较包含 alpha 和透明像素 RGB；diff.png 为最大通道差灰度图。
四通道绝对差可由双端 PNG 重算，不另存 diff.rgba。
前端 Worker 使用 fast-png 直接读取规范 PNG 的字节，保留透明像素 RGB 和半透明通道，
避免浏览器 Canvas 预乘 Alpha 读回。原图直接引用 PNG，结构化比较统计仍以运行时记录为准；
Worker 解码统计与记录不一致时显示错误，不静默重新判断通过。
ROI 是同一测量点双端未裁剪布局框的并集，先按 environment.density 从 DIP 换算到像素，
再向外取整并裁到画布；统计的是 full 合成图，
不代表对象独立贡献，重叠区域不能相加。

4× 采集保持几何、文字行位置、规范圆角和阴影 elevation 的 DIP 语义。Compose 读数器将
原生像素值除以 density；字号、字距的 sp 值及 UTF-16 范围不缩放。Button 默认只提交
双端定义一致的规范圆角；排查时若另行采集原生圆角，必须保留各自单位和定义，不强行配对。
Button 的 `button.container.normalized-visual-rounded-outline` 按可见物理左上、右上、右下、左下
记录规范半径；Avalonia 根据 Surface 到窗口的实际 x 轴反射映射角的身份，Compose 读取已解析
布局方向的绘制轮廓。旧 `button.container.normalized-rounded-outline` 不被自动重新解释。
当前 Button 场景无旋转，布局默认只提交 box/size，不比较框架内部局部轴导致的四角排列差异。
Button 几何内容的 color 来自实际绘制时使用的前景色，转换为 srgba8：Compose 使用 Color.toArgb()，
Avalonia 保留字节 RGB，并将 Color.A × Brush.Opacity 四舍五入为字节 alpha。这是绘制颜色的
8 位表示，不是从截图读取的最终合成像素，也不是输入配置的预期颜色。
原始 PNG、metrics.differenceBounds 和 regions.bounds 始终使用实际像素；不同 density/fontScale
的图像标为 notCompared。报告的不同像素总数不能直接用于比较不同倍率的严重程度。

报告的显示阈值只控制高亮，不改变任何底层数据。

## Raster 容差判定

固定 4× 捕获的 Raster 同时记录两种结果：`exactStatus` 是原始 RGBA 逐字节比较，`metrics`、
`regions` 和差分 PNG 也保持原始 4×；`tolerance` 是派生的 1× 判定。不会覆盖或重新编码原图。

- 过滤算法固定为 `box-4x4-straight-srgba8-half-up`：从画布原点划分 4×4 块，R/G/B/A 各通道
  独立累计原始字节，以 `(sum + 8) / 16` 整数除法四舍五入为 8 位值。直接平均 sRGB 编码数值，
  不转换线性光、不进行配准或额外模糊。alpha 与完全透明像素的 RGB 也参与；这不是背景合成或感知距离。
- 每个 1× 像素的误差为四个通道绝对差的最大值；区域均值包含区域内所有像素，包括零误差。
  整图与每个非空配对点区域均须同时满足 **最大值 ≤20、均值 ≤0.2**，单位为 0～255，边界包含等号。
- `tolerance.full` 覆盖整图；`tolerance.regions` 使用现有双端布局框并集，原始像素边界除以 4 后向外取整。
  bounds 均为 1× 比较像素坐标，空区域不参与判定，多个区域分别判定而不累加。没有有效区域时只检查整图。
  布局区域包含该处完整合成像素，不是单个控件的独立绘制。布局框外的阴影由整图检查覆盖，
  尚无自动推导的阴影专用区域，其平均误差可能受背景面积稀释；需要局部约束时应提交相应测量区域。
- `tolerance` 保存算法、倍率、两个阈值及各区域的最大值、均值和 accepted。只对 density=4 且宽高
  为 4 的整数倍的图像应用；其他可配对输入保留精确判定且没有 tolerance，不能静默按其他比例缩放。

Raster `status`：原始像素相等为 `equal`；原始不同但容差接受为 `withinTolerance`；超出容差为
`different`；无法配对为 `notCompared`。样本/用例汇总优先级为 different → notCompared →
withinTolerance → equal，执行错误优先为 error。coverage 单独统计 withinTolerance，不计入 equal。
结构化字段和重复性自检继续精确比较；`--require-match` 接受容差内结果，`--require-equal` 保持严格。

20 / 0.2 是本轮试用阈值，不是已校准的感知一致标准；状态不自动证明差异来自抗锯齿。

## 用文件工具读取

基本文件读取与 rg 示例见 [README](README.md#输出与阅读)。以下展示可选的结构化分析。

Python 示例（需要 PyYAML；它只是可选的读取工具，不是运行框架的依赖）：

```python
from pathlib import Path
import yaml

case = Path('artifacts/visual-parity/runs/<run>/cases/<case-id>')
for path in sorted((case / 'samples').glob('*.yaml')):
    sample = yaml.safe_load(path.read_text(encoding='utf-8'))
    field = sample['points']['container']['motion']['cornerRadii']
    if 'value' in field:
        compose = avalonia = field['value']
    else:
        compose, avalonia = field['compose'], field['avalonia']
    print(sample['sample'], sample['timeUs'], field['status'], compose, avalonia)
```

按需读取某个像素（以下使用 Pillow；项目生成的 sRGB PNG 可直接读取，外部带色彩配置的 PNG 应使用一致的 sRGB 转换）：

```python
from PIL import Image

sample = yaml.safe_load((case / 'samples/0000-initial.yaml').read_text(encoding='utf-8'))
raster = sample['rasters']['full']
def side(value, name):
    return value[name] if isinstance(value, dict) and 'compose' in value else value

x, y = 120, 80
width = side(raster['width'], 'compose')
height = side(raster['height'], 'compose')
assert 0 <= x < width and 0 <= y < height
with Image.open(case / side(raster['image'], 'compose')) as image:
    print(image.convert('RGBA').getpixel((x, y)))
```

目录支持批量读取、rg 上下文搜索和自行编程分析；不需要为字段历史、区域、像素分别调用接口。
任意区域的直方图、行列统计可以从 PNG 解码后的字节计算。
报告保留全图及 point ROI 的不同像素数、通道 max/mean 和差异 AABB。
同一字段跨样本分析前仍应检查语义、单位和点身份是否发生变化；首次观测到差异不等于实际起始时刻。
