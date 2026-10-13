using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ComposeAnimation.Test;

public class LookaheadBoundaryInheritanceTest
{

    [Fact]
    public Task ContentHost_CutsLookaheadContextButPreservesOrdinaryInheritance() => OnUi(() =>
    {
        var child = new Border();
        var boundary = new LookaheadBoundary { Child = child };
        var root = new LookaheadRoot
        {
            DataContext = "outer data",
            FlowDirection = FlowDirection.RightToLeft,
            Child = boundary
        };

        var contentHost = Assert.IsAssignableFrom<Control>(child.GetVisualParent());
        Assert.Same(boundary, child.Parent);
        Assert.Same(boundary, contentHost.GetVisualParent());
        Assert.Same(root.Context, LookaheadEnvironment.GetContext(boundary));
        Assert.Null(LookaheadEnvironment.GetContext(contentHost));
        Assert.Null(LookaheadEnvironment.GetContext(child));
        Assert.Equal("outer data", child.DataContext);
        Assert.Equal(FlowDirection.RightToLeft, child.FlowDirection);

        root.DataContext = "updated data";
        root.FlowDirection = FlowDirection.LeftToRight;
        Assert.Equal("updated data", child.DataContext);
        Assert.Equal(FlowDirection.LeftToRight, child.FlowDirection);

        contentHost.ClearValue(LookaheadEnvironment.ContextProperty);
        Assert.Same(root.Context, LookaheadEnvironment.GetContext(contentHost));
        Assert.Same(root.Context, LookaheadEnvironment.GetContext(child));

        LookaheadEnvironment.SetContext(contentHost, null);
        Assert.Null(LookaheadEnvironment.GetContext(contentHost));
        Assert.Null(LookaheadEnvironment.GetContext(child));
    });

    [Fact]
    public Task ReplacingAndRemovingChild_RestoresOldParentageAndConnectsNewChild() => OnUi(() =>
    {
        var first = new Border();
        var second = new TextBlock();
        var boundary = new LookaheadBoundary
        {
            DataContext = "boundary data",
            FlowDirection = FlowDirection.RightToLeft,
            Child = first
        };
        var contentHost = Assert.IsAssignableFrom<Control>(first.GetVisualParent());

        boundary.Child = second;

        Assert.Null(first.Parent);
        Assert.Null(first.GetVisualParent());
        Assert.Null(first.DataContext);
        Assert.Equal(FlowDirection.LeftToRight, first.FlowDirection);
        Assert.Same(boundary, second.Parent);
        Assert.Same(contentHost, second.GetVisualParent());
        Assert.Equal("boundary data", second.DataContext);
        Assert.Equal(FlowDirection.RightToLeft, second.FlowDirection);
        Assert.Null(LookaheadEnvironment.GetContext(second));

        boundary.Child = null;

        Assert.Null(second.Parent);
        Assert.Null(second.GetVisualParent());
        Assert.Null(second.DataContext);
        Assert.Equal(FlowDirection.LeftToRight, second.FlowDirection);
    });

    [Fact]
    public Task DynamicFontDataAndResources_FlowThroughHost_AndLocalValuesSurviveReuse() => OnUi(() =>
    {
        const string inheritedResource = "inherited-resource";
        const string localResource = "local-resource";
        var inherited = new TextBlock { Text = "inherited" };
        var local = new TextBlock
        {
            Text = "local",
            DataContext = "local data",
            FontSize = 31
        };
        local.Resources[localResource] = "local value";
        var boundary = new LookaheadBoundary { Child = inherited };
        var root = new LookaheadRoot
        {
            DataContext = "outer data",
            Child = boundary
        };
        root.SetValue(TextElement.FontSizeProperty, 18d);
        root.Resources[inheritedResource] = "first value";

        Assert.Equal("outer data", inherited.DataContext);
        Assert.Equal(18, inherited.FontSize);
        Assert.Equal("first value", inherited.FindResource(inheritedResource));

        root.DataContext = "updated data";
        root.SetValue(TextElement.FontSizeProperty, 22d);
        root.Resources[inheritedResource] = "second value";
        Assert.Equal("updated data", inherited.DataContext);
        Assert.Equal(22, inherited.FontSize);
        Assert.Equal("second value", inherited.FindResource(inheritedResource));

        boundary.Child = local;
        Assert.Null(inherited.Parent);
        Assert.False(inherited.TryFindResource(inheritedResource, out _));
        Assert.Equal("local data", local.DataContext);
        Assert.Equal(31, local.FontSize);
        Assert.Equal("local value", local.FindResource(localResource));
        Assert.Equal("second value", local.FindResource(inheritedResource));
        Assert.Null(LookaheadEnvironment.GetContext(local));

        boundary.Child = inherited;
        Assert.Null(local.Parent);
        Assert.Null(local.GetVisualParent());
        Assert.Equal("local data", local.DataContext);
        Assert.Equal(31, local.FontSize);
        Assert.Equal("local value", local.FindResource(localResource));
        Assert.False(local.TryFindResource(inheritedResource, out _));
        Assert.Equal("updated data", inherited.DataContext);
        Assert.Equal(22, inherited.FontSize);
        Assert.Equal("second value", inherited.FindResource(inheritedResource));
        Assert.Null(LookaheadEnvironment.GetContext(inherited));
    });
}
