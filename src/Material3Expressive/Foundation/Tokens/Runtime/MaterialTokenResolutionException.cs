using System.ComponentModel;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>Thrown when a token graph cannot be resolved safely.</summary>
[Obsolete(
    "Legacy requester-local token resolution exception. Use MaterialValueResolutionException.",
    DiagnosticId = "M3E1002")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MaterialTokenResolutionException : InvalidOperationException
{
    internal MaterialTokenResolutionException(string message)
        : base(message)
    {
    }
}
