@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable

@Serializable
data class ListItemLongClickConfig(val segmented:Boolean=false,val mode:String="click",val action:String="invoke")

@ParitySuite("list-item-long-click")
class ListItemLongClickCases {
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<ListItemLongClickConfig>()
        ComposeHost(writer,320,180).use { host ->
            val probe=Probe()
            var mode by mutableStateOf(if(config.action=="passive") "none" else config.mode)
            var label by mutableStateOf<String?>(if(config.action=="no-label") null else "More actions")
            var hasCallback by mutableStateOf(config.action!="no-handler")
            var selected by mutableStateOf(false)
            val enabled=config.action!="disabled"
            var longClicks=0
            var clicks=0
            var requests=0
            val callback:()->Unit={longClicks++; if(config.action=="mode-change") {mode="multiple";selected=true}}
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        val modifier=probe.modifier().width(280.dp)
                        val longClick=if(hasCallback)callback else null
                        val shapes=ListItemDefaults.segmentedShapes(0,1)
                        val content:@Composable ()->Unit={
                            val color=LocalContentColor.current
                            Canvas(Modifier.size(64.dp,24.dp)) {drawRect(color)}
                        }
                        if(!config.segmented) when(mode) {
                            "none"->ListItem(modifier=modifier,enabled=enabled,content=content)
                            "single"->ListItem(selected=selected,onClick={clicks++},modifier=modifier,enabled=enabled,onLongClick=longClick,onLongClickLabel=label,content=content)
                            "multiple"->ListItem(checked=selected,onCheckedChange={requests++},modifier=modifier,enabled=enabled,onLongClick=longClick,onLongClickLabel=label,content=content)
                            else->ListItem(onClick={clicks++},modifier=modifier,enabled=enabled,onLongClick=longClick,onLongClickLabel=label,content=content)
                        } else when(mode) {
                            "none"->SegmentedListItem(shapes=shapes,modifier=modifier,enabled=enabled,content=content)
                            "single"->SegmentedListItem(selected=selected,onClick={clicks++},shapes=shapes,modifier=modifier,enabled=enabled,onLongClick=longClick,onLongClickLabel=label,content=content)
                            "multiple"->SegmentedListItem(checked=selected,onCheckedChange={requests++},shapes=shapes,modifier=modifier,enabled=enabled,onLongClick=longClick,onLongClickLabel=label,content=content)
                            else->SegmentedListItem(onClick={clicks++},shapes=shapes,modifier=modifier,enabled=enabled,onLongClick=longClick,onLongClickLabel=label,content=content)
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun semantics(node:Any):List<SemanticsConfiguration> {
                val own=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                val children=node.javaClass.methods.single {it.name.startsWith("getChildren$") && it.parameterCount==0}.invoke(node) as List<*>
                return listOfNotNull(own)+children.flatMap {semantics(checkNotNull(it))}
            }
            fun actionConfig():SemanticsConfiguration? {
                val configs=semantics(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode")))
                // Fixed Core Desktop ComposeAccessible only exports actions with a non-null label.
                return configs.firstOrNull {it.getOrNull(SemanticsActions.OnLongClick)?.label!=null}
            }
            fun measure():List<ProbePoint> {
                val semantics=actionConfig()
                val action=semantics?.getOrNull(SemanticsActions.OnLongClick)
                return listOf(ProbePoint("item","list-item",null,probe.geometry().filter {it.name=="box" || it.name=="size"}+listOf(
                    flag("hasNamedAction",action!=null,definition="list-item.named-long-click-present"),
                    flag("actionEnabled",action!=null && semantics?.contains(SemanticsProperties.Disabled)!=true,definition="list-item.named-long-click-enabled"),
                    text("label",action?.label?:"",definition="list-item.named-long-click-label"),
                    number("longClicks",longClicks.toDouble(),"count",definition="list-item.long-clicks"),
                    number("clicks",clicks.toDouble(),"count",definition="list-item.clicks"),
                    number("requests",requests.toDouble(),"count",definition="list-item.checked-requests"),
                    flag("selected",selected && mode in listOf("single","multiple"),definition="list-item.selected"))))
            }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,measure()))
            var next=16000L
            fun advance(time:Long) {while(next<=time) {host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,mutation:()->Unit) {advance(time);mutation();writer.record(time,"mutation",id);host.scheduler.runCurrent();host.draw()}
            fun at(time:Long) {advance(time);host.draw();sample("time-$time")}
            fun invoke() {actionConfig()?.getOrNull(SemanticsActions.OnLongClick)?.action?.invoke()}
            sample("initial")
            // Deliberately retain the disabled raw-semantics versus native-provider difference.
            change(1000,"invoke-long-click",::invoke)
            at(16000);at(64000);at(160000)
            if(config.action=="lifecycle") {
                change(161000,"rename") {label="Archive"};at(176000)
                change(177000,"remove-handler") {hasCallback=false};at(192000)
                change(193000,"invoke-absent",::invoke);at(208000)
                change(209000,"restore-handler") {hasCallback=true};at(224000)
                change(225000,"invoke-restored",::invoke);at(240000)
                change(241000,"remove-label") {label=null};at(256000)
            }
            at(400000)
        }
    }
}
