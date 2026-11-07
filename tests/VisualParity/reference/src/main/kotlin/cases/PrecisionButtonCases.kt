@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class PrecisionButtonConfig(val toggle:Boolean=false,val style:String="filled",val size:String="small",
    val icon:Boolean=false,val rtl:Boolean=false,val dynamic:Boolean=false,val customPadding:Boolean=false)

@ParitySuite("precision-button")
class PrecisionButtonCases {
    @ParityScenario("sizing") suspend fun sizing(writer:CaptureWriter) {
        val c=writer.readConfig<PrecisionButtonConfig>()
        // The pinned Core desktop listener is intentionally empty. Exercise its real
        // internal state explicitly, and restore it before another scenario runs.
        @Suppress("UNCHECKED_CAST")
        val precision=Class.forName("androidx.compose.material3.PrecisionPointer").declaredMethods
            .single { it.name.startsWith("getShouldUsePrecisionPointerComponentSizing") }.invoke(null) as MutableState<Boolean>
        val previous=precision.value
        precision.value=!c.dynamic
        try {
            ComposeHost(writer,400,240).use { host ->
                val button=Probe();val label=Probe();val icon=Probe()
                host.mount {
                    MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                                val height=when(c.size) { "xsmall"->32.dp;"medium"->56.dp;"large"->96.dp;"xlarge"->136.dp;else->if(c.toggle)ToggleButtonDefaults.MinHeight else ButtonDefaults.MinHeight }
                                val custom=PaddingValues(start=5.dp,top=3.dp,end=27.dp,bottom=9.dp)
                                val labelContent:@Composable RowScope.()->Unit={ Box(label.modifier().size(32.dp,16.dp).background(Color(0xFF000080))) }
                                if(c.toggle) {
                                    val size=when(c.size) { "xsmall"->ToggleButtonSize.ExtraSmall;"medium"->ToggleButtonSize.Medium;"large"->ToggleButtonSize.Large;"xlarge"->ToggleButtonSize.ExtraLarge;else->ToggleButtonSize.Small }
                                    val iconContent:(@Composable ()->Unit)?=if(c.icon)({ Box(icon.modifier().fillMaxSize().background(Color(0xFFFFD700))) })else null
                                    val padding=if(c.customPadding)custom else ToggleButtonDefaults.contentPaddingFor(height,hasStartIcon=c.icon)
                                    when(c.style) {
                                        "tonal"->FilledTonalToggleButton(false,{},button.modifier(),buttonSize=size,icon=iconContent,contentPadding=padding,content=labelContent)
                                        "elevated"->ElevatedToggleButton(false,{},button.modifier(),buttonSize=size,icon=iconContent,contentPadding=padding,content=labelContent)
                                        "outlined"->OutlinedToggleButton(false,{},button.modifier(),buttonSize=size,icon=iconContent,contentPadding=padding,content=labelContent)
                                        else->ToggleButton(false,{},button.modifier(),buttonSize=size,icon=iconContent,contentPadding=padding,content=labelContent)
                                    }
                                } else {
                                    val modifier=button.modifier().heightIn(min=height)
                                    val padding=if(c.customPadding)custom else ButtonDefaults.contentPaddingFor(height,hasStartIcon=c.icon)
                                    val body:@Composable RowScope.()->Unit={
                                        if(c.icon) { Box(icon.modifier().size(ButtonDefaults.iconSizeFor(height)).background(Color(0xFFFFD700)));Spacer(Modifier.width(ButtonDefaults.iconSpacingFor(height))) }
                                        labelContent()
                                    }
                                    when(c.style) {
                                        "tonal"->FilledTonalButton({},modifier,contentPadding=padding,content=body)
                                        "elevated"->ElevatedButton({},modifier,contentPadding=padding,content=body)
                                        "outlined"->OutlinedButton({},modifier,contentPadding=padding,content=body)
                                        "text"->TextButton({},modifier,contentPadding=padding,content=body)
                                        else->Button({},modifier,contentPadding=padding,content=body)
                                    }
                                }
                            }
                        }
                    }
                }
                fun sample(id:String) {
                    fun geometry(probe:Probe)=probe.geometry().filter { it.name=="box" || it.name=="size" }
                    val points=mutableListOf(ProbePoint("button","button",null,geometry(button)),ProbePoint("content","content","button",geometry(label)))
                    if(c.icon)points+=ProbePoint("icon","icon","button",geometry(icon))
                    writer.submit(host.snapshot(id,id,points))
                }
                host.frame(-1000);host.frame(0);sample("initial")
                if(c.dynamic)for(time in listOf(16000L,32000L,64000L,80000L,96000L,160000L)) {
                    host.advanceTo(time)
                    if(time==16000L || time==80000L)precision.value=time==16000L
                    host.frame(time);sample("time-$time")
                }
            }
        } finally { precision.value=previous }
    }
}
