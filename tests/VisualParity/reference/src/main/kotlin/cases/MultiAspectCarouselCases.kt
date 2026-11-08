@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.*
import androidx.compose.foundation.lazy.grid.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.carousel.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class MultiAspectCarouselConfig(val vertical:Boolean=false,val rtl:Boolean=false,
    val itemSize:Double=120.0,val crossSize:Double=90.0,val offset:Double=40.0,val radius:Double=16.0,
    val border:Double=0.0,val action:String="none",val grid:Boolean=false,val shape:String="none")

@ParitySuite("multi-aspect-carousel")
class MultiAspectCarouselCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer)

    suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<MultiAspectCarouselConfig>()
        ComposeHost(writer,380,380).use { host ->
            val d=ComposeHost.RenderScale.toFloat()
            val state=LazyListState()
            val gridState=LazyGridState()
            val infos=List(8) { if(config.grid) MultiAspectCarouselItemDrawInfo(it,gridState) else MultiAspectCarouselItemDrawInfo(it,state) }
            val viewport=Probe()
            var main by mutableStateOf(config.itemSize)
            var radius by mutableStateOf(config.radius)
            var shapeKind by mutableStateOf(config.shape)
            host.mount {
                MaterialTheme {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(24.dp)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            MultiAspectCarouselScope {
                                val itemContent:@Composable (Int)->Unit = { index ->
                                        val shape=if(shapeKind=="none")RoundedCornerShape(radius.toFloat().dp) else carouselTestShape(shapeKind)
                                        var modifier=Modifier.size(
                                            (if(config.vertical)config.crossSize else main).toFloat().dp,
                                            (if(config.vertical)main else config.crossSize).toFloat().dp)
                                        if(config.border>0) modifier=modifier.maskBorder(BorderStroke(config.border.toFloat().dp,Color.Black),shape,infos[index])
                                        modifier=modifier.maskClip(shape,infos[index])
                                        Canvas(modifier) {
                                            drawRect(listOf(Color(0xFF6495ED),Color(0xFFFF7F50),Color(0xFF8FBC8F))[index%3])
                                            drawRect(Color(0xFF000080),size=Size(size.width/3,size.height/2))
                                            drawRect(Color(0xFFFFD700),topLeft=Offset(size.width*.6f,size.height*.5f),size=Size(size.width*.2f,size.height*.4f))
                                        }
                                }
                                val content:LazyListScope.()->Unit = { items(8) { index -> itemContent(index) } }
                                val gridContent:LazyGridScope.()->Unit = { items(8) { index -> itemContent(index) } }
                                val cross=if(config.grid)config.crossSize*2+8 else config.crossSize
                                if(config.grid && config.vertical) LazyVerticalGrid(GridCells.Fixed(2),
                                    viewport.modifier().size(cross.toFloat().dp,300.dp),gridState,
                                    verticalArrangement=Arrangement.spacedBy(8.dp),horizontalArrangement=Arrangement.spacedBy(8.dp),content=gridContent)
                                else if(config.grid) LazyHorizontalGrid(GridCells.Fixed(2),
                                    viewport.modifier().size(300.dp,cross.toFloat().dp),gridState,
                                    verticalArrangement=Arrangement.spacedBy(8.dp),horizontalArrangement=Arrangement.spacedBy(8.dp),content=gridContent)
                                else if(config.vertical) LazyColumn(viewport.modifier().size(config.crossSize.toFloat().dp,300.dp),state,
                                    verticalArrangement=Arrangement.spacedBy(8.dp),content=content)
                                else LazyRow(viewport.modifier().size(300.dp,config.crossSize.toFloat().dp),state,
                                    horizontalArrangement=Arrangement.spacedBy(8.dp),content=content)
                            }
                        }
                    }
                }
            }
            host.frame(0)
            fun scroll(offset:Double) {
                if(config.grid)gridState.requestScrollToItem(0,(offset*d).toInt())
                else state.requestScrollToItem(0,(offset*d).toInt())
            }
            scroll(config.offset);host.draw()
            fun sample(id:String) {
                val points=mutableListOf(ProbePoint("viewport","viewport",null,viewport.geometry().filter { it.name=="box" || it.name=="size" }))
                infos.forEachIndexed { index,info ->
                    if(info.size!=0f) points+=ProbePoint("item-$index","item","viewport",listOf(
                        number("maskStart",(info.maskStart/d).toDouble(),"dip",definition="carousel.mask.start"),
                        number("maskEnd",(info.maskEnd/d).toDouble(),"dip",definition="carousel.mask.end"),
                        number("parallax",(info.parallax/d).toDouble(),"dip",definition="carousel.content.parallax"),
                        number("minSize",(info.minSize/d).toDouble(),"dip",definition="carousel.mask.minimum")))
                }
                writer.submit(host.snapshot(id,id,points))
            }
            sample("initial")
            if(config.action=="none")return
            for((time,value) in listOf(16000L to 20.0,32000L to 80.0,48000L to 140.0,64000L to 40.0,80000L to 0.0)) {
                host.advanceTo(time)
                when(config.action) { "scroll" -> scroll(value); "resize" -> main=100+value; "shape" -> radius=value/2 }
                if(config.action=="replace-shape")shapeKind=if(value>40)"round-percent" else "percent"
                host.draw();host.frame(time);sample("time-$time")
            }
        }
    }
}
