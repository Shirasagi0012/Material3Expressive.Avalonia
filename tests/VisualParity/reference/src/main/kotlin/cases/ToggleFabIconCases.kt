@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
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
data class ToggleFabIconConfig(val size:String="default",val source:String="vector",val checked:Boolean=false,
    val rtl:Boolean=false,val custom:Boolean=false,val constrained:Boolean=false,val action:String="none")

@ParitySuite("toggle-fab-icon")
class ToggleFabIconCases {
    @ParityScenario("render")
    suspend fun render(writer:CaptureWriter) {
        val c=writer.readConfig<ToggleFabIconConfig>()
        ComposeHost(writer,200,180).use { host ->
            val fab=Probe();val icon=Probe()
            var checked by mutableStateOf(c.checked)
            var source by mutableStateOf(c.source)
            var narrow by mutableStateOf(false)
            var progressReader:()->Float={if(checked)1f else 0f}
            var drawingWidthReader:()->Float={20f}
            val bitmap=ImageBitmap(128,64)
            Canvas(bitmap).apply {
                drawRect(Rect(0f,0f,80f,64f),Paint().apply {color=Color.Red})
                drawRect(Rect(48f,0f,128f,32f),Paint().apply {color=Color(0x800000FF)})
            }
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            val containerSize=remember(c.size) {when(c.size) {
                                "medium"->ToggleFloatingActionButtonDefaults.containerSizeMedium()
                                "large"->ToggleFloatingActionButtonDefaults.containerSizeLarge()
                                else->ToggleFloatingActionButtonDefaults.containerSize()
                            }}
                            val radius=remember(c.size) {when(c.size) {
                                "medium"->ToggleFloatingActionButtonDefaults.containerCornerRadiusMedium()
                                "large"->ToggleFloatingActionButtonDefaults.containerCornerRadiusLarge()
                                else->ToggleFloatingActionButtonDefaults.containerCornerRadius()
                            }}
                            Box(if(c.constrained)Modifier.size(24.dp)else Modifier,contentAlignment=Alignment.Center) {
                            Box(fab.modifier()) {
                            ToggleFloatingActionButton(checked,{checked=it},Modifier,
                                containerSize=containerSize,containerCornerRadius=radius) {
                                val progress=checkedProgress;progressReader={progress}
                                val defaultSize=when(c.size) {
                                    "medium"->ToggleFloatingActionButtonDefaults.iconSizeMedium()
                                    "large"->ToggleFloatingActionButtonDefaults.iconSizeLarge()
                                    else->ToggleFloatingActionButtonDefaults.iconSize()
                                }
                                val defaultColor=ToggleFloatingActionButtonDefaults.iconColor()
                                val iconSizeFunction:(Float)->Dp=if(c.custom)({p->(25.25+8*p).toFloat().dp})else defaultSize
                                val iconColorFunction:(Float)->Color=if(c.custom)({p->Color((40+80*p).toInt(),140,30,128)})else defaultColor
                                // Read state in composition before the path-builder lambda is memoized.
                                val redWidth=if(narrow)8f else 20f
                                drawingWidthReader={redWidth}
                                val painter=when(source) {
                                    "bitmap"->BitmapPainter(bitmap)
                                    "vector"->rememberVectorPainter(ImageVector.Builder(defaultWidth=32.dp,defaultHeight=16.dp,
                                        viewportWidth=32f,viewportHeight=16f,autoMirror=c.rtl).apply {
                                        path(fill=SolidColor(Color.Red)) {moveTo(0f,0f);lineTo(redWidth,0f);lineTo(redWidth,16f);lineTo(0f,16f);close()}
                                        path(fill=SolidColor(Color(0x800000FF))) {moveTo(12f,0f);lineTo(32f,0f);lineTo(32f,8f);lineTo(12f,8f);close()}
                                    }.build())
                                    else->object:Painter() {
                                        override val intrinsicSize=Size(32f*ComposeHost.RenderScale,16f*ComposeHost.RenderScale)
                                        override fun DrawScope.onDraw() {
                                            fun DrawScope.pattern() {
                                                drawRect(Color.Red,size=Size(size.width*20/32,size.height))
                                                drawRect(Color(0x800000FF),topLeft=Offset(size.width*12/32,0f),size=Size(size.width*20/32,size.height/2))
                                            }
                                            if(c.rtl)scale(-1f,1f){pattern()}else pattern()
                                        }
                                    }
                                }
                                val animation=with(ToggleFloatingActionButtonDefaults){icon.modifier().animateIcon({progress},iconColorFunction,iconSizeFunction)}
                                // Tint belongs to animateIcon's whole layer, not to the image painter.
                                // BitmapPainter has no auto-mirror flag, so its explicit transform matches MaterialIcon.AutoMirror.
                                if(source=="bitmap" && c.rtl) {
                                    val mirrored=object:Painter() {
                                        override val intrinsicSize=painter.intrinsicSize
                                        override fun DrawScope.onDraw() {with(painter){scale(-1f,1f){draw(size)}}}
                                    }
                                    Icon(mirrored,null,animation,tint=Color.Unspecified)
                                } else Icon(painter,null,animation,tint=Color.Unspecified)
                            }
                            }
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("fab","fab",null,fab.geometry().filter {it.name=="box"||it.name=="size"}+number("checkedProgress",progressReader().toDouble(),"ratio",category="motion",definition="fab.checked-progress")),
                ProbePoint("icon","icon","fab",icon.geometry().filter {it.name=="box"||it.name=="size"}+number("drawingWidth",drawingWidthReader().toDouble(),"dip",definition="icon.source.red-rectangle-width")))))
            sample("initial")
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun at(time:Long){advance(time);host.draw();sample("time-$time")}
            if(c.action!="none") {
                advance(1000)
                when(c.action){"source"->source="bitmap";"drawing","drawing-repaint"->narrow=true;else->checked=!checked}
                host.frame(1000);sample("changed");at(16000);at(64000)
                if(c.action=="reverse"){advance(65000);checked=!checked;host.frame(65000);sample("reversed")}
                if(c.action=="drawing-repaint"){advance(65000);checked=true;host.frame(65000);sample("repaint")}
                at(96000);at(160000);at(320000);at(800000)
            }
        }
    }
}
