# Loading Indicator

`LoadingIndicator` 用不断变化的形状表达加载状态；`ContainedLoadingIndicator` 另带容器背景。
两者是库控件，默认不确定模式，也可让 0～1 进度直接控制形状。
先完成[公共接入](../../../../docs/GETTING_STARTED.md)，在窗口加载内容前初始化动画作用域。
机制与移植说明见 [INTERNALS.md](INTERNALS.md)。

## 最小示例

以下示例无需 ViewModel。确定模式需显式设置 `IsIndeterminate="False"`：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia">
  <StackPanel Orientation="Horizontal" Spacing="24">
    <m3:LoadingIndicator />
    <m3:LoadingIndicator IsIndeterminate="False" Progress="0.6" />
    <m3:ContainedLoadingIndicator CornerRadius="12"
                                  IndicatorColor="White" ContainerColor="#6750A4" />
  </StackPanel>
</UserControl>
```

后续片段沿用相同命名空间。绑定时让 DataContext 提供 0～1 的 Progress，编译绑定还需
对应 `x:DataType`。确定进度改变直接改变形状，不自动插值。
默认容器 48×48 DIP、active size 38 DIP，容器是首选尺寸，可被 Width/Height 或父约束改变。
非正方形容器中 active shape 保持正方形并居中。

## 颜色与形状

普通版本使用 `Color`，带容器版本使用 `IndicatorColor` / `ContainerColor`。
两者都支持 `CornerRadius`，物理四角顺序遵循 Avalonia；也可从 C# 提供 `ContainerShape`，
同一返回轮廓用于背景与裁剪，并优先于 CornerRadius。
统一默认值见[公共 Token 接入](../../../../docs/GETTING_STARTED.md#主题与定制)。

`Polygons` 是至少含两个 `MaterialShapes.RoundedPolygon` 的只读列表。
null 按当前模式选择 `LoadingIndicatorDefaults` 的默认序列；要更新内容必须替换列表，
组件不监听列表的原地修改。以下片段假设 `indicator` 是已有的 ContainedLoadingIndicator：

```csharp
using Avalonia;
using Avalonia.Media;
using MaterialShapes;

indicator.Polygons = [MaterialShapeCatalog.Pill, MaterialShapeCatalog.SoftBurst];
indicator.ContainerShape = (size, direction) => new EllipseGeometry(new Rect(size));
```

回调接收实际 DIP 尺寸与 FlowDirection。默认确定序列是旋转 18° 的 Circle 到 SoftBurst，
默认不确定序列包含多种形状并循环返回首形状。元素相等的列表替换保留当前 effect；
改变列表保留已记忆的动画值与旋转目标，但重置形状索引，模式切换或卸载重挂则重建状态。
两种控件没有内容槽、命令或点击事件。

## 语义与使用边界

组件暴露只读 ProgressBar automation，确定模式数值范围为 0～1，不确定模式无 RangeValue。
属性保留输入原值，绘制按 Float 限制范围；NaN 不填充 active path，automation 映射为 0。
语义范围不额外扩大。

无容器颜色的版本也能自定义 CornerRadius/ContainerShape，这是已确认的本地接口扩展。
没有 Compose InfiniteAnimationPolicy 自定义拦截入口；常规动画与 DurationScale 仍由共享动画环境管理。
实际桌面播放、辅助技术客户端和任意用户 polygon/geometry 未经过全面验证。
完整示例见 [LoadingIndicatorDemo](../../../Material3Expressive.Catalog/LoadingIndicatorDemo.axaml)，
算法、延时与回归范围见 [INTERNALS](INTERNALS.md#一致性边界与验证)。
