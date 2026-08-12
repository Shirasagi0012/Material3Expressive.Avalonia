using Xunit;

namespace Material3Expressive.TokenGenerator.Tests;

public sealed class TokenTableGeneratorTests
{
    [Fact]
    public void TokenSet_EmitsTheRepresentativeRawTableWithoutAThemeAccessor()
    {
        var run = GeneratorTestHost.Run(("Reference.ref.jsonc", """
            {
              // Token comments are deliberately allowed in hand-maintained tables.
              "kind": "referenceTokens",
              "name": "ReferenceTokens",
              "namespace": "Material3Expressive.Foundation.Tokens",
              "provenance": "androidx-token",
              "source": "Material Design token",
              "tokens": {
                "ContainerHeight": { "type": "dimension", "value": 40, },
                "ContainerColor": { "type": "color", "value": "#FF6750A4" }
              },
            }
            """));

        Assert.Empty(run.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        var source = run.Source("ReferenceTokens.Tokens.g.cs");
        Assert.Contains("internal static class ReferenceTokens", source);
        Assert.Contains("ContainerHeight = 40", source);
        Assert.Contains("Color.Parse(\"#FF6750A4\")", source);
        Assert.Contains("public static class ReferenceTokensKeys", source);
        Assert.Contains("MaterialToken<double> ContainerHeight", source);
        Assert.Contains("\"Reference.ReferenceTokens.ContainerHeight\"", source);
        Assert.Contains("TokenLayer.Reference", source);
        Assert.Contains("Token.Literal((double)40)", source);
        Assert.Contains("MaterialToken<global::Avalonia.Media.Color> ContainerColor", source);
        Assert.Contains("Token.Literal(global::Avalonia.Media.Color.Parse(\"#FF6750A4\"))", source);
        Assert.Contains("public sealed class ReferenceTokensOverrides", source);
        Assert.Contains(
            "[global::System.ComponentModel.TypeConverter(typeof(" +
            "global::Material3Expressive.Foundation.Tokens.Runtime.DoubleTokenDefinitionTypeConverter))]",
            source);
        Assert.Contains("TokenDefinition<double>? ContainerHeight", source);
        Assert.Contains("get => GetDefinition(ReferenceTokensKeys.ContainerHeight);", source);
        Assert.Contains(
            "if (value is null) Clear(ReferenceTokensKeys.ContainerHeight); else SetDefinition(ReferenceTokensKeys.ContainerHeight, value);",
            source);
        Assert.Contains("Source: Material Design token", source);
        Assert.Contains("Provenance: androidx-token", source);
        Assert.DoesNotContain(run.Sources, result => result.HintName == "ResolvedTheme.Components.g.cs");
    }

    [Fact]
    public void ComponentTokenSet_ResolvesRawReferencesWithoutGeneratingDefaults()
    {
        var run = GeneratorTestHost.Run(
            ("ButtonSmall.comp.jsonc", """
                {
                  "kind": "componentTokens",
                  "name": "ButtonSmallTokens",
                  "namespace": "Material3Expressive.Foundation.Tokens",
                  "tokens": {
                    "ContainerHeight": { "type": "dimension", "value": 40 },
                    "ContainerColor": { "type": "color", "value": "Primary" }
                  }
                }
                """),
            ("ButtonLarge.comp.jsonc", """
                {
                  "kind": "componentTokens",
                  "name": "ButtonLargeTokens",
                  "namespace": "Material3Expressive.Foundation.Tokens",
                  "tokens": {
                    "ContainerHeight": { "type": "dimension", "value": "@ButtonSmallTokens.ContainerHeight" },
                    "ContainerColor": { "type": "color", "value": "@ButtonSmallTokens.ContainerColor" }
                  }
                }
                """));

        Assert.Empty(run.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        var source = run.Source("ButtonLargeTokens.Tokens.g.cs");
        Assert.Contains("internal static class ButtonLargeTokens", source);
        Assert.Contains("ButtonSmallTokens.ContainerHeight", source);
        Assert.Contains("ButtonSmallTokens.ContainerColor", source);
        Assert.Contains("public static class ButtonLargeTokensKeys", source);
        Assert.Contains("Token.Alias(ButtonSmallTokensKeys.ContainerHeight)", source);
        Assert.Contains("Token.Alias(ButtonSmallTokensKeys.ContainerColor)", source);
        Assert.Contains("TokenLayer.Component", source);
        Assert.DoesNotContain("partial record ButtonLargeTokens", source);
        Assert.Contains("public sealed class ButtonLargeTokensOverrides", source);
        Assert.DoesNotContain(run.Sources, result => result.HintName == "ResolvedTheme.Components.g.cs");
    }

    [Fact]
    public void TokenDescriptors_EmitLiteralAndCustomSystemColors()
    {
        var run = GeneratorTestHost.Run(("Colors.comp.jsonc", """
            {
              "kind": "componentTokens",
              "name": "ColorTokens",
              "namespace": "Material3Expressive.Foundation.Tokens",
              "tokens": {
                "Literal": { "type": "color", "value": "#FF123456" },
                "Custom": { "type": "color", "value": "Custom:brand" }
              }
            }
            """));

        Assert.Empty(run.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        var source = run.Source("ColorTokens.Tokens.g.cs");
        Assert.Contains("Token.Literal(global::Avalonia.Media.Color.Parse(\"#FF123456\"))", source);
        Assert.Contains(
            "[global::System.ComponentModel.TypeConverter(typeof(" +
            "global::Material3Expressive.Foundation.Tokens.Runtime.ColorTokenDefinitionTypeConverter))]",
            source);
        Assert.Contains(
            "Token.Alias(global::Material3Expressive.Foundation.Tokens.Runtime.SystemColorTokens.Get(" +
            "global::MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Custom, \"brand\"))",
            source);
    }

    [Fact]
    public void TokenOverrides_ReuseOneConcreteConverterPerCustomEnumType()
    {
        var run = GeneratorTestHost.Run(("Enums.comp.jsonc", """
            {
              "kind": "componentTokens",
              "name": "EnumTokens",
              "namespace": "Material3Expressive.Foundation.Tokens",
              "tokens": {
                "First": { "type": "enum", "enumType": "Example.Mode", "value": "One" },
                "Second": { "type": "enum", "enumType": "Example.Mode", "value": "Two" },
                "Other": { "type": "enum", "enumType": "Example.OtherMode", "value": "One" }
              }
            }
            """));

        Assert.Empty(run.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        var source = run.Source("EnumTokens.Tokens.g.cs");
        Assert.Equal(
            2,
            source.Split(
                "typeof(EnumTokenDefinitionTypeConverter0)",
                System.StringSplitOptions.None).Length - 1);
        Assert.Equal(
            1,
            source.Split(
                "public sealed class EnumTokenDefinitionTypeConverter0",
                System.StringSplitOptions.None).Length - 1);
        Assert.Contains(
            "TokenDefinitionTypeConverter<global::Example.Mode>",
            source);
        Assert.Contains(
            "public sealed class EnumTokenDefinitionTypeConverter1",
            source);
        Assert.Contains(
            "TokenDefinitionTypeConverter<global::Example.OtherMode>",
            source);
    }

    [Fact]
    public void LegacyDefaultFilesAreIgnored()
    {
        var run = GeneratorTestHost.Run(("Button.default.jsonc", """
            {
              "kind": "componentDefaults",
              "name": "ButtonTokens",
              "properties": { "Size": "dimension" },
              "sets": [
                { "name": "Default", "values": { "Size": 40 } }
              ]
            }
            """));

        Assert.Empty(run.Diagnostics);
        Assert.Empty(run.Sources);
    }

    [Fact]
    public void ComponentDefaultsKindIsRejectedWhenPassedThroughARawSuffix()
    {
        var run = GeneratorTestHost.Run(("Button.comp.jsonc", """
            {
              "kind": "componentDefaults",
              "name": "ButtonTokens",
              "properties": { "Size": "dimension" },
              "sets": [
                { "name": "Default", "values": { "Size": 40 } }
              ]
            }
            """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK001"));
        Assert.Contains("'kind' must be 'referenceTokens', 'systemTokens', or 'componentTokens'", diagnostic.GetMessage());
        Assert.DoesNotContain(run.Sources, source => source.HintName == "ButtonTokens.Tokens.g.cs");
    }

    [Fact]
    public void SystemScheme_IsEmittedAsAPublicSystemRecordWithoutAComponentAccessor()
    {
        var run = GeneratorTestHost.Run(("TypeScale.sys.jsonc", """
            {
              "kind": "systemTokens",
              "name": "TestTypeScale",
              "namespace": "Material3Expressive.Foundation",
              "variantEnum": "TestTypographyRole",
              "properties": { "FontSize": "dimension" },
              "sets": [
                { "name": "Body", "values": { "FontSize": 14 } }
              ]
            }
            """));

        Assert.Empty(run.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        var source = run.Source("TestTypeScale.Tokens.g.cs");
        Assert.Contains("public enum TestTypographyRole", source);
        Assert.Contains("public sealed partial record TestTypeScale", source);
        Assert.Contains("public static TestTypeScale For(TestTypographyRole variant)", source);
        Assert.DoesNotContain(run.Sources, result => result.HintName == "ResolvedTheme.Components.g.cs");
    }

    [Fact]
    public void UnknownTokenSetReferenceProducesACatalogDiagnostic()
    {
        var run = GeneratorTestHost.Run(("Broken.comp.jsonc", """
            {
              "kind": "componentTokens",
              "name": "BrokenTokens",
              "tokens": {
                "Value": { "type": "dimension", "value": "@MissingTokens.Value" }
              }
            }
            """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK003"));
        Assert.Contains("does not name a known token-set member", diagnostic.GetMessage());
        Assert.DoesNotContain(run.Sources, source => source.HintName == "BrokenTokens.Tokens.g.cs");
    }

    [Fact]
    public void ReferenceTypeMismatchProducesACatalogDiagnostic()
    {
        var run = GeneratorTestHost.Run(
            ("Raw.comp.jsonc", """
                {
                  "kind": "componentTokens",
                  "name": "RawTokens",
                  "tokens": { "Value": { "type": "dimension", "value": 40 } }
                }
                """),
            ("Broken.comp.jsonc", """
                {
                  "kind": "componentTokens",
                  "name": "BrokenTokens",
                  "tokens": {
                    "Value": { "type": "color", "value": "@RawTokens.Value" }
                  }
                }
                """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK003"));
        Assert.Contains("is Dimension", diagnostic.GetMessage());
        Assert.Contains("requires Color", diagnostic.GetMessage());
    }

    [Fact]
    public void ReverseLayerReferenceProducesACatalogDiagnostic()
    {
        var run = GeneratorTestHost.Run(
            ("Component.comp.jsonc", """
                {
                  "kind": "componentTokens",
                  "name": "ComponentTokens",
                  "tokens": { "Value": { "type": "dimension", "value": 40 } }
                }
                """),
            ("Reference.ref.jsonc", """
                {
                  "kind": "referenceTokens",
                  "name": "ReferenceTokens",
                  "tokens": { "Value": { "type": "dimension", "value": "@ComponentTokens.Value" } }
                }
                """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK003"));
        Assert.Contains("Reference", diagnostic.GetMessage());
        Assert.Contains("Component", diagnostic.GetMessage());
    }

    [Fact]
    public void ReferenceColorTokenCannotAliasASystemColorRole()
    {
        var run = GeneratorTestHost.Run(("Reference.ref.jsonc", """
            {
              "kind": "referenceTokens",
              "name": "ReferenceTokens",
              "tokens": {
                "Primary": { "type": "color", "value": "Primary" }
              }
            }
            """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK003"));
        Assert.Contains("Reference color token 'Primary'", diagnostic.GetMessage());
        Assert.Contains("literal color", diagnostic.GetMessage());
        Assert.Contains("Reference-layer color token", diagnostic.GetMessage());
        Assert.DoesNotContain(run.Sources, source => source.HintName == "ReferenceTokens.Tokens.g.cs");
    }

    [Fact]
    public void ReferenceColorTokenCanAliasAnotherReferenceColorToken()
    {
        var run = GeneratorTestHost.Run(
            ("Palette.ref.jsonc", """
                {
                  "kind": "referenceTokens",
                  "name": "PaletteTokens",
                  "tokens": {
                    "Primary40": { "type": "color", "value": "#FF6750A4" }
                  }
                }
                """),
            ("Aliases.ref.jsonc", """
                {
                  "kind": "referenceTokens",
                  "name": "AliasTokens",
                  "tokens": {
                    "Brand": { "type": "color", "value": "@PaletteTokens.Primary40" }
                  }
                }
                """));

        Assert.Empty(run.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.Contains(
            "Token.Alias(PaletteTokensKeys.Primary40)",
            run.Source("AliasTokens.Tokens.g.cs"));
    }

    [Fact]
    public void TokenSetReferenceCycleProducesACatalogDiagnostic()
    {
        var run = GeneratorTestHost.Run(
            ("A.comp.jsonc", """
                {
                  "kind": "componentTokens",
                  "name": "ATokens",
                  "tokens": { "Value": { "type": "dimension", "value": "@BTokens.Value" } }
                }
                """),
            ("B.comp.jsonc", """
                {
                  "kind": "componentTokens",
                  "name": "BTokens",
                  "tokens": { "Value": { "type": "dimension", "value": "@ATokens.Value" } }
                }
                """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK003"));
        Assert.Contains("reference cycle", diagnostic.GetMessage());
        Assert.Contains("ATokens.Value", diagnostic.GetMessage());
        Assert.Contains("BTokens.Value", diagnostic.GetMessage());
    }

    [Fact]
    public void DuplicatePropertiesProduceAGeneratorDiagnostic()
    {
        var run = GeneratorTestHost.Run(("Broken.ref.jsonc", """
            {
              "kind": "referenceTokens",
              "name": "BrokenTokens",
              "tokens": {
                "Value": { "type": "dimension", "value": 1 },
                "Value": { "type": "dimension", "value": 2 }
              }
            }
            """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK001"));
        Assert.Contains("declared twice", diagnostic.GetMessage());
        Assert.DoesNotContain(run.Sources, source => source.HintName == "BrokenTokens.Tokens.g.cs");
    }

    [Fact]
    public void AMalformedReferenceProducesAGeneratorDiagnostic()
    {
        var run = GeneratorTestHost.Run(("Broken.comp.jsonc", """
            {
              "kind": "componentTokens",
              "name": "BrokenTokens",
              "tokens": {
                "Value": { "type": "dimension", "value": "@Missing-Tokens.Value" }
              }
            }
            """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK001"));
        Assert.Contains("malformed reference", diagnostic.GetMessage());
    }

    [Fact]
    public void StrictJsonSyntaxErrorsKeepTheirSourceLocation()
    {
        var run = GeneratorTestHost.Run(("Broken.ref.jsonc", """
            {
              "kind": "referenceTokens",
              "name": "BrokenTokens",
              "tokens": {
                "Value": { "type": "dimension", "value": +1 }
              }
            }
            """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK001"));
        Assert.Contains("(5,", diagnostic.GetMessage());
    }

    [Fact]
    public void UnknownProvenanceProducesATableDiagnostic()
    {
        var run = GeneratorTestHost.Run(("Broken.ref.jsonc", """
            {
              "kind": "referenceTokens",
              "name": "BrokenTokens",
              "provenance": "hand-edited",
              "tokens": { "Value": { "type": "dimension", "value": 1 } }
            }
            """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK001"));
        Assert.Contains("'provenance'", diagnostic.GetMessage());
    }

    [Fact]
    public void FilenameSuffixMustMatchRootKind()
    {
        var run = GeneratorTestHost.Run(("Wrong.ref.jsonc", """
            {
              "kind": "componentTokens",
              "name": "WrongTokens",
              "tokens": { "Value": { "type": "dimension", "value": 1 } }
            }
            """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK004"));
        Assert.Contains("componentTokens", diagnostic.GetMessage());
        Assert.Contains("referenceTokens", diagnostic.GetMessage());
        Assert.DoesNotContain(run.Sources, source => source.HintName == "WrongTokens.Tokens.g.cs");
    }

    [Fact]
    public void IncrementalParsingCachesAnUnchangedTable()
    {
        var unchanged = """
            {
              "kind": "referenceTokens",
              "name": "UnchangedTokens",
              "tokens": { "Value": { "type": "dimension", "value": 1 } }
            }
            """;
        var changed = """
            {
              "kind": "referenceTokens",
              "name": "ChangedTokens",
              "tokens": { "Value": { "type": "dimension", "value": 1 } }
            }
            """;

        var result = GeneratorTestHost.RunIncremental(
            new[]
            {
                ("unchanged.ref.jsonc", unchanged),
                ("changed.ref.jsonc", changed)
            },
            ("changed.ref.jsonc", changed.Replace("\"value\": 1", "\"value\": 2", StringComparison.Ordinal)));

        Assert.True(result.After.TrackedSteps.TryGetValue("TokenTable.Parse", out var steps));
        Assert.Contains(steps.SelectMany(step => step.Outputs),
            output => output.Reason == Microsoft.CodeAnalysis.IncrementalStepRunReason.Cached);
        Assert.Contains(steps.SelectMany(step => step.Outputs),
            output => output.Reason is Microsoft.CodeAnalysis.IncrementalStepRunReason.Modified
                or Microsoft.CodeAnalysis.IncrementalStepRunReason.New);

        Assert.True(result.After.TrackedSteps.TryGetValue("TokenTable.Declaration", out var declarations));
        Assert.All(declarations.SelectMany(step => step.Outputs),
            output => Assert.Contains(output.Reason, new[]
            {
                Microsoft.CodeAnalysis.IncrementalStepRunReason.Cached,
                Microsoft.CodeAnalysis.IncrementalStepRunReason.Unchanged
            }));
        Assert.True(result.After.TrackedSteps.TryGetValue("TokenTable.Validation", out var validation));
        Assert.All(validation.SelectMany(step => step.Outputs),
            output => Assert.Contains(output.Reason, new[]
            {
                Microsoft.CodeAnalysis.IncrementalStepRunReason.Cached,
                Microsoft.CodeAnalysis.IncrementalStepRunReason.Unchanged
            }));
        Assert.False(result.After.TrackedSteps.ContainsKey("TokenTable.Shape"));
    }
}
