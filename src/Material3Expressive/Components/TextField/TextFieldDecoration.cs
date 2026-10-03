using Avalonia.Controls;
using Avalonia.Media;
namespace Material3Expressive.Components;

// Kept separate because Avalonia Panel seals Render. Shares its parent's coordinate space.
internal sealed class TextFieldDecoration : Control
{
    public override void Render(DrawingContext context) => (Parent as TextFieldLayout)?.Draw(context);
}
