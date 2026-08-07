using System;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Internal capabilities implemented by controls that consume Material runtime state.</summary>
internal interface IMaterialThemeConsumer
{
    void OnMaterialThemeChanged(MaterialThemeState state);
}

internal interface IMaterialMotionConsumer
{
    void OnMaterialMotionFrame();
}

internal interface IMaterialInteractionConsumer
{
    void OnMaterialInteractionChanged(InteractionState state);
}

internal interface IMaterialInteractionObserver
{
    void OnMaterialInteractionChanged(InteractionState state);
    void OnMaterialInteractionFrame();
}
