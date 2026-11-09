@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.MutatorMutex
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.*
import androidx.compose.ui.graphics.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class TooltipContainerShapeConfig(val rich:Boolean=false,val shape:String="cut",val caret:Boolean=false,
    val rtl:Boolean=false,val position:String="Above",val dynamic:Boolean=false,val sameWindingCaret:Boolean=false)

@ParitySuite("tooltip-container-shape")
class TooltipContainerShapeCases {
    @ParityScenario("render")
    suspend fun render(writer:CaptureWriter) {
        val c=writer.readConfig<TooltipContainerShapeConfig>()
        ComposeHost(writer,256,192).use {host->
            val anchor=Probe();val tooltip=Probe()
            var topStart by mutableStateOf(18f);var bottomEnd by mutableStateOf(14f)
            var width by mutableStateOf(96f);var rtl by mutableStateOf(c.rtl)
            var caret by mutableStateOf(c.caret);var kind by mutableStateOf(c.shape)
            val state=TooltipState(initialIsVisible=true,mutatorMutex=MutatorMutex())
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=AbsoluteAlignment.TopLeft) {
                            val shapeKind=kind
                            val container:Shape=when(shapeKind) {
                                "cut"->CutCornerShape(topStart.dp,4.dp,bottomEnd.dp,2.dp)
                                "round"->RoundedCornerShape(CornerSize(35),CornerSize(4.dp),CornerSize(25),CornerSize(2.dp))
                                "hole","winding-hole","curve"->GenericShape {size,_->
                                    val w=size.width;val h=size.height
                                    fillType=PathFillType.EvenOdd
                                    if(shapeKind=="hole"||shapeKind=="winding-hole") {
                                        moveTo(0f,0f);lineTo(w,0f);lineTo(w,h);lineTo(0f,h);close()
                                        if(shapeKind=="hole") {
                                            moveTo(w/3,h/3);lineTo(w*2/3,h/3);lineTo(w*2/3,h*2/3);lineTo(w/3,h*2/3);close()
                                        } else {
                                            moveTo(w/3,h/3);lineTo(w/3,h*2/3);lineTo(w*2/3,h*2/3);lineTo(w*2/3,h/3);close()
                                        }
                                    } else {
                                        moveTo(0f,0f);lineTo(w,0f);lineTo(w,h/2);cubicTo(w,h,w/2,h,0f,h/2);close()
                                    }
                                }
                                else->if(c.rich)TooltipDefaults.richTooltipContainerShape else TooltipDefaults.plainTooltipContainerShape
                            }
                            val position=when(c.position) {
                                "Below"->TooltipAnchorPosition.Below;"Start"->TooltipAnchorPosition.Start;"End"->TooltipAnchorPosition.End
                                else->TooltipAnchorPosition.Above
                            }
                            Box(Modifier.absoluteOffset(116.dp,80.dp)) {
                                TooltipBox(positionProvider=TooltipDefaults.rememberTooltipPositionProvider(position),state=state,enableUserInput=false,
                                    tooltip={
                                        val body:@Composable ()->Unit={
                                            Canvas(Modifier.size(64.dp,16.dp)) {
                                                val d=ComposeHost.RenderScale.toFloat()
                                                drawRect(Color(0xFFFFD600),Offset(-200*d,-200*d),Size(500*d,500*d))
                                            }
                                        }
                                        val caretShape=if(c.sameWindingCaret)GenericShape {_,_->
                                            val d=ComposeHost.RenderScale.toFloat();fillType=PathFillType.EvenOdd
                                            moveTo(-12*d,0f);lineTo(12*d,0f);lineTo(12*d,12*d);lineTo(-12*d,12*d);close()
                                            moveTo(-4*d,3*d);lineTo(4*d,3*d);lineTo(4*d,8*d);lineTo(-4*d,8*d);close()
                                        }else if(caret)TooltipDefaults.caretShape()else null
                                        if(c.rich) RichTooltip(modifier=tooltip.modifier().size(width.dp,64.dp),shape=container,caretShape=caretShape,
                                            colors=TooltipDefaults.richTooltipColors(containerColor=Color(0xFF6750A4)),shadowElevation=6.dp,text=body)
                                        else PlainTooltip(modifier=tooltip.modifier().size(width.dp,64.dp),shape=container,caretShape=caretShape,
                                            containerColor=Color(0xFF6750A4),shadowElevation=6.dp,content=body)
                                    }) {Box(anchor.modifier().size(24.dp).background(Color.Blue))}
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("anchor","anchor",null,anchor.geometry().filter{it.name=="box"||it.name=="size"}),
                ProbePoint("tooltip","tooltip",null,tooltip.geometry().filter{it.name=="box"||it.name=="size"}))))
            sample("initial")
            fun change(time:Long,id:String,change:()->Unit){host.advanceTo(time);change();host.frame(time);sample(id)}
            if(c.dynamic) {
                change(1000,"mutate"){topStart=30f;bottomEnd=0f}
                change(2000,"resize"){width=120f}
                change(3000,"rtl"){rtl=true}
                change(4000,"caret"){caret=!c.caret}
                change(5000,"replace"){kind="curve"}
                change(6000,"clear"){kind="none"}
            }
        }
    }
}
