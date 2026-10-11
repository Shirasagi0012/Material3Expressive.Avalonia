# GroupBox

M3 没有带标题的分组容器。原生 `GroupBox` 加载本主题后由 Outlined Card 的容器组合而成：
OutlinedCard 的容器色、描边色/宽度与形状 Token 绘制在 MaterialSurface 上，内容色同 OutlinedCard（OnSurface）。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)；标题排版的内部做法见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

```xml
<GroupBox Header="网络">
  <StackPanel Spacing="8">
    <CheckBox Content="使用代理" />
    <TextBox Watermark="代理地址" />
  </StackPanel>
</GroupBox>
```

## 外观

- 标题位于表面内部顶部，使用 TitleSmall、OnSurfaceVariant。这是本库的组合选择，并非 M3 规范值：
  安静的标签能命名分组而不与内容争夺注意力，Primary 色则容易被读成链接或文字按钮
- `Padding` 默认 16 DIP，作用于表面内侧（标题与内容一起），标题与内容间距 8 DIP。没有合适的间距 Token，
  这两个值是本地值，16 DIP 与 Card 示例的内容留白一致
- `Header` 为 null 或空字符串时不显示标题行，也不保留间距
- `HeaderTemplate` 照常使用；字符串标题、模板根元素或作为标题的控件都会得到 TitleSmall，
  应用自己设置在标题元素上的 `MaterialText.Style` / `DefaultStyle` 优先。标题排版不会影响内容
- `HorizontalContentAlignment` / `VerticalContentAlignment` 作用于内容；标题沿起始边排列，RTL 下位于右侧
- `Background`、`BorderBrush`、`BorderThickness`、`CornerRadius`、`Padding` 的局部值、Style 或绑定优先于主题

## 交互、禁用与无障碍

- 与普通（不可点击）Card 相同：没有状态层、ripple 或焦点，GroupBox 自身不可聚焦；内容的 Tab 顺序与焦点保持原生
- `IsEnabled="False"` 不改变容器颜色（同普通 Card），内容按原生规则禁用并显示各自的禁用外观
- 自动化沿用原生：Avalonia 12.1.2 的 GroupBox 没有专用 peer（为 NoneAutomationPeer），也不会把标题作为名称；
  需要时在 GroupBox 上设置 `AutomationProperties.Name`

## 与 Fluent 的差异

- Fluent 的标题压在描边的缺口上，`Padding` 只作用于内容；这里标题位于表面内部，`Padding` 包含标题
- 暂无 Filled / Elevated 等键控变体；若需要，可作为基于 Card 其余两种容器的提议，待维护者批准
