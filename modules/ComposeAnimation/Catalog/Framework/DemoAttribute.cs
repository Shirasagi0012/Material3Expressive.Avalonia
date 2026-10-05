namespace Catalog.Framework;

/// <summary>标记一个 demo 控件（继承 <see cref="DemoControlBase"/> 的非抽象类）。</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DemoAttribute(string id) : Attribute
{
    /// <summary>稳定标识，如 "properties/basic"。</summary>
    public string Id { get; } = id;

    /// <summary>显示名；缺省用 <see cref="Id"/>。</summary>
    public string? Title { get; init; }

    /// <summary>所属页面名；缺省 "未分组"。</summary>
    public string? Page { get; init; }

    /// <summary>页面排序，默认 0。</summary>
    public int PageOrder { get; init; }

    /// <summary>页内排序，默认 0。</summary>
    public int Order { get; init; }

    /// <summary>是否为实验性功能的 demo；为 true 时外壳在标题旁展示「实验性」徽章。</summary>
    public bool Experimental { get; init; }
}

/// <summary>标记一个可自动化执行的 demo 动作。方法须为 public instance、无参、返回 Task 或 void。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DemoActionAttribute(string? description = null) : Attribute
{
    public string? Description { get; } = description;
}
