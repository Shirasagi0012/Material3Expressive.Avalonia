using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting;
using Material3Expressive.VisualTesting.Cases;
using Material3Expressive.VisualTesting.Model;
using Xunit;

namespace Material3Expressive.VisualTests;

/// <summary>Guards the boundary between discovery-time case metadata and UI-thread visual creation.</summary>
public sealed class VisualCaseFactoryTests
{
    [AvaloniaFact]
    public void EveryCaseCreatesAnIndependentAvaloniaTree()
    {
        foreach (var visualCase in VisualCaseCatalog.All)
        {
            var context = new VisualCaseContext(visualCase.Theme, FontEnvironment.Roboto);
            var first = visualCase.CreateContent(context);
            var second = visualCase.CreateContent(context);

            Assert.NotSame(first, second);

            var firstTree = first
                .GetLogicalDescendants()
                .Prepend(first)
                .OfType<AvaloniaObject>()
                .ToHashSet(ReferenceEqualityComparer.Instance);
            var shared = second
                .GetLogicalDescendants()
                .Prepend(second)
                .OfType<AvaloniaObject>()
                .Where(firstTree.Contains)
                .ToArray();

            Assert.True(
                shared.Length == 0,
                $"Visual case '{visualCase.Id}' reuses Avalonia objects across CreateContent calls: " +
                string.Join(", ", shared.Select(item => item.GetType().Name)));
        }
    }
}
