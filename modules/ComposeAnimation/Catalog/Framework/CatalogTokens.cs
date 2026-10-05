using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Catalog.Framework;

/// <summary>
/// 设计语言令牌（Themes/CatalogTheme.axaml）的代码侧入口。
/// XAML 里用 <c>{DynamicResource CatalogXxxBrush}</c> 或样式类；
/// 代码生成的控件用这个扩展方法绑定同一个令牌，主题切换时跟随更新。
/// </summary>
public static class CatalogTokens
{
    /// <summary>把控件属性绑定到设计令牌，返回控件本身以便链式构造。</summary>
    public static TControl WithToken<TControl>(this TControl control, AvaloniaProperty property, string token)
        where TControl : Control
    {
        control.Bind(property, new DynamicResourceExtension(token));
        return control;
    }
}
