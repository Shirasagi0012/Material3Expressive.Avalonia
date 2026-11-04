# 组件全景索引

本文为系统全部 51 个组件的全局索引全景图。
每个组件目录严格遵循双文件架构：`README.md` 面向外部业务开发者，提供使用示例与参数说明；`INTERNALS.md` 面向框架维护者与 AI Agent，提供上游基线映射、核心机制与已知差异。通用规则见 [组件文档与维护规约](COMPONENT_DOCS.md)。

---

## 概览统计

- **已纳管组件总数**：51 个

---

## 1. 按钮与操作

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `Button` | 原生按钮的五种 Material 主题与尺寸变体 | [README](../src/Material3Expressive/Components/Button/README.md) | [INTERNALS](../src/Material3Expressive/Components/Button/INTERNALS.md) | |
| `IconButton` | 图标按钮，提供标准、填充、浅色与轮廓样式 | [README](../src/Material3Expressive/Components/IconButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/IconButton/INTERNALS.md) | |
| `ToggleButton` | 原生切换按钮的四种 Material 主题与选中状态形变 | [README](../src/Material3Expressive/Components/ToggleButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/ToggleButton/INTERNALS.md) | |
| `SegmentedButton` | 单选与多选分段按钮，支持首尾平滑圆角 | [README](../src/Material3Expressive/Components/SegmentedButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/SegmentedButton/INTERNALS.md) | |
| `SplitButton` | 双操作按钮组合，主操作与次级菜单联动 | [README](../src/Material3Expressive/Components/SplitButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/SplitButton/INTERNALS.md) | |
| `ButtonGroup` | 按压扩宽、相邻项压缩与自动溢出收纳 | [README](../src/Material3Expressive/Components/ButtonGroup/README.md) | [INTERNALS](../src/Material3Expressive/Components/ButtonGroup/INTERNALS.md) | |
| `Chip` | 辅助、建议、过滤与输入四类操作标签 | [README](../src/Material3Expressive/Components/Chip/README.md) | [INTERNALS](../src/Material3Expressive/Components/Chip/INTERNALS.md) | |
| `FloatingActionButton` | 浮动操作按钮、扩展按钮、切换按钮与菜单 | [README](../src/Material3Expressive/Components/FloatingActionButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/FloatingActionButton/INTERNALS.md) | |
| `CommandBar` | 原生命令栏的三套紧凑操作主题 | [README](../src/Material3Expressive/Components/CommandBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/CommandBar/INTERNALS.md) | |
| `FloatingToolbar` | 横向与纵向浮动工具栏，支持两轴留白平衡 | [README](../src/Material3Expressive/Components/FloatingToolbar/README.md) | [INTERNALS](../src/Material3Expressive/Components/FloatingToolbar/INTERNALS.md) | |

---

## 2. 输入与选择

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `TextField` | 原生文本输入框的填充与轮廓样式、浮动标签及图标槽 | [README](../src/Material3Expressive/Components/TextField/README.md) | [INTERNALS](../src/Material3Expressive/Components/TextField/INTERNALS.md) | |
| `SearchBar` | 收起搜索表面、独立停靠弹层、原位搜索容器与搜索输入 | [README](../src/Material3Expressive/Components/SearchBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/SearchBar/INTERNALS.md) | SearchBarState / ExpandedDockedSearchBar 已接入；输入仍为布尔重载；全屏、间隙及 AppBar 变体未实现 |
| `SecureTextField` | 密码输入限制、显示切换与安全边界防护 | [README](../src/Material3Expressive/Components/SecureTextField/README.md) | [INTERNALS](../src/Material3Expressive/Components/SecureTextField/INTERNALS.md) | |
| `NumericUpDown` | 数字增减微调框与内置步进按钮 | [README](../src/Material3Expressive/Components/NumericUpDown/README.md) | - | |
| `ComboBox` | 原生下拉单选框字段装饰与单组下拉菜单 | [README](../src/Material3Expressive/Components/ComboBox/README.md) | [INTERNALS](../src/Material3Expressive/Components/ComboBox/INTERNALS.md) | |
| `CheckBox` | 原生复选框的两态与三态 Material 主题 | [README](../src/Material3Expressive/Components/CheckBox/README.md) | [INTERNALS](../src/Material3Expressive/Components/CheckBox/INTERNALS.md) | |
| `RadioButton` | 原生单选按钮与组互斥交互 | [README](../src/Material3Expressive/Components/RadioButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/RadioButton/INTERNALS.md) | |
| `Switch` | 原生滑动开关与滑块图标动画 | [README](../src/Material3Expressive/Components/Switch/README.md) | [INTERNALS](../src/Material3Expressive/Components/Switch/INTERNALS.md) | |
| `Slider` | 原生滑块与连续数值选择 | [README](../src/Material3Expressive/Components/Slider/README.md) | [INTERNALS](../src/Material3Expressive/Components/Slider/INTERNALS.md) | |
| `DatePicker` | 月历选择、日期文本输入与日期范围选择 | [README](../src/Material3Expressive/Components/DatePicker/README.md) | [INTERNALS](../src/Material3Expressive/Components/DatePicker/INTERNALS.md) | |
| `TimePicker` | 钟面指针交互、时间文本输入与滚动时间选择 | [README](../src/Material3Expressive/Components/TimePicker/README.md) | [INTERNALS](../src/Material3Expressive/Components/TimePicker/INTERNALS.md) | |
| `DateTimePicker` | 原生日期/时间选择器的 Material 外观主题 | [README](../src/Material3Expressive/Components/DateTimePicker/README.md) | - | |

---

## 3. 导航与栏目

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `AppBar` | 顶部与底部应用栏，支持滚动视口折叠联动 | [README](../src/Material3Expressive/Components/AppBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/AppBar/INTERNALS.md) | |
| `NavigationBar` | 标准底部导航栏与指示胶囊动画 | [README](../src/Material3Expressive/Components/NavigationBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/NavigationBar/INTERNALS.md) | |
| `ShortNavigationBar` | 紧凑型底部导航栏，适合小屏视口 | [README](../src/Material3Expressive/Components/ShortNavigationBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/ShortNavigationBar/INTERNALS.md) | |
| `NavigationRail` | 侧边导航轨与指示器动效 | [README](../src/Material3Expressive/Components/NavigationRail/README.md) | [INTERNALS](../src/Material3Expressive/Components/NavigationRail/INTERNALS.md) | |
| `WideNavigationRail` | 宽导航轨与模态展开导航 | [README](../src/Material3Expressive/Components/WideNavigationRail/README.md) | [INTERNALS](../src/Material3Expressive/Components/WideNavigationRail/INTERNALS.md) | |
| `NavigationDrawer` | 固定式、关闭式与模态抽屉导航 | [README](../src/Material3Expressive/Components/NavigationDrawer/README.md) | [INTERNALS](../src/Material3Expressive/Components/NavigationDrawer/INTERNALS.md) | |
| `TabRow` | 独立标签栏容器及原生选项卡主题 | [README](../src/Material3Expressive/Components/TabRow/README.md) | [INTERNALS](../src/Material3Expressive/Components/TabRow/INTERNALS.md) | |

---

## 4. 容器与布局

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `Surface` | 基础表面容器，承载背景、阴影、多角圆角与描边 | [README](../src/Material3Expressive/Components/Surface/README.md) | [INTERNALS](../src/Material3Expressive/Components/Surface/INTERNALS.md) | |
| `Card` | 填充、浅色、抬升与描边卡片容器 | [README](../src/Material3Expressive/Components/Card/README.md) | [INTERNALS](../src/Material3Expressive/Components/Card/INTERNALS.md) | |
| `Scaffold` | 屏幕标准槽位骨架，自动处理窗口插桩与内边距传播 | [README](../src/Material3Expressive/Components/Scaffold/README.md) | [INTERNALS](../src/Material3Expressive/Components/Scaffold/INTERNALS.md) | |
| `ListItem` | 标准五槽列表行、分段列表行与受控选择状态 | [README](../src/Material3Expressive/Components/ListItem/README.md) | [INTERNALS](../src/Material3Expressive/Components/ListItem/INTERNALS.md) | |
| `DragHandle` | 三状态竖向拖动手柄与共享交互反馈 | [README](../src/Material3Expressive/Components/DragHandle/README.md) | [INTERNALS](../src/Material3Expressive/Components/DragHandle/INTERNALS.md) | |
| `SwipeToDismissBox` | 双向滑动消除、背景槽与可控状态 | [README](../src/Material3Expressive/Components/SwipeToDismissBox/README.md) | [INTERNALS](../src/Material3Expressive/Components/SwipeToDismissBox/INTERNALS.md) | |
| `GroupBox` | 分组框容器，以内嵌标题与描边表面呈现 | [README](../src/Material3Expressive/Components/GroupBox/README.md) | [INTERNALS](../src/Material3Expressive/Components/GroupBox/INTERNALS.md) | |
| `Expander` | 原生折叠展开框，列表风格与动画显隐过渡 | [README](../src/Material3Expressive/Components/Expander/README.md) | [INTERNALS](../src/Material3Expressive/Components/Expander/INTERNALS.md) | |
| `Divider` | 原生分隔条的水平与垂直主题 | [README](../src/Material3Expressive/Components/Divider/README.md) | [INTERNALS](../src/Material3Expressive/Components/Divider/INTERNALS.md) | |

---

## 5. 提示、反馈与弹层

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `Menu` | 独立下拉菜单、级联子菜单与原生弹层主题 | [README](../src/Material3Expressive/Components/Menu/README.md) | [INTERNALS](../src/Material3Expressive/Components/Menu/INTERNALS.md) | |
| `AlertDialog` | 标准确认对话框与自定义内容弹窗 | [README](../src/Material3Expressive/Components/AlertDialog/README.md) | [INTERNALS](../src/Material3Expressive/Components/AlertDialog/INTERNALS.md) | |
| `DatePickerDialog` | 日期选择器专用对话框容器 | [README](../src/Material3Expressive/Components/DatePickerDialog/README.md) | [INTERNALS](../src/Material3Expressive/Components/DatePickerDialog/INTERNALS.md) | |
| `TimePickerDialog` | 时间选择器专用对话框容器 | [README](../src/Material3Expressive/Components/TimePickerDialog/README.md) | [INTERNALS](../src/Material3Expressive/Components/TimePickerDialog/INTERNALS.md) | |
| `Tooltip` | 简单与富内容提示、悬浮提示框与交互标签 TooltipLabel | [README](../src/Material3Expressive/Components/Tooltip/README.md) | [INTERNALS](../src/Material3Expressive/Components/Tooltip/INTERNALS.md) | |
| `Snackbar` | 底部短消息与全局消息队列宿主 | [README](../src/Material3Expressive/Components/Snackbar/README.md) | [INTERNALS](../src/Material3Expressive/Components/Snackbar/INTERNALS.md) | |
| `Badge` | 数字与圆点徽标，配合徽标容器定位 | [README](../src/Material3Expressive/Components/Badge/README.md) | [INTERNALS](../src/Material3Expressive/Components/Badge/INTERNALS.md) | |
| `ProgressIndicator` | 线性与圆形进度条，支持直线与表现型波浪动画 | [README](../src/Material3Expressive/Components/ProgressIndicator/README.md) | [INTERNALS](../src/Material3Expressive/Components/ProgressIndicator/INTERNALS.md) | |
| `LoadingIndicator` | 动态多边形形状变换加载指示器 | [README](../src/Material3Expressive/Components/LoadingIndicator/README.md) | [INTERNALS](../src/Material3Expressive/Components/LoadingIndicator/INTERNALS.md) | |

---

## 6. 滚动与视口

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `ScrollViewer` | 原生滚动容器的平滑物理滚动与惯性接入 | [README](../src/Material3Expressive/Components/ScrollViewer/README.md) | [INTERNALS](../src/Material3Expressive/Components/ScrollViewer/INTERNALS.md) | |
| `PullToRefresh` | 下拉刷新状态、传统/Expressive 指示器与显式滚动消费策略 | [README](../src/Material3Expressive/Components/PullToRefresh/README.md) | [INTERNALS](../src/Material3Expressive/Components/PullToRefresh/INTERNALS.md) | 自动 ScrollViewer 接入尚未实现；回弹途中卸载的末帧保留值存在已记录差异 |
| `ScrollBar` | 原生滚动条的 Material 外观与交互动效 | [README](../src/Material3Expressive/Components/ScrollBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/ScrollBar/INTERNALS.md) | |

---

## 7. 基础控件与基元

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `Primitives` | 原生基础控件（标签、窗口、网格分割条、表格单元格等）的 Material 默认样式 | [README](../src/Material3Expressive/Components/Primitives/README.md) | [INTERNALS](../src/Material3Expressive/Components/Primitives/INTERNALS.md) | |
