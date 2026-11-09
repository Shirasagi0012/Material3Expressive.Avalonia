@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class NativeMenuPrecisionConfig(val kind:String="native",val precision:Boolean=false,val rtl:Boolean=false,
    val defaultIconSize:Boolean=true,val supporting:Boolean=false,val width:Double=200.0,val dark:Boolean=false,
    val customPadding:Boolean=false,val action:String="none")

@ParitySuite("native-menu-precision")
class NativeMenuPrecisionCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val c=writer.readConfig<NativeMenuPrecisionConfig>()
        @Suppress("UNCHECKED_CAST")
        val precision=Class.forName("androidx.compose.material3.PrecisionPointer").declaredMethods
            .single {it.name.startsWith("getShouldUsePrecisionPointerComponentSizing")}.invoke(null) as MutableState<Boolean>
        val previous=precision.value
        precision.value=c.precision
        try {
            ComposeHost(writer,320,160).use { host ->
                val allocation=Probe();val item=Probe();val header=Probe();val leading=Probe();val trailing=Probe();val supporting=Probe()
                var clicks=0
                host.mount {
                    MaterialTheme(colorScheme=if(c.dark)darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr,
                            LocalContentColor provides MaterialTheme.colorScheme.onSurface) {
                            Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                                Box((if(c.width>0)Modifier.width(c.width.dp) else Modifier).then(allocation.modifier())) {
                                    val body:@Composable ()->Unit={Box(header.modifier().size(48.dp,16.dp).background(LocalContentColor.current))}
                                    val start:@Composable ()->Unit={Box(leading.modifier().size(20.dp).background(LocalContentColor.current))}
                                    val end:@Composable ()->Unit={
                                        val size=if(c.defaultIconSize) Modifier.size(MenuDefaults.TrailingIconSize) else Modifier.size(8.dp,12.dp)
                                        Box(trailing.modifier().then(size).background(LocalContentColor.current))
                                    }
                                    val support:@Composable (() -> Unit)?=if(c.supporting) ({Box(supporting.modifier().size(80.dp,12.dp).background(LocalContentColor.current))}) else null
                                    val padding=if(c.customPadding)PaddingValues(start=3.dp,top=5.dp,end=9.dp,bottom=7.dp) else MenuDefaults.DropdownMenuSelectableItemContentPadding
                                    when(c.kind) {
                                        "ordinary" -> DropdownMenuItem(onClick={clicks++},text=body,leadingIcon=start,trailingContent=end,supportingText=support,
                                            shape=MenuDefaults.itemShapes().shape,modifier=item.modifier(),contentPadding=padding)
                                        "checkable" -> CheckableDropdownMenuItem(checked=false,onCheckedChange={clicks++},text=body,leadingIcon=start,trailingContent=end,supportingText=support,
                                            shapes=MenuDefaults.itemShapes(),modifier=item.modifier(),contentPadding=padding)
                                        else -> SelectableDropdownMenuItem(selected=false,onClick={clicks++},text=body,leadingIcon=start,trailingContent=end,supportingText=support,
                                            shapes=MenuDefaults.itemShapes(),modifier=item.modifier(),contentPadding=padding)
                                    }
                                }
                            }
                        }
                    }
                }
                host.frame(-1000);host.frame(0)
                fun geometry(id:String,probe:Probe)=ProbePoint(id,id,null,probe.geometry().filter{it.name=="box"||it.name=="size"})
                fun sample(id:String) {
                    val points=mutableListOf(geometry("allocation",allocation),geometry("header",header),geometry("leading",leading),geometry("trailing",trailing),
                        ProbePoint("activation","activation",null,listOf(number("clicks",clicks.toDouble(),"count",definition="menu.click-count"))))
                    if(c.supporting)points.add(geometry("supporting",supporting))
                    writer.submit(host.snapshot(id,id,points))
                }
                sample("initial")
                if(dynamic) {
                    var next=16000L
                    fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
                    fun times(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
                    fun mode(time:Long,precise:Boolean,id:String){advance(time);precision.value=precise;host.draw();sample(id)}
                    if(c.action=="mode") {
                        mode(1000,true,"precision-on");times(16000)
                        mode(17000,false,"precision-off");times(32000)
                        mode(33000,true,"precision-again");times(48000)
                    } else {
                        val coords=checkNotNull(item.coordinates)
                        val center=coords.localToRoot(Offset(coords.size.width/2f,coords.size.height/2f))/ComposeHost.RenderScale.toFloat()
                        advance(1000);host.input("move",center);host.draw()
                        advance(2000);host.input("down",center);host.draw();sample("pressed");times(16000,32000)
                        mode(33000,true,"precision-pressed");times(48000,64000)
                        advance(65000);host.input("up",center);host.draw();sample("released");times(80000,240000,400000)
                    }
                }
            }
        } finally {precision.value=previous}
    }
}
