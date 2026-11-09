@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.RectangleShape
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class CardColumnConfig(val height:Double=101.0,val width:Double=101.0,val firstWeight:Double=1.0,
    val secondWeight:Double=2.0,val thirdWeight:Double=3.0,val fill:Boolean=true,val allUnfilled:Boolean=false,
    val fixedHeight:Double=13.0,val rtl:Boolean=false,val unbounded:Boolean=false,val minimum:Double=0.0,val action:String="none")

@ParitySuite("card-column")
class CardColumnCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val c=writer.readConfig<CardColumnConfig>()
        ComposeHost(writer,320,240).use { host ->
            val card=Probe();val column=Probe();val fixed=Probe()
            val items=List(3){Probe()};val content=List(3){Probe()}
            var firstWeight by mutableStateOf(c.firstWeight.toFloat());var fill by mutableStateOf(c.fill)
            var secondFill by mutableStateOf(!c.allUnfilled);var height by mutableStateOf(c.height)
            var width by mutableStateOf(c.width);var rtl by mutableStateOf(c.rtl)
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            val body:@Composable ()->Unit={
                                Card(modifier=card.modifier(),shape=RectangleShape,colors=CardDefaults.cardColors(containerColor=Color.White)) {
                                    Column(column.modifier().width(width.dp).heightIn(min=c.minimum.dp,max=if(c.unbounded)Dp.Infinity else height.dp)) {
                                        val weights=listOf(firstWeight,c.secondWeight.toFloat(),c.thirdWeight.toFloat())
                                        val colors=listOf(Color(0xff336699),Color(0xff996633),Color(0xff339966))
                                        for(i in 0..2) {
                                            val weighted=if(weights[i]>0) Modifier.weight(weights[i],if(i==0)fill else if(i==1)secondFill else !c.allUnfilled) else Modifier
                                            Box(items[i].modifier().then(weighted).align(listOf(Alignment.Start,Alignment.CenterHorizontally,Alignment.End)[i])) {
                                                Box(content[i].modifier().size((20+i*10).dp,(10+i).dp).background(colors[i]))
                                            }
                                            if(i==0)Box(fixed.modifier().size(51.dp,c.fixedHeight.dp).background(Color(0xff888888)))
                                        }
                                    }
                                }
                            }
                            if(c.unbounded) {
                                Layout(content=body) { measurables,constraints ->
                                    val placeable=measurables.single().measure(constraints.copy(minHeight=0,maxHeight=Constraints.Infinity))
                                    layout(placeable.width,placeable.height){placeable.place(0,0)}
                                }
                            } else body()
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun geometry(id:String,probe:Probe)=ProbePoint(id,id,null,probe.geometry().filter{it.name=="box"||it.name=="size"})
            fun sample(id:String){
                val points=mutableListOf(geometry("card",card),geometry("column",column),geometry("fixed",fixed))
                for(i in 0..2){points.add(geometry("item-$i",items[i]));points.add(geometry("content-$i",content[i]))}
                writer.submit(host.snapshot(id,id,points))
            }
            sample("initial")
            if(dynamic)for(step in 1..3){
                host.advanceTo(step*16000L-1000)
                when(c.action){
                    "weights"->firstWeight=if(step==2)0f else step*2f
                    "fill"->{fill=step==2;secondFill=step!=3}
                    "constraints"->{height=if(step==1)77.0 else if(step==2)5.0 else 123.0;width=if(step==2)73.0 else 101.0}
                    "direction"->rtl=(step%2==1)!=c.rtl
                }
                host.frame(step*16000L);sample("frame-$step")
            }
        }
    }
}
