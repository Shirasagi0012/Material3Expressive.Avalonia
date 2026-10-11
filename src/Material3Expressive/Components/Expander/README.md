# Expander

M3 没有展开面板。原生 `Expander` 加载本主题后由单行列表项风格的标题和带动画的内容区组合而成，
采用列表风格：没有容器、背景透明，适合放在设置类列表中。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)；内部决定见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

```xml
<StackPanel>
  <Expander Header="Wi-Fi" IsExpanded="True">
    <StackPanel Spacing="4" Margin="0,0,0,16">
      <CheckBox Content="自动连接" />
      <TextBox Watermark="网络名称" />
    </StackPanel>
  </Expander>
  <Separator />
  <Expander Header="蓝牙">
    <ToggleSwitch Content="对附近设备可见" />
  </Expander>
</StackPanel>
```

## 标题

- 标题行是单行列表项：最小高度 56 DIP，起止边距 16 DIP、上下 10 DIP，标签 BodyLarge、OnSurface，
  尾部 24 DIP 的 `keyboard_arrow_down` 箭头为 OnSurfaceVariant，与标签间距 12 DIP。
  MaterialTokens 尚无 List Token，这些是取自上游 ListTokens（md.comp.list.list-item.*）的本地值，未新增 Token 键
- 字符串标题、`HeaderTemplate` 的根元素或作为标题的控件都会得到 BodyLarge；应用设置在标题元素上的
  `MaterialText.Style` / `DefaultStyle` 优先，标题排版不影响内容。多行标题（如标题加 BodyMedium 说明）可直接放控件
- 悬停、按下、焦点由 MaterialInteraction 状态层表示（标签色）
- 箭头指向内容出现的方向，展开时旋转 180°，使用与 DropDownButton 相同的弹簧
- 标题仍是 `ToggleButton`：Space / Enter 切换、`:focus-visible`、原生 `ExpanderAutomationPeer`（展开/折叠模式）保持不变

## 内容与动画

- `Padding` 默认 `16,0`（起止各 16 DIP），作用于内容；内容在展开方向的交叉轴上填满（Down/Up 填满宽度）
- 展开/折叠的动画同 Compose 的 `AnimatedVisibility`：`expandVertically + fadeIn` / `shrinkVertically + fadeOut`，
  沿展开轴并锚定在标题一侧，使用 ComposeAnimation 声明的默认弹簧（即 Compose 这些过渡的默认 spec）
- 折叠后内容不在可视树中：不占布局空间，Tab 跳过；初始折叠的内容在首次展开前不会被测量。退出动画期间内容仍在
- `ExpandDirection`：
  - `Down`（默认）：标题在上，内容在下
  - `Up`：与 Down 镜像，标题在下，箭头朝上，内容向上展开
  - `Left` / `Right`：标题在内容的右/左侧并填满高度，箭头横向，内容水平展开（`expandHorizontally`）。
    标题仍是横排的列表项，不是竖排标签
- `ContentTransition`：主题不使用它，动画由上面的 AnimatedVisibility 提供。Avalonia 仍会在 `PART_ContentPresenter`
  上运行显式设置的 ContentTransition，与主题动画叠加，不建议同时使用

## RTL、禁用与深色

- RTL 下标签位于右侧、箭头位于左侧（尾部）；`Left` / `Right` 按 Avalonia 语义随 FlowDirection 镜像（同 Fluent 的 DockPanel）
- `IsEnabled="False"`：标签与箭头为 OnSurface × 禁用内容不透明度，无状态层，不能切换
- 颜色全部来自系统色 Token，随深色方案变化

## 已知限制

- 内容为了填满宽度会占满可用宽度：非 Stretch 对齐的 Down/Up Expander 展开后也会变宽到可用宽度
  （Compose 中对应 `fillMaxWidth()`）
- 动画 spec 是 ComposeAnimation 声明的默认值，而非 MotionScheme Token；如需接入 MotionScheme 可另行提议

## 提议（未实现，待维护者批准）

- 描边变体：类似 Divider 的列表分隔，在折叠/展开的 Expander 之间或底部绘制 OutlineVariant 1 DIP 分隔线
  （目前可在 Expander 之间放 `Separator` 达到同样效果，见 Catalog）
