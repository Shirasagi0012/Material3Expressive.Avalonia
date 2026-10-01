package parity

import androidx.collection.ScatterMap
import androidx.compose.animation.core.Animatable
import androidx.compose.runtime.State
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Outline
import androidx.compose.ui.layout.LayoutInfo
import androidx.compose.ui.util.lerp
import java.lang.reflect.Field

// A source-pinned, read-only tooling adapter for these Button examples. No shape wrappers or replacement indication.
private fun findField(owner:Any,name:String):Field? {
    var type:Class<*>?=owner.javaClass
    while(type!=null) {
        try { return type.getDeclaredField(name).also { it.isAccessible=true } }
        catch(_:NoSuchFieldException) { type=type.superclass }
    }
    return null
}
private fun field(owner:Any,name:String):Any? =
    (findField(owner,name) ?: error("Pinned reader member missing: ${owner.javaClass.name}.$name")).get(owner)
private fun nodes(layout:Any):List<Any> {
    val output=mutableListOf<Any>(); val visited=java.util.IdentityHashMap<Any,Boolean>()
    fun walk(node:Any?) {
        if(node==null || visited.put(node,true)!=null)return
        output+=node
        walk(findField(node,"delegate")?.get(node))
        walk(findField(node,"child")?.get(node))
    }
    walk(field(checkNotNull(field(layout,"nodes")),"head"));return output
}

fun buttonMeasurements(probe:Probe,pressed:Boolean,hovered:Boolean):List<ProbePoint> {
    val layout=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
    val info=layout as LayoutInfo
    val backgrounds=info.getModifierInfo().filter { it.modifier.javaClass.name=="androidx.compose.foundation.BackgroundElement" }
    check(backgrounds.size==1) { "Pinned Button background modifier is ambiguous or missing." }
    val background=backgrounds.single(); val all=nodes(layout)
    val node=all.single { it.javaClass.name=="androidx.compose.foundation.BackgroundNode" }
    val color=Color((field(node,"color") as Long).toULong())
    val container=probe.geometry(background.coordinates).toMutableList()
    container+=vector("fillColor","color",listOf(color.red.toDouble(),color.green.toDouble(),color.blue.toDouble(),color.alpha.toDouble()),
        "srgb","none","properties","container.fill.solid-srgba","BackgroundNode.color; brush=null checked")
    check(field(node,"brush")==null) { "Button reader supports a solid color, not a background brush." }
    // Read the outline already used by draw, rather than invoking Shape.createOutline and changing its cache.
    val outline=field(node,"lastOutline")
    if(outline is Outline.Rounded) {
        val r=outline.roundRect
        val radii=listOf(r.topLeftCornerRadius.x,r.topLeftCornerRadius.y,r.topRightCornerRadius.x,r.topRightCornerRadius.y,
            r.bottomRightCornerRadius.x,r.bottomRightCornerRadius.y,r.bottomLeftCornerRadius.x,r.bottomLeftCornerRadius.y).map { it.toDouble() }
        val components=listOf("topLeft.x","topLeft.y","topRight.x","topRight.y","bottomRight.x","bottomRight.y","bottomLeft.x","bottomLeft.y")
        container+=vector("nativeCornerRadii","quad",radii,"dip","container","motion","compose.background.raw-rounded-outline.corner-radii.xy",
            "BackgroundNode.lastOutline.roundRect",components)
        container+=vector("cornerRadii","quad",radii,
            "dip","container","motion","container.rounded-outline.corner-radii.xy","BackgroundNode.lastOutline (last actual draw)",
            listOf("topLeft.x","topLeft.y","topRight.x","topRight.y","bottomRight.x","bottomRight.y","bottomLeft.x","bottomLeft.y"))
    } else container+=unavailable("cornerRadii","motion","quad","dip","container","Last drawn background outline is not rounded.","container.rounded-outline.corner-radii.xy")
    val layers=info.getModifierInfo().filter { it.modifier.javaClass.name=="androidx.compose.ui.graphics.GraphicsLayerElement" }
    val elevation=layers.map { field(it.modifier,"shadowElevation") as Float }.singleOrNull() ?: 0f
    check(layers.size<=1)
    container+=number("shadowElevation",elevation.toDouble(),"dip",category="properties",definition="container.shadow-elevation",
        source="Surface graphics-layer shadowElevation; absent layer means zero in pinned Surface.surface")
    val feedback=mutableListOf(flag("pressed",pressed,definition="interaction.press.active",source="observed MutableInteractionSource active PressInteraction set"),
        flag("hovered",hovered,definition="interaction.hover.active",source="observed MutableInteractionSource active HoverInteraction set"),
        unavailable("hitBounds","geometry","rect","dip","scene","No complete native hit-region reader installed.","interaction.hit-region.aabb"))
    val rippleNodes=all.filter { it.javaClass.name=="androidx.compose.material3.ripple.CommonRippleNode" }.filter {
        val config=checkNotNull(field(it,"_rippleNodeConfiguration"))
        checkNotNull(field(config,"pressConfiguration")).javaClass.simpleName=="Opacity"
    }
    if(rippleNodes.size!=1) {
        feedback+=unavailable("stateLayerOpacity","motion","number","ratio","none","No unique CommonRippleNode in pinned Button tree.","interaction.state-layer.alpha")
        feedback+=unavailable("rippleCount","properties","number","count","none","No unique CommonRippleNode in pinned Button tree.","interaction.active-ripple-count")
    } else {
        val rippleNode=rippleNodes.single()
        val alpha=(field(rippleNode,"animatedAlpha") as Animatable<*,*>).value as Float
        feedback+=number("stateLayerOpacity",alpha.toDouble(),"ratio",category="motion",definition="interaction.state-layer.alpha",
            source="Material3 RippleNode.animatedAlpha.value; configured press-indication node")
        @Suppress("UNCHECKED_CAST") val ripples=field(rippleNode,"ripples") as ScatterMap<Any?,Any?>
        feedback+=number("rippleCount",ripples.size.toDouble(),"count",definition="interaction.active-ripple-count",source="CommonRippleNode.ripples.size")
        check(ripples.size<=1) { "Single-press example needs event identities before observing multiple ripples." }
        ripples.forEachValue { value ->
            val ripple=checkNotNull(value)
            val alphaAnimation=field(ripple,"animatedAlpha") as Animatable<*,*>
            val radiusAnimation=field(ripple,"animatedRadiusPercent") as Animatable<*,*>
            val centerAnimation=field(ripple,"animatedCenterPercent") as Animatable<*,*>
            val finish=(field(ripple,"finishRequested\$delegate") as State<*>).value as Boolean
            val fadedIn=(field(ripple,"finishedFadingIn\$delegate") as State<*>).value as Boolean
            val fraction=if(finish && !fadedIn)1f else alphaAnimation.value as Float
            feedback+=number("ripple.0.alphaFraction",fraction.toDouble(),"ratio",category="motion",definition="ripple.animation.alpha-fraction",
                source="derived: RippleAnimation.animatedAlpha + actual early-release flags; pinned draw expression")
            val start=field(ripple,"startRadius") as Float?; val target=field(ripple,"radius") as Float
            val origin=field(ripple,"origin") as Offset?; val center=field(ripple,"targetCenter") as Offset?
            if(start!=null && origin!=null && center!=null) {
                val a=origin; val b=center; val f=centerAnimation.value as Float
                feedback+=number("ripple.0.radius",lerp(start,target,radiusAnimation.value as Float).toDouble(),"dip","container","motion","ripple.draw.radius",
                    "derived: stored startRadius/radius and animatedRadiusPercent; pinned float lerp")
                feedback+=vector("ripple.0.center","point",listOf(lerp(a.x,b.x,f).toDouble(),lerp(a.y,b.y,f).toDouble()),"dip","container","motion","ripple.draw.center",
                    "derived: stored origin/targetCenter and animatedCenterPercent; pinned float lerp")
            } else {
                feedback+=unavailable("ripple.0.radius","motion","number","dip","container","Ripple has no initialized draw geometry.","ripple.draw.radius")
                feedback+=unavailable("ripple.0.center","motion","point","dip","container","Ripple has no initialized draw geometry.","ripple.draw.center")
            }
            feedback+=flag("ripple.0.running",alphaAnimation.isRunning||radiusAnimation.isRunning||centerAnimation.isRunning,"motion","ripple.animation.running","RippleAnimation three actual Animatable.isRunning flags")
        }
    }
    return listOf(ProbePoint("container","container","button",container),ProbePoint("interaction","interaction","container",feedback))
}
