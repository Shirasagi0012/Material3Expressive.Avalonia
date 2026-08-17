using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialDesignTokens.Avalonia;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialValueRuntimeTests
{
    [Fact]
    public void LiteralAliasAndRecipeDefinitionsResolveThroughOneSession()
    {
        var first = NumberToken("Reference.First", TokenLayer.Reference);
        var second = NumberToken("Reference.Second", TokenLayer.Reference);
        var alias = NumberToken("System.Alias", TokenLayer.System);
        var sum = NumberToken("System.Sum", TokenLayer.System);
        var contribution = new TestContribution("numbers", [first, second, alias, sum])
            .Define(first, Token.Literal(4))
            .Define(second, Token.Literal(6))
            .Define(alias, Token.Alias(first))
            .Define(sum, Token.Recipe(new SumRecipe(first, second)));
        var session = Session(new Border(), contribution);

        Assert.Equal(4, session.Resolve(MaterialValue.FromRaw("Value.Alias", alias)));
        Assert.Equal(10, session.Resolve(MaterialValue.FromRaw("Value.Sum", sum)));
    }

    [Fact]
    public void MultiDependencyValueRecipeUsesSingleSessionMemoization()
    {
        var source = NumberToken("Reference.Counted", TokenLayer.Reference);
        var counting = new CountingRecipe();
        var contribution = new TestContribution("counted", [source])
            .Define(source, Token.Recipe(counting));
        var plan = new RawRecipePlan<int>(new ReadTwiceRecipe(source));
        var key = new MaterialValueKey<int>("Value.ReadTwice", plan);

        var result = Session(new Border(), contribution).Resolve(key);

        Assert.Equal(14, result);
        Assert.Equal(1, counting.EvaluationCount);
    }

    [AvaloniaFact]
    public void AncestorResourceOverrideWinsBeforeTerminalCatalog()
    {
        var token = NumberToken("System.Ancestor", TokenLayer.System);
        var contribution = new TestContribution("defaults", [token])
            .Define(token, Token.Literal(1));
        var placementOwner = new Border();
        var root = new Border { Child = placementOwner };
        root.Resources[token] = 9;
        ThemeTestHelper.ShowWindow(root);

        var result = Session(placementOwner, contribution)
            .Resolve(MaterialValue.FromRaw("Value.Ancestor", token));

        Assert.Equal(9, result);
    }

    [Fact]
    public void OriginalThemeIsPassedUnchangedAcrossAliasHops()
    {
        var target = NumberToken("System.Target", TokenLayer.System);
        var alias = NumberToken("Component.Alias", TokenLayer.Component);
        var contribution = new TestContribution("themed", [target, alias])
            .Define(target, Token.Literal(8))
            .Define(alias, Token.Alias(target));
        var requested = new ThemeVariant("HighContrastDark", ThemeVariant.Dark);

        var result = Session(new Border(), requested, contribution)
            .Resolve(MaterialValue.FromRaw("Value.Themed", alias));

        Assert.Equal(8, result);
        Assert.Equal(2, contribution.Queries.Count);
        Assert.All(contribution.Queries, query => Assert.Same(requested, query.Theme));
    }

    [Fact]
    public void CycleReportsStructuredPath()
    {
        var first = NumberToken("System.First", TokenLayer.System);
        var second = NumberToken("System.Second", TokenLayer.System);
        var owner = new Border();
        owner.Resources[first] = Token.Alias(second);
        owner.Resources[second] = Token.Alias(first);

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            Session(owner).Resolve(MaterialValue.FromRaw("Value.Cycle", first)));

        Assert.Equal(MaterialResolutionFailureKind.Cycle, exception.FailureKind);
        Assert.Equal([first.Path, second.Path, first.Path], exception.RawPath.Select(x => x.Path));
    }

    [Fact]
    public void WrongTypeResourceHitDoesNotFallBackToCatalog()
    {
        var token = NumberToken("System.Wrong", TokenLayer.System);
        var owner = new Border();
        owner.Resources[token] = "invalid";
        var contribution = new TestContribution("defaults", [token])
            .Define(token, Token.Literal(12));

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            Session(owner, contribution).Resolve(MaterialValue.FromRaw("Value.Wrong", token)));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Equal(typeof(int), exception.ExpectedType);
        Assert.Equal(typeof(string), exception.ActualType);
    }

    [Fact]
    public void NullResourceHitDoesNotBecomeMiss()
    {
        var token = new MaterialToken<string>("System.Null", TokenLayer.System);
        var owner = new Border();
        owner.Resources[token] = null;

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            Session(owner).Resolve(MaterialValue.FromRaw("Value.Null", token)));

        Assert.Equal(MaterialResolutionFailureKind.NullDefinition, exception.FailureKind);
    }

    [Fact]
    public void RawRecipeCannotReadUndeclaredDependency()
    {
        var declared = NumberToken("Reference.Declared", TokenLayer.Reference);
        var hidden = NumberToken("Reference.Hidden", TokenLayer.Reference);
        var recipe = NumberToken("System.Recipe", TokenLayer.System);
        var contribution = new TestContribution("recipes", [declared, hidden, recipe])
            .Define(declared, Token.Literal(1))
            .Define(hidden, Token.Literal(2))
            .Define(recipe, Token.Recipe(new UndeclaredReadRecipe(declared, hidden)));

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            Session(new Border(), contribution)
                .Resolve(MaterialValue.FromRaw("Value.Undeclared", recipe)));

        Assert.Equal(MaterialResolutionFailureKind.UndeclaredDependency, exception.FailureKind);
    }

    [Fact]
    public void ReverseLayerDependencyFailsFast()
    {
        var component = NumberToken("Component.Downstream", TokenLayer.Component);
        var reference = NumberToken("Reference.Upstream", TokenLayer.Reference);
        var contribution = new TestContribution("layers", [component, reference])
            .Define(component, Token.Literal(1))
            .Define(reference, Token.Alias(component));

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            Session(new Border(), contribution)
                .Resolve(MaterialValue.FromRaw("Value.Layer", reference)));

        Assert.Equal(MaterialResolutionFailureKind.IllegalDependencyDirection, exception.FailureKind);
    }

    [Fact]
    public void MissingPoliciesRemainDistinct()
    {
        var catalogRequired = NumberToken("System.CatalogRequired", TokenLayer.System);
        var sourceRequired = NumberToken("System.SourceRequired", TokenLayer.System);
        var contribution = new TestContribution("missing", [catalogRequired, sourceRequired])
            .WithPolicy(token => ReferenceEquals(token, sourceRequired)
                ? MaterialTokenMissingPolicy.SourceRequired
                : MaterialTokenMissingPolicy.CatalogRequired);
        var session = Session(new Border(), contribution);

        var catalogException = Assert.Throws<MaterialValueResolutionException>(() =>
            session.Resolve(MaterialValue.FromRaw("Value.CatalogMissing", catalogRequired)));
        var sourceException = Assert.Throws<MaterialValueResolutionException>(() =>
            session.Resolve(MaterialValue.FromRaw("Value.SourceMissing", sourceRequired)));

        Assert.Equal(MaterialResolutionFailureKind.MissingCatalogDefinition, catalogException.FailureKind);
        Assert.Equal(MaterialResolutionFailureKind.MissingTokenDefinition, sourceException.FailureKind);
    }

    [Fact]
    public void UnknownExtensionTokenDefaultsToSourceRequired()
    {
        var token = NumberToken("Application.Extension", TokenLayer.Component);

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            Session(new Border()).Resolve(MaterialValue.FromRaw("Value.Extension", token)));

        Assert.Equal(MaterialResolutionFailureKind.MissingTokenDefinition, exception.FailureKind);
    }

    [Fact]
    public void UnexpectedCatalogFailureIsNotMisclassifiedAsAProjectorFailure()
    {
        var token = NumberToken("System.ThrowingCatalog", TokenLayer.System);
        var contribution = new TestContribution(
            "throwing",
            [],
            owns: _ => throw new InvalidOperationException("Injected catalog failure."));
        var key = MaterialValue.FromRaw("Value.ThrowingCatalog", token);

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            Session(new Border(), contribution).Resolve(key));

        Assert.Equal(MaterialResolutionFailureKind.InvalidCatalogContribution, exception.FailureKind);
        Assert.Same(key, exception.ValueKey);
        Assert.Equal([token.Path], exception.RawPath.Select(frame => frame.Path));
        Assert.DoesNotContain("projector", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DuplicateOwnerIsRejectedIndependentlyOfContributionOrder()
    {
        var token = NumberToken("System.Duplicate", TokenLayer.System);
        var first = new TestContribution("first", [token]).Define(token, Token.Literal(1));
        var second = new TestContribution("second", [token]).Define(token, Token.Literal(2));

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            CompositeRawTokenCatalog.Create(first, second));

        Assert.Equal(MaterialResolutionFailureKind.DuplicateCatalogOwner, exception.FailureKind);
        Assert.Equal(["first", "second"], exception.ContributionIds);
    }

    [Fact]
    public void ParameterizedOwnershipIsCheckedAtLookup()
    {
        var token = NumberToken("Reference.Custom.Brand.40", TokenLayer.Reference);
        var contribution = new TestContribution(
                "family",
                [],
                owns: candidate => candidate.Path.StartsWith("Reference.Custom.", StringComparison.Ordinal))
            .Define(token, Token.Literal(40));

        var result = Session(new Border(), contribution)
            .Resolve(MaterialValue.FromRaw("Value.Custom", token));

        Assert.Equal(40, result);
    }

    [Fact]
    public void ContributionRequirementsAreValidatedAtComposition()
    {
        var consumer = new TestContribution("consumer", [])
        {
            Requirements = [new MaterialTokenCatalogRequirement("domain", 2, 3)],
        };
        var domain = new TestContribution("domain", [], schemaVersion: 1);

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            CompositeRawTokenCatalog.Create(consumer, domain));

        Assert.Equal(MaterialResolutionFailureKind.MissingCatalogRequirement, exception.FailureKind);
    }

    [Fact]
    public void InvalidDefaultRequirementReportsStructuredCatalogFailure()
    {
        var consumer = new TestContribution("consumer", [])
        {
            Requirements = [default],
        };

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            CompositeRawTokenCatalog.Create(consumer));

        Assert.Equal(MaterialResolutionFailureKind.MissingCatalogRequirement, exception.FailureKind);
    }

    [Fact]
    public void DuplicateParameterizedFamilyOwnerIsRejectedAtComposition()
    {
        var family = new MaterialTokenFamilyOwnership("Tests.Parameterized.Number");
        var first = new TestContribution("first", []) { DeclaredFamilies = [family] };
        var second = new TestContribution("second", []) { DeclaredFamilies = [family] };

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            CompositeRawTokenCatalog.Create(first, second));

        Assert.Equal(MaterialResolutionFailureKind.DuplicateCatalogOwner, exception.FailureKind);
        Assert.Equal(["first", "second"], exception.ContributionIds);
    }

    [Fact]
    public void CatalogUsesExactInheritedAndDefaultThemeCandidatesPerRawHop()
    {
        var token = NumberToken("System.ThemedFallback", TokenLayer.System);
        var contribution = new ExactThemeContribution(token);
        var custom = new ThemeVariant("HighContrastDark", ThemeVariant.Dark);

        var result = Session(new Border(), custom, contribution)
            .Resolve(MaterialValue.FromRaw("Value.ThemedFallback", token));

        Assert.Equal(91, result);
        Assert.Equal([custom, ThemeVariant.Dark], contribution.Queries);
    }

    [Fact]
    public void NullAndDefaultQueriesUseOnlyTheDefaultCatalogCandidate()
    {
        var token = NumberToken("System.DefaultCandidate", TokenLayer.System);
        var contribution = new ExactThemeContribution(token);

        var fromNull = Session(new Border(), (ThemeVariant?)null, contribution)
            .Resolve(MaterialValue.FromRaw("Value.NullCandidate", token));

        Assert.Equal(42, fromNull);
        Assert.Equal([ThemeVariant.Default], contribution.Queries);

        contribution.Queries.Clear();
        var fromDefault = Session(new Border(), ThemeVariant.Default, contribution)
            .Resolve(MaterialValue.FromRaw("Value.DefaultCandidate", token));

        Assert.Equal(42, fromDefault);
        Assert.Equal([ThemeVariant.Default], contribution.Queries);
    }

    [Fact]
    public void DomainConfigurationFailureKeepsRawPathAndGainsFlatEndpoint()
    {
        var token = NumberToken("Reference.Configured", TokenLayer.Reference);
        var contribution = new TestContribution("configured", [token])
            .Define(token, Token.Recipe(new MissingConfigurationRecipe()));
        var key = MaterialValue.FromRaw("Value.Configured", token);

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            Session(new Border(), contribution).Resolve(key));

        Assert.Equal(MaterialResolutionFailureKind.DomainConfiguration, exception.FailureKind);
        Assert.Same(key, exception.ValueKey);
        Assert.Same(token, exception.Token);
        Assert.Equal("Tests.Configuration", exception.DomainId);
        Assert.Equal("Brand", exception.ConfigurationKey);
        Assert.Equal([token.Path], exception.RawPath.Select(frame => frame.Path));
    }

    [Fact]
    public void StandardValueOperatorsProduceConsumerReadyImmutableValues()
    {
        var color = new MaterialToken<Color>("Component.Color", TokenLayer.Component);
        var opacity = new MaterialToken<double>("Component.Opacity", TokenLayer.Component);
        var left = DoubleToken("Component.Left", TokenLayer.Component);
        var top = DoubleToken("Component.Top", TokenLayer.Component);
        var right = DoubleToken("Component.Right", TokenLayer.Component);
        var bottom = DoubleToken("Component.Bottom", TokenLayer.Component);
        var contribution = new TestContribution(
                "values",
                [color, opacity, left, top, right, bottom])
            .Define(color, Token.Literal(Color.FromArgb(200, 10, 20, 30)))
            .Define(opacity, Token.Literal(0.5d))
            .Define(left, Token.Literal(1d))
            .Define(top, Token.Literal(2d))
            .Define(right, Token.Literal(3d))
            .Define(bottom, Token.Literal(4d));
        var session = Session(new Border(), contribution);

        var brush = session.Resolve(MaterialValue.SolidBrush("Value.Brush", color, opacity));
        var thickness = session.Resolve(MaterialValue.Thickness(
            "Value.Padding", left, top, right, bottom));

        var solid = Assert.IsType<ImmutableSolidColorBrush>(brush);
        Assert.Equal(Color.FromArgb(100, 10, 20, 30), solid.Color);
        Assert.Equal(new Thickness(1, 2, 3, 4), thickness);
    }

    private static MaterialToken<int> NumberToken(string path, TokenLayer layer) => new(path, layer);

    private static MaterialToken<double> DoubleToken(string path, TokenLayer layer) => new(path, layer);

    private static MaterialResolutionSession Session(
        IResourceHost owner,
        params IMaterialTokenCatalogContribution[] contributions) =>
        Session(owner, null, contributions);

    private static MaterialResolutionSession Session(
        IResourceHost owner,
        ThemeVariant? theme,
        params IMaterialTokenCatalogContribution[] contributions) =>
        new(owner, theme, CompositeRawTokenCatalog.Create(contributions));

    private sealed class SumRecipe(
        MaterialToken<int> first,
        MaterialToken<int> second) : ITokenRecipe<int>
    {
        public IReadOnlyList<MaterialToken> Dependencies { get; } = [first, second];

        public int Evaluate(TokenRecipeContext context) => context.Get(first) + context.Get(second);
    }

    private sealed class CountingRecipe : ITokenRecipe<int>
    {
        public int EvaluationCount { get; private set; }

        public IReadOnlyList<MaterialToken> Dependencies => Array.Empty<MaterialToken>();

        public int Evaluate(TokenRecipeContext context)
        {
            EvaluationCount++;
            return 7;
        }
    }

    private sealed class ReadTwiceRecipe(MaterialToken<int> token) : IRawValueRecipe<int>
    {
        public IReadOnlyList<MaterialToken> Dependencies { get; } = [token];

        public int Evaluate(MaterialValueContext context) => context.Get(token) + context.Get(token);
    }

    private sealed class UndeclaredReadRecipe(
        MaterialToken<int> declared,
        MaterialToken<int> hidden) : ITokenRecipe<int>
    {
        public IReadOnlyList<MaterialToken> Dependencies { get; } = [declared];

        public int Evaluate(TokenRecipeContext context) => context.Get(hidden);
    }

    private sealed class TestContribution : IMaterialTokenCatalogContribution
    {
        private readonly Dictionary<MaterialToken, TokenDefinition> _definitions =
            new(ReferenceEqualityComparer.Instance);
        private readonly Func<MaterialToken, bool> _owns;
        private Func<MaterialToken, MaterialTokenMissingPolicy> _missingPolicy =
            static _ => MaterialTokenMissingPolicy.CatalogRequired;

        internal TestContribution(
            string id,
            IReadOnlyCollection<MaterialToken> declaredTokens,
            int schemaVersion = 1,
            Func<MaterialToken, bool>? owns = null)
        {
            Id = id;
            SchemaVersion = schemaVersion;
            DeclaredTokens = declaredTokens;
            var declared = new HashSet<MaterialToken>(declaredTokens, ReferenceEqualityComparer.Instance);
            _owns = owns ?? declared.Contains;
        }

        public string Id { get; }

        public int SchemaVersion { get; }

        public IReadOnlyList<MaterialTokenCatalogRequirement> Requirements { get; init; } =
            Array.Empty<MaterialTokenCatalogRequirement>();

        public IReadOnlyCollection<MaterialToken> DeclaredTokens { get; }

        public IReadOnlyCollection<MaterialTokenFamilyOwnership> DeclaredFamilies { get; init; } =
            Array.Empty<MaterialTokenFamilyOwnership>();

        internal List<(MaterialToken Token, ThemeVariant? Theme)> Queries { get; } = [];

        public bool Owns(MaterialToken token) => _owns(token);

        public MaterialTokenMissingPolicy GetMissingPolicy(MaterialToken token) =>
            _missingPolicy(token);

        public bool TryGetDefinition(
            MaterialToken token,
            ThemeVariant? queryTheme,
            out TokenDefinition? definition)
        {
            Queries.Add((token, queryTheme));
            return _definitions.TryGetValue(token, out definition);
        }

        internal TestContribution Define<T>(MaterialToken<T> token, TokenDefinition<T> definition)
        {
            _definitions[token] = definition;
            return this;
        }

        internal TestContribution WithPolicy(
            Func<MaterialToken, MaterialTokenMissingPolicy> policy)
        {
            _missingPolicy = policy;
            return this;
        }
    }

    private sealed class ExactThemeContribution(MaterialToken<int> token) :
        IMaterialTokenCatalogContribution
    {
        public string Id => "exact-theme";

        public int SchemaVersion => 1;

        public IReadOnlyList<MaterialTokenCatalogRequirement> Requirements => [];

        public IReadOnlyCollection<MaterialToken> DeclaredTokens => [token];

        public List<ThemeVariant?> Queries { get; } = [];

        public bool Owns(MaterialToken candidate) => ReferenceEquals(candidate, token);

        public MaterialTokenMissingPolicy GetMissingPolicy(MaterialToken candidate) =>
            MaterialTokenMissingPolicy.CatalogRequired;

        public bool TryGetDefinition(
            MaterialToken candidate,
            ThemeVariant? queryTheme,
            out TokenDefinition? definition)
        {
            Queries.Add(queryTheme);
            if (queryTheme == ThemeVariant.Dark)
            {
                definition = Token.Literal(91);
                return true;
            }

            if (queryTheme == ThemeVariant.Default)
            {
                definition = Token.Literal(42);
                return true;
            }

            definition = null;
            return false;
        }
    }

    private sealed class MissingConfigurationRecipe : ITokenRecipe<int>
    {
        public IReadOnlyList<MaterialToken> Dependencies => [];

        public int Evaluate(TokenRecipeContext context) => throw new TestConfigurationException();
    }

    private sealed class TestConfigurationException : InvalidOperationException,
        IMaterialTokenConfigurationException
    {
        public string DomainId => "Tests.Configuration";

        public string ConfigurationKey => "Brand";
    }
}
