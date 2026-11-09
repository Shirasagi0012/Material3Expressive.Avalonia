@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.input.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.semantics.*
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.text.TextRange
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class SecureObfuscationConfig(val outlined:Boolean=false,val mode:String="RevealLastTyped",val action:String="typing")

@ParitySuite("secure-obfuscation")
class SecureTextFieldObfuscationCases {
    @ParityScenario("input") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<SecureObfuscationConfig>()
        ComposeHost(writer,360,160).use { host ->
            val state=TextFieldState();val requester=FocusRequester();val probe=Probe()
            lateinit var focus:FocusManager
            var mode by mutableStateOf(when(c.mode) {
                "Hidden"->TextObfuscationMode.Hidden;"Visible"->TextObfuscationMode.Visible
                "System"->TextObfuscationMode.System;else->TextObfuscationMode.RevealLastTyped
            })
            var enabled by mutableStateOf(true)
            var layout:TextLayoutResult?=null
            host.mount {
                focus=LocalFocusManager.current
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive(),
                    typography=Typography(fontFamily=CaseDefaults.font(writer))) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        val modifier=probe.modifier().width(280.dp).focusRequester(requester)
                        // Both public cursor colors are transparent to isolate the actual
                        // password display from unrelated native caret timer implementations.
                        if(c.outlined)OutlinedSecureTextField(state,modifier,enabled=enabled,textObfuscationMode=mode,
                            colors=OutlinedTextFieldDefaults.colors(cursorColor=Color.Transparent),
                            onTextLayout={get->layout=get()})
                        else SecureTextField(state,modifier,enabled=enabled,textObfuscationMode=mode,
                            colors=TextFieldDefaults.colors(cursorColor=Color.Transparent),
                            onTextLayout={get->layout=get()})
                    }
                }
            };host.frame(0);requester.requestFocus();host.draw();host.frame(0)
            var next=16000L
            fun advance(time:Long) {while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun semantics(node:Any):List<SemanticsConfiguration> {
                val own=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                val children=node.javaClass.methods.single{it.name.startsWith("getChildren$")&&it.parameterCount==0}.invoke(node) as List<*>
                return listOfNotNull(own)+children.flatMap{semantics(checkNotNull(it))}
            }
            fun all()=semantics(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode")))
            fun type(character:Char) {
                // Fixed desktop Foundation recognizes text only from AWT KEY_TYPED;
                // a generic codePoint KeyDown is a navigation event, not text input.
                val native=java.awt.event.KeyEvent(java.awt.Canvas(),java.awt.event.KeyEvent.KEY_TYPED,
                    1000+host.timeUs/1000,0,java.awt.event.KeyEvent.VK_UNDEFINED,character)
                host.scene.sendKeyEvent(KeyEvent(Key.Unknown,KeyEventType.Unknown,codePoint=character.code,nativeEvent=native))
                host.scheduler.runCurrent()
            }
            fun insert(value:String) {
                val edit=all().single{it.contains(SemanticsActions.InsertTextAtCursor)}
                check(checkNotNull(edit[SemanticsActions.InsertTextAtCursor].action).invoke(AnnotatedString(value)))
            }
            fun sample(id:String) {
                writer.submit(host.snapshot(id,id,listOf(ProbePoint("field","secure-field",null,listOf(
                    text("display",checkNotNull(layout).layoutInput.text.text,"text","secure-field.displayed-text"),
                    number("length",state.text.length.toDouble(),"utf16",definition="secure-field.input-length"),
                    flag("focused",all().any{it.getOrElse(SemanticsProperties.Focused){false}},definition="secure-field.focused"))))))
            }
            advance(16000);type('a');host.draw();host.frame(16000);sample("first-input")
            advance(516000)
            when(c.action) {
                "replacement"->{state.edit{selection=TextRange(0,length)};type('b')}
                "multi"->insert("cd")
                "program"->state.edit{replace(0,length,"z")}
                "blur"->focus.clearFocus()
                "hidden"->mode=TextObfuscationMode.Hidden
                "supplementary"->insert("😀")
                "disable"->enabled=false
                else->type('b')
            }
            writer.record(516000,"input",c.action);host.draw();host.frame(516000);sample("second-input")
            listOf(532000L,1515000L,1516000L,2015000L,2016000L).forEach{advance(it);host.draw();sample("time-$it")}
        }
    }
}
