using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Styles;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class ComponentTokenCoverageTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void MaterialTheme_ResolvesEveryComponentTokenToItsDeclaredValueType(bool dark)
    {
        var window = new Window();
        window.Styles.Add(new MaterialTheme());
        var variant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        try
        {
            foreach (var key in TokenScan.KeysUnder(typeof(MaterialTokens.Comp)))
            {
                Assert.True(TokenResolver.TryResolve(window, key, variant, out var value), $"Unresolved component token: {key.DebugName}");
                Assert.True(value is not null && key.ValueType.IsInstanceOfType(value),
                    $"{key.DebugName}: resolved {value?.GetType().FullName ?? "null"}, expected {key.ValueType.FullName}");
            }
        }
        finally { window.Close(); }
    }
}
