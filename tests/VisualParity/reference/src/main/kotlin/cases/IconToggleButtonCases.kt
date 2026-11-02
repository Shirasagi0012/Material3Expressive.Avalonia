@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class IconToggleConfig(val selected:Boolean=false,val enabled:Boolean=true,val dark:Boolean=false,val action:String="none",
    val style:String="standard",val size:String="small",val widthOption:String="uniform",val morph:Boolean=false,val preset:String="none")
@ParitySuite("icon-toggle-button")
class IconToggleButtonCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<IconToggleConfig>()
        ComposeHost(writer,280,220).use {host ->
            var selected by mutableStateOf(config.selected);var enabled by mutableStateOf(config.enabled);var requests=0
            var widthOption by mutableStateOf(config.widthOption)
            val button=Probe()
            host.mount {
                MaterialTheme(colorScheme=if(config.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalContentColor provides Color(0xff245a80)) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            val width=when(widthOption) { "narrow" -> IconButtonDefaults.IconButtonWidthOption.Narrow; "wide" -> IconButtonDefaults.IconButtonWidthOption.Wide; else -> IconButtonDefaults.IconButtonWidthOption.Uniform }
                            val dimensions=when(config.size) {
                                "xsmall" -> IconButtonDefaults.extraSmallContainerSize(width)
                                "medium" -> IconButtonDefaults.mediumContainerSize(width)
                                "large" -> IconButtonDefaults.largeContainerSize(width)
                                "xlarge" -> IconButtonDefaults.extraLargeContainerSize(width)
                                else -> IconButtonDefaults.smallContainerSize(width)
                            }
                            val iconSize=when(config.size) { "xsmall" -> IconButtonDefaults.extraSmallIconSize; "medium" -> IconButtonDefaults.mediumIconSize; "large" -> IconButtonDefaults.largeIconSize; "xlarge" -> IconButtonDefaults.extraLargeIconSize; else -> IconButtonDefaults.smallIconSize }
                            var modifier=button.modifier()
                            if(config.size!="small") modifier=modifier.size(dimensions)
                            else if(widthOption!="uniform") modifier=modifier.minimumInteractiveComponentSize().size(dimensions)
                            val content:@Composable ()->Unit = {
                                val color=LocalContentColor.current
                                Canvas(Modifier.size(iconSize)){drawRect(color,Offset(4.dp.toPx(),4.dp.toPx()),Size(size.width-8.dp.toPx(),size.height-8.dp.toPx()))}
                            }
                            // Approved constructor semantics: the pinned default helper reads the
                            // standard cache by mistake. Explicit slots isolate this accepted correction
                            // without patching the reference library or depending on suite call order.
                            val outlinedColors=if(config.style=="outlined") IconToggleButtonColors(
                                containerColor=Color.Transparent, contentColor=LocalContentColor.current,
                                disabledContainerColor=Color.Transparent,
                                disabledContentColor=LocalContentColor.current.copy(alpha=0.38f),
                                checkedContainerColor=MaterialTheme.colorScheme.inverseSurface,
                                checkedContentColor=contentColorFor(MaterialTheme.colorScheme.inverseSurface)) else null
                            val change:(Boolean)->Unit={requests++;if(config.action!="ignore")selected=it}
                            if(config.morph || config.preset!="none") {
                                val square=when(config.size) { "xsmall","small" -> MaterialTheme.shapes.medium; "medium" -> MaterialTheme.shapes.large; else -> MaterialTheme.shapes.extraLarge }
                                val pressed=when(config.size) { "xsmall","small" -> MaterialTheme.shapes.small; "medium" -> MaterialTheme.shapes.medium; else -> MaterialTheme.shapes.large }
                                val shapes=if(config.morph) IconToggleButtonShapes(IconButtonDefaults.standardShape,RoundedCornerShape(8.dp),RoundedCornerShape(12.dp))
                                    else IconToggleButtonShapes(if(config.preset=="square")square else IconButtonDefaults.standardShape,
                                        pressed,if(config.preset=="square")IconButtonDefaults.standardShape else square)
                                when(config.style) {
                                    "filled" -> FilledIconToggleButton(checked=selected,onCheckedChange=change,enabled=enabled,modifier=modifier,shapes=shapes,content=content)
                                    // The expressive overload defaults to vibrant colors AND border;
                                    // this theme keeps the ordinary ambient defaults with opt-in shapes.
                                    "outlined" -> OutlinedIconToggleButton(checked=selected,onCheckedChange=change,enabled=enabled,modifier=modifier,shapes=shapes,colors=outlinedColors!!,border=IconButtonDefaults.outlinedIconToggleButtonBorder(enabled,selected),content=content)
                                    "tonal" -> FilledTonalIconToggleButton(checked=selected,onCheckedChange=change,enabled=enabled,modifier=modifier,shapes=shapes,content=content)
                                    else -> IconToggleButton(checked=selected,onCheckedChange=change,enabled=enabled,modifier=modifier,shapes=shapes,colors=IconButtonDefaults.iconToggleButtonColors(),content=content)
                                }
                            } else when(config.style) {
                                "filled" -> FilledIconToggleButton(checked=selected,onCheckedChange=change,enabled=enabled,modifier=modifier,content=content)
                                "outlined" -> OutlinedIconToggleButton(checked=selected,onCheckedChange=change,enabled=enabled,modifier=modifier,colors=outlinedColors!!,content=content)
                                "tonal" -> FilledTonalIconToggleButton(checked=selected,onCheckedChange=change,enabled=enabled,modifier=modifier,content=content)
                                else -> IconToggleButton(checked=selected,onCheckedChange=change,enabled=enabled,modifier=modifier,content=content)
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("button","container",null,button.geometry().filter{it.name=="box"||it.name=="size"}),
                ProbePoint("state","state",null,listOf(flag("checked",selected,definition="icon-toggle.checked"),number("requests",requests.toDouble(),"count",definition="icon-toggle.requests"))))))
            sample("initial");if(!dynamic)return
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"input",id);host.draw();host.frame(time);sample(id)}
            val center=Offset(140f,110f)
            change(1000,"down"){host.input("move",center);host.input("down",center)}
            advance(64000);host.draw();sample("pressed")
            if(config.action=="width")change(65000,"pressed-wide"){widthOption="wide"}
            if(config.action=="disable")change(65000,"disabled"){enabled=false}
            if(config.action=="cancel")change(65000,"drag-out"){host.input("move",Offset(4f,4f))}
            change(80000,"up"){host.input("up",if(config.action=="cancel")Offset(4f,4f) else center)}
            if(config.action=="reverse") {
                advance(96000);host.draw();sample("checked-in-flight")
                change(97000,"repress"){host.input("down",center)}
                advance(112000);host.draw();sample("repress-in-flight")
                change(113000,"reverse-up"){host.input("up",center)}
            }
            (if(config.action=="reverse")listOf(128000L,160000L,320000L,800000L)else listOf(96000L,160000L,320000L,800000L)).forEach{advance(it);host.draw();sample("time-$it")}
            change(801000,"leave"){host.input("move",Offset(4f,4f))}
            advance(1200000);host.draw();sample("late")
            if(config.action=="width") {
                change(1201000,"checked-narrow"){widthOption="narrow"}
                change(1202000,"restore-uniform"){widthOption="uniform"}
            }
        }
    }
}
