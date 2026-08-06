using Xunit;

namespace Material3Expressive.TokenGenerator.Tests;

public sealed class TokenTableGeneratorTests
{
    [Fact]
    public void TokenSet_EmitsTheRepresentativeTableAndThemeAccessor()
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
                "ContainerColor": { "type": "color", "value": "Primary" }
              },
            }
            """));

        Assert.Empty(run.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        var source = run.Source("ReferenceTokens.Tokens.g.cs");
        Assert.Contains("internal static class ReferenceTokens", source);
        Assert.Contains("ContainerHeight = 40", source);
        Assert.Contains("SysColorToken.Primary", source);
        Assert.Contains("Source: Material Design token", source);
        Assert.Contains("Provenance: androidx-token", source);

        Assert.Contains(
            run.Sources,
            result => result.HintName == "ResolvedTheme.Components.g.cs");
    }

    [Fact]
    public void Component_ResolvesReferencesAndExtendsWithoutCopyingValues()
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
            ("Button.default.jsonc", """
                {
                  "kind": "componentDefaults",
                  "name": "ButtonTokens",
                  "namespace": "Material3Expressive.Foundation",
                  "variantEnum": "ButtonVariant",
                  "properties": {
                    "ContainerHeight": "dimension",
                    "ContainerColor": "color",
                    "Enabled": "bool"
                  },
                  "sets": [
                    {
                      "name": "Filled",
                      "values": {
                        "ContainerHeight": "@ButtonSmallTokens.ContainerHeight",
                        "ContainerColor": "@ButtonSmallTokens.ContainerColor",
                        "Enabled": true
                      }
                    },
                    {
                      "name": "Tonal",
                      "extends": "Filled",
                      "values": { "ContainerColor": "SecondaryContainer" }
                    }
                  ]
                }
                """));

        Assert.Empty(run.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        var source = run.Source("ButtonTokens.Tokens.g.cs");
        Assert.Contains("ButtonSmallTokens.ContainerHeight", source);
        Assert.Contains("ButtonSmallTokens.ContainerColor", source);
        Assert.Contains("SysColorToken.SecondaryContainer", source);
        Assert.Contains("true);", source);
        Assert.DoesNotContain("ContainerHeight = 40", source);
        Assert.Contains("public enum ButtonVariant", source);
        Assert.Contains("internal sealed partial record ButtonTokens", source);
        Assert.Contains("public sealed partial class ButtonOverrides", source);
        Assert.Contains("internal ButtonTokens ApplyTo(ButtonTokens baseTokens)", source);

        var accessors = run.Source("ResolvedTheme.Components.g.cs");
        Assert.Contains("internal global::Material3Expressive.Foundation.ButtonTokens GetButtonTokens", accessors);
        Assert.DoesNotContain("public global::Material3Expressive.Foundation.ButtonTokens GetButtonTokens", accessors);
    }

    [Fact]
    public void SystemScheme_IsEmittedButDoesNotBecomeAComponentAccessor()
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
        Assert.Contains("record TestTypeScale", run.Source("TestTypeScale.Tokens.g.cs"));
        Assert.DoesNotContain("GetTestTypeScale", run.Source("ResolvedTheme.Components.g.cs"));
    }

    [Fact]
    public void UnknownTokenSetReferenceProducesACatalogDiagnostic()
    {
        var run = GeneratorTestHost.Run(("Broken.default.jsonc", """
            {
              "kind": "componentDefaults",
              "name": "BrokenTokens",
              "properties": { "Value": "dimension" },
              "sets": [
                { "name": "Default", "values": { "Value": "@MissingTokens.Value" } }
              ]
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
            ("Broken.default.jsonc", """
                {
                  "kind": "componentDefaults",
                  "name": "BrokenTokens",
                  "properties": { "Value": "color" },
                  "sets": [
                    { "name": "Default", "values": { "Value": "@RawTokens.Value" } }
                  ]
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
        var run = GeneratorTestHost.Run(("Broken.default.jsonc", """
            {
              "kind": "componentDefaults",
              "name": "BrokenTokens",
              "properties": { "Value": "dimension" },
              "sets": [
                {
                  "name": "Default",
                  "values": { "Value": "@Missing-Tokens.Value" }
                }
              ]
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
        var run = GeneratorTestHost.Run(("Wrong.default.jsonc", """
            {
              "kind": "componentTokens",
              "name": "WrongTokens",
              "tokens": { "Value": { "type": "dimension", "value": 1 } }
            }
            """));

        var diagnostic = Assert.Single(run.Diagnostics.Where(d => d.Id == "M3ETOK004"));
        Assert.Contains("componentTokens", diagnostic.GetMessage());
        Assert.Contains("componentDefaults", diagnostic.GetMessage());
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
        Assert.True(result.After.TrackedSteps.TryGetValue("TokenTable.Shape", out var shapes));
        Assert.All(shapes.SelectMany(step => step.Outputs),
            output => Assert.Contains(output.Reason, new[]
            {
                Microsoft.CodeAnalysis.IncrementalStepRunReason.Cached,
                Microsoft.CodeAnalysis.IncrementalStepRunReason.Unchanged
            }));
    }
}
