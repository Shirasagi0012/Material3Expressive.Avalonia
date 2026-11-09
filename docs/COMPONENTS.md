# 组件全景索引

本文为系统全部 58 个组件的全局索引全景图。
每个组件目录严格遵循双文件架构：`README.md` 面向外部业务开发者，提供使用示例与参数说明；`INTERNALS.md` 面向框架维护者与 AI Agent，提供上游基线映射、核心机制与已知差异。通用规则见 [组件文档与维护规约](COMPONENT_DOCS.md)。

---

## 概览统计

- **已纳管组件总数**：58 个

---

## 1. 按钮与操作

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `Button` | 原生按钮五种主题、精确指针尺寸及圆角/切角按压形变 | [README](../src/Material3Expressive/Components/Button/README.md) | [INTERNALS](../src/Material3Expressive/Components/Button/INTERNALS.md) | |
| `IconButton` | 图标按钮六种主题及四种切换主题、自定义内容和三状态形变 | [README](../src/Material3Expressive/Components/IconButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/IconButton/INTERNALS.md) | |
| `Icon` | 位图、矢量与自定义图片图标，内容色着色、Fit 缩放和可选 RTL 镜像 | [README](../src/Material3Expressive/Components/Icon/README.md) | [INTERNALS](../src/Material3Expressive/Components/Icon/INTERNALS.md) | |
| `ToggleButton` | 原生切换按钮四种主题、精确指针尺寸及选中/按压切角形变 | [README](../src/Material3Expressive/Components/ToggleButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/ToggleButton/INTERNALS.md) | |
| `SegmentedButton` | 单选与多选分段按钮、首尾圆角及显式切角/路径形状 | [README](../src/Material3Expressive/Components/SegmentedButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/SegmentedButton/INTERNALS.md) | |
| `SplitButton` | 双操作按钮组合、主副独立路径及按压/选中圆角、切角与混合形变 | [README](../src/Material3Expressive/Components/SplitButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/SplitButton/INTERNALS.md) | |
| `ButtonGroup` | 任意水平排列、按压扩宽、相邻项压缩与自动溢出收纳 | [README](../src/Material3Expressive/Components/ButtonGroup/README.md) | [INTERNALS](../src/Material3Expressive/Components/ButtonGroup/INTERNALS.md) | |
| `Chip` | 辅助、建议、过滤与输入标签，含 Elevated、路径状态切换、切角/混合形变及逻辑/绝对/自定义水平排列 | [README](../src/Material3Expressive/Components/Chip/README.md) | [INTERNALS](../src/Material3Expressive/Components/Chip/INTERNALS.md) | |
| `FloatingActionButton` | 浮动操作按钮、可定制路径的扩展按钮、切换按钮与菜单 | [README](../src/Material3Expressive/Components/FloatingActionButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/FloatingActionButton/INTERNALS.md) | |
| `CommandBar` | 原生命令栏的三套紧凑操作主题 | [README](../src/Material3Expressive/Components/CommandBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/CommandBar/INTERNALS.md) | |
| `FloatingToolbar` | 横向与纵向浮动工具栏、独立容器/FAB 形状及两轴留白平衡 | [README](../src/Material3Expressive/Components/FloatingToolbar/README.md) | [INTERNALS](../src/Material3Expressive/Components/FloatingToolbar/INTERNALS.md) | |

---

## 2. 输入与选择

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `TextField` | 原生文本输入框的填充与轮廓样式、浮动标签、图标槽与容器路径形状 | [README](../src/Material3Expressive/Components/TextField/README.md) | [INTERNALS](../src/Material3Expressive/Components/TextField/INTERNALS.md) | |
| `SearchBar` | 搜索顶栏、收起表面、停靠/带间隙弹层、普通/contained 全屏搜索、可定制路径的容器与输入 | [README](../src/Material3Expressive/Components/SearchBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/SearchBar/INTERNALS.md) | 含 gap 默认状态工厂与顶栏显式滚动策略；输入含状态式与布尔入口；自动 ScrollViewer 接入未实现 |
| `SecureTextField` | 密码输入限制、显示切换与安全边界防护 | [README](../src/Material3Expressive/Components/SecureTextField/README.md) | [INTERNALS](../src/Material3Expressive/Components/SecureTextField/INTERNALS.md) | |
| `NumericUpDown` | 数字增减微调框与内置步进按钮 | [README](../src/Material3Expressive/Components/NumericUpDown/README.md) | - | |
| `ExposedDropdownMenu` | 独立字段下拉菜单，支持编辑、只读、锚点约束与开闭动画 | [README](../src/Material3Expressive/Components/ExposedDropdownMenu/README.md) | [INTERNALS](../src/Material3Expressive/Components/ExposedDropdownMenu/INTERNALS.md) | |
| `ComboBox` | 原生下拉单选框字段装饰与单组下拉菜单 | [README](../src/Material3Expressive/Components/ComboBox/README.md) | [INTERNALS](../src/Material3Expressive/Components/ComboBox/INTERNALS.md) | |
| `CheckBox` | 原生复选框主题与非交互两态/三态展示 | [README](../src/Material3Expressive/Components/CheckBox/README.md) | [INTERNALS](../src/Material3Expressive/Components/CheckBox/INTERNALS.md) | |
| `RadioButton` | 原生单选按钮、组互斥与非交互状态展示 | [README](../src/Material3Expressive/Components/RadioButton/README.md) | [INTERNALS](../src/Material3Expressive/Components/RadioButton/INTERNALS.md) | |
| `Switch` | 原生滑动开关、非交互展示与滑块图标动画 | [README](../src/Material3Expressive/Components/Switch/README.md) | [INTERNALS](../src/Material3Expressive/Components/Switch/INTERNALS.md) | |
| `Slider` | 原生滑块、共享交互源、Label 组合及焦点环 | [README](../src/Material3Expressive/Components/Slider/README.md) | [INTERNALS](../src/Material3Expressive/Components/Slider/INTERNALS.md) | |
| `ScrollField` | 循环选择、虚拟页、触摸/滚轮吸附与可复用数字动画 | [README](../src/Material3Expressive/Components/ScrollField/README.md) | [INTERNALS](../src/Material3Expressive/Components/ScrollField/INTERNALS.md) | 设备专用触控板/滚轮元数据及嵌套滚动尚未实现 |
| `DatePicker` | 月份滚动与吸附、键盘导航、日期/范围输入及自定义日期 formatter | [README](../src/Material3Expressive/Components/DatePicker/README.md) | [INTERNALS](../src/Material3Expressive/Components/DatePicker/INTERNALS.md) | |
| `TimePicker` | 钟面指针交互、时间文本输入；TimeScroll 分页拖动、惯性吸附、循环与键盘 | [README](../src/Material3Expressive/Components/TimePicker/README.md) | [INTERNALS](../src/Material3Expressive/Components/TimePicker/INTERNALS.md) | |
| `DateTimePicker` | 原生日期/时间选择器的 Material 外观主题 | [README](../src/Material3Expressive/Components/DateTimePicker/README.md) | - | |

---

## 3. 导航与栏目

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `AppBar` | 顶部与底部应用栏，支持滚动视口折叠联动及自定义动作排列 | [README](../src/Material3Expressive/Components/AppBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/AppBar/INTERNALS.md) | |
| `NavigationBar` | 标准底部导航栏与指示胶囊动画 | [README](../src/Material3Expressive/Components/NavigationBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/NavigationBar/INTERNALS.md) | |
| `ShortNavigationBar` | 紧凑型底部导航栏与嵌套 Insets 消费，适合小屏视口 | [README](../src/Material3Expressive/Components/ShortNavigationBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/ShortNavigationBar/INTERNALS.md) | |
| `NavigationRail` | 侧边导航轨、指示器动效与嵌套 Insets 消费 | [README](../src/Material3Expressive/Components/NavigationRail/README.md) | [INTERNALS](../src/Material3Expressive/Components/NavigationRail/INTERNALS.md) | |
| `WideNavigationRail` | 宽导航轨、模态展开、自定义纵向排列、普通宽栏 Insets 消费及独立容器形状 | [README](../src/Material3Expressive/Components/WideNavigationRail/README.md) | [INTERNALS](../src/Material3Expressive/Components/WideNavigationRail/INTERNALS.md) | |
| `NavigationDrawer` | 固定式、关闭式与模态抽屉导航及 Sheet/项目路径形状 | [README](../src/Material3Expressive/Components/NavigationDrawer/README.md) | [INTERNALS](../src/Material3Expressive/Components/NavigationDrawer/INTERNALS.md) | |
| `TabRow` | 独立标签栏容器、原生选项卡主题及独立指示器宽度策略 | [README](../src/Material3Expressive/Components/TabRow/README.md) | [INTERNALS](../src/Material3Expressive/Components/TabRow/INTERNALS.md) | |

---

## 4. 容器与布局

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `Surface` | 基础表面容器，承载背景、阴影、圆角/切角/自定义轮廓与描边 | [README](../src/Material3Expressive/Components/Surface/README.md) | [INTERNALS](../src/Material3Expressive/Components/Surface/INTERNALS.md) | |
| `Card` | 填充、抬升与描边卡片，支持切角及自定义轮廓 | [README](../src/Material3Expressive/Components/Card/README.md) | [INTERNALS](../src/Material3Expressive/Components/Card/INTERNALS.md) | |
| `Scaffold` | 屏幕标准槽位骨架，自动处理窗口插桩与内边距传播 | [README](../src/Material3Expressive/Components/Scaffold/README.md) | [INTERNALS](../src/Material3Expressive/Components/Scaffold/INTERNALS.md) | |
| `ListItem` | 标准五槽列表行、分段列表行、六状态圆角/切角形变及受控选择 | [README](../src/Material3Expressive/Components/ListItem/README.md) | [INTERNALS](../src/Material3Expressive/Components/ListItem/INTERNALS.md) | |
| `DragHandle` | 三状态竖向拖动手柄、切角/自定义轮廓与共享交互反馈 | [README](../src/Material3Expressive/Components/DragHandle/README.md) | [INTERNALS](../src/Material3Expressive/Components/DragHandle/INTERNALS.md) | |
| `SwipeToDismissBox` | 双向滑动消除、背景槽与可控状态 | [README](../src/Material3Expressive/Components/SwipeToDismissBox/README.md) | [INTERNALS](../src/Material3Expressive/Components/SwipeToDismissBox/INTERNALS.md) | |
| `GroupBox` | 分组框容器，以内嵌标题与描边表面呈现 | [README](../src/Material3Expressive/Components/GroupBox/README.md) | [INTERNALS](../src/Material3Expressive/Components/GroupBox/INTERNALS.md) | |
| `Expander` | 原生折叠展开框，列表风格与动画显隐过渡 | [README](../src/Material3Expressive/Components/Expander/README.md) | [INTERNALS](../src/Material3Expressive/Components/Expander/INTERNALS.md) | |
| `Divider` | 原生分隔条的水平与垂直主题 | [README](../src/Material3Expressive/Components/Divider/README.md) | [INTERNALS](../src/Material3Expressive/Components/Divider/INTERNALS.md) | |

---

## 5. 提示、反馈与弹层

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `Menu` | 独立/原生菜单项的任意排列与选中路径形变、容器/分组形状、尾随标签、精确指针项布局及级联子菜单 | [README](../src/Material3Expressive/Components/Menu/README.md) | [INTERNALS](../src/Material3Expressive/Components/Menu/INTERNALS.md) | |
| `AlertDialog` | 标准确认对话框、显式形状、精确指针尺寸与自定义内容弹窗 | [README](../src/Material3Expressive/Components/AlertDialog/README.md) | [INTERNALS](../src/Material3Expressive/Components/AlertDialog/INTERNALS.md) | |
| `BottomSheet` | 树内三态面板、常驻脚手架、模态面板、显式路径形状及命名无障碍动作 | [README](../src/Material3Expressive/Components/BottomSheet/README.md) | [INTERNALS](../src/Material3Expressive/Components/BottomSheet/INTERNALS.md) | |
| `Scrim` | 可点击或装饰性遮罩 | [README](../src/Material3Expressive/Components/Scrim/README.md) | [INTERNALS](../src/Material3Expressive/Components/Scrim/INTERNALS.md) | |
| `DatePickerDialog` | 日期选择器专用对话框容器与显式形状 | [README](../src/Material3Expressive/Components/DatePickerDialog/README.md) | [INTERNALS](../src/Material3Expressive/Components/DatePickerDialog/INTERNALS.md) | |
| `TimePickerDialog` | 普通与 Vibrant 时间对话框、显式形状、默认标题及两种模式提示按钮 | [README](../src/Material3Expressive/Components/TimePickerDialog/README.md) | [INTERNALS](../src/Material3Expressive/Components/TimePickerDialog/INTERNALS.md) | |
| `Tooltip` | 默认/自定义箭头的简单与富内容提示、提示框与交互标签 | [README](../src/Material3Expressive/Components/Tooltip/README.md) | [INTERNALS](../src/Material3Expressive/Components/Tooltip/INTERNALS.md) | |
| `Snackbar` | 可定制路径轮廓的底部短消息与全局消息队列宿主 | [README](../src/Material3Expressive/Components/Snackbar/README.md) | [INTERNALS](../src/Material3Expressive/Components/Snackbar/INTERNALS.md) | |
| `Badge` | 数字与圆点徽标，配合徽标容器定位 | [README](../src/Material3Expressive/Components/Badge/README.md) | [INTERNALS](../src/Material3Expressive/Components/Badge/INTERNALS.md) | |
| `ProgressIndicator` | 线性与圆形进度条，支持直线与表现型波浪动画 | [README](../src/Material3Expressive/Components/ProgressIndicator/README.md) | [INTERNALS](../src/Material3Expressive/Components/ProgressIndicator/INTERNALS.md) | |
| `LoadingIndicator` | 动态多边形形状变换加载指示器 | [README](../src/Material3Expressive/Components/LoadingIndicator/README.md) | [INTERNALS](../src/Material3Expressive/Components/LoadingIndicator/INTERNALS.md) | |

---

## 6. 滚动与视口

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `ScrollViewer` | 原生滚动容器的平滑物理滚动与惯性接入 | [README](../src/Material3Expressive/Components/ScrollViewer/README.md) | [INTERNALS](../src/Material3Expressive/Components/ScrollViewer/INTERNALS.md) | |
| `ScrollView` | 面向照片与自由画布的平滑缩放、漫游与悬浮滚动条主题接入 | [README](../src/Material3Expressive/Components/ScrollView/README.md) | [INTERNALS](../src/Material3Expressive/Components/ScrollView/INTERNALS.md) | |
| `Carousel` | 三种经典 keyline 布局、视口虚拟化、Tab 边界搜索、触摸拖动、水平滚轮、键盘分页、焦点滚入与视口缩小跟随、吸附／衰减、导航动画、辅助技术分页及多宽高比路径遮罩与视差 | [README](../src/Material3Expressive/Components/Carousel/README.md) | [INTERNALS](../src/Material3Expressive/Components/Carousel/INTERNALS.md) | 支持切角、百分比圆角与自定义路径；设备专用触控板、嵌套滚动与合成线程接入尚未实现 |
| `PullToRefresh` | 内置/外部下拉刷新状态、可定制轮廓的传统/Expressive 指示器及显式滚动消费策略 | [README](../src/Material3Expressive/Components/PullToRefresh/README.md) | [INTERNALS](../src/Material3Expressive/Components/PullToRefresh/INTERNALS.md) | 自动 ScrollViewer 接入尚未实现；回弹途中卸载的末帧保留值存在已记录差异 |
| `ScrollBar` | 原生滚动条外观与交互动效、只读滚动指示 | [README](../src/Material3Expressive/Components/ScrollBar/README.md) | [INTERNALS](../src/Material3Expressive/Components/ScrollBar/INTERNALS.md) | |

---

## 7. 基础控件与基元

| 组件 | 功能说明 | 使用指南 | 内部契约 | 状态说明 |
| --- | --- | --- | --- | --- |
| `Primitives` | 原生基础控件（标签、窗口、网格分割条、表格单元格等）的 Material 默认样式 | [README](../src/Material3Expressive/Components/Primitives/README.md) | [INTERNALS](../src/Material3Expressive/Components/Primitives/INTERNALS.md) | |
