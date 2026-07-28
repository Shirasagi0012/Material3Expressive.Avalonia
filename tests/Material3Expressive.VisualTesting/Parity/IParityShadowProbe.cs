using MaterialSkiaShadow;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>Opt-in capture hook for renderer-level shadow contract cases.</summary>
internal interface IParityShadowProbe
{
    ComposeShadowShape ShadowShape { get; }

    ComposeShadowParameters ShadowParameters { get; }

    /// <summary>Whether an occluder visual is painted over the shadow mesh.</summary>
    bool OccluderDrawn { get; }
}
