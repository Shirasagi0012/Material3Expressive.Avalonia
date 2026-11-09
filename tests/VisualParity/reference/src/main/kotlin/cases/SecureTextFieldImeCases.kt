@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
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
import androidx.compose.ui.platform.*
import androidx.compose.ui.semantics.*
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.text.TextRange
import androidx.compose.ui.unit.*
import java.awt.Canvas
import java.awt.event.InputMethodEvent
import java.awt.font.TextHitInfo
import java.text.AttributedString
import kotlin.coroutines.intrinsics.suspendCoroutineUninterceptedOrReturn
import kotlinx.serialization.Serializable

@Serializable data class SecureImeConfig(val outlined:Boolean=false,val finish:String="cancel")

@ParitySuite("secure-ime")
class SecureTextFieldImeCases {
    @ParityScenario("composition") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<SecureImeConfig>()
        // Run the pinned desktop adapter itself. Its Empty platform component only
        // replaces the OS window; request.editText and all IME transaction logic remain
        // the production DesktopTextInputService2 / Foundation implementation.
        val componentType=Class.forName("androidx.compose.ui.platform.PlatformComponent")
        val companion=componentType.getField("Companion").get(null)
        val component=companion.javaClass.getMethod("getEmpty").invoke(companion)
        val serviceType=Class.forName("androidx.compose.ui.platform.DesktopTextInputService2")
        val service=serviceType.getConstructor(componentType).newInstance(component)
        var activeRequest:PlatformTextInputMethodRequest?=null
        val platform=object:PlatformContext by PlatformContext.Empty() {
            override suspend fun startInputMethod(request:PlatformTextInputMethodRequest):Nothing {
                activeRequest=request
                return suspendCoroutineUninterceptedOrReturn { continuation ->
                    serviceType.getMethod("startInputMethod",PlatformTextInputMethodRequest::class.java,
                        kotlin.coroutines.Continuation::class.java).invoke(service,request,continuation)
                }
            }
        }
        ComposeHost(writer,360,160,platform).use { host ->
            val state=TextFieldState("abc",TextRange(3));val requester=FocusRequester();val probe=Probe()
            var layout:TextLayoutResult?=null
            var focused=false
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive(),
                    typography=Typography(fontFamily=CaseDefaults.font(writer))) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        val modifier=probe.modifier().width(280.dp).onFocusChanged{focused=it.isFocused}.focusRequester(requester)
                        if(c.outlined)OutlinedSecureTextField(state,modifier,
                            colors=OutlinedTextFieldDefaults.colors(cursorColor=Color.Transparent),onTextLayout={get->layout=get()})
                        else SecureTextField(state,modifier,
                            colors=TextFieldDefaults.colors(cursorColor=Color.Transparent),onTextLayout={get->layout=get()})
                    }
                }
            };host.frame(0);requester.requestFocus();host.draw();host.frame(0)
            checkNotNull(activeRequest){"Secure field did not open its real platform IME session."}
            var next=16000L
            fun advance(time:Long) {while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun sample(id:String) {
                val composition=checkNotNull(activeRequest).state.composition?.length?:0
                writer.submit(host.snapshot(id,id,listOf(ProbePoint("field","secure-field",null,listOf(
                    text("display",checkNotNull(layout).layoutInput.text.text,"text","secure-field.displayed-text"),
                    number("committed-length",(state.text.length-composition).toDouble(),"utf16",definition="secure-field.committed-length"),
                    number("composition-length",composition.toDouble(),"utf16",definition="secure-field.composition-length"),
                    flag("focused",focused,definition="secure-field.focused"))))))
            }
            fun ime(value:String?,committed:Int) {
                val event=InputMethodEvent(Canvas(),InputMethodEvent.INPUT_METHOD_TEXT_CHANGED,
                    1000+host.timeUs/1000,value?.let{AttributedString(it).iterator},committed,
                    value?.let{TextHitInfo.leading(it.length-committed)},null)
                serviceType.getMethod("inputMethodTextChanged",InputMethodEvent::class.java).invoke(service,event)
                check(event.isConsumed){"Desktop IME adapter did not accept the event."}
                host.scheduler.runCurrent();writer.record(host.timeUs,"ime","committed=$committed; composing=${(value?.length?:0)-committed}")
                host.draw();host.frame(host.timeUs)
            }
            advance(16000)
            val typed=java.awt.event.KeyEvent(Canvas(),java.awt.event.KeyEvent.KEY_TYPED,1016,0,
                java.awt.event.KeyEvent.VK_UNDEFINED,'d')
            host.scene.sendKeyEvent(KeyEvent(Key.Unknown,KeyEventType.Unknown,codePoint='d'.code,nativeEvent=typed))
            host.scheduler.runCurrent();host.draw();host.frame(16000);sample("typed-before-composition")
            advance(516000)
            repeat(4) {
                host.scene.sendKeyEvent(KeyEvent(Key.DirectionLeft,KeyEventType.KeyDown))
                host.scene.sendKeyEvent(KeyEvent(Key.DirectionLeft,KeyEventType.KeyUp))
            }
            host.scheduler.runCurrent();check(state.selection==TextRange(0))
            ime("x",0);sample("preedit-before-reveal")
            advance(716000);ime("😀",0);sample("preedit-supplementary")
            advance(916000);ime("yz",0);sample("preedit-multiple")
            advance(1116000)
            if(c.finish=="commit")ime("z",1) else ime(null,0)
            sample("composition-${c.finish}")
            for(time in listOf(1515000L,1516000L,2615000L,2616000L)) {
                advance(time);host.draw();sample("time-$time")
            }
        }
    }
}
