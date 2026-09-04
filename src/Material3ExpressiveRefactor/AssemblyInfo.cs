using Avalonia.Metadata;

// The shared Material3Expressive xmlns. Co-referencing this assembly and the legacy
// Material3Expressive assembly merges both namespace sets under one URI, which can produce
// type-name collisions — a transitional configuration that is not supported.
[assembly: XmlnsDefinition("https://github.com/Shirasagi0012/Material3Expressive.Avalonia", "Material3ExpressiveRefactor.Tokens")]
[assembly: XmlnsDefinition("https://github.com/Shirasagi0012/Material3Expressive.Avalonia", "Material3ExpressiveRefactor.Baseline")]
[assembly: XmlnsDefinition("https://github.com/Shirasagi0012/Material3Expressive.Avalonia", "Material3ExpressiveRefactor.Controls")]
[assembly: XmlnsDefinition("https://github.com/Shirasagi0012/Material3Expressive.Avalonia", "Material3ExpressiveRefactor.Styles")]
[assembly: XmlnsDefinition("https://github.com/Shirasagi0012/Material3Expressive.Avalonia", "Material3ExpressiveRefactor.Foundation")]
[assembly: XmlnsDefinition("https://github.com/Shirasagi0012/Material3Expressive.Avalonia", "Material3ExpressiveRefactor.Foundation.Tokens")]
[assembly: XmlnsDefinition("https://github.com/Shirasagi0012/Material3Expressive.Avalonia", "Material3ExpressiveRefactor.Foundation.Rendering")]
[assembly: XmlnsDefinition("https://github.com/Shirasagi0012/Material3Expressive.Avalonia", "Material3ExpressiveRefactor.Foundation.Interaction")]
