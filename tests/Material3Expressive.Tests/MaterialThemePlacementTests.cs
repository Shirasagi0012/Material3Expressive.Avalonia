using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using System.Runtime.CompilerServices;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia;
using MaterialDesignTokens.Avalonia;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using MaterialTokenOverrides = Material3Expressive.Foundation.Tokens.Runtime.MaterialTokenOverrides;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialThemePlacementTests
{
    [Fact]
    public void RawLookupIsLocalSparseAndLaterChildWins()
    {
        var token = new MaterialToken<int>("Tests.Raw", TokenLayer.Component);
        var first = new MaterialTokenOverrides();
        var second = new MaterialTokenOverrides();
        first.Set(token, 1);
        second.Set(token, 2);
        var theme = new MaterialTheme { Children = { first, second } };

        Assert.True(theme.TryGetResource(token, ThemeVariant.Dark, out var value));
        Assert.Equal(2, Assert.IsType<LiteralTokenDefinition<int>>(value).Value);

        second.Clear(token);
        Assert.True(theme.TryGetResource(token, ThemeVariant.Dark, out value));
        Assert.Equal(1, Assert.IsType<LiteralTokenDefinition<int>>(value).Value);

        first.Clear(token);
        Assert.False(theme.TryGetResource(token, ThemeVariant.Dark, out value));
        Assert.Null(value);
    }

    [Fact]
    public void InvalidRawSourceHitThrowsStructuredFailure()
    {
        var token = new MaterialToken<int>("Tests.InvalidRaw", TokenLayer.Component);
        var theme = new MaterialTheme { Children = { new InvalidSource(token, "wrong") } };

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            theme.TryGetResource(token, ThemeVariant.Light, out _));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(token, exception.Token);
        Assert.Same(ThemeVariant.Light, exception.RequestedTheme);
    }

    [AvaloniaFact]
    public void InvalidLocalSourceHitKeepsTheCompleteAliasPath()
    {
        var target = new MaterialToken<int>("System.InvalidTarget", TokenLayer.System);
        var alias = new MaterialToken<int>("Component.InvalidAlias", TokenLayer.Component);
        var invalid = new InvalidSource(target, "wrong");
        var definitions = new MaterialTokenOverrides();
        definitions.Alias(alias, target);
        var theme = new MaterialTheme { Children = { invalid, definitions } };
        var host = new Border();
        host.Resources.MergedDictionaries.Add(theme);
        var key = MaterialValue.FromRaw("Value.InvalidAlias", alias);

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            ((IResourceHost)host).TryFindResource(key, ThemeVariant.Light, out _));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Equal([alias.Path, target.Path], exception.RawPath.Select(frame => frame.Path));
    }

    [AvaloniaFact]
    public void ThrowingRawSourceIsClassifiedAtTheRawStageAndKeepsItsPath()
    {
        var token = new MaterialToken<int>("System.ThrowingSource", TokenLayer.System);
        var theme = new MaterialTheme { Children = { new ThrowingSource() } };
        var host = new Border();
        host.Resources.MergedDictionaries.Add(theme);
        var key = MaterialValue.FromRaw("Value.ThrowingSource", token);

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            ((IResourceHost)host).TryFindResource(key, ThemeVariant.Dark, out _));

        Assert.Equal(MaterialResolutionFailureKind.RecipeFailure, exception.FailureKind);
        Assert.Same(key, exception.ValueKey);
        Assert.Equal([token.Path], exception.RawPath.Select(frame => frame.Path));
        Assert.DoesNotContain("projector", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SourceMissWithANonNullDefinitionFailsFast()
    {
        var token = new MaterialToken<int>("System.InvalidMiss", TokenLayer.System);
        var theme = new MaterialTheme { Children = { new InvalidMissSource() } };

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            theme.TryGetResource(token, ThemeVariant.Light, out _));

        Assert.Equal(MaterialResolutionFailureKind.RecipeFailure, exception.FailureKind);
        Assert.Same(token, exception.Token);
        Assert.Contains("returned false", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FlatLookupRequiresAPlacementOwner()
    {
        var token = new MaterialToken<int>("Tests.Unowned", TokenLayer.Component);
        var key = MaterialValue.FromRaw("Tests.Value.Unowned", token);

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            new MaterialTheme().TryGetResource(key, null, out _));

        Assert.Equal(MaterialResolutionFailureKind.UnownedPlacement, exception.FailureKind);
        Assert.Same(key, exception.ValueKey);
    }

    [AvaloniaFact]
    public void DynamicResourceUsesRealPlacementAndTracksChildMutation()
    {
        var token = McuSystemColorTokens.Primary;
        var values = new MaterialTokenOverrides();
        values.Set(token, Colors.Red);
        var theme = new MaterialTheme { Children = { values } };
        var key = MaterialValue.SolidBrush("Tests.Value.PrimaryBrush", token);
        var consumer = new Border();
        consumer.Bind(Border.BackgroundProperty, new DynamicResourceExtension(key));
        var scope = new Border { Child = consumer };

        MaterialThemeScope.SetTheme(scope, theme);
        ThemeTestHelper.ShowWindow(scope);

        Assert.Same(theme, Assert.Single(scope.Resources.MergedDictionaries));
        Assert.Equal(Colors.Red, Assert.IsType<ImmutableSolidColorBrush>(consumer.Background).Color);

        var notifications = 0;
        theme.Changed += (_, _) => notifications++;
        values.Set(token, Colors.Blue);

        Assert.Equal(1, notifications);
        Assert.Equal(Colors.Blue, Assert.IsType<ImmutableSolidColorBrush>(consumer.Background).Color);
    }

    [AvaloniaFact]
    public void EmptyChildPlacementInheritsAncestorColorContextAndPreservesQueryVariant()
    {
        var ancestor = new MaterialTheme { ColorScheme = new TonalSpotScheme(Colors.Red) };
        var child = new MaterialTheme();
        var token = McuSystemColorTokens.Primary;
        var key = MaterialValue.FromRaw("Tests.Value.Primary", token);
        var childHost = new Border();
        var root = new Border { Child = childHost };
        root.Resources.MergedDictionaries.Add(ancestor);
        childHost.Resources.MergedDictionaries.Add(child);
        ThemeTestHelper.ShowWindow(root);

        Assert.True(((IResourceHost)childHost).TryFindResource(key, ThemeVariant.Light, out var light));
        Assert.True(((IResourceHost)childHost).TryFindResource(key, ThemeVariant.Dark, out var dark));
        Assert.True(((IResourceHost)root).TryFindResource(key, ThemeVariant.Light, out var ancestorLight));
        Assert.True(((IResourceHost)root).TryFindResource(key, ThemeVariant.Dark, out var ancestorDark));

        Assert.NotEqual(Assert.IsType<Color>(light), Assert.IsType<Color>(dark));
        Assert.Equal(ancestorLight, light);
        Assert.Equal(ancestorDark, dark);
        Assert.Null(child.ColorScheme);
    }

    [AvaloniaFact]
    public void McuTerminalBaselineResolvesWithoutALocalScheme()
    {
        var theme = new MaterialTheme();
        var host = new Border();
        host.Resources.MergedDictionaries.Add(theme);
        ThemeTestHelper.ShowWindow(host);
        var key = MaterialValue.FromRaw("Tests.Value.BaselinePrimary", McuSystemColorTokens.Primary);

        Assert.True(((IResourceHost)host).TryFindResource(key, ThemeVariant.Light, out var value));
        Assert.IsType<Color>(value);
    }

    [AvaloniaFact]
    public void FlatResolutionStartsAtThePlacementOwner()
    {
        var token = McuSystemColorTokens.Primary;
        var theme = new MaterialTheme();
        var host = new Border();
        host.Resources[token] = Token.Literal(Colors.HotPink);
        host.Resources.MergedDictionaries.Add(theme);
        ThemeTestHelper.ShowWindow(host);
        var key = MaterialValue.FromRaw("Tests.Value.OwnerPrimary", token);

        Assert.True(((IResourceHost)host).TryFindResource(key, ThemeVariant.Dark, out var value));
        Assert.Equal(Colors.HotPink, Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void SameThemeCannotBeOwnedByASecondScopeAndFailedEntryIsRemoved()
    {
        var theme = new MaterialTheme();
        var first = new Border();
        var second = new Border();

        MaterialThemeScope.SetTheme(first, theme);
        Assert.Throws<InvalidOperationException>(() => MaterialThemeScope.SetTheme(second, theme));

        Assert.Same(theme, Assert.Single(first.Resources.MergedDictionaries));
        Assert.DoesNotContain(theme, second.Resources.MergedDictionaries);
        Assert.Null(MaterialThemeScope.GetTheme(second));
    }

    [Fact]
    public void IndependentlyOwnedSourceCannotAlsoBeAChildAndMutationBroadcastsOnce()
    {
        var token = new MaterialToken<int>("Tests.SingleBroadcast", TokenLayer.Component);
        var source = new MaterialTokenOverrides();
        var theme = new MaterialTheme();
        var host = new Border();
        host.Resources.MergedDictionaries.Add(source);

        var exception = Assert.Throws<InvalidOperationException>(() => theme.Children.Add(source));
        Assert.Contains("cannot be installed independently", exception.Message, StringComparison.Ordinal);
        Assert.Empty(theme.Children);

        host.Resources.MergedDictionaries.Add(theme);
        var notifications = 0;
        ((IResourceHost)host).ResourcesChanged += (_, _) => notifications++;

        source.Set(token, 1);

        Assert.Equal(1, notifications);
    }

    [Fact]
    public void ChildSourceThatIsLaterInstalledIndependentlyFailsBeforeASecondBroadcast()
    {
        var token = new MaterialToken<int>("Tests.LateDoubleInstall", TokenLayer.Component);
        var source = new MaterialTokenOverrides();
        var theme = new MaterialTheme { Children = { source } };
        var host = new Border();
        host.Resources.MergedDictionaries.Add(theme);
        host.Resources.MergedDictionaries.Add(source);
        var notifications = 0;
        ((IResourceHost)host).ResourcesChanged += (_, _) => notifications++;

        var exception = Assert.Throws<InvalidOperationException>(() => source.Set(token, 1));

        Assert.Contains("cannot be installed independently", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, notifications);
    }

    [AvaloniaFact]
    public void ClearingScopeDoesNotRemoveAManuallyInstalledTheme()
    {
        var theme = new MaterialTheme();
        var scope = new Border();
        scope.Resources.MergedDictionaries.Add(theme);

        MaterialThemeScope.SetTheme(scope, theme);
        MaterialThemeScope.SetTheme(scope, null);

        Assert.Same(theme, Assert.Single(scope.Resources.MergedDictionaries));
    }

    [AvaloniaFact]
    public void FailedReplacementRestoresAnOwnedPlacementAndCompatibilityState()
    {
        var active = new MaterialTheme();
        var unavailable = new MaterialTheme();
        var scope = new Border();
        var otherOwner = new Border();
        MaterialThemeScope.SetTheme(scope, active);
        MaterialThemeScope.SetTheme(otherOwner, unavailable);
        var snapshot = MaterialThemeScope.GetResolvedTheme(scope);

        Assert.Throws<InvalidOperationException>(() =>
            MaterialThemeScope.SetTheme(scope, unavailable));

        Assert.Same(active, MaterialThemeScope.GetTheme(scope));
        Assert.Same(active, Assert.Single(scope.Resources.MergedDictionaries));
        Assert.Same(scope, active.Owner);
        Assert.Same(otherOwner, unavailable.Owner);
        Assert.Same(snapshot, MaterialThemeScope.GetResolvedTheme(scope));

        active.ShapeScheme = new ShapeScheme();
        Assert.NotSame(snapshot, MaterialThemeScope.GetResolvedTheme(scope));
    }

    [AvaloniaFact]
    public void FailedReplacementRestoresAManualPlacementWithoutClaimingItsEntry()
    {
        var active = new MaterialTheme();
        var unavailable = new MaterialTheme();
        var scope = new Border();
        var otherOwner = new Border();
        scope.Resources.MergedDictionaries.Add(active);
        MaterialThemeScope.SetTheme(scope, active);
        MaterialThemeScope.SetTheme(otherOwner, unavailable);
        var snapshot = MaterialThemeScope.GetResolvedTheme(scope);

        Assert.Throws<InvalidOperationException>(() =>
            MaterialThemeScope.SetTheme(scope, unavailable));

        Assert.Same(active, MaterialThemeScope.GetTheme(scope));
        Assert.Same(active, Assert.Single(scope.Resources.MergedDictionaries));
        Assert.Same(snapshot, MaterialThemeScope.GetResolvedTheme(scope));

        MaterialThemeScope.SetTheme(scope, null);
        Assert.Same(active, Assert.Single(scope.Resources.MergedDictionaries));
    }

    [AvaloniaFact]
    public void FailedPublishRemovesTheStagedCandidateAndRestoresTheActiveTheme()
    {
        var active = new MaterialTheme();
        var invalid = new MaterialTheme { ShapeScheme = null! };
        var scope = new Border();
        MaterialThemeScope.SetTheme(scope, active);
        var snapshot = MaterialThemeScope.GetResolvedTheme(scope);

        Assert.ThrowsAny<Exception>(() => MaterialThemeScope.SetTheme(scope, invalid));

        Assert.Same(active, MaterialThemeScope.GetTheme(scope));
        Assert.Same(active, Assert.Single(scope.Resources.MergedDictionaries));
        Assert.Same(scope, active.Owner);
        Assert.Null(invalid.Owner);
        Assert.Same(snapshot, MaterialThemeScope.GetResolvedTheme(scope));
    }

    [AvaloniaFact]
    public void FailedOldPlacementRemovalRestoresTheActiveTheme()
    {
        var active = new MaterialTheme();
        var candidate = new MaterialTheme();
        var scope = new Border();
        MaterialThemeScope.SetTheme(scope, active);
        var snapshot = MaterialThemeScope.GetResolvedTheme(scope);
        var throwOnRemoval = true;
        ((IResourceHost)scope).ResourcesChanged += (_, _) =>
        {
            if (!throwOnRemoval || scope.Resources.MergedDictionaries.Contains(active))
                return;

            throwOnRemoval = false;
            throw new InvalidOperationException("Injected failure after the active placement was removed.");
        };

        Assert.Throws<InvalidOperationException>(() => MaterialThemeScope.SetTheme(scope, candidate));

        Assert.Same(active, MaterialThemeScope.GetTheme(scope));
        Assert.Same(active, Assert.Single(scope.Resources.MergedDictionaries));
        Assert.Same(scope, active.Owner);
        Assert.Null(candidate.Owner);
        Assert.Same(snapshot, MaterialThemeScope.GetResolvedTheme(scope));

        active.ShapeScheme = new ShapeScheme();
        Assert.NotSame(snapshot, MaterialThemeScope.GetResolvedTheme(scope));
    }

    [AvaloniaFact]
    public void FailedOwnedPlacementClearRestoresTheActiveTheme()
    {
        var active = new MaterialTheme();
        var scope = new Border();
        MaterialThemeScope.SetTheme(scope, active);
        var snapshot = MaterialThemeScope.GetResolvedTheme(scope);
        var throwOnRemoval = true;
        ((IResourceHost)scope).ResourcesChanged += (_, _) =>
        {
            if (!throwOnRemoval || scope.Resources.MergedDictionaries.Contains(active))
                return;

            throwOnRemoval = false;
            throw new InvalidOperationException("Injected failure after the active placement was removed.");
        };

        Assert.Throws<InvalidOperationException>(() => MaterialThemeScope.SetTheme(scope, null));

        Assert.Same(active, MaterialThemeScope.GetTheme(scope));
        Assert.Same(active, Assert.Single(scope.Resources.MergedDictionaries));
        Assert.Same(scope, active.Owner);
        Assert.Same(snapshot, MaterialThemeScope.GetResolvedTheme(scope));

        active.ShapeScheme = new ShapeScheme();
        Assert.NotSame(snapshot, MaterialThemeScope.GetResolvedTheme(scope));
    }

    [Fact]
    public void ColorSchemeAlsoInChildrenIsSubscribedOnlyOnce()
    {
        var scheme = new TonalSpotScheme(Colors.Red);
        var theme = new MaterialTheme { ColorScheme = scheme, Children = { scheme } };
        var host = new Border();
        host.Resources.MergedDictionaries.Add(theme);
        var notifications = 0;
        theme.Changed += (_, _) => notifications++;

        scheme.Color = Colors.Blue;

        Assert.Equal(1, notifications);
    }

    [Fact]
    public void TypographySchemePropertiesInvalidateTheSnapshotExactlyOnce()
    {
        var theme = new MaterialTheme();
        var host = new Border();
        host.Resources.MergedDictionaries.Add(theme);
        var notifications = 0;
        theme.Changed += (_, _) => notifications++;
        var initial = theme.Resolved;

        theme.TypographyScheme.BrandFontFamily = new FontFamily("Arial");

        Assert.Equal(1, notifications);
        var afterFamily = theme.Resolved;
        Assert.NotSame(initial, afterFamily);

        theme.TypographyScheme.WeightBold = FontWeight.Black;

        Assert.Equal(2, notifications);
        Assert.NotSame(afterFamily, theme.Resolved);
    }

    [Fact]
    public void DuplicateTypeScaleOverrideIsSubscribedOnceAndClearUnsubscribesIt()
    {
        var theme = new MaterialTheme();
        var item = new TypeScaleOverrides();
        theme.TypographyScheme.TypeScaleOverrides.Add(item);
        theme.TypographyScheme.TypeScaleOverrides.Add(item);
        var host = new Border();
        host.Resources.MergedDictionaries.Add(theme);
        var notifications = 0;
        theme.Changed += (_, _) => notifications++;

        item.FontSize = 17;

        Assert.Equal(1, notifications);

        theme.TypographyScheme.TypeScaleOverrides.Clear();
        notifications = 0;
        item.FontSize = 18;

        Assert.Equal(0, notifications);
    }

    [Fact]
    public void ClearedTypeScaleOverrideIsNotKeptAlive()
    {
        var theme = new MaterialTheme();
        var reference = AddAndClearOverride(theme);

        Collect(reference);

        Assert.False(reference.IsAlive);
        GC.KeepAlive(theme);
    }

    [Fact]
    public void DetachedConfiguredThemeStopsCallbacksAndReattachesToCurrentAuthoringState()
    {
        var scheme = new TonalSpotScheme(Colors.Red);
        var theme = new MaterialTheme { ColorScheme = scheme };
        var host = new Border();
        host.Resources.MergedDictionaries.Add(theme);
        host.Resources.MergedDictionaries.Remove(theme);
        var snapshot = theme.Resolved;
        var notifications = 0;
        theme.Changed += (_, _) => notifications++;

        scheme.Color = Colors.Blue;

        Assert.Null(theme.Owner);
        Assert.Equal(0, notifications);

        host.Resources.MergedDictionaries.Add(theme);
        Assert.Same(host, theme.Owner);
        Assert.NotSame(snapshot, theme.Resolved);

        scheme.Color = Colors.Green;
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void RetainedChildSourceDoesNotKeepADetachedThemeAlive()
    {
        var source = new MaterialTokenOverrides();
        var reference = AddAndDetachTheme(source);

        Collect(reference);

        Assert.False(reference.IsAlive);
        GC.KeepAlive(source);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddAndClearOverride(MaterialTheme theme)
    {
        var item = new TypeScaleOverrides();
        theme.TypographyScheme.TypeScaleOverrides.Add(item);
        theme.TypographyScheme.TypeScaleOverrides.Clear();
        return new WeakReference(item);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddAndDetachTheme(MaterialTokenOverrides source)
    {
        var theme = new MaterialTheme { Children = { source } };
        var host = new Border();
        host.Resources.MergedDictionaries.Add(theme);
        host.Resources.MergedDictionaries.Remove(theme);
        return new WeakReference(theme);
    }

    private static void Collect(WeakReference reference)
    {
        for (var attempt = 0; attempt < 5 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private sealed class InvalidSource(MaterialToken token, object? value) : IMaterialTokenSource
    {
        public event EventHandler? Changed;

        public bool TryGetRawToken(
            MaterialToken candidate,
            ThemeVariant? queryTheme,
            out object? definition)
        {
            definition = ReferenceEquals(candidate, token) ? value : null;
            return ReferenceEquals(candidate, token);
        }

        public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class ThrowingSource : IMaterialTokenSource
    {
        public event EventHandler? Changed;

        public bool TryGetRawToken(
            MaterialToken candidate,
            ThemeVariant? queryTheme,
            out object? definition)
        {
            definition = null;
            throw new InvalidOperationException("Injected raw source failure.");
        }
    }

    private sealed class InvalidMissSource : IMaterialTokenSource
    {
        public event EventHandler? Changed;

        public bool TryGetRawToken(
            MaterialToken candidate,
            ThemeVariant? queryTheme,
            out object? definition)
        {
            definition = Token.Literal(123);
            return false;
        }
    }
}
