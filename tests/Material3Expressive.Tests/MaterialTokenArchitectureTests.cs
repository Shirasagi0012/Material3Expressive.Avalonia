using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Tests.TestUtils;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// Architectural invariants for the reference -> system -> component token graph. These tests
/// intentionally use synthetic tokens so they exercise the runtime independently of generated
/// Material tables.
/// </summary>
public sealed class MaterialTokenArchitectureTests
{
    private readonly MaterialTokenResolver _resolver = new();

    [AvaloniaFact]
    public void AncestorComponentAlias_ResolvesTargetFromRequesterScope()
    {
        var reference = Literal("Reference.Palette.Primary40", TokenLayer.Reference, 40);
        var system = Alias("System.Color.Primary", TokenLayer.System, reference);
        var component = Literal("Component.Button.Container", TokenLayer.Component, -1);

        var consumer = new Border();
        var scope = new Border { Child = consumer };
        var root = Show(new Border { Child = scope });

        // The component definition is found at an ancestor, but its alias must restart from the
        // actual requester. Otherwise the nearer system override would be skipped.
        root.Resources[component] = Token.Alias(system);
        scope.Resources[system] = Token.Literal(99);

        Assert.Equal(99, _resolver.Resolve(consumer, component));
    }

    [AvaloniaFact]
    public void SparseComponentOverride_DoesNotMaskOtherComponentTokens()
    {
        var reference = Literal("Reference.Number.Base", TokenLayer.Reference, 1);
        var first = Alias("Component.Probe.First", TokenLayer.Component, reference);
        var second = Alias("Component.Probe.Second", TokenLayer.Component, reference);
        var consumer = Show(new Border());
        var overrides = new MaterialTokenOverrides();
        consumer.Resources.MergedDictionaries.Add(overrides);

        overrides.Set(first, 10);

        Assert.Equal(10, _resolver.Resolve(consumer, first));
        Assert.Equal(1, _resolver.Resolve(consumer, second));
    }

    [AvaloniaFact]
    public void Alias_CannotDependOnADownstreamLayer()
    {
        var component = Literal("Component.Probe.Value", TokenLayer.Component, 3);
        var system = Literal("System.Probe.Value", TokenLayer.System, 2);
        var consumer = Show(new Border());
        consumer.Resources[system] = Token.Alias(component);

        var exception = Assert.Throws<MaterialTokenResolutionException>(
            () => _resolver.Resolve(consumer, system));

        Assert.Contains(system.Path, exception.Message, StringComparison.Ordinal);
        Assert.Contains(component.Path, exception.Message, StringComparison.Ordinal);
        Assert.Contains("downstream", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [AvaloniaFact]
    public void Recipe_UsesRequesterLocalValuesAndRequiresDeclaredDependencies()
    {
        var reference = Literal("Reference.Number.Base", TokenLayer.Reference, 2);
        var system = new MaterialToken<int>(
            "System.Number.Doubled",
            TokenLayer.System,
            Token.Recipe(new MultiplyRecipe(reference, 2)));
        var consumer = Show(new Border());
        consumer.Resources[reference] = 7;

        Assert.Equal(14, _resolver.Resolve(consumer, system));

        var undeclared = new MaterialToken<int>(
            "System.Number.InvalidRecipe",
            TokenLayer.System,
            Token.Recipe(new UndeclaredDependencyRecipe(reference)));
        Assert.Throws<InvalidOperationException>(() => _resolver.Resolve(consumer, undeclared));
    }

    [AvaloniaFact]
    public void RecipeCycle_PreservesTokenResolutionDiagnosticInsteadOfReflectionWrapper()
    {
        var token = Literal("System.Number.CyclicRecipe", TokenLayer.System, 1);
        var consumer = Show(new Border());
        consumer.Resources[token] = Token.Recipe(new IdentityRecipe(token));

        var exception = Assert.Throws<MaterialTokenResolutionException>(
            () => _resolver.Resolve(consumer, token));

        Assert.Contains(token.Path, exception.Message, StringComparison.Ordinal);
        Assert.Contains("cycle", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [AvaloniaFact]
    public void SeparateReads_DoNotRetainValuesAfterAResourceMutation()
    {
        var system = Literal("System.Number.Mutable", TokenLayer.System, 1);
        var component = Alias("Component.Probe.Mutable", TokenLayer.Component, system);
        var consumer = Show(new Border());
        var overrides = new MaterialTokenOverrides();
        consumer.Resources.MergedDictionaries.Add(overrides);

        Assert.Equal(1, _resolver.Resolve(consumer, component));
        overrides.Set(system, 8);
        Assert.Equal(8, _resolver.Resolve(consumer, component));
    }

    [AvaloniaFact]
    public void GeneratedOverrideSurface_AcceptsLiteralAndUpstreamAliasDefinitions()
    {
        var consumer = Show(new Border());
        consumer.Resources.MergedDictionaries.Add(new MaterialTheme());
        var overrides = new FilledButtonTokensOverrides
        {
            ContainerColor = SharedToken.Literal(Colors.Red),
        };
        consumer.Resources.MergedDictionaries.Add(overrides);

        Assert.Equal(
            Colors.Red,
            SharedTokenTestHelper.Resolve(consumer, FilledButtonTokensKeys.ContainerColor));

        consumer.Resources[McuSystemColorTokens.Secondary] = SharedToken.Literal(Colors.Blue);
        overrides.ContainerColor = SharedToken.Alias(McuSystemColorTokens.Secondary);

        Assert.Equal(
            Colors.Blue,
            SharedTokenTestHelper.Resolve(consumer, FilledButtonTokensKeys.ContainerColor));
    }

    [AvaloniaFact]
    public void OmittedThemeArgument_UsesTheRequestersActualThemeVariant()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        using var restore = ThemeTestHelper.AppTheme(theme);
        var consumer = new Border();
        var window = ThemeTestHelper.ShowWindow(consumer);
        window.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(
            ThemeTestHelper.Expected(
                theme,
                MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary,
                isDark: true),
            SharedTokenTestHelper.Resolve(consumer, McuSystemColorTokens.Primary));
    }

    [AvaloniaFact]
    public void GeneratedOverrides_AreAuthorableWithLiteralAndTokenRefInCompiledXaml()
    {
        var probe = Show(new TokenOverrideXamlProbe());
        probe.Resources.MergedDictionaries.Insert(0, new MaterialTheme());
        probe.Resources[McuSystemColorTokens.Primary] = SharedToken.Literal(Colors.Blue);

        Assert.Equal(
            Colors.Red,
            SharedTokenTestHelper.Resolve(probe, FilledButtonTokensKeys.ContainerColor));
        Assert.Equal(
            Colors.Blue,
            SharedTokenTestHelper.Resolve(probe, FilledButtonTokensKeys.LabelTextColor));
        var bodyLarge = SystemTypeScaleTokens.For(TypographyRole.BodyLarge);
        Assert.Equal(17.5, _resolver.Resolve(probe, bodyLarge.FontSize));
        Assert.Equal(FontWeightToken.Bold, _resolver.Resolve(probe, bodyLarge.Weight));
    }

    private static MaterialToken<int> Literal(string path, TokenLayer layer, int value) =>
        new(path, layer, Token.Literal(value));

    private static MaterialToken<int> Alias(
        string path,
        TokenLayer layer,
        MaterialToken<int> target) =>
        new(path, layer, Token.Alias(target));

    private static T Show<T>(T control)
        where T : Control
    {
        ThemeTestHelper.ShowWindow(control);
        return control;
    }

    private sealed class MultiplyRecipe(MaterialToken<int> dependency, int factor) : ITokenRecipe<int>
    {
        public IReadOnlyList<MaterialToken> Dependencies { get; } = [dependency];

        public int Evaluate(TokenRecipeContext context) => context.Get(dependency) * factor;
    }

    private sealed class UndeclaredDependencyRecipe(MaterialToken<int> dependency) : ITokenRecipe<int>
    {
        public IReadOnlyList<MaterialToken> Dependencies => [];

        public int Evaluate(TokenRecipeContext context) => context.Get(dependency);
    }

    private sealed class IdentityRecipe(MaterialToken<int> dependency) : ITokenRecipe<int>
    {
        public IReadOnlyList<MaterialToken> Dependencies { get; } = [dependency];

        public int Evaluate(TokenRecipeContext context) => context.Get(dependency);
    }
}
