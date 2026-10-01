# 测量语义与文件读取

本系统呈现提交的事实与差异，不推断来源、不修复数据、不用容差产生等价结论。
先读取 `overview.md`，再读取 `samples/<序号>-<ID>.yaml`。完整数据在文件系统中，
运行与目录结构见 [README](README.md)，编写入口和时钟边界见 [AUTHORING](AUTHORING.md)。
本文描述结果 schema 5；版本迁移统一见 README。

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
- rasters 按 ID 分组，共同图像描述合并，双端 image/pixels 路径分别保留。路径均相对用例目录，
  不是相对 samples 子目录。像素、差分和 ROI 的统计含义不变。

每个采样文件自包含，没有跨文件描述符字典、锚点或字段编号。
case.yaml 的 metadata 保存用例身份、suite/scenario 与实际 config；它们来自运行前冻结的 schema 4 元数据。
同一文件还保存样本索引、采集身份/错误、重复性和清理信息；environment.yaml 保存环境。
manifest.json 验证全部权威文件；overview.md 和 report.html 可以从它们重新生成。

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

## 几何与按钮例子

`layout.box.*` 表示未裁剪布局框，不是像素轮廓或命中范围。
Button 例子分别提交 button/allocation、container/container、label/text-block 和 interaction/interaction。
container 的父点是 button；label 与 interaction 的父点是 container。

按钮读数器属于例子代码，不是框架统一组件采集接口。它读取固定版本的内部成员，缺少必需成员时
保留执行错误，不通过搜索第一个相似对象或经验公式回退：

- Compose：读取 BackgroundElement 的坐标、BackgroundNode.color 和上次 draw 的 lastOutline；
  不调用 Shape.createOutline，不替换 Shape 或 indication。通过 hoisted MutableInteractionSource 的事件集合记录 press/hover。
- Ripple：按固定 Core 的 `material3/material3-ripple` 路径读取启用 press 的 CommonRippleNode，
  读取 animatedAlpha 和实际 ripple Animatable。半径、中心按固定 draw 表达式，从真实存储的端点和当前 fraction 推导。
- Avalonia：读取具名 PART_Container、内部 chrome 和现有交互控制器；圆角原值保留为 nativeCornerRadii。
  无边框的圆角经当前 Avalonia Skia `ToSKRoundRect` 的原生归一化后提交 cornerRadii；有边框的复杂路径明确 unsupported。
- 单次 press 例子最多观察一个 ripple，ripple.0 属于这一事件。多按压用例必须由作者提供稳定事件身份。

上次绘制缓存可能与动画当前值处于不同阶段；报告按各自来源陈述数据，不猜测或重算“应当画成什么”。
当前没有完整命中区域、Compose 实际字体、真实文本绘制原点和 glyph ink 读数。

## 文字行范围

| 字段 | definition / 精确含义 |
| --- | --- |
| nativeRange | 框架原值；Compose 与 Avalonia 保持不同 definition，不直接比较 |
| range | `text.line.content-range.utf16.no-paragraph-break.v1`：原生范围裁到输入 UTF-16 索引域，再去掉区间末尾的 CR/LF/U+2028/U+2029；保留尾随空格 |
| consumedRange | `text.line.consumed-range.utf16.includes-paragraph-break.v1`：当前行起点到下一行起点，末行到输入末尾；包含段落分隔符 |

这两类规范范围描述不同事实。空行允许空内容范围；代理对仍使用 UTF-16 索引。
范围规范化不会修改原生数据或行起点。`text.paragraph-breaks` 例子覆盖 CRLF、空行、emoji、
尾随空格、U+2028 和 U+2029，仍保留框架真实行归属差异。
旧 `text.line.source-range` 不被自动重新解释为新定义；跨新旧定义比较会标为 notCompared。
行宽的尾随空白语义仍未统一，原值保留、定义分开。

## 采集选择、未比较原因与统计

可选 Sample.rasterCollection 记录 captured / notRequested / unsupported / missing / error。
captured 必须与实际 rasters 一一对应，其他状态必须附原因。宿主测量-only 样本明确写 notRequested；
ExtraImages 可以将它改为 captured。老包缺少选择记录时，遗漏意图标为未知；任意未提交字段也仍为未知。

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

保留原始 PNG，以及从 PNG 直接解码的 **straight sRGBA8、行优先、RGBA 通道顺序**字节。
逐通道比较包含 alpha 和透明像素 RGB；diff.rgba 为绝对通道差，diff.png 为最大通道差灰度图。
规范 RGBA 允许不依赖任意图片库的预乘/色彩转换直接检查像素。
ROI 是同一测量点双端未裁剪布局框的并集，向外取整并裁到画布；统计的是 full 合成图，
不代表对象独立贡献，重叠区域不能相加。

HTML 的显示阈值只控制高亮，不改变任何底层数据。

## 用文件工具读取

基本文件读取与 rg 示例见 [README](README.md#输出与阅读)。以下展示可选的结构化分析。

Python 示例（需要 PyYAML；它只是可选的读取工具，不是运行框架的依赖）：

```python
from pathlib import Path
import yaml

case = Path('artifacts/visual-parity/<run>/cases/button.filled.press')
for path in sorted((case / 'samples').glob('*.yaml')):
    sample = yaml.safe_load(path.read_text(encoding='utf-8'))
    field = sample['points']['container']['motion']['cornerRadii']
    if 'value' in field:
        compose = avalonia = field['value']
    else:
        compose, avalonia = field['compose'], field['avalonia']
    print(sample['sample'], sample['timeUs'], field['status'], compose, avalonia)
```

按需读取某个像素无需图片解码库：

```python
sample = yaml.safe_load((case / 'samples/0000-initial.yaml').read_text(encoding='utf-8'))
raster = sample['rasters']['full']
def side(value, name):
    return value[name] if isinstance(value, dict) and 'compose' in value else value

x, y = 120, 80
width = side(raster['width'], 'compose')
height = side(raster['height'], 'compose')
assert 0 <= x < width and 0 <= y < height
pixels = (case / side(raster['pixels'], 'compose')).read_bytes()
offset = (y * width + x) * 4
print(list(pixels[offset:offset + 4]))  # straight sRGBA8
```

目录支持批量读取、rg 上下文搜索和自行编程分析；不需要为字段历史、区域、像素分别调用接口。
任意区域的直方图、行列统计可以直接从规范字节计算。
报告保留全图及 point ROI 的不同像素数、通道 max/mean 和差异 AABB。
同一字段跨样本分析前仍应检查语义、单位和点身份是否发生变化；首次观测到差异不等于实际起始时刻。
