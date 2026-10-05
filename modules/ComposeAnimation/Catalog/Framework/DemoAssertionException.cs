namespace Catalog.Framework;

/// <summary>demo 内 <see cref="DemoControlBase.Assert"/> 失败时抛出的异常。</summary>
public sealed class DemoAssertionException(string message) : Exception(message);
