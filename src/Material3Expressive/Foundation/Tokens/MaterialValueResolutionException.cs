using Avalonia.Styling;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>Identifies a structured Material value resolution failure.</summary>
public enum MaterialResolutionFailureKind
{
    NullDefinition,
    WrongType,
    IllegalDependencyDirection,
    UndeclaredDependency,
    Cycle,
    MissingCatalogDefinition,
    MissingTokenDefinition,
    DuplicateCatalogOwner,
    DuplicateCatalogContribution,
    MissingCatalogRequirement,
    InvalidCatalogContribution,
    NullValue,
    RecipeFailure,
    ProjectorFailure,
    DomainConfiguration,
    UnownedPlacement,
}

/// <summary>Describes one raw-token frame in a failed Material value resolution.</summary>
public sealed record MaterialResolutionFrame(
    string Path,
    TokenLayer Layer,
    Type ExpectedType);

/// <summary>Thrown when a recognized Material value cannot be resolved safely.</summary>
public sealed class MaterialValueResolutionException : InvalidOperationException
{
    internal MaterialValueResolutionException(
        MaterialResolutionFailureKind failureKind,
        string message,
        MaterialValueKey? valueKey = null,
        MaterialToken? token = null,
        ThemeVariant? requestedTheme = null,
        IReadOnlyList<MaterialResolutionFrame>? rawPath = null,
        Type? expectedType = null,
        Type? actualType = null,
        IReadOnlyList<string>? contributionIds = null,
        Exception? innerException = null,
        string? domainId = null,
        string? configurationKey = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        ValueKey = valueKey;
        Token = token;
        RequestedTheme = requestedTheme;
        RawPath = rawPath ?? Array.Empty<MaterialResolutionFrame>();
        ExpectedType = expectedType;
        ActualType = actualType;
        ContributionIds = contributionIds ?? Array.Empty<string>();
        DomainId = domainId;
        ConfigurationKey = configurationKey;
    }

    public MaterialResolutionFailureKind FailureKind { get; }

    public MaterialValueKey? ValueKey { get; }

    public MaterialToken? Token { get; }

    public ThemeVariant? RequestedTheme { get; }

    public IReadOnlyList<MaterialResolutionFrame> RawPath { get; }

    public Type? ExpectedType { get; }

    public Type? ActualType { get; }

    public IReadOnlyList<string> ContributionIds { get; }

    public string? DomainId { get; }

    public string? ConfigurationKey { get; }
}

internal static class MaterialResolutionFailures
{
    internal static MaterialValueResolutionException Create(
        MaterialResolutionFailureKind kind,
        string message,
        MaterialValueKey? valueKey = null,
        MaterialToken? token = null,
        ThemeVariant? theme = null,
        IReadOnlyList<MaterialResolutionFrame>? path = null,
        Type? expected = null,
        Type? actual = null,
        IReadOnlyList<string>? contributionIds = null,
        Exception? inner = null,
        string? domainId = null,
        string? configurationKey = null) =>
        new(
            kind,
            message,
            valueKey,
            token,
            theme,
            path,
            expected,
            actual,
            contributionIds,
            inner,
            domainId,
            configurationKey);

    internal static MaterialValueResolutionException AttachValueKey(
        MaterialValueResolutionException exception,
        MaterialValueKey key)
    {
        if (exception.ValueKey is not null)
            return exception;

        return new MaterialValueResolutionException(
            exception.FailureKind,
            exception.Message,
            key,
            exception.Token,
            exception.RequestedTheme,
            exception.RawPath,
            exception.ExpectedType,
            exception.ActualType,
            exception.ContributionIds,
            exception,
            exception.DomainId,
            exception.ConfigurationKey);
    }

    internal static MaterialValueResolutionException AttachRawPath(
        MaterialValueResolutionException exception,
        MaterialToken token,
        ThemeVariant? theme,
        IReadOnlyList<MaterialResolutionFrame> path)
    {
        if (exception.RawPath.Count == path.Count &&
            exception.RawPath.Select(static frame => frame.Path)
                .SequenceEqual(path.Select(static frame => frame.Path), StringComparer.Ordinal))
        {
            return exception;
        }

        return new MaterialValueResolutionException(
            exception.FailureKind,
            exception.Message,
            exception.ValueKey,
            exception.Token ?? token,
            exception.RequestedTheme ?? theme,
            path,
            exception.ExpectedType,
            exception.ActualType,
            exception.ContributionIds,
            exception,
            exception.DomainId,
            exception.ConfigurationKey);
    }
}
