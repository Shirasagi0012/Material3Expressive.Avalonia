# Icon

`MaterialIcon` 显示位图或矢量绘图，并用当前内容色着色。字体图标继续使用 `MaterialSymbol`，可点击图标放入 `IconButton`。

## 基础用法

```xml
<m3:MaterialIcon xmlns="https://github.com/avaloniaui"
                 xmlns:m3="https://github.com/Shirasagi0012/Material3Expressive.Avalonia"
                 ContentDescription="下载">
  <DrawingImage>
    <GeometryDrawing Brush="Black" Geometry="M 4,0 L 12,0 L 12,10 L 16,10 L 8,18 L 0,10 L 4,10 Z" />
  </DrawingImage>
</m3:MaterialIcon>
```

加载 Material 主题后，可将 `Source` 绑定到 `Bitmap`、`DrawingImage` 或自定义 `IImage`；图片资源由调用方管理和释放。

## 变体与属性

- 默认 `IsTintEnabled=True`，使用继承的 `Foreground`，保留来源透明度；显式 Foreground 可覆盖。
- `IsTintEnabled=False` 保留来源的全部颜色，适合多色标志。
- 来源有固有尺寸时使用它，保持宽高比并以 Fit 缩放；无固有尺寸时默认 24 DIP。Width / Height、最小尺寸及父约束仍有效。
- `AutoMirror=True` 在 RTL 下翻转方向性图案；默认关闭，普通图片不自动翻转。
- `ContentDescription` 提供本地化无障碍名称；null 表示装饰图标，不新增可聚焦操作。

## 内容槽与定制

`Source` 是内容属性，支持 XAML 内联 DrawingImage 和绑定。控件直接绘制，不提供 ControlTemplate。
在按钮、菜单或导航图标槽中使用时，前景色继承对应槽的实际状态色，显式 Width / Height 选择槽内尺寸。

```xml
<m3:MaterialIcon Source="{Binding Artwork}" Width="24" Height="24" IsTintEnabled="False" />
```

无固有尺寸来源的默认尺寸沿用 `Comp.IconButtonSmall.IconSize` Token；这不改变具有固有尺寸的图片。
DrawingImage 内的几何、画笔和 Viewbox 变化自动重测/重绘；自定义可变 IImage 应在变化后通知控件重绘或替换 Source。

## 使用限制与注意事项

Bitmap 的自然大小使用 Avalonia IImage 的 DIP 尺寸，完整像素区域用于绘制；高 DPI 图片应提供正确 DPI 元数据或明确 Width / Height。
文字图标的字体轴、字体注册与光学尺寸由 MaterialSymbol 管理。真实屏幕阅读器和非 Skia 后端尚需专项验证。
Catalog 的 “Bitmap / vector icons” 展示原色、着色、缩放、RTL 和按钮内容继承；移植机制见 [INTERNALS.md](INTERNALS.md)。
