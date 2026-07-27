using Avalonia.Headless;
using Material3Expressive.VisualTesting.Rendering;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(VisualTestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
