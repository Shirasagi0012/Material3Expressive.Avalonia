using Avalonia;
using Avalonia.Media.Fonts;

namespace MaterialTypography.Roboto;

public class RobotoFontCollection : EmbeddedFontCollection
{
    public const string CollectionUri = "fonts:Roboto";
    public const string FamilyUri = "fonts:Roboto#Roboto";
    
    public RobotoFontCollection() : base(new Uri(CollectionUri, UriKind.Absolute), new Uri("avares://MaterialTypography.Roboto/Assets", UriKind.Absolute))
    {
        
    }
}

public static class AppBuilderExtensions
{
    public static AppBuilder WithRobotoFont(this AppBuilder builder)
        => builder.ConfigureFonts(manager =>
        {
            manager.AddFontCollection(new RobotoFontCollection());
        });
}
