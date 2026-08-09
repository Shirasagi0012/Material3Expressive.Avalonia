using Material3Expressive.Controls;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class ComponentApiOwnershipTests
{
    [Fact]
    public void ComponentAxesAreOwnedByTheControlsNamespace()
    {
        const string controlsNamespace = "Material3Expressive.Controls";

        Assert.Equal(controlsNamespace, typeof(ButtonVariant).Namespace);
        Assert.Equal(controlsNamespace, typeof(ButtonSize).Namespace);
        Assert.Equal(controlsNamespace, typeof(ButtonGroupVariant).Namespace);
        Assert.Equal(controlsNamespace, typeof(CardVariant).Namespace);
        Assert.Equal(controlsNamespace, typeof(FabVariant).Namespace);
        Assert.Equal(controlsNamespace, typeof(FabSize).Namespace);
        Assert.Equal(controlsNamespace, typeof(IconButtonVariant).Namespace);
        Assert.Equal(controlsNamespace, typeof(IconButtonSize).Namespace);
        Assert.Equal(controlsNamespace, typeof(IconButtonWidth).Namespace);
    }
}
