# 组件文档

先按[接入指南](GETTING_STARTED.md)初始化主题、字体及每个 TopLevel 的动画作用域。
以下为当前源码的组件入口；README 面向使用者，同目录 INTERNALS 面向维护与移植。
组件已经存在不代表完整上游一致性，各自的未实现功能、安全限制和验证范围见对应文档。

## 按钮与操作

- [Button](../src/Material3Expressive/Components/Button/README.md)：原生按钮的五种主题与尺寸
- [IconButton](../src/Material3Expressive/Components/IconButton/README.md)：图标按钮与外观
- [ToggleButton](../src/Material3Expressive/Components/ToggleButton/README.md)：原生切换按钮
- [SegmentedButton](../src/Material3Expressive/Components/SegmentedButton/README.md)：单选/多选分段按钮
- [SplitButton](../src/Material3Expressive/Components/SplitButton/README.md)：双操作按钮组合
- [Chip](../src/Material3Expressive/Components/Chip/README.md)：操作与选择标签
- [FloatingActionButton](../src/Material3Expressive/Components/FloatingActionButton/README.md)：FAB、Extended、Toggle 与菜单
- [CommandBar](../src/Material3Expressive/Components/CommandBar/README.md)：原生命令栏的三套紧凑主题
- [FloatingToolbar](../src/Material3Expressive/Components/FloatingToolbar/README.md)：横向/纵向浮动工具栏

## 输入与选择

- [TextField](../src/Material3Expressive/Components/TextField/README.md)：原生 TextBox 装饰与标签
- [SecureTextField](../src/Material3Expressive/Components/SecureTextField/README.md)：密码显示、输入限制及安全边界
- [ComboBox](../src/Material3Expressive/Components/ComboBox/README.md)：原生 ComboBox 的字段装饰与单组下拉菜单
- [CheckBox](../src/Material3Expressive/Components/CheckBox/README.md)：原生复选框与三态
- [RadioButton](../src/Material3Expressive/Components/RadioButton/README.md)：原生单选按钮与分组
- [Switch](../src/Material3Expressive/Components/Switch/README.md)：原生 ToggleSwitch 与拖动
- [Slider](../src/Material3Expressive/Components/Slider/README.md)：原生 Slider 与 RangeSlider
- [DatePicker](../src/Material3Expressive/Components/DatePicker/README.md)：月历与日期输入，以及日期范围
- [TimePicker](../src/Material3Expressive/Components/TimePicker/README.md)：钟面、时间输入与滚动时间
- [原生日期与时间](../src/Material3Expressive/Components/DateTimePicker/README.md)：原生 DatePicker、TimePicker、CalendarDatePicker 的 Material 主题

## 导航与栏

- [AppBar](../src/Material3Expressive/Components/AppBar/README.md)：顶部/底部栏与滚动联动
- [NavigationBar](../src/Material3Expressive/Components/NavigationBar/README.md)：常规底部导航
- [ShortNavigationBar](../src/Material3Expressive/Components/ShortNavigationBar/README.md)：紧凑导航栏
- [NavigationRail](../src/Material3Expressive/Components/NavigationRail/README.md)：常规导航轨
- [WideNavigationRail](../src/Material3Expressive/Components/WideNavigationRail/README.md)：宽导航轨与模态入口
- [NavigationDrawer](../src/Material3Expressive/Components/NavigationDrawer/README.md)：固定/关闭式/模态抽屉
- [TabRow](../src/Material3Expressive/Components/TabRow/README.md)：独立标签栏及原生 TabControl 主题

## 容器与布局

- [Surface](../src/Material3Expressive/Components/Surface/README.md)：表面、圆角、描边与高度
- [Card](../src/Material3Expressive/Components/Card/README.md)：填充、阴影与描边卡片
- [Scaffold](../src/Material3Expressive/Components/Scaffold/README.md)：屏幕槽位、Padding 与 Insets
- [Divider](../src/Material3Expressive/Components/Divider/README.md)：原生 Separator 与水平/垂直分隔
- [Others](../src/Material3Expressive/Components/Others/README.md)：ItemsControl、Label、Window、PopupRoot、GridSplitter、TableViewCell 等通用控件的结构主题

## 提示与状态

- [Menu / DropdownMenu](../src/Material3Expressive/Components/Menu/README.md)：独立下拉菜单、分组与原生 MenuFlyout 主题

- [AlertDialog](../src/Material3Expressive/Components/AlertDialog/README.md)：标准与自定义内容对话框
- [DatePickerDialog](../src/Material3Expressive/Components/DatePickerDialog/README.md)：日期编辑器对话框容器
- [TimePickerDialog](../src/Material3Expressive/Components/TimePickerDialog/README.md)：时间编辑器的标题、内容与动作容器
- [Tooltip](../src/Material3Expressive/Components/Tooltip/README.md)：静态提示外观与 TooltipBox 行为
- [Snackbar](../src/Material3Expressive/Components/Snackbar/README.md)：短消息及队列宿主
- [Badge](../src/Material3Expressive/Components/Badge/README.md)：徽标与 BadgedBox
- [ProgressIndicator](../src/Material3Expressive/Components/ProgressIndicator/README.md)：线形/圆形、直线/波浪进度
- [LoadingIndicator](../src/Material3Expressive/Components/LoadingIndicator/README.md)：形状变换加载指示

## 滚动

- [ScrollViewer](../src/Material3Expressive/Components/ScrollViewer/README.md)：原生滚动容器与 SmoothScroll 接入
- [ScrollBar](../src/Material3Expressive/Components/ScrollBar/README.md)：原生滚动条的 Material 外观

普通文字与图标排版另见 [Typography](../src/Material3Expressive/Foundation/Typography/README.md)，
弹层宿主见 [Overlays](../src/Material3Expressive/Foundation/Overlays/README.md)。维护组件时参照
[文档约定](COMPONENT_DOCS.md)、[移植指南](PORTING.md)、[架构](ARCHITECTURE.md)与[共享决定](DECISIONS.md)。
