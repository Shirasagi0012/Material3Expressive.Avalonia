using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using ComposeAnimation.TestSupport;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record ListItemConfig(bool Segmented = false, string Mode = "none", int Lines = 1,
    bool Selected = false, bool Enabled = true, bool Dark = false, bool Rtl = false,
    int Index = 0, int Count = 1, double Width = 280, double? Height = null,
    double Leading = 24, bool Sides = true, bool Unbounded = false, string Alignment = "auto",
    bool CustomPadding = false, string Action = "none", string Path = "none");

[ParitySuite("list-item")]
public sealed class ListItemCases
{
    static CaseData<ListItemConfig> Data(string id, ListItemConfig c) => new(id.ToLowerInvariant(), id.Replace('.', ' '), c);
    public static IEnumerable<CaseData<ListItemConfig>> StaticConfigurations()
    {
        foreach (var path in new[] { "cut", "mixed", "generic" })
        foreach (var segmented in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var selected in new[] { false, true })
            yield return Data($"static.path.{path}.{segmented}.{rtl}.{selected}", new(Segmented: segmented, Mode: "multiple", Selected: selected, Rtl: rtl, Path: path));
        foreach (var segmented in new[] { false, true })
        foreach (var mode in new[] { "none", "click", "single", "multiple" })
        foreach (var lines in new[] { 1, 2, 3 })
            yield return Data($"static.appearance.{(segmented ? "segmented" : "standard")}.{mode}.{lines}", new(segmented, mode, lines));
        // Colors are shared by both variants and all line layouts; selection only belongs to selectable modes.
        foreach (var dark in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var mode in new[] { "none", "click", "single", "multiple" })
            yield return Data($"static.colors.{mode}.{enabled}.{dark}", new(Mode: mode, Lines: 3, Selected: true, Enabled: enabled, Dark: dark));
        foreach (var index in new[] { 0, 1, 2 })
            yield return Data($"static.group.{index}", new(true, Index: index, Count: 3));
        foreach (var (id, config) in new (string, ListItemConfig)[] {
            ("rtl", new(Rtl:true, Lines:3)), ("large-leading", new(Leading:80)),
            ("no-sides", new(Sides:false)), ("unbounded",new(Unbounded:true)),
            ("narrow",new(Width:80,Lines:3)), ("short",new(Height:32,Lines:3)),
            ("padding",new(CustomPadding:true)), ("tall-auto",new(Height:100)),
            ("tall-center",new(Height:100,Alignment:"center")), ("tall-bottom",new(Height:100,Alignment:"bottom")) })
            yield return Data($"static.layout.{id}", config);
    }
    public static IEnumerable<CaseData<ListItemConfig>> DynamicConfigurations()
    {
        foreach (var path in new[] { "cut", "mixed", "generic" })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "press", "quick", "select", "drag", "path-change", "path-resize", "space" })
            yield return Data($"dynamic.path.{path}.{rtl}.{action}", new(Segmented: true, Mode: "multiple", Rtl: rtl, Path: path, Action: action));
        foreach (var action in new[] { "press", "quick", "cancel", "disable", "select", "drag", "theme", "group" })
            yield return Data($"dynamic.{action}", new(Segmented:action=="group", Mode:action=="select"?"multiple":"click", Lines:3, Action:action));
        foreach(var action in new[] { "space", "enter", "long", "key-long", "nested", "disabled-input", "passive-input", "single-click", "multiple-click", "shapes" })
            yield return Data($"dynamic.{action}",new(Mode:action=="passive-input"?"none":action=="single-click"?"single":action=="multiple-click"?"multiple":"click",Enabled:action!="disabled-input",Action:action));
    }
    [ParityScenario("static", Data=nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 320, 200);
        var scene = new Scene(host, writer.Metadata.ReadConfig<ListItemConfig>());
        host.Mount(scene.Root); host.Draw(); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }
    [ParityScenario("behavior", Data=nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ListItemConfig>();
        await using var host = new AvaloniaHost(writer, 320, 200, new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess));
        var scene = new Scene(host, config);
        host.Mount(scene.Root); host.Draw(); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id,id,scene.Measure(), image:id!="time-32000"));
        long next=16000;
        void Advance(long time) { while(next<=time) { host.Frame(next);next+=16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach(var time in times) { Advance(time);host.Draw();Sample($"time-{time}"); } }
        void Change(long time,string id,Action change) { Advance(time);change();writer.Record(time,"mutation",id);host.Draw();host.Frame(time);Sample(id); }
        void Input(long time,string action,Point p) { Advance(time);host.Input(action,p);host.Draw();host.Frame(time); }
        var center=scene.Item.TranslatePoint(new Rect(scene.Item.Bounds.Size).Center,host.Window)!.Value;
        Sample("initial");
        switch(config.Action)
        {
            case "path-change":
                Input(1000,"move",center); Input(2000,"down",center); Sample("pressed"); Times(16000,64000);
                Change(65000,"generic",()=>scene.SetPath("generic")); Times(80000,160000);
                Input(161000,"up",center); Sample("released"); Times(176000);
                Change(177000,"cut",()=>scene.SetPath("cut")); Times(192000,400000);
                Change(401000,"restore-corners",()=>scene.SetPath("none")); Times(416000,800000); break;
            case "path-resize":
                var hold=new MaterialInteractionEvent.Press(new Point(config.Rtl ? scene.Item.Bounds.Width - 30 : 30,30));
                Change(1000,"press",()=>scene.Source.Emit(hold)); Times(16000,32000);
                Change(33000,"grow",()=>scene.Item.Height=100); Times(48000,64000);
                Change(65000,"release",()=>scene.Source.Emit(new MaterialInteractionEvent.Release(hold))); Times(80000,160000,400000); break;
            case "key-long":
                Change(1000,"focus",()=>scene.Item.Focus(NavigationMethod.Tab));Times(16000,64000,160000);
                Change(161000,"enter-down",()=>host.Window.KeyPress(Key.Enter,RawInputModifiers.None,PhysicalKey.None,null));
                Change(162000,"space-down",()=>host.Window.KeyPress(Key.Space,RawInputModifiers.None,PhysicalKey.None,null));
                Change(163000,"other-up",()=>host.Window.KeyRelease(Key.A,RawInputModifiers.None,PhysicalKey.None,null));
                Times(176000,661000,662000);
                Change(663000,"enter-up",()=>host.Window.KeyRelease(Key.Enter,RawInputModifiers.None,PhysicalKey.None,null));
                Change(664000,"space-up",()=>host.Window.KeyRelease(Key.Space,RawInputModifiers.None,PhysicalKey.None,null));
                Times(672000,800000);break;
            case "space": case "enter":
                Change(1000,"focus",()=>scene.Item.Focus(NavigationMethod.Tab));Times(16000,64000,160000);
                var key=config.Action=="space"?Key.Space:Key.Enter;
                Change(161000,"key-down",()=>host.Window.KeyPress(key,RawInputModifiers.None,PhysicalKey.None,null));Times(176000,240000);
                Change(241000,"key-up",()=>host.Window.KeyRelease(key,RawInputModifiers.None,PhysicalKey.None,null));Times(256000,400000,800000);break;
            case "nested":
                var child = scene.Child!;
                var childCenter=child.TranslatePoint(new Rect(child.Bounds.Size).Center,host.Window)!.Value;
                Input(1000,"move",childCenter);Input(2000,"down",childCenter);Sample("child-pressed");Times(16000,64000);
                Input(65000,"up",childCenter);Sample("child-released");Times(80000,240000,400000);break;
            case "long":
                Input(1000,"move",center);Input(2000,"down",center);Sample("pressed");Times(16000,32000,501000,502000);
                Input(503000,"up",center);Sample("released");Times(512000,800000);break;
            case "shapes":
                Input(1000,"move",center);Times(16000,64000);
                Change(65000,"configuration",()=>scene.Item.Shapes=new(new(2),new(8),new(24),new(8),new(6),new(8)));Times(80000,160000);
                Input(161000,"down",center);Times(176000,240000);Input(241000,"up",center);Sample("released");Times(256000,800000);break;
            case "theme": Change(1000,"dark",()=>SetColors(host,true));Times(16000,32000,80000,400000);break;
            case "select": Change(1000,"selected",()=>scene.Item.IsChecked=true);Times(16000,32000,80000);Change(81000,"deselected",()=>scene.Item.IsChecked=false);Times(96000,240000,400000);break;
            case "group": Change(1000,"first",()=>((SegmentedListItem)scene.Item).GroupCount=3);Times(16000,32000);Change(33000,"last",()=>((SegmentedListItem)scene.Item).GroupIndex=2);Times(48000,160000);break;
            case "drag":
                var first=new MaterialInteractionEvent.DragStart();var second=new MaterialInteractionEvent.DragStart();
                Change(1000,"drag",()=>scene.Source.Emit(first));Times(16000,32000,80000);
                Change(81000,"second",()=>scene.Source.Emit(second));Change(82000,"stop-first",()=>scene.Source.Emit(new MaterialInteractionEvent.DragStop(first)));Times(96000,160000);
                Change(161000,"cancel-second",()=>scene.Source.Emit(new MaterialInteractionEvent.DragCancel(second)));Times(176000,400000,800000);break;
            default:
                Input(1000,"move",center);Input(2000,"down",center);Sample("pressed");
                if(config.Action is "quick" or "disabled-input" or "passive-input" or "single-click" or "multiple-click") { Input(5000,"up",center);Sample("released");Times(16000,32000,80000,400000);break; }
                Times(16000,32000,64000);
                if(config.Action=="cancel") Input(65000,"move",new Point(4,4));
                if(config.Action=="disable") Change(65000,"disabled",()=>scene.Item.IsInteractionEnabled=false);
                Times(80000,160000);Input(161000,"up",config.Action=="cancel"?new Point(4,4):center);Sample("released");Times(176000,400000,800000);break;
        }
    }
    sealed class Scene
    {
        readonly AvaloniaHost host;
        readonly ListItemConfig c;
        readonly List<(string Name,Marker Control)> slots=[];
        int clicks,requests,longClicks,childClicks;
        public Button? Child { get; }
        public ListItem Item { get; }
        public MaterialInteractionSource Source { get; }=new();
        public Control Root { get; }
        public Scene(AvaloniaHost host,ListItemConfig c)
        {
            this.host=host;this.c=c;SetColors(host,c.Dark);
            if(c.Rtl) host.Window.FlowDirection=FlowDirection.RightToLeft;
            Item=c.Segmented?new SegmentedListItem { GroupIndex=c.Index,GroupCount=c.Count }:new ListItem();
            Item.InteractionMode=c.Mode switch { "click"=>ListItemInteractionMode.Click,"single"=>ListItemInteractionMode.SingleSelection,"multiple"=>ListItemInteractionMode.MultipleSelection,_=>ListItemInteractionMode.None };
            Item.IsInteractionEnabled=c.Enabled;Item.IsSelected=c.Selected;Item.IsChecked=c.Selected;Item.InteractionSource=Source;
            SetPath(c.Path);
            if(c.Path!="none" && c.Action=="space")
            {
                MaterialInteraction.SetFocusFeedback(Item,FocusFeedback.InsetRing);
                MaterialInteraction.SetFocusRingOuterColor(Item,Colors.Red); MaterialInteraction.SetFocusRingInnerColor(Item,Colors.Blue);
            }
            Item.ItemVerticalAlignment=Enum.Parse<ListItemVerticalAlignment>(c.Alignment,true);
            if(c.Height is {} h) Item.Height=h;
            if(c.CustomPadding) Item.Padding=new Thickness(8,4,20,14);
            Marker Add(string name,double w,double h) {var m=new Marker(new Size(w,h));slots.Add((name,m));return m;}
            Item.Content=Add("main",64,24);
            if(c.Sides) { Item.LeadingContent=Add("leading",c.Leading,c.Leading);Item.TrailingContent=Add("trailing",16,16); }
            if(c.Action=="nested")
            {
                var trailing = Item.TrailingContent;
                Item.TrailingContent = null;
                Child=new Button { Width=64,Height=32,Padding=new Thickness(8),Content=trailing };
                Child.Click+=(_,_)=>childClicks++;
                Item.TrailingContent=Child;
            }
            if(c.Lines>=2) Item.SupportingContent=Add("supporting",80,20);
            if(c.Lines==3) Item.OverlineContent=Add("overline",40,16);
            Item.Click+=(_,e)=> { if(ReferenceEquals(e.Source,Item)) clicks++; };
            Item.CheckedChangeRequested+=(_,_)=>requests++;
            if(c.Action is "long" or "key-long") Item.LongClick+=(_,_)=>longClicks++;
            Control container=c.Unbounded?new StackPanel { Orientation=Orientation.Horizontal,Children={Item} }:Item;
            Root=new Grid { Width=c.Width,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Children={container} };
        }
        public void SetPath(string path)
        {
            Item.Shape=Item.SelectedShape=Item.PressedShape=Item.FocusedShape=Item.HoveredShape=Item.DraggedShape=null;
            if(path=="none")return;
            Item.Shape=new CutCornerShape { TopStart=new(20,RelativeUnit.Absolute),TopEnd=new(4,RelativeUnit.Absolute),BottomEnd=new(8,RelativeUnit.Absolute),BottomStart=new(2,RelativeUnit.Absolute) };
            Item.PressedShape=new CutCornerShape(new RelativeScalar(.1,RelativeUnit.Relative));
            Item.SelectedShape=new CutCornerShape(12);Item.FocusedShape=new CutCornerShape(8);
            Item.HoveredShape=new CutCornerShape(4);Item.DraggedShape=new CutCornerShape(new RelativeScalar(.25,RelativeUnit.Relative));
            if(path=="mixed") {Item.SelectedShape=null;Item.PressedShape=null;}
            if(path is "rounded" or "round-cut")
            {
                Item.Shape = new RoundedCornerShape { TopStart=new(20,RelativeUnit.Absolute),TopEnd=new(4,RelativeUnit.Absolute),BottomEnd=new(8,RelativeUnit.Absolute),BottomStart=new(2,RelativeUnit.Absolute) };
                Item.SelectedShape=new RoundedCornerShape(new RelativeScalar(.25,RelativeUnit.Relative));
                if(path=="rounded") {Item.PressedShape=new RoundedCornerShape(new RelativeScalar(.1,RelativeUnit.Relative));Item.FocusedShape=new RoundedCornerShape(8);Item.HoveredShape=new RoundedCornerShape(4);Item.DraggedShape=new RoundedCornerShape(new RelativeScalar(.25,RelativeUnit.Relative));}
            }
            if(path=="generic") {Item.Shape=Item.DraggedShape=new NotchedShape();}
        }
        public ProbePoint[] Measure()=>[new("item","list-item",null,[..AvaloniaMeasurements.Geometry(Item,host.Window).Where(m=>m.Name is "box" or "size"),
            Measurement.Numeric("corner",c.Rtl ? Item.Surface!.EffectiveCornerRadius.TopRight : Item.Surface!.EffectiveCornerRadius.TopLeft,"dip",category:"motion",definition:"list-item.top-left-outline-radius"),
            ..c.Path!="none" ? new[] {Measurement.Text("outlineKind",Item.Surface.ContainerGeometry is not null?"generic":"rounded",definition:"list-item.rendered-outline-kind"),
                Measurement.Numeric("pathStartY",Item.Surface.ContainerGeometry is PathGeometry geometry?geometry.Figures![0].StartPoint.Y:0,"dip",category:"motion",definition:"list-item.generic-outline-first-move-y-or-zero")} : [],
            Measurement.Numeric("shadow",Item.Surface!.ShadowElevation,"dip",category:"motion",definition:"list-item.shadow-elevation"),
            Measurement.Numeric("longClicks",longClicks,"count",definition:"list-item.long-clicks"),
            Measurement.Numeric("childClicks",childClicks,"count",definition:"list-item.child-clicks"),
            Measurement.Numeric("clicks",clicks,"count",definition:"list-item.clicks"),Measurement.Numeric("requests",requests,"count",definition:"list-item.checked-requests")]),
            ..slots.Select(s=>new ProbePoint(s.Name,"content","item",AvaloniaMeasurements.Geometry(s.Control,host.Window).Where(m=>m.Name is "box" or "size").ToArray()))];
    }
    sealed class NotchedShape:MaterialShape
    {
        public override PathGeometry CreateOutline(Size size,FlowDirection layoutDirection,double density)
        {
            var path=new PathGeometry(); using var context=path.Open();
            context.BeginFigure(default,true);context.LineTo(new(size.Width,0));context.LineTo(new(size.Width-12,size.Height/2));
            context.LineTo(new(size.Width,size.Height));context.LineTo(new(0,size.Height));context.EndFigure(true);return path;
        }
    }
    static void SetColors(AvaloniaHost host,bool dark)
    {
        CaseDefaults.SetThemeColors(host,dark);
        host.Window.Resources[Sys.Color.Surface]=Color.Parse(dark?"#141218":"#FEF7FF");
        host.Window.Resources[Sys.Color.TertiaryContainer]=Color.Parse(dark?"#633B48":"#FFD8E4");
        host.Window.Resources[Sys.Color.OnTertiaryContainer]=Color.Parse(dark?"#FFD8E4":"#31111D");
    }
    sealed class Marker(Size size):Control
    {
        protected override bool BypassFlowDirectionPolicies=>true;
        static Marker()=>AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size available)=>new(Math.Min(size.Width,available.Width),Math.Min(size.Height,available.Height));
        public override void Render(DrawingContext context)=>context.DrawRectangle(TextElement.GetForeground(this),null,new Rect(Bounds.Size));
    }
}
