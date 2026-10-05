using System.Reflection;

namespace Catalog.Framework;

/// <summary>一个目录页面（左侧导航项）。</summary>
public sealed record CatalogPage(string Title, IReadOnlyList<DemoDescriptor> Demos);

/// <summary>一个 demo 的元数据。</summary>
public sealed record DemoDescriptor(
    string Id,
    string Title,
    string Page,
    int Order,
    bool Experimental,
    Type Type,
    IReadOnlyList<DemoActionDescriptor> Actions);

/// <summary>一个 demo 动作的元数据。</summary>
public sealed record DemoActionDescriptor(string Name, string? Description, MethodInfo Method);

/// <summary>扫描程序集中带 <see cref="DemoAttribute"/> 的 demo 控件并构建目录。</summary>
public static class DemoRegistry
{
    private const string DefaultPage = "未分组";

    private static readonly Lazy<Registry> _registry = new(Build);

    public static IReadOnlyList<CatalogPage> Pages => _registry.Value.Pages;

    public static DemoDescriptor? Find(string id)
        => _registry.Value.ById.TryGetValue(id, out var descriptor) ? descriptor : null;

    /// <summary>用 <see cref="DemoContext"/> 创建 demo 实例。</summary>
    public static DemoControlBase Create(DemoDescriptor descriptor, DemoContext context)
    {
        try
        {
            return (DemoControlBase)Activator.CreateInstance(descriptor.Type, context)!;
        }
        catch (MissingMethodException error)
        {
            throw new InvalidOperationException(
                $"Demo 类型 {descriptor.Type.FullName} 需要 public 构造函数 {descriptor.Type.Name}(DemoContext)。", error);
        }
    }

    private static Registry Build()
    {
        var descriptors = new List<(DemoDescriptor Descriptor, int PageOrder)>();

        foreach (var type in typeof(DemoControlBase).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(DemoControlBase).IsAssignableFrom(type)) continue;
            var demoAttribute = type.GetCustomAttribute<DemoAttribute>();
            if (demoAttribute is null) continue;

            var actions = ScanActions(type);
            var descriptor = new DemoDescriptor(
                demoAttribute.Id,
                demoAttribute.Title ?? demoAttribute.Id,
                demoAttribute.Page ?? DefaultPage,
                demoAttribute.Order,
                demoAttribute.Experimental,
                type,
                actions);
            descriptors.Add((descriptor, demoAttribute.PageOrder));
        }

        var pages = descriptors
            .GroupBy(entry => entry.Descriptor.Page)
            .OrderBy(group => group.Min(entry => entry.PageOrder))
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new CatalogPage(
                group.Key,
                group.Select(entry => entry.Descriptor)
                    .OrderBy(demo => demo.Order)
                    .ThenBy(demo => demo.Id, StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();

        var byId = new Dictionary<string, DemoDescriptor>(StringComparer.Ordinal);
        foreach (var (descriptor, _) in descriptors)
        {
            if (!byId.TryAdd(descriptor.Id, descriptor))
                throw new InvalidOperationException($"重复的 demo id: \"{descriptor.Id}\"。");
        }

        return new Registry(pages, byId);
    }

    private static DemoActionDescriptor[] ScanActions(Type type)
    {
        var actions = new List<DemoActionDescriptor>();
        foreach (var method in type.GetMethods(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            var attribute = method.GetCustomAttribute<DemoActionAttribute>();
            if (attribute is null) continue;

            var location = $"{type.FullName}.{method.Name}";
            if (method.IsStatic)
                throw new InvalidOperationException($"[DemoAction] 方法 {location} 必须是实例方法。");
            if (!method.IsPublic)
                throw new InvalidOperationException($"[DemoAction] 方法 {location} 必须是 public。");
            if (method.IsGenericMethodDefinition)
                throw new InvalidOperationException($"[DemoAction] 方法 {location} 不能是泛型方法。");
            if (method.GetParameters().Length != 0)
                throw new InvalidOperationException($"[DemoAction] 方法 {location} 必须无参数。");
            if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
                throw new InvalidOperationException($"[DemoAction] 方法 {location} 必须返回 void 或 Task。");

            actions.Add(new DemoActionDescriptor(method.Name, attribute.Description, method));
        }
        return actions.OrderBy(action => action.Method.MetadataToken).ToArray();
    }

    private sealed record Registry(IReadOnlyList<CatalogPage> Pages, IReadOnlyDictionary<string, DemoDescriptor> ById);
}
