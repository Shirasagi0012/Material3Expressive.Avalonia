# GroupBox 实现说明

## 标题排版的提供位置

`PART_HeaderPresenter` 是 HeaderedContentControl 注册的宿主 presenter。
标题内容的逻辑父级是所有者，但 Avalonia 的继承父级是承载它的 ContentPresenter。因此直接在标题 presenter 上设置
`m3:MaterialText.Style="{m3:Typography TitleSmall}"`，经原生属性继承到达字符串生成的 TextBlock、HeaderTemplate 根元素或作为标题的控件，
且不影响内容区。标题元素上的局部 Style 或原生属性仍然优先（见 [DECISIONS](../../../../docs/DECISIONS.md#文字样式基于原生继承)）。
保留 `PART_HeaderPresenter` 这一名称，标题控件仍是 GroupBox 的逻辑子项（与 Fluent 一致）。

## 标题有无

本地转换器 `GroupBoxConverters.HasHeader` 把 null 与空字符串视为无标题，隐藏标题 presenter，其 8 DIP 下边距随之不占空间。
内置 `StringConverters.IsNotNullOrEmpty` 只处理字符串，控件标题会落到 UnsetValue 回退，因此不用它。
