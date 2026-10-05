using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class TokenConfigurationLifecycleTests
{
    public static readonly MaterialTokenKey<double> Horizontal = new("test.configuration.horizontal");
    public static readonly MaterialTokenKey<double> Vertical = new("test.configuration.vertical");
    private static readonly MaterialTokenKey<double> HorizontalScalar = new("test.configuration.horizontal-scalar");
    private static readonly MaterialTokenKey<double> VerticalScalar = new("test.configuration.vertical-scalar");
    public static IMultiValueConverter PaddingConverter { get; } = new PaddingFromTokensConverter();

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NearestInvalidAlias_UsesNativeMetadataDefault_AndRecovers(bool intermediate, bool cycle)
    {
        var root = new MaterialTokenKey<double>("test.configuration.root");
        var scalar = new MaterialTokenKey<double>("test.configuration.scalar");
        var absent = new MaterialTokenKey<double>("test.configuration.absent");
        var border = BoundBorder(root, out var binding);
        var host = new Panel { Children = { border } };
        host.Resources[root] = new TokenRef(scalar);
        host.Resources[scalar] = 20d;
        var window = Show(host);
        try
        {
            Assert.Equal(20, border.Width);
            var overridden = intermediate ? scalar : root;
            border.Resources[overridden] = new TokenRef(cycle ? overridden : absent);
            Flush(window);

            // The nearest definition is invalid. Do not resume the resource search at
            // the valid outer definition, or keep the binding's previous resolved value.
            // Avalonia 12.1.2 keeps an unresolved MultiBinding at its original priority
            // and supplies property metadata; UnsetValue does not withdraw that binding.
            Assert.Equal(BindingPriority.LocalValue, border.GetDiagnostic(Border.WidthProperty).Priority);
            Assert.Equal(Border.WidthProperty.GetDefaultValue(border), border.Width);
            host.Resources[scalar] = 24d;
            Flush(window);
            Assert.Equal(Border.WidthProperty.GetDefaultValue(border), border.Width);

            border.Resources[overridden] = 36d;
            Flush(window);
            Assert.Equal(36, border.Width);
            border.Resources.Remove(overridden);
            Flush(window);
            Assert.Equal(24, border.Width);

            border.Resources[overridden] = new TokenRef(absent);
            Flush(window);
            Assert.Equal(Border.WidthProperty.GetDefaultValue(border), border.Width);
            binding.Dispose();
            Flush(window);
            Assert.Equal(7, border.Width);
            Assert.Equal(BindingPriority.Style, border.GetDiagnostic(Border.WidthProperty).Priority);
            host.Resources[scalar] = 48d;
            Flush(window);
            Assert.Equal(7, border.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ResourceDictionaryReplacement_RebindsIntermediateToken_AndIgnoresOldSource()
    {
        var root = new MaterialTokenKey<double>("test.configuration.root");
        var scalar = new MaterialTokenKey<double>("test.configuration.scalar");
        var first = new TrackedTokenSource(scalar, 20d);
        var second = new TrackedTokenSource(scalar, 40d);
        var border = BoundBorder(root, out _);
        var host = new Panel { Children = { border }, Resources = ResourcesFor(first) };
        var window = Show(host, root, new TokenRef(scalar));
        try
        {
            Assert.Equal(20, border.Width);
            host.Resources = ResourcesFor(second);
            Flush(window);
            Assert.Equal(40, border.Width);
            second.Set(44d);
            Flush(window);
            Assert.Equal(44, border.Width);

            var reads = second.ReadCount;
            first.Set(90d);
            Flush(window);
            Assert.Equal(44, border.Width);
            Assert.Equal(reads, second.ReadCount);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MaterialResourceSourceReplacementAndRemoval_UnsubscribeOldSources()
    {
        var root = new MaterialTokenKey<double>("test.configuration.root");
        var scalar = new MaterialTokenKey<double>("test.configuration.scalar");
        var first = new TrackedTokenSource(scalar, 20d);
        var second = new TrackedTokenSource(scalar, 40d);
        var material = new MaterialResource { Children = { first } };
        var border = BoundBorder(root, out var binding);
        var host = new Panel { Children = { border } };
        host.Resources.MergedDictionaries.Add(material);
        var window = Show(host, root, new TokenRef(scalar));
        try
        {
            Assert.Equal(20, border.Width);
            Assert.Equal(1, first.SubscriberCount);
            material.Children[0] = second;
            Flush(window);
            Assert.Equal(40, border.Width);
            Assert.Equal(0, first.SubscriberCount);
            Assert.Equal(1, second.SubscriberCount);

            var reads = second.ReadCount;
            first.Set(90d);
            Flush(window);
            Assert.Equal(40, border.Width);
            Assert.Equal(reads, second.ReadCount);
            second.Set(44d);
            Flush(window);
            Assert.Equal(44, border.Width);

            material.Children.Clear();
            Flush(window);
            Assert.Equal(0, second.SubscriberCount);
            Assert.Equal(BindingPriority.LocalValue, border.GetDiagnostic(Border.WidthProperty).Priority);
            Assert.Equal(Border.WidthProperty.GetDefaultValue(border), border.Width);
            second.Set(94d);
            Flush(window);
            Assert.Equal(Border.WidthProperty.GetDefaultValue(border), border.Width);
            material.Children.Add(first);
            Flush(window);
            Assert.Equal(90, border.Width);

            material.Children.Clear();
            Flush(window);
            Assert.Equal(Border.WidthProperty.GetDefaultValue(border), border.Width);
            binding.Dispose();
            Flush(window);
            Assert.Equal(7, border.Width);
            Assert.Equal(BindingPriority.Style, border.GetDiagnostic(Border.WidthProperty).Priority);
            material.Children.Add(second);
            Flush(window);
            Assert.Equal(7, border.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Reparenting_RebindsFromCurrentConsumer_AndIgnoresPreviousScope()
    {
        var root = new MaterialTokenKey<double>("test.configuration.root");
        var scalar = new MaterialTokenKey<double>("test.configuration.scalar");
        var first = new TrackedTokenSource(scalar, 20d);
        var second = new TrackedTokenSource(scalar, 40d);
        var border = BoundBorder(root, out _);
        var left = new Panel { Children = { border }, Resources = ResourcesFor(first) };
        var right = new Panel { Resources = ResourcesFor(second) };
        var window = Show(new Panel { Children = { left, right } }, root, new TokenRef(scalar));
        try
        {
            Assert.Equal(20, border.Width);
            left.Children.Remove(border);
            Flush(window);
            var detached = border.Width;
            first.Set(24d);
            Flush(window);
            Assert.Equal(detached, border.Width);

            right.Children.Add(border);
            Flush(window);
            Assert.Equal(40, border.Width);
            var reads = second.ReadCount;
            first.Set(28d);
            Flush(window);
            Assert.Equal(40, border.Width);
            Assert.Equal(reads, second.ReadCount);
            second.Set(44d);
            Flush(window);
            Assert.Equal(44, border.Width);

            right.Children.Remove(border);
            left.Children.Add(border);
            Flush(window);
            Assert.Equal(28, border.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CompiledNestedBindingInSetter_RespectsStylesLocalValuesAndBindingDisposal()
    {
        var host = LoadFixture();
        var window = Show(host);
        var owner = host.FindControl<ContentControl>("Configured")!;
        var style = new Style(s => s.OfType<ContentControl>())
        {
            Setters = { new Setter(ContentControl.PaddingProperty, new Thickness(30)) },
        };
        try
        {
            Assert.Equal(new Thickness(6, 8), owner.Padding);
            window.Styles.Add(style);
            Flush(window);
            Assert.Equal(new Thickness(30), owner.Padding);

            owner.Padding = new Thickness(50);
            host.Resources[HorizontalScalar] = 10d;
            Flush(window);
            Assert.Equal(new Thickness(50), owner.Padding);
            owner.ClearValue(ContentControl.PaddingProperty);
            Flush(window);
            Assert.Equal(new Thickness(30), owner.Padding);
            window.Styles.Remove(style);
            Flush(window);
            Assert.Equal(new Thickness(10, 8), owner.Padding);

            var input = new Border { Padding = new Thickness(60) };
            using (owner.Bind(ContentControl.PaddingProperty,
                       new Binding(nameof(Border.Padding)) { Source = input }))
            {
                host.Resources[VerticalScalar] = 12d;
                Flush(window);
                Assert.Equal(new Thickness(60), owner.Padding);
                input.Padding = new Thickness(70);
                Flush(window);
                Assert.Equal(new Thickness(70), owner.Padding);
            }
            Flush(window);
            Assert.Equal(new Thickness(10, 12), owner.Padding);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CompiledNestedBindingInTemplate_UsesTemplatePriority_AndRestoresLatestDefault()
    {
        var host = LoadFixture();
        var window = Show(host);
        var owner = host.FindControl<ContentControl>("Configured")!;
        var part = owner.GetVisualDescendants().OfType<Border>().Single();
        var ordinary = new Style(s => s.OfType<ContentControl>().Template().OfType<Border>())
        {
            Setters =
            {
                new Setter(Border.PaddingProperty, new Thickness(20)),
                new Setter(Control.TagProperty, "ordinary"),
            },
        };
        var conditional = new Style(s => s.OfType<ContentControl>().Template().OfType<Border>().Class("override"))
        {
            Setters =
            {
                new Setter(Border.PaddingProperty, new Thickness(30)),
                new Setter(Control.TagProperty, "conditional"),
            },
        };
        try
        {
            window.Styles.Add(ordinary);
            window.Styles.Add(conditional);
            Flush(window);
            Assert.Equal("ordinary", part.Tag);
            Assert.Equal(BindingPriority.Template, part.GetDiagnostic(Border.PaddingProperty).Priority);
            Assert.Equal(new Thickness(6, 8), part.Padding);
            part.Classes.Add("override");
            Flush(window);
            Assert.Equal("conditional", part.Tag);
            Assert.Equal(new Thickness(30), part.Padding);

            part.Padding = new Thickness(50);
            host.Resources[HorizontalScalar] = 10d;
            Flush(window);
            Assert.Equal(new Thickness(50), part.Padding);
            part.ClearValue(Border.PaddingProperty);
            Flush(window);
            Assert.Equal(new Thickness(30), part.Padding);
            part.Classes.Remove("override");
            Flush(window);
            Assert.Equal("ordinary", part.Tag);
            Assert.Equal(new Thickness(10, 8), part.Padding);

            var input = new Border { Padding = new Thickness(60) };
            using (part.Bind(Border.PaddingProperty,
                       new Binding(nameof(Border.Padding)) { Source = input }))
            {
                host.Resources[VerticalScalar] = 12d;
                Flush(window);
                Assert.Equal(new Thickness(60), part.Padding);
                input.Padding = new Thickness(70);
                Flush(window);
                Assert.Equal(new Thickness(70), part.Padding);
            }
            Flush(window);
            Assert.Equal(new Thickness(10, 12), part.Padding);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CompiledNestedBindings_FollowThemeVariantAndDictionaryReplacement()
    {
        var host = LoadFixture();
        host.Resources.Remove(HorizontalScalar);
        host.Resources.Remove(VerticalScalar);
        host.Resources.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary
        {
            [HorizontalScalar] = 6d, [VerticalScalar] = 8d,
        };
        host.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            [HorizontalScalar] = 10d, [VerticalScalar] = 12d,
        };
        var window = Show(host);
        var owner = host.FindControl<ContentControl>("Configured")!;
        var part = owner.GetVisualDescendants().OfType<Border>().Single();
        void Check(Thickness expected)
        {
            Flush(window);
            Assert.Equal(expected, owner.Padding);
            Assert.Equal(expected, part.Padding);
        }
        try
        {
            Check(new Thickness(6, 8));
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Check(new Thickness(10, 12));
            host.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
            {
                [HorizontalScalar] = 14d, [VerticalScalar] = 16d,
            };
            Check(new Thickness(14, 16));
            window.RequestedThemeVariant = ThemeVariant.Light;
            Check(new Thickness(6, 8));
        }
        finally { window.Close(); }
    }

    private static Border BoundBorder(MaterialTokenKey<double> key, out IDisposable binding)
    {
        var border = new Border { Height = 10 };
        // Install and verify the lower-priority value independently of style attachment.
        // Disposing the binding must restore this; an unresolved binding remains active.
        border.SetValue(Border.WidthProperty, 7d, BindingPriority.Style);
        Assert.Equal(7, border.Width);
        Assert.Equal(BindingPriority.Style, border.GetDiagnostic(Border.WidthProperty).Priority);
        binding = border.Bind(Border.WidthProperty, new TokenExtension(key).ProvideValue(new StubServiceProvider()));
        return border;
    }

    private static ResourceDictionary ResourcesFor(ITokenSource source)
    {
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(new MaterialResource { Children = { source } });
        return resources;
    }

    private static StackPanel LoadFixture()
    {
        var host = (StackPanel)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3Expressive.Tests/Fixtures/TokenConfigurationLifecycle.axaml"));
        host.Resources[Horizontal] = new TokenRef(HorizontalScalar);
        host.Resources[Vertical] = new TokenRef(VerticalScalar);
        host.Resources[HorizontalScalar] = 6d;
        host.Resources[VerticalScalar] = 8d;
        return host;
    }

    private static Window Show(Control content, MaterialTokenKey? key = null, object? value = null)
    {
        var window = new MotionTestWindow
        {
            Width = 300, Height = 200, Content = content, RequestedThemeVariant = ThemeVariant.Light,
        };
        if (key is not null)
            window.Resources[key] = value;
        window.Show();
        Flush(window);
        return window;
    }

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private sealed class PaddingFromTokensConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values.Count == 2 && values[0] is double horizontal && values[1] is double vertical
                ? new Thickness(horizontal, vertical)
                : AvaloniaProperty.UnsetValue;
    }

    private sealed class TrackedTokenSource(MaterialTokenKey key, object value) : ITokenSource
    {
        public event EventHandler? Changed;
        public int ReadCount { get; private set; }
        public int SubscriberCount => Changed?.GetInvocationList().Length ?? 0;

        public void Set(object replacement)
        {
            value = replacement;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public bool TryGetToken(MaterialTokenKey requested, ThemeVariant? theme, out object result)
        {
            ReadCount++;
            result = value;
            return ReferenceEquals(key, requested);
        }
    }
}
