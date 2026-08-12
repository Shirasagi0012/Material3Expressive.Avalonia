namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>Thrown when a token graph cannot be resolved safely.</summary>
public sealed class MaterialTokenResolutionException : InvalidOperationException
{
    internal MaterialTokenResolutionException(string message)
        : base(message)
    {
    }
}
