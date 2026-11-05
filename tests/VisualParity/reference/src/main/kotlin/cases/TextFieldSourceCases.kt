@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.interaction.*
import androidx.compose.ui.geometry.Offset
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class TextFieldSourceConfig(val outlined:Boolean,val action:String,val rtl:Boolean=false,val legacy:Boolean=false)

@ParitySuite("text-field-source")
class TextFieldSourceCases {
    @ParityScenario("source") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TextFieldSourceConfig>()
        ComposeHost(writer,400,200).use { host ->
            val scene=TextFieldCases.FieldScene(TextFieldConfig(c.outlined,
                if(c.outlined) "Cutout" else "Inside",slots=true,height=96.0,rtl=c.rtl),legacy=c.legacy)
            var source=MutableInteractionSource()
            val starts=mutableListOf<Interaction>();val events=mutableListOf<String>()
            fun observe(observed:MutableInteractionSource){host.scope.launch{observed.interactions.collect{interaction->
                val (name,start)=when(interaction){
                    is FocusInteraction.Focus->"focus" to interaction
                    is FocusInteraction.Unfocus->"unfocus" to interaction.focus
                    is PressInteraction.Press->"press" to interaction
                    is PressInteraction.Release->"release" to interaction.press
                    is PressInteraction.Cancel->"cancel" to interaction.press
                    else->"" to interaction
                }
                if(name.isNotEmpty()){
                    if(start===interaction)starts.add(start)
                    events.add("$name:${starts.indexOfFirst{it===start}}")
                }
            }}}
            observe(source)
            scene.interactions=source
            host.mount { scene.Content() };host.frame(-1000);host.frame(0)
            val first=FocusInteraction.Focus();val second=FocusInteraction.Focus()
            val native=c.action.startsWith("native")||c.action.startsWith("pointer")
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()+ProbePoint("events","events",null,
                listOf(text("sequence",events.joinToString(","),definition="text-field.interactions.paired-sequence"))),image=!native))
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"source",id);host.draw();host.frame(time);sample(id)}
            fun at(time:Long){advance(time);host.draw();sample("time-$time")}
            fun emit(interaction:Interaction){check(source.tryEmit(interaction))}
            sample("initial")
            if(c.action.startsWith("pointer")){
                val point=checkNotNull(scene.field.coordinates).localToRoot(Offset(140f,40f)*ComposeHost.RenderScale.toFloat())/ComposeHost.RenderScale.toFloat()
                change(1000,"press"){host.input("move",point);host.input("down",point)}
                at(16000);at(64000)
                if(c.action=="pointer-cancel")change(65000,"outside"){host.input("move",Offset(380f,190f))}
                if(c.action=="pointer-replace")change(65000,"replace"){source=MutableInteractionSource();observe(source);scene.interactions=source}
                at(96000);change(97000,"release"){host.input("up",point)}
                at(160000);at(400000);change(401000,"blur"){scene.focusManager.clearFocus()};at(800000)
                return
            }
            change(1000,"focus"){if(native)scene.requester.requestFocus() else emit(first)}
            at(16000);at(64000)
            when(c.action){
                "reverse"->{
                    change(65000,"unfocus"){emit(FocusInteraction.Unfocus(first))};at(96000)
                    change(97000,"refocus"){emit(second)}
                }
                "replace","native-replace"->{
                    change(65000,"replace"){source=MutableInteractionSource();observe(source);scene.interactions=source};at(96000)
                    change(97000,"new-source-event"){emit(HoverInteraction.Enter())}
                }
                "disable"->{change(65000,"disable"){scene.enabled=false};at(96000)}
                "overlap"->{
                    change(65000,"overlap"){emit(second)}
                    change(97000,"first-end"){emit(FocusInteraction.Unfocus(first))}
                }
            }
            at(160000);at(400000);at(800000)
            change(801000,"end"){
                if(native)scene.focusManager.clearFocus()
                else if(c.action=="disable"){scene.enabled=true;emit(FocusInteraction.Unfocus(first))}
                else emit(FocusInteraction.Unfocus(second))
            }
            at(864000);at(960000);at(1200000);at(1600000)
        }
    }
}
