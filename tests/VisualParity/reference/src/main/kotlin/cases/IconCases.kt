@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.*
import androidx.compose.ui.graphics.*
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.scale
import androidx.compose.ui.graphics.painter.*
import androidx.compose.ui.graphics.vector.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class IconConfig(val source:String="painter",val intrinsic:String="wide",val tint:Boolean=true,val rtl:Boolean=false,
    val mirror:Boolean=false,val width:Double=0.0,val height:Double=0.0,val availableWidth:Double=100.0,val availableHeight:Double=100.0,
    val minimumWidth:Double=0.0,val minimumHeight:Double=0.0,val action:String="none")

@ParitySuite("icon")
class IconCases {
    @ParityScenario("render")
    suspend fun render(writer:CaptureWriter) {
        val c=writer.readConfig<IconConfig>()
        ComposeHost(writer,160,160).use { host ->
            val probe=Probe()
            var tinted by mutableStateOf(c.tint)
            var color by mutableStateOf(Color(0xC0008000))
            var source by mutableStateOf(c.source)
            var rtl by mutableStateOf(c.rtl)
            var mirror by mutableStateOf(c.mirror)
            var width by mutableStateOf(c.width)
            var height by mutableStateOf(c.height)
            var firstColor by mutableStateOf(Color.Red)
            val density=ComposeHost.RenderScale.toFloat()
            val intrinsic=when(c.intrinsic) {
                "none"->Size.Unspecified;"infinite"->Size(Float.POSITIVE_INFINITY,Float.POSITIVE_INFINITY)
                "width-only"->Size(32*density,Float.POSITIVE_INFINITY);"height-only"->Size(Float.POSITIVE_INFINITY,16*density)
                "zero"->Size.Zero;"fractional"->Size(31.3f*density,15.7f*density);else->Size(32*density,16*density)
            }
            val bitmap=ImageBitmap(128,64)
            Canvas(bitmap).apply {
                drawRect(Rect(0f,0f,64f,64f),Paint().apply {this.color=Color.Red})
                drawRect(Rect(64f,0f,128f,32f),Paint().apply {this.color=Color(0x800000FF)})
            }
            host.mount {
                MaterialTheme {
                    CompositionLocalProvider(LocalLayoutDirection provides if(rtl) LayoutDirection.Rtl else LayoutDirection.Ltr,
                        LocalContentColor provides color) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            Box(Modifier.size(c.availableWidth.toFloat().dp,c.availableHeight.toFloat().dp)) {
                                var modifier=probe.modifier()
                                if(width>0)modifier=modifier.width(width.toFloat().dp)
                                if(height>0)modifier=modifier.height(height.toFloat().dp)
                                modifier=modifier.sizeIn(minWidth=c.minimumWidth.toFloat().dp,minHeight=c.minimumHeight.toFloat().dp)
                                val painter=when {
                                    source=="bitmap" -> BitmapPainter(bitmap)
                                    source=="vector" || c.action=="drawing" -> rememberVectorPainter(
                                        ImageVector.Builder(defaultWidth=32.dp,defaultHeight=16.dp,viewportWidth=32f,viewportHeight=16f,autoMirror=mirror).apply {
                                            path(fill=SolidColor(firstColor)) {moveTo(0f,0f);lineTo(16f,0f);lineTo(16f,16f);lineTo(0f,16f);close()}
                                            path(fill=SolidColor(Color(0x800000FF))) {moveTo(16f,0f);lineTo(32f,0f);lineTo(32f,8f);lineTo(16f,8f);close()}
                                        }.build())
                                    else -> object:Painter() {
                                        override val intrinsicSize=intrinsic
                                        override fun DrawScope.onDraw() {
                                            fun DrawScope.pattern() {
                                                drawRect(Color.Red,size=Size(size.width/2,size.height))
                                                drawRect(Color(0x800000FF),topLeft=Offset(size.width/2,0f),size=Size(size.width/2,size.height/2))
                                            }
                                            if(mirror && rtl) scale(-1f,1f) {pattern()} else pattern()
                                        }
                                    }
                                }
                                Icon(painter,contentDescription=null,modifier=modifier,tint=if(tinted) LocalContentColor.current else Color.Unspecified)
                            }
                        }
                    }
                }
            }
            host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("icon","icon",null,probe.geometry().filter {it.name=="box" || it.name=="size"}))))
            sample("initial")
            if(c.action!="none") {
                host.advanceTo(1000)
                when(c.action) {
                    "tint"->color=Color(0xFF800080);"source"->source="bitmap";"size"->{width=55.25;height=33.75}
                    "direction"->{rtl=true;mirror=true};"drawing"->firstColor=Color(0xFFFFD700)
                }
                host.frame(1000);sample("changed")
                host.advanceTo(2000);tinted=false;host.frame(2000);sample("untinted")
            }
        }
    }
}
