# GroupBox 实现说明

## 标题排版的提供位置

`PART_HeaderPresenter` 是 HeaderedContentControl 注册的宿主 presenter，标题根元素的逻辑父级是
GroupBox 而不是 presenter。本库的文字样式沿逻辑父级传递，所以 presenter 上的 `MaterialText.DefaultStyle`
到不了标题；放在 GroupBox 上又会同时作用于内容（内容的逻辑父级也是 GroupBox）。

因此使用 Foundation 的内部附加属性 [`PresenterTextStyle.DefaultStyle`](../../Foundation/Typography/PresenterTextStyle.cs)
（与 Expander 共用，待维护者确认，见 [DECISIONS](../../../../docs/DECISIONS.md#presenter-子元素的默认文字样式)），
把 TitleSmall 以 Template 优先级设为标题根元素自己的 `DefaultStyle`：字符串标题生成的 TextBlock、HeaderTemplate 的根元素或作为标题的控件都由此成为提供者，
其子元素按常规合并；应用在该元素上的局部 Style / DefaultStyle 仍然优先。根元素更换时撤销旧值。
保留 `PART_HeaderPresenter` 这一名称，标题控件仍是 GroupBox 的逻辑子项（与 Fluent 一致）。

## 标题有无

本地转换器 `GroupBoxConverters.HasHeader` 把 null 与空字符串视为无标题，隐藏标题 presenter，其 8 DIP 下边距随之不占空间。
内置 `StringConverters.IsNotNullOrEmpty` 只处理字符串，控件标题会落到 UnsetValue 回退，因此不用它。
