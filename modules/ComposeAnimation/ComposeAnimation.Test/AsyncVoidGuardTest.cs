using System.Reflection;
using System.Runtime.CompilerServices;

namespace ComposeAnimation.Test;

/// <summary>
/// Guard: an async lambda bound to an <see cref="Action"/> (for example a test body passed to an
/// OnUi(Action) overload) compiles to an async void method, so the test would end before its body
/// runs. No async void method may exist in this assembly.
/// </summary>
public class AsyncVoidGuardTest
{
    [Fact]
    public void TestAssembly_HasNoAsyncVoidMethods()
    {
        var offenders = typeof(AsyncVoidGuardTest).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(m => m.ReturnType == typeof(void) && m.GetCustomAttribute<AsyncStateMachineAttribute>() is not null)
            .Select(m => $"{m.DeclaringType!.FullName}.{m.Name}")
            // Deliberate: this test passes an async void callback to check it is rejected.
            .Where(name => !name.Contains("MoveContent_RejectsAsyncVoidCallbackBeforeItRuns"))
            .ToList();
        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }
}
