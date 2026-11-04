@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
@file:Suppress("DEPRECATION")
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class SearchBarInputConfig(val dark:Boolean=false,val enabled:Boolean=true,val icons:Boolean=true,
    val rtl:Boolean=false,val hostWidth:Int=420,val action:String="none")

@ParitySuite("search-bar-input")
class SearchBarInputFieldCases {
    @ParityScenario("presentation") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<SearchBarInputConfig>()
        val field=Probe(); val slots=linkedMapOf<String,Probe>(); val requester=FocusRequester()
        var expanded by mutableStateOf(false); var focused=false; var requests=0
        @Composable fun Slot(name:String,width:Int,height:Int) {
            val probe=remember { Probe() }; SideEffect { slots[name]=probe }
            Box(probe.modifier().size(width.dp,height.dp).background(LocalContentColor.current))
        }
        ComposeHost(writer,c.hostWidth,160).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme()else lightColorScheme()) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            val leading:(@Composable ()->Unit)?=if(c.icons)({Slot("leading",24,24)})else null
                            val trailing:(@Composable ()->Unit)?=if(c.icons)({Slot("trailing",24,24)})else null
                            SearchBarDefaults.InputField(query="",onQueryChange={},onSearch={},expanded=expanded,
                                onExpandedChange={requests++;expanded=it},enabled=c.enabled,
                                modifier=field.modifier().focusRequester(requester).onFocusChanged{focused=it.isFocused},
                                placeholder={Slot("placeholder",48,16)},leadingIcon=leading,trailingIcon=trailing)
                        }
                    }
                }
            }; host.frame(-1000);host.frame(0)
            fun sample(id:String) {
                val points=mutableListOf(ProbePoint("field","search-input",null,field.geometry()),
                    ProbePoint("state","search-input-state","field",listOf(
                        flag("focused",focused,definition="search-input.focused"),flag("expanded",expanded,definition="search-input.expanded"),
                        number("requests",requests.toDouble(),"count",definition="search-input.requests"))))
                slots.forEach{(name,probe)->points.add(ProbePoint(name,name,"field",probe.geometry()))}
                writer.submit(host.snapshot(id,id,points,image=c.action=="none"))
            }
            sample("initial");if(c.action=="none")return
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
            change(1000,"focus"){requester.requestFocus()}
            advance(800000);sample("focused")
            change(800000,"collapse"){expanded=false}
            listOf(816000L,864000L,899000L,900000L,912000L,1000000L).forEach {
                advance(it);sample("time-$it")
                if(c.action=="reverse"&&it==864000L)change(it,"reverse"){expanded=true}
            }
        }
    }
}
