using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using MaterialColorUtilities.Avalonia.Tokens;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Tests.TestUtils;
using Xunit;
using SystemColorTokens = Material3Expressive.Foundation.Tokens.Runtime.SystemColorTokens;

namespace Material3Expressive.Tests;

public sealed class MaterialTokenRuntimeTests
{
    private readonly MaterialTokenResolver _resolver = new();

    [AvaloniaFact]
    public void LiteralDefault_ResolvesWhenScopeContainsNoDefinition()
    {
        var token = LiteralToken("Reference.Number", TokenLayer.Reference, 17);
        var consumer = Show(new Border());

        Assert.Equal(17, _resolver.Resolve(consumer, token));
    }

    [AvaloniaFact]
    public void ComponentAlias_ResolvesItsSystemTokenDefault()
    {
        var system = LiteralToken("System.Number.Primary", TokenLayer.System, 23);
        var component = AliasToken("Component.Button.Number", TokenLayer.Component, system);
        var consumer = Show(new Border());

        Assert.Equal(23, _resolver.Resolve(consumer, component));
    }

    [AvaloniaFact]
    public void LocalSystemOverride_RebindsComponentAliasWithoutChangingSiblingScopeOrToken()
    {
        var primary = LiteralToken("System.Number.Primary", TokenLayer.System, 1);
        var secondary = LiteralToken("System.Number.Secondary", TokenLayer.System, 2);
        var container = AliasToken("Component.Button.Container", TokenLayer.Component, primary);
        var label = AliasToken("Component.Button.Label", TokenLayer.Component, secondary);

        var localConsumer = new Border();
        var siblingConsumer = new Border();
        var localScope = new Border { Child = localConsumer };
        var root = new StackPanel { Children = { localScope, siblingConsumer } };
        Show(root);

        var overrides = AddOverrides(localScope);
        overrides.Set(primary, 101);

        Assert.Equal(101, _resolver.Resolve(localConsumer, container));
        Assert.Equal(2, _resolver.Resolve(localConsumer, label));
        Assert.Equal(1, _resolver.Resolve(siblingConsumer, container));
        Assert.Equal(2, _resolver.Resolve(siblingConsumer, label));
    }

    [AvaloniaFact]
    public void MultipleSparseOverrideLayers_ComposePerToken()
    {
        var firstSystem = LiteralToken("System.Number.First", TokenLayer.System, 1);
        var secondSystem = LiteralToken("System.Number.Second", TokenLayer.System, 2);
        var firstComponent = AliasToken("Component.Probe.First", TokenLayer.Component, firstSystem);
        var secondComponent = AliasToken("Component.Probe.Second", TokenLayer.Component, secondSystem);

        var consumer = new Border();
        var innerScope = new Border { Child = consumer };
        var middleScope = new Border { Child = innerScope };
        var sibling = new Border();
        var root = new StackPanel { Children = { middleScope, sibling } };
        Show(root);

        AddOverrides(root).Set(firstSystem, 10);
        AddOverrides(middleScope).Set(secondSystem, 20);
        AddOverrides(innerScope).Set(firstSystem, 30);

        Assert.Equal(30, _resolver.Resolve(consumer, firstComponent));
        Assert.Equal(20, _resolver.Resolve(consumer, secondComponent));
        Assert.Equal(10, _resolver.Resolve(sibling, firstComponent));
        Assert.Equal(2, _resolver.Resolve(sibling, secondComponent));
    }

    [AvaloniaFact]
    public void ResourceDictionary_AcceptsRawTypedLiteral()
    {
        var token = LiteralToken("System.Number.Raw", TokenLayer.System, 1);
        var consumer = Show(new Border());
        consumer.Resources[token] = 42;

        Assert.Equal(42, _resolver.Resolve(consumer, token));
    }

    [AvaloniaFact]
    public void ResourceWithWrongType_ReportsTokenPathAndExpectedType()
    {
        var token = LiteralToken("System.Number.WrongType", TokenLayer.System, 1);
        var consumer = Show(new Border());
        consumer.Resources[token] = "not an integer";

        var exception = Assert.Throws<MaterialTokenResolutionException>(
            () => _resolver.Resolve(consumer, token));

        Assert.Contains(token.Path, exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(int).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(string).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void ScopeOverrides_CanCreateACycleAndCyclePathIsReported()
    {
        var first = LiteralToken("System.Number.First", TokenLayer.System, 1);
        var second = LiteralToken("System.Number.Second", TokenLayer.System, 2);
        var consumer = Show(new Border());
        consumer.Resources[first] = Token.Alias(second);
        consumer.Resources[second] = Token.Alias(first);

        var exception = Assert.Throws<MaterialTokenResolutionException>(
            () => _resolver.Resolve(consumer, first));

        Assert.Contains(
            $"{first.Path} -> {second.Path} -> {first.Path}",
            exception.Message,
            StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void ThemeDictionary_DefinitionIsSelectedByRequestedThemeVariant()
    {
        var token = LiteralToken("System.Number.Themed", TokenLayer.System, 1);
        var consumer = Show(new Border());
        var light = new ResourceDictionary { [token] = 10 };
        var dark = new ResourceDictionary { [token] = 90 };
        consumer.Resources.ThemeDictionaries[ThemeVariant.Light] = light;
        consumer.Resources.ThemeDictionaries[ThemeVariant.Dark] = dark;

        Assert.Equal(10, _resolver.Resolve(consumer, token, ThemeVariant.Light));
        Assert.Equal(90, _resolver.Resolve(consumer, token, ThemeVariant.Dark));
    }

    [AvaloniaFact]
    public void MaterialTokenOverrides_MutationNotifiesOwnerAndChangesSubsequentResolution()
    {
        var first = LiteralToken("System.Number.First", TokenLayer.System, 1);
        var second = LiteralToken("System.Number.Second", TokenLayer.System, 2);
        var component = AliasToken("Component.Probe.Number", TokenLayer.Component, first);
        var consumer = Show(new Border());
        var overrides = AddOverrides(consumer);
        var notificationCount = 0;
        ((IResourceHost)consumer).ResourcesChanged += (_, _) => notificationCount++;

        using (overrides.BeginUpdate())
        {
            overrides.Set(first, 11);
            overrides.Set(second, 22);
            overrides.Alias(component, second);
        }

        Assert.Equal(1, notificationCount);
        Assert.Equal(22, _resolver.Resolve(consumer, component));

        overrides.Clear(component);

        Assert.Equal(2, notificationCount);
        Assert.Equal(11, _resolver.Resolve(consumer, component));
    }

    [Fact]
    public void CustomSystemColorKeys_UseTheSchemesCaseInsensitiveIdentity()
    {
        var original = SystemColorTokens.Get(SysColorToken.Custom, "Brand");
        var differentCase = SystemColorTokens.Get(SysColorToken.Custom, "brAND");

        Assert.Same(original, differentCase);
        Assert.True(SystemColorTokens.TryGetRole(
            original,
            out var role,
            out var customKey));
        Assert.Equal(SysColorToken.Custom, role);
        Assert.Equal("BRAND", customKey);
    }

    private static MaterialToken<int> LiteralToken(string path, TokenLayer layer, int value) =>
        new(path, layer, Token.Literal(value));

    private static MaterialToken<int> AliasToken(
        string path,
        TokenLayer layer,
        MaterialToken<int> target) =>
        new(path, layer, Token.Alias(target));

    private static MaterialTokenOverrides AddOverrides(Control owner)
    {
        var overrides = new MaterialTokenOverrides();
        owner.Resources.MergedDictionaries.Add(overrides);
        return overrides;
    }

    private static T Show<T>(T control)
        where T : Control
    {
        ThemeTestHelper.ShowWindow(control);
        return control;
    }
}
