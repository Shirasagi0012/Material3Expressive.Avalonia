namespace Catalog.Framework;

/// <summary>demo 宿主环境：推进帧、截图、记录断言。</summary>
internal interface IDemoEnvironment
{
    Task AdvanceAsync(DemoControlBase demo, int frames);

    Task CaptureAsync(DemoControlBase demo, string name);

    void RecordAssertion(bool success, string message);
}

/// <summary>一条断言记录。</summary>
internal sealed record AssertionRecord(bool Success, string Message);
