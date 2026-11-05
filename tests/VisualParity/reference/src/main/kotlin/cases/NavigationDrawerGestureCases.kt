@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.ui.geometry.Offset

@ParitySuite("navigation-drawer-gesture")
class NavigationDrawerGestureCases {
    @ParityScenario("interruption")
    suspend fun interruption(writer:CaptureWriter) {
        val c=writer.readConfig<NavigationDrawerConfig>()
        ComposeHost(writer,c.hostWidth,320,NavigationDrawerCases.desktopContext(c.hostWidth)).use { host ->
            val scene=NavigationDrawerCases.Scene(c)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            var nextFrame=16000L
            fun advance(time:Long) {
                while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }
                host.advanceTo(time)
            }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            fun input(time:Long,action:String,point:Offset) { advance(time); host.input(action,point); host.draw() }
            val sign=if(c.action in listOf("child-press","state-child-press")) 0 else (if(c.rtl) -1 else 1)*(if(c.action in listOf("child","native-thumb")) -1 else 1)
            val origin=if(c.action in listOf("child-press","state-child-press")) {
                val box=checkNotNull(scene.item.geometry().first { it.name=="box" }.numbers)
                Offset(box[0].toFloat()+80f,box[1].toFloat()+28f)
            } else Offset(if(c.rtl) 240f else 280f,250f)
            sample("initial")
            input(1000,"move",origin); input(2000,"down",origin)
            input(18000,"move",origin-Offset(40f*sign,0f)); sample("drag-start")
            advance(33000)
            when(c.action) {
                "state", "state-child-press" -> scene.stateGeneration++
                "direction" -> scene.rtl=!c.rtl
                "gestures", "child-press" -> scene.gestures=false
            }
            writer.record(33000,"mutation",c.action); host.scheduler.runCurrent(); host.draw(); sample("changed")
            advance(48000); host.draw(); sample("committed")
            input(65000,"move",origin-Offset(80f*sign,0f)); sample("move-after-change")
            input(90000,"up",origin-Offset(80f*sign,0f)); sample("release-after-change")
            for(time in listOf(96000L,160000L,400000L,800000L)) {
                advance(time); host.draw(); sample("time-$time")
            }
        }
    }
}
