@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.focusable
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.input.TextFieldState
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.coroutines.launch
import kotlinx.coroutines.CoroutineScope
import kotlinx.serialization.Serializable

@Serializable data class StateSearchInputConfig(val action:String="none",val dark:Boolean=false,val rtl:Boolean=false,
    val readOnly:Boolean=false,val enabled:Boolean=true,val initiallyExpanded:Boolean=false,val matchedFont:Boolean=false)

@ParitySuite("state-search-input")
class StateSearchInputCases {
    @ParityScenario("input") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<StateSearchInputConfig>()
        val field=Probe();val requester=FocusRequester();val text=TextFieldState()
        lateinit var state:SearchBarState
        lateinit var scope:CoroutineScope
        var source by mutableStateOf(MutableInteractionSource())
        var generation by mutableIntStateOf(0)
        var focused=false;var suggestionFocused=false
        @Composable fun Marker(w:Int,h:Int) { Box(Modifier.size(w.dp,h.dp).background(LocalContentColor.current)) }
        ComposeHost(writer,420,180).use { host ->
            host.mount {
                val family=remember { CaseDefaults.font(writer) }
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    scope=rememberCoroutineScope()
                    state=key(generation){rememberSearchBarState(if(c.initiallyExpanded)SearchBarValue.Expanded else SearchBarValue.Collapsed)}
                    Box(Modifier.fillMaxSize().background(Color.White)) {
                        Box(Modifier.padding(16.dp)) {
                            CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                                SearchBarDefaults.InputField(textFieldState=text,searchBarState=state,onSearch={},
                                    enabled=c.enabled,readOnly=c.readOnly,interactionSource=source,
                                    textStyle=if(c.matchedFont)MaterialTheme.typography.bodyLarge.copy(fontFamily=family)else LocalTextStyle.current,
                                    modifier=field.modifier().focusRequester(requester).onFocusChanged{focused=it.isFocused},
                                    placeholder={Marker(48,16)},leadingIcon={Marker(24,24)},trailingIcon={Marker(24,24)})
                            }
                        }
                        Box(Modifier.offset(16.dp,96.dp).size(32.dp,24.dp).onFocusChanged{suggestionFocused=it.isFocused}.focusable())
                    }
                }
            };host.frame(-1000);host.frame(0)
            fun sample(id:String) { writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("field","input",null,field.geometry()),
                ProbePoint("state","search-state","field",listOf(
                    number("progress",state.progress.toDouble(),"fraction",definition="search-state.progress"),
                    flag("expanded",state.currentValue==SearchBarValue.Expanded,definition="search-state.expanded"),
                    flag("targetExpanded",state.targetValue==SearchBarValue.Expanded,definition="search-state.target"),
                    flag("focused",focused,definition="search-input.focused"),
                    flag("suggestionFocused",suggestionFocused,definition="search-input.suggestion-focused"),
                    number("length",text.text.length.toDouble(),"count",definition="search-input.length")))
            ),image=c.action=="none")) }
            sample("initial");if(c.action=="none")return
            var next=16000L
            fun advance(target:Long){while(next<=target){host.frame(next);next+=16000};host.advanceTo(target)}
            fun change(at:Long,id:String,action:()->Unit){advance(at);action();host.draw();host.frame(at);sample(id)}
            fun key(){host.scene.sendKeyEvent(KeyEvent(Key.DirectionDown,KeyEventType.KeyDown));host.scheduler.runCurrent()}
            change(1000,"focus"){if(c.enabled)requester.requestFocus()}
            if(c.initiallyExpanded)change(2000,"seed-collapse"){scope.launch{state.snapTo(0f)}}
            val press=PressInteraction.Press(Offset(30f,25f))
            change(81000,"trigger"){
                when(c.action){
                    "down","collapse-keyboard"->key()
                    "click","collapse-pointer","reverse"->host.input("down",Offset(60f,40f))
                    "growth"->text.edit{append("x")}
                    "coalesced"->{text.edit{append("x")};text.edit{replace(0,length,"")}}
                    "coalesced-grow"->{text.edit{append("x")};text.edit{append("y")}}
                    "state-growth"->{generation++;text.edit{append("x")}}
                    "release"->source.tryEmit(PressInteraction.Release(press))
                    "cancel"->{source.tryEmit(press);source.tryEmit(PressInteraction.Cancel(press))}
                    "replace"->{val old=source;source=MutableInteractionSource();host.draw();old.tryEmit(PressInteraction.Release(press))}
                }
            }
            if(c.action in listOf("click","collapse-pointer","reverse"))change(97000,"release"){host.input("up",Offset(60f,40f))}
            listOf(112000L,160000L,240000L,400000L,800000L).forEach{advance(it);sample("time-$it")}
            if(c.action=="down"&&c.enabled)change(801000,"suggestions"){key()}
            if(c.action.startsWith("collapse-")||c.action=="reverse"){
                change(801000,"collapse"){scope.launch{state.animateToCollapsed()}}
                if(c.action=="reverse")change(881000,"reverse"){scope.launch{state.animateToExpanded()}}
                listOf(896000L,1008000L,1200000L,1600000L,1800000L).forEach{advance(it);sample("time-$it")}
            }
        }
    }
}

@ParitySuite("state-search-batched")
class StateSearchBatchedCases {
    @ParityScenario("input") suspend fun capture(writer:CaptureWriter)=StateSearchInputCases().capture(writer)
}
