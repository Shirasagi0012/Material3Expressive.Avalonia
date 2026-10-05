using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace MaterialShapes.Markups;

public class MaterialShapeGeometryExtension : AvaloniaObject
{
    public MaterialShapeGeometryExtension(BindingBase shape) : this()
    {
        this[!ShapeProperty] = shape;
    }

    public MaterialShapeGeometryExtension(RoundedPolygon shape) : this()
    {
        Shape = shape;
    }

    private MaterialShapeGeometryExtension()
    {
        ShapeProperty.Changed.AddClassHandler<MaterialShapeGeometryExtension>((x, args) =>
        {
            if (args.NewValue is RoundedPolygon p)
                x.Geometry = new(p);
        });
    }
    
    public static readonly StyledProperty<RoundedPolygon> ShapeProperty =
        AvaloniaProperty.Register<MaterialShapeGeometryExtension, RoundedPolygon>(nameof(Shape));

    private static readonly StyledProperty<MaterialShapeGeometry> GeometryProperty =
        AvaloniaProperty.Register<MaterialShapeGeometryExtension, MaterialShapeGeometry>(nameof(Geometry));
    
    public RoundedPolygon Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    private MaterialShapeGeometry Geometry
    {
        get => GetValue(GeometryProperty);
        set => SetValue(GeometryProperty, value);
    }
    
    public BindingBase ProvideValue(IServiceProvider provider)
    {
        return this[!GeometryProperty];
    }

    private class MaterialShapeGeometry : StreamGeometry
    {
        public static readonly StyledProperty<RoundedPolygon?> ShapeProperty =
            AvaloniaProperty.Register<MaterialShapeGeometry, RoundedPolygon?>(nameof(Shape));

        static MaterialShapeGeometry()
        {
            AffectsGeometry(ShapeProperty);
            ShapeProperty.Changed.AddClassHandler<MaterialShapeGeometry>((x, _) => x.UpdateGeometry());
        }

        public RoundedPolygon? Shape
        {
            get => GetValue(ShapeProperty);
            set => SetValue(ShapeProperty, value);
        }

        internal MaterialShapeGeometry(RoundedPolygon? polygon)
        {
            Shape = polygon;
        }

        private void UpdateGeometry()
        {
            var shape = Shape;
            using var context = Open();
            if (shape != null)
                context.DrawRoundedPolygon(shape);
        }

        public override Geometry Clone()
        {
            var clone = new MaterialShapeGeometry(Shape)
            {
                Transform = Transform
            };
            return clone;
        }
    }
}
