@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CutCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class AppBarOverflowIndicatorConfig(val rtl:Boolean=false,val enabled:Boolean=true,
    val cut:Boolean=false,val customColors:Boolean=false,val dark:Boolean=false,val action:String="none",val culture:String="en-US",val font:String="Roboto")

@ParitySuite("appbar-overflow-indicator")
class AppBarOverflowIndicatorCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)

    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<AppBarOverflowIndicatorConfig>()
        val previousLocale=java.util.Locale.getDefault()
        try {
        java.util.Locale.setDefault(java.util.Locale.forLanguageTag(config.culture))
        ComposeHost(writer,240,160).use {host ->
            val original=AppBarMenuState()
            var state by mutableStateOf(original)
            var enabled by mutableStateOf(config.enabled)
            var source by mutableStateOf(MutableInteractionSource())
            val indicator=Probe()
            host.mount {
                MaterialTheme(colorScheme=if(config.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive(),
                    typography=Typography(fontFamily=remember { CaseDefaults.font(writer) })) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr,
                        LocalContentColor provides Color(if(config.dark)0xA0D0BCFF else 0x40208070)) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=AbsoluteAlignment.TopLeft) {
                            val colors=if(config.customColors)IconButtonDefaults.iconButtonColors(
                                containerColor=Color(0xFFE8DEF8),contentColor=Color(0x7F173D8F),
                                disabledContainerColor=Color(0xFFFFAACC),disabledContentColor=Color(0x551122EE))
                                else IconButtonDefaults.iconButtonColors()
                            // Place the whole tooltip/button composition, as Canvas does on Avalonia.
                            // AppBarOverflowIndicator's modifier belongs only to its inner IconButton.
                            Box(Modifier.absoluteOffset(96.dp,56.dp)) {
                                AppBarOverflowIndicator(state,indicator.modifier(),enabled,
                                    shape=if(config.cut)CutCornerShape(2.dp,8.dp,12.dp,4.dp)else IconButtonDefaults.standardShape,
                                    colors=colors,interactionSource=source)
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            val pointer=Offset(120f,80f)
            // Press coordinates belong to the actual clipped clickable container, excluding its touch-target padding.
            var coordinator:Any?=checkNotNull(indicator.coordinates)
            var pressPosition=Offset.Zero
            while(coordinator!=null){
                val layer=field(coordinator,"layer")
                if(layer!=null){
                    val graphics=checkNotNull(field(layer,"graphicsLayer"))
                    if(graphics.javaClass.methods.single{it.name=="getClip"&&it.parameterCount==0}.invoke(graphics)==true){
                        val size=(coordinator as androidx.compose.ui.layout.LayoutCoordinates).size
                        pressPosition=Offset(size.width/2f,size.height/2f)
                    }
                }
                coordinator=field(coordinator,"wrapped")
            }
            val press=PressInteraction.Press(pressPosition)
            val hover=HoverInteraction.Enter();val focus=FocusInteraction.Focus()
            var nextFrame=16000L
            fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("indicator","container",null,indicator.geometry().filter{it.name=="box"||it.name=="size"}),
                ProbePoint("state","state",null,listOf(flag("showing",state.isShowing,definition="appbar.menu.showing"),
                    flag("originalShowing",original.isShowing,definition="appbar.menu.originalShowing"),
                    flag("enabled",enabled,definition="appbar.indicator.enabled"))))))
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"input",id);host.draw();sample(id)}
            fun times(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            fun click(){host.input("move",pointer);host.input("down",pointer);host.input("up",pointer);host.input("move",Offset(8f,8f))}
            fun emit(interaction:Interaction){check(source.tryEmit(interaction))}
            sample("initial");if(!dynamic)return
            when(config.action){
                "disabled-hover" -> {
                    change(1000,"enter"){host.input("move",pointer)};times(16000,64000,224000,400000)
                    change(401000,"disabled-click"){host.input("down",pointer);host.input("up",pointer)};times(416000)
                    change(417000,"exit"){host.input("move",Offset(8f,8f))};times(432000,512000,800000)
                }
                "click" -> {
                    change(1000,"press"){host.input("move",pointer);host.input("down",pointer)};times(16000,64000)
                    change(65000,"release"){host.input("up",pointer);host.input("move",Offset(8f,8f))};times(80000,160000,400000)
                    change(401000,"close"){click()};times(416000,480000,800000)
                }
                "disable" -> {
                    change(1000,"press"){host.input("move",pointer);host.input("down",pointer)};times(16000,64000)
                    change(65000,"disable"){enabled=false};times(80000,128000)
                    change(129000,"release"){host.input("up",pointer);host.input("move",Offset(8f,8f))};times(160000,400000)
                    change(401000,"enable"){enabled=true};times(416000,800000)
                }
                "state-replace" -> {
                    change(1000,"open-original"){click()};times(16000,64000)
                    change(65000,"replace"){state=AppBarMenuState()};times(80000)
                    change(81000,"open-current"){click()};times(96000,160000)
                    change(161000,"close-current"){click()};times(176000,400000)
                }
                "source-replace" -> {
                    change(1000,"press"){emit(press)};times(16000,64000)
                    val old=source
                    change(65000,"replace"){source=MutableInteractionSource()};times(80000,128000)
                    change(129000,"old-release"){check(old.tryEmit(PressInteraction.Release(press)))};times(160000)
                    change(161000,"new-hover"){emit(hover)};times(176000,240000)
                    change(241000,"new-exit"){emit(HoverInteraction.Exit(hover))};times(256000,400000)
                }
                "source" -> {
                    change(1000,"hover"){emit(hover)};times(16000,64000)
                    change(65000,"focus"){emit(focus)};times(80000,128000)
                    change(129000,"press"){emit(press)};times(144000,192000)
                    change(193000,"release"){emit(PressInteraction.Release(press))};times(208000,240000)
                    change(241000,"unfocus"){emit(FocusInteraction.Unfocus(focus))};times(256000,320000)
                    change(321000,"exit"){emit(HoverInteraction.Exit(hover))};times(336000,400000,800000)
                }
            }
        }
        } finally { java.util.Locale.setDefault(previousLocale) }
    }
}
