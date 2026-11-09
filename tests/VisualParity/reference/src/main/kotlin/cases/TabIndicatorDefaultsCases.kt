@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CornerSize
import androidx.compose.foundation.shape.CutCornerShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Outline
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class TabIndicatorDefaultsConfig(val primary:Boolean=true,val indicatorWidth:Double=24.0,val indicatorHeight:Double=3.0,
    val outerWidth:Double?=null,val outerHeight:Double?=null,val unspecifiedWidth:Boolean=false,
    val shape:String="default",val color:String?=null,val rtl:Boolean=false,val dark:Boolean=false,val action:String="none")

@ParitySuite("tab-indicator-defaults")
class TabIndicatorDefaultsCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("changes") suspend fun changes(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,changes:Boolean) {
        ComposeHost(writer,240,120).use { host ->
            val config=writer.readConfig<TabIndicatorDefaultsConfig>(); val scene=Scene(config)
            host.mount { scene.Content() }; host.frame(0)
            fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("indicator","indicator",null,scene.probe.geometry().filter { it.name=="box" || it.name=="size" })
            ),image))
            fun change(time:Long,id:String,action:()->Unit){host.advanceTo(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
            sample("initial");if(!changes)return
            when(config.action) {
                "size" -> {
                    change(1000,"grow"){scene.height=12.0;if(config.primary)scene.width=60.0}
                    change(2000,"constrain"){scene.outerWidth=9.0;scene.outerHeight=2.0}
                    change(3000,"restore"){scene.outerWidth=null;scene.outerHeight=null;scene.height=3.0;if(config.primary)scene.width=24.0}
                }
                "shape" -> {change(1000,"cut"){scene.shape="cut"};change(2000,"path"){scene.shape="path"};change(3000,"restore"){scene.shape="default"}}
                "color" -> {change(1000,"alpha"){scene.color="#70336699"};change(2000,"transparent"){scene.color="#00000000"};change(3000,"restore"){scene.color=null}}
                "theme" -> {change(1000,"dark"){scene.dark=true};change(2000,"light"){scene.dark=false}}
            }
            host.advanceTo(5000);host.draw();sample("passive",false)
        }
    }
    private class Scene(val config:TabIndicatorDefaultsConfig) {
        var width by mutableStateOf(config.indicatorWidth);var height by mutableStateOf(config.indicatorHeight)
        var outerWidth by mutableStateOf(config.outerWidth);var outerHeight by mutableStateOf(config.outerHeight)
        var shape by mutableStateOf(config.shape);var color by mutableStateOf(config.color);var dark by mutableStateOf(config.dark)
        val probe=Probe()
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark)darkColorScheme() else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        var modifier=probe.modifier()
                        outerWidth?.let { modifier=modifier.width(it.toFloat().dp) }
                        outerHeight?.let { modifier=modifier.height(it.toFloat().dp) }
                        val paint=color?.let { Color(it.removePrefix("#").toLong(16)) } ?: MaterialTheme.colorScheme.primary
                        if(config.primary) TabRowDefaults.PrimaryIndicator(modifier,
                            width=if(config.unspecifiedWidth)Dp.Unspecified else width.toFloat().dp,
                            height=height.toFloat().dp,color=paint,shape=when(shape) {
                                "cut" -> CutCornerShape(topStart=8.dp,topEnd=1.dp,bottomEnd=4.dp,bottomStart=0.dp)
                                "percent" -> RoundedCornerShape(topStart=CornerSize(80),topEnd=CornerSize(0),bottomEnd=CornerSize(40),bottomStart=CornerSize(0))
                                "path" -> IndicatorPath
                                else -> RoundedCornerShape(3.dp)
                            })
                        else TabRowDefaults.SecondaryIndicator(modifier,height=height.toFloat().dp,color=paint)
                    }
                }
            }
        }
    }
    private object IndicatorPath:Shape {
        override fun createOutline(size:Size,layoutDirection:LayoutDirection,density:Density):Outline {
            fun x(value:Float)=if(layoutDirection==LayoutDirection.Rtl)size.width-value else value
            return Outline.Generic(Path().apply {
                moveTo(x(0f),size.height);lineTo(x(size.width*.2f),0f);lineTo(x(size.width),0f);lineTo(x(size.width*.8f),size.height);close()
            })
        }
    }
}
