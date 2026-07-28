using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public class ElevationClippingTests
{
    [AvaloniaFact]
    public void ElevationHosts_DoNotClipAtTheirOwnBounds()
    {
        Control[] controls =
        [
            new MaterialButton(),
            new MaterialFab(),
            new MaterialCard(),
            new MaterialNavigationBar(),
            new MaterialSearchBar(),
            new MaterialSnackbar(),
            new MaterialTooltip(),
            new MaterialFabMenu()
        ];

        Assert.All(controls, control => Assert.False(control.ClipToBounds));
    }

    [AvaloniaFact]
    public void FabMenu_ItemsHostDoesNotClipItemShadows()
    {
        var menu = new MaterialFabMenu { IsExpanded = true };
        menu.Items.Add(new MaterialFab { Label = "Item" });
        ThemeTestHelper.ShowWindow(menu);

        var itemsHost = Assert.Single(menu.GetVisualDescendants().OfType<ItemsControl>());

        Assert.False(itemsHost.ClipToBounds);
    }
}
