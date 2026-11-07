@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class MenuLabelConfig(val group:Boolean=true,val rtl:Boolean=false,val precision:Boolean=false,
    val contentWidth:Double=24.0,val contentHeight:Double=12.0,val availableWidth:Double=360.0,
    val width:Double=0.0,val height:Double=0.0,val customPadding:Boolean=false,val dynamic:Boolean=false,val inMenu:Boolean=false)

@ParitySuite("menu-label")
class MenuLabelCases {
    @ParityScenario("layout")
    suspend fun layout(writer:CaptureWriter) {
        val c=writer.readConfig<MenuLabelConfig>()
        @Suppress("UNCHECKED_CAST")
        val precision=Class.forName("androidx.compose.material3.PrecisionPointer").declaredMethods
            .single { it.name.startsWith("getShouldUsePrecisionPointerComponentSizing") }.invoke(null) as MutableState<Boolean>
        val previous=precision.value
        precision.value=c.precision
        try {
            ComposeHost(writer,400,200).use { host ->
                val label=Probe();val content=Probe()
                host.mount {
                    MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr,
                            LocalContentColor provides Color(0xFF000080)) {
                            Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                                Box(Modifier.width(c.availableWidth.toFloat().dp)) {
                                    val labelContent:@Composable ()->Unit={
                                        var modifier=label.modifier()
                                        if(c.width>0) modifier=modifier.width(c.width.toFloat().dp)
                                        if(c.height>0) modifier=modifier.height(c.height.toFloat().dp)
                                        val marker:@Composable ()->Unit={ Box(content.modifier().size(c.contentWidth.toFloat().dp,c.contentHeight.toFloat().dp).background(LocalContentColor.current)) }
                                        val padding=if(c.customPadding) PaddingValues(start=3.dp,top=5.dp,end=9.dp,bottom=7.dp)
                                            else if(c.group) MenuDefaults.DropdownMenuGroupLabelHorizontalPadding else MenuDefaults.DropdownMenuItemTrailingLabelHorizontalPadding
                                        if(c.group) MenuDefaults.DropdownMenuGroupLabel(modifier=modifier,padding=padding,content=marker)
                                        else MenuDefaults.DropdownMenuItemTrailingLabel(modifier=modifier,padding=padding,content=marker)
                                    }
                                    if(c.inMenu) SelectableDropdownMenuItem(selected=false,onClick={},shapes=MenuDefaults.itemShapes(),modifier=Modifier.width(220.dp),
                                        text={Box(Modifier.size(48.dp,16.dp).background(LocalContentColor.current))},trailingContent=labelContent)
                                    else labelContent()
                                }
                            }
                        }
                    }
                }
                host.frame(-1000);host.frame(0)
                fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                    ProbePoint("label","label",null,label.geometry().filter {it.name=="box" || it.name=="size"}),
                    ProbePoint("content","content","label",content.geometry().filter {it.name=="box" || it.name=="size"}))))
                sample("initial")
                if(c.dynamic) {
                    host.advanceTo(1000);precision.value=true;host.frame(1000);sample("precision-on")
                    host.advanceTo(2000);precision.value=false;host.frame(2000);sample("precision-off")
                }
            }
        } finally {precision.value=previous}
    }
}
