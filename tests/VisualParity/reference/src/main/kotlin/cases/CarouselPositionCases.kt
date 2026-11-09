@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.pager.PagerState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.carousel.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.Saver
import androidx.compose.runtime.saveable.SaverScope
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class CarouselPositionConfig(val variant:String,val rtl:Boolean,val action:String,val item:Int=3,
    val fraction:Float=.25f,val width:Double=320.0,val itemWidth:Double=160.0,val spacing:Double=8.0)

@ParitySuite("carousel-position")
class CarouselPositionCases {
    @Suppress("UNCHECKED_CAST")
    @ParityScenario("restore") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<CarouselPositionConfig>()
        ComposeHost(writer,400,180).use { host ->
            var count by mutableStateOf(8)
            var width by mutableStateOf(c.width)
            var itemWidth by mutableStateOf(c.itemWidth)
            var state by mutableStateOf(CarouselState(c.item,c.fraction) { count })
            lateinit var scope:CoroutineScope
            val saver=CarouselState.Saver as Saver<CarouselState,Any>
            val saverScope=object:SaverScope { override fun canBeSaved(value:Any)=true }
            fun save()=with(saver) { checkNotNull(saverScope.save(state)) }
            fun pager()=CarouselState::class.java.getDeclaredField("pagerState").apply { isAccessible=true }.get(state) as PagerState
            fun restore(saved:Any) {
                state=checkNotNull(saver.restore(saved))
                // rememberCarouselState immediately replaces the saved count with its current
                // itemCount lambda. Keep that exact operation while calling the real Saver.
                val pager=pager()
                val pageCountState=pager.javaClass.getDeclaredField("pageCountState").apply { isAccessible=true }.get(pager) as MutableState<()->Int>
                pageCountState.value={ count }
            }
            host.mount {
                MaterialTheme {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(24.dp)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            // Replacing the restored owner disposes its old navigation scope.
                            key(state) {
                                scope=rememberCoroutineScope()
                                val content:@Composable CarouselItemScope.(Int)->Unit = { index ->
                                    Canvas(Modifier.fillMaxSize().maskClip(RoundedCornerShape(16.dp))) {
                                        drawRect(listOf(Color(0xFF6495ED),Color(0xFFFF7F50),Color(0xFF8FBC8F))[index%3])
                                        drawRect(Color(0xFF000080),size=Size(size.width/3,size.height/2))
                                        drawRect(Color(0xFFFFD700),topLeft=Offset(size.width*.6f,size.height*.5f),size=Size(size.width*.2f,size.height*.4f))
                                    }
                                }
                                val modifier=Modifier.size(width.toFloat().dp,120.dp)
                                when(c.variant) {
                                    "MultiBrowse" -> HorizontalMultiBrowseCarousel(state,itemWidth.toFloat().dp,modifier,itemSpacing=c.spacing.toFloat().dp,content=content)
                                    "Uncontained" -> HorizontalUncontainedCarousel(state,itemWidth.toFloat().dp,modifier,itemSpacing=c.spacing.toFloat().dp,content=content)
                                    else -> HorizontalCenteredHeroCarousel(state,modifier,maxItemWidth=itemWidth.toFloat().dp,itemSpacing=c.spacing.toFloat().dp,content=content)
                                }
                            }
                        }
                    }
                }
            }
            host.frame(0)
            val saved=save()
            fun sample(id:String) {
                val captured=save() as List<*>
                writer.submit(host.snapshot(id,id,listOf(
                    ProbePoint("position","carousel",null,listOf(
                        number("item",state.currentItem.toDouble(),"index",definition="carousel.currentItem"),
                        number("fraction",pager().currentPageOffsetFraction.toDouble(),"ratio",definition="carousel.currentItemOffsetFraction"),
                        flag("scrolling",state.isScrollInProgress,definition="carousel.isScrollInProgress"))),
                    ProbePoint("captured","position",null,listOf(
                        number("item",(captured[0] as Int).toDouble(),"index",definition="carousel.savedItem"),
                        number("fraction",(captured[1] as Float).toDouble(),"ratio",definition="carousel.savedOffsetFraction")))
                ),true))
            }
            sample("initial")
            if(c.action=="interrupt") {
                for(time in 16000L..320000L step 16000L) {
                    host.advanceTo(time)
                    if(time==16000L)scope.launch { state.animateScrollToItem(7) }
                    if(time==80000L)restore(saved)
                    host.frame(time)
                    if(time<=96000L || time==160000L || time==320000L)sample("time-$time")
                }
                return
            }
            host.advanceTo(16000)
            width=when(c.action) { "viewport"->240.0;"fractional"->273.0;else->c.width }
            itemWidth=when(c.action) { "item-width"->190.0;"fractional"->137.7;else->c.itemWidth }
            count=when(c.action) { "empty"->0;"shrink","shrink-negative"->3;else->8 }
            restore(saved)
            host.frame(16000);sample("restored")
            if(c.action=="empty") {
                host.advanceTo(48000);count=8;host.frame(48000);sample("populated")
            }
            host.frame(160000);sample("settled")
        }
    }
}
