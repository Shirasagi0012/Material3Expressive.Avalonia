using MaterialSkiaShadow;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>Opt-in capture hook for renderer-level shadow contract cases.</summary>
internal interface IParityShadowProbe
{
    ComposeShadowShape ShadowShape { get; }

    ComposeShadowParameters ShadowParameters { get; }
}
