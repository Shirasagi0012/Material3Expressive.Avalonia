@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.input.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class SecureTextFieldConfig(val outlined:Boolean=false, val position:String="Inside", val minimized:Boolean=false,
    val label:Boolean=true, val slots:Boolean=false, val enabled:Boolean=true, val error:Boolean=false, val dark:Boolean=false,
    val rtl:Boolean=false, val alignment:String="Start", val width:Double?=null, val height:Double?=null,
    val labelWidth:Double=40.0, val labelHeight:Double=12.0, val action:String="none")

@ParitySuite("secure-text-field")
class SecureTextFieldCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,400,200).use { host ->
            val scene=FieldScene(writer.readConfig<SecureTextFieldConfig>())
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }

    @ParityScenario("changes")
    suspend fun changes(writer:CaptureWriter) {
        val config=writer.readConfig<SecureTextFieldConfig>()
        ComposeHost(writer,400,200).use { host ->
            val scene=FieldScene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            // Focused native carets use unrelated platform clocks. Compare decoration geometry and
            // actual label progress here; static/state cases retain full, unmodified raster captures.
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure(),image=config.action in listOf("error","theme")))
            var next=16000L
            fun advance(time:Long) { while(next<=time) { host.frame(next); next+=16000 }; host.advanceTo(time) }
            fun change(time:Long,id:String,action:()->Unit) { advance(time); action(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id) }
            sample("initial")
            change(1000,"start") {
                when(config.action) { "error" -> scene.error=true; "theme" -> scene.dark=true; else -> scene.requester.requestFocus() }
            }
            listOf(16000L,32000L,64000L).forEach { advance(it); sample("time-$it") }
            if(config.action=="reverse") change(65000,"reverse") { scene.focusManager.clearFocus() }
            if(config.action=="disable") change(65000,"disable") { scene.enabled=false }
            listOf(96000L,160000L,240000L,400000L,800000L).forEach { advance(it); sample("time-$it") }
        }
    }

    @ParityScenario("auto-blur")
    suspend fun autoBlur(writer:CaptureWriter) {
        val config=writer.readConfig<SecureTextFieldConfig>()
        ComposeHost(writer,400,200).use { host ->
            val scene=FieldScene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure(),image=false))
            var next=16000L
            fun advance(time:Long) { while(next<=time) { host.frame(next); next+=16000 }; host.advanceTo(time) }
            scene.requester.requestFocus(); host.draw(); host.frame(1000)
            advance(800000); sample("focused-800000")
            scene.focusManager.clearFocus(); writer.record(800000,"mutation","blur"); host.draw(); host.frame(800000)
            listOf(816000L,864000L,928000L,960000L,976000L,1008000L,1040000L,1200000L,1600000L).forEach {
                advance(it); sample("time-$it")
                if(config.action=="refocus" && it==976000L) {
                    scene.requester.requestFocus(); writer.record(it,"mutation","refocus"); host.draw(); host.frame(it)
                }
            }
        }
    }

    private class FieldScene(val config:SecureTextFieldConfig) {
        val field=Probe()
        val probes=linkedMapOf<String,Probe>()
        val requester=FocusRequester()
        lateinit var focusManager:FocusManager
        var enabled by mutableStateOf(config.enabled)
        var error by mutableStateOf(config.error)
        var dark by mutableStateOf(config.dark)
        var progress=0f

        @Composable fun Slot(name:String,width:Double,height:Double) {
            val probe=remember(name) { Probe() }
            SideEffect { probes[name]=probe }
            DisposableEffect(probe) { onDispose { if(probes[name]===probe) probes.remove(name) } }
            Box(probe.modifier().size(width.toFloat().dp,height.toFloat().dp).background(LocalContentColor.current))
        }

        @Composable fun Content() {
            focusManager=LocalFocusManager.current
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        var modifier=field.modifier().focusRequester(requester)
                        if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                        if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
                        val align=when(config.alignment) { "Center" -> Alignment.CenterHorizontally; "End" -> Alignment.End; else -> Alignment.Start }
                        val position=when(config.position) {
                            "Cutout" -> TextFieldLabelPosition.Cutout(config.minimized,align,align)
                            "Above" -> TextFieldLabelPosition.Above(align)
                            else -> TextFieldLabelPosition.Inside(config.minimized,align,align)
                        }
                        val label:(@Composable TextFieldLabelScope.()->Unit)?=if(config.label) ({ progress=labelMinimizedProgress; Slot("label",config.labelWidth,config.labelHeight) }) else null
                        val leading:(@Composable ()->Unit)?=if(config.slots) ({ Slot("leading",24.0,24.0) }) else null
                        val trailing:(@Composable ()->Unit)?=if(config.slots) ({ Slot("trailing",24.0,24.0) }) else null
                        val supporting:(@Composable ()->Unit)?=if(config.slots) ({ Slot("supporting",60.0,12.0) }) else null
                        val prefix:(@Composable ()->Unit)?=if(config.slots) ({ Slot("prefix",16.0,12.0) }) else null
                        val suffix:(@Composable ()->Unit)?=if(config.slots) ({ Slot("suffix",20.0,12.0) }) else null
                        val placeholder:(@Composable ()->Unit)?=if(config.slots) ({ Slot("placeholder",48.0,16.0) }) else null
                        val state=rememberTextFieldState()
                        if(config.outlined) OutlinedSecureTextField(state=state,modifier=modifier,enabled=enabled,isError=error,
                            labelPosition=position,label=label,leadingIcon=leading,trailingIcon=trailing,supportingText=supporting,
                            prefix=prefix,suffix=suffix,placeholder=placeholder,textObfuscationMode=TextObfuscationMode.Hidden)
                        else SecureTextField(state=state,modifier=modifier,enabled=enabled,isError=error,
                            labelPosition=position,label=label,leadingIcon=leading,trailingIcon=trailing,supportingText=supporting,
                            prefix=prefix,suffix=suffix,placeholder=placeholder,textObfuscationMode=TextObfuscationMode.Hidden)
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            val result=mutableListOf(ProbePoint("field","field",null,field.geometry().filter { it.name=="box" || it.name=="size" }))
            probes.forEach { (name,probe) -> result.add(ProbePoint(name,name,"field",probe.geometry().filter { it.name=="box" || it.name=="size" })) }
            if(config.label) result.add(ProbePoint("motion","motion","field",listOf(number("labelProgress",progress.toDouble(),"ratio",category="motion",definition="text-field.label-minimized-progress"))))
            return result
        }
    }
}
