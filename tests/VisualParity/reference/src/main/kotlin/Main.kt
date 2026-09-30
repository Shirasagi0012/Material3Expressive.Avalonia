@file:OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class, androidx.compose.ui.text.ExperimentalTextApi::class,
    androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.snapshots.Snapshot
import androidx.compose.ui.*
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.layout.boundsInRoot
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.FrameRecomposer
import androidx.compose.ui.platform.registerSkikoComposeImplementation
import androidx.compose.ui.scene.CanvasLayersComposeScene
import androidx.compose.ui.scene.ComposeScene
import androidx.compose.ui.graphics.asComposeCanvas
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.text.font.*
import androidx.compose.ui.text.platform.Font
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.coroutines.test.*
import kotlinx.serialization.*
import kotlinx.serialization.json.*
import org.jetbrains.skia.EncodedImageFormat
import org.jetbrains.skia.Surface
import java.io.File
import java.security.MessageDigest

@Serializable data class InputEvent(val atUs: Long, val action: String, val value: Double = 0.0)
@Serializable data class Scenario(
    val schemaVersion: Int = 1, val id: String, val component: String, val variant: String = "filled",
    val content: String = "slot", val text: String = "Save", val direction: String = "LTR",
    val dark: Boolean = false, val enabled: Boolean = true,
    val viewportWidth: Int = 240, val viewportHeight: Int = 160,
    val width: Double = 100.0, val height: Double = 50.0, val radius: Double = 12.0,
    val elevation: Double = 0.0, val fontSize: Double = 14.0, val fontWeight: Int = 400,
    val lineHeight: Double = 20.0, val letterSpacing: Double = 0.0, val wrap: Boolean = false, val density: Double = 1.0,
    val fontScale: Double = 1.0, val motionSpec: String = "tween",
    val background: String = "#FFFFFF", val ink: String = "#6750A4",
    val framesUs: List<Long> = listOf(0), val checkpoints: List<Int> = listOf(0),
    val events: List<InputEvent> = emptyList(), val required: List<String> = listOf("layout", "raster", "behavior")
)
@Serializable data class Bounds(val x: Double, val y: Double, val width: Double, val height: Double)
@Serializable data class TextLine(val baseline: Double, val width: Double, val height: Double, val ellipsized: Boolean)
@Serializable data class TextObservation(val text: String, val fontSize: Double, val fontWeight: Int,
    val lines: List<TextLine>, val resolvedFonts: List<String>, val letterSpacing: Double = 0.0)
@Serializable data class Node(val id: String, val bounds: Bounds, val text: TextObservation? = null)
@Serializable data class FrameObservation(val index: Int, val timeUs: Long, val image: String?, val nodes: List<Node>,
    val values: Map<String, Double>, val clicks: Int, val enabled: Boolean, val withoutTextImage: String? = null)
@Serializable data class Capture(val runner: String, val scenarioSha256: String, val baselineSha256: String,
    val environment: Map<String, String>, val capabilities: List<String>, val frames: List<FrameObservation>, val errors: List<String>)
@Serializable data class ArtifactManifest(val schemaVersion: Int, val runner: String,
    val files: Map<String, String>, val environment: Map<String, String>)

val json = Json { prettyPrint = true; encodeDefaults = true }
fun hash(file: File) = MessageDigest.getInstance("SHA-256").digest(file.readBytes()).joinToString("") { "%02x".format(it) }
fun color(text: String) = Color((0xff000000L or text.removePrefix("#").toLong(16)).toInt())

class Fixture(val s: Scenario, val roboto: FontFamily, val symbols: FontFamily, val scope: CoroutineScope) {
    val geometry = linkedMapOf<String, Bounds>()
    val texts = linkedMapOf<String, TextLayoutResult>()
    var clicks = 0
    var enabled by mutableStateOf(s.enabled)
    var detached by mutableStateOf(false)
    val value = Animatable(0f)
    var motionJob: Job? = null
    var mountedScope: CoroutineScope? = null
    var hideText by mutableStateOf(false)
    fun Modifier.probe(id: String) = onGloballyPositioned {
        val b = it.boundsInRoot()
        geometry[id] = Bounds(b.left.toDouble() / s.density, b.top.toDouble() / s.density,
            b.width.toDouble() / s.density, b.height.toDouble() / s.density)
    }
    @Composable fun label() {
        if (s.content == "slot") Box(Modifier.size(18.dp).probe("content").background(Color.White))
        else Text(s.text, Modifier.probe("label").drawWithContent { if (!hideText) drawContent() }, onTextLayout = { texts["label"] = it })
    }
    @Composable fun icon() {
        Text("\uE88A", Modifier.size(24.dp).probe("icon"), fontFamily = symbols,
            fontSize = 24.sp, lineHeight = 24.sp, letterSpacing = 0.sp, onTextLayout = { texts["icon"] = it })
    }
    @Composable fun render() {
        mountedScope = rememberCoroutineScope()
        val ink = color(s.ink)
        val type = Typography().let { it.copy(labelLarge = it.labelLarge.copy(fontFamily = roboto)) }
        CompositionLocalProvider(LocalLayoutDirection provides if (s.direction == "RTL") LayoutDirection.Rtl else LayoutDirection.Ltr,
            LocalDensity provides Density(s.density.toFloat(), s.fontScale.toFloat())) {
            MaterialTheme(colorScheme = if (s.dark) darkColorScheme() else lightColorScheme(), typography = type) {
                Box(Modifier.fillMaxSize().background(color(s.background)), contentAlignment = Alignment.Center) {
                    if (!detached) when (s.component) {
                        "surface" -> Surface(Modifier.size(s.width.dp, s.height.dp).probe("container"),
                            shape = RoundedCornerShape(s.radius.dp), color = ink, shadowElevation = s.elevation.dp) {}
                        "button" -> {
                            val mod = Modifier.height(40.dp).probe("allocation")
                            val colors = ButtonDefaults.buttonColors(containerColor = ink, contentColor = Color.White,
                                disabledContainerColor = ink.copy(alpha = .12f), disabledContentColor = Color.White.copy(alpha = .38f))
                            when (s.variant) {
                                "filled" -> Button({ clicks++ }, ButtonDefaults.shapes(), mod, enabled = enabled, colors = colors) { label() }
                                "tonal" -> FilledTonalButton({ clicks++ }, ButtonDefaults.shapes(), mod, enabled = enabled, colors = colors) { label() }
                                "elevated" -> ElevatedButton({ clicks++ }, ButtonDefaults.shapes(), mod, enabled = enabled, colors = colors) { label() }
                                "outlined" -> OutlinedButton({ clicks++ }, ButtonDefaults.shapes(), mod, enabled = enabled,
                                    colors = ButtonDefaults.outlinedButtonColors(contentColor = ink)) { label() }
                                "text" -> TextButton({ clicks++ }, ButtonDefaults.shapes(), mod, enabled = enabled,
                                    colors = ButtonDefaults.textButtonColors(contentColor = ink)) { label() }
                                else -> error("Unsupported button variant")
                            }
                        }
                        "iconbutton" -> {
                            val mod = Modifier.probe("allocation")
                            when (s.variant) {
                                "filled" -> FilledIconButton({ clicks++ }, IconButtonDefaults.shapes(), mod, enabled = enabled,
                                    colors = IconButtonDefaults.filledIconButtonColors(containerColor = ink, contentColor = Color.White)) { icon() }
                                "standard" -> IconButton({ clicks++ }, IconButtonDefaults.shapes(), mod, enabled = enabled,
                                    colors = IconButtonDefaults.iconButtonColors(contentColor = ink)) { icon() }
                                "tonal" -> FilledTonalIconButton({ clicks++ }, IconButtonDefaults.shapes(), mod, enabled = enabled,
                                    colors = IconButtonDefaults.filledTonalIconButtonColors(containerColor = ink, contentColor = Color.White)) { icon() }
                                "outlined" -> OutlinedIconButton({ clicks++ }, IconButtonDefaults.shapes(), mod, enabled = enabled,
                                    colors = IconButtonDefaults.outlinedIconButtonColors(contentColor = ink)) { icon() }
                                else -> error("Unsupported icon variant")
                            }
                        }
                        "text" -> Text(s.text, Modifier.width(s.width.dp).probe("label").drawWithContent { if (!hideText) drawContent() }, color = ink,
                            fontFamily = roboto, fontSize = s.fontSize.sp, fontWeight = FontWeight(s.fontWeight),
                            lineHeight = s.lineHeight.sp, letterSpacing = s.letterSpacing.sp, softWrap = s.wrap, onTextLayout = { texts["label"] = it })
                        "motion" -> Box(Modifier.fillMaxSize()) {
                            Box(Modifier.offset(x = (24 + value.value).dp, y = 64.dp).size(18.dp)
                                .probe("container").background(ink))
                        }
                        else -> error("Unsupported component")
                    }
                }
            }
        }
    }
    fun event(e: InputEvent, scene: ComposeScene) {
        val b = geometry["allocation"] ?: geometry["container"] ?: geometry["label"] ?: error("No input target")
        val point = Offset(((b.x + b.width / 2) * s.density).toFloat(), ((b.y + b.height / 2) * s.density).toFloat())
        when (e.action) {
            // Animatable's mutex captures velocity/start time before interrupting the old job.
            "target" -> { motionJob = checkNotNull(mountedScope).launch {
                value.animateTo(e.value.toFloat(), if (s.motionSpec == "spring") spring(stiffness = 1600f, visibilityThreshold = .01f)
                    else tween(100, easing = LinearEasing))
            } }
            "disable" -> enabled = false
            "detach" -> { detached = true; motionJob?.cancel(); geometry.clear(); texts.clear() }
            else -> scene.sendPointerEvent(when (e.action) {
                "down" -> PointerEventType.Press; "up" -> PointerEventType.Release
                "leave" -> PointerEventType.Exit; else -> PointerEventType.Move
            }, if (e.action == "leave") Offset(-1f, -1f) else point,
                buttons = PointerButtons(isPrimaryPressed = e.action == "down"),
                timeMillis = 1000 + e.atUs / 1000, type = PointerType.Mouse,
                button = if (e.action in listOf("down", "up")) PointerButton.Primary else null)
        }
    }
    fun nodes() = geometry.map { (id, b) -> Node(id, b, texts[id]?.takeIf { id == "label" }?.let { t ->
        val style = t.layoutInput.style
        TextObservation(t.layoutInput.text.text, style.fontSize.value.toDouble(), style.fontWeight?.weight ?: 400,
            (0 until t.lineCount).map { i -> TextLine(b.y + t.getLineBaseline(i) / s.density,
                (t.getLineRight(i) - t.getLineLeft(i)) / s.density,
                (t.getLineBottom(i) - t.getLineTop(i)) / s.density, t.isLineEllipsized(i)) }, emptyList(),
            style.letterSpacing.value.takeIf { it.isFinite() }?.toDouble() ?: 0.0)
    }) }
}

fun main(args: Array<String>) {
    require(args.size == 3) { "scenario.json output-directory repository-root" }
    val input = File(args[0]); val output = File(args[1]); val root = File(args[2])
    require(!output.exists()) { "Output already exists: $output" }
    output.mkdirs()
    val s = json.decodeFromString<Scenario>(input.readText())
    val baseline = json.parseToJsonElement(File(root, "docs/upstream-baseline.json").readText()).jsonObject
    check(baseline.getValue("composeMultiplatformCore").jsonObject.getValue("version").jsonPrimitive.content == "1.13.0-alpha01") {
        "Producer version differs from baseline; upgrade its build and capture metadata together."
    }
    check(baseline.getValue("composeMultiplatformCore").jsonObject.getValue("commit").jsonPrimitive.content == "a9e47e8aa04c95cfe572ab4297c770f3cbfb234e" &&
        baseline.getValue("androidx").jsonObject.getValue("commit").jsonPrimitive.content == "f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea") {
        "Producer source pins differ from baseline; reverify artifact provenance before changing these pins."
    }
    require(s.schemaVersion == 1 && s.density == 1.0 && s.fontScale == 1.0) { "Only density/fontScale 1 are currently supported" }
    val font = File(root, "modules/MaterialTypography.Roboto/Assets/Roboto.ttf")
    val symbol = File(root, "modules/MaterialSymbols/assets/MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf")
    val family = FontFamily(listOf(400, 500, 700).map { w -> Font(font, weight = FontWeight(w),
        variationSettings = FontVariation.Settings(FontVariation.weight(w))) })
    val symbols = FontFamily(Font(symbol, variationSettings = FontVariation.Settings(
        FontVariation.Setting("wght", 400f), FontVariation.Setting("opsz", 24f))))
    val scheduler = TestCoroutineScheduler()
    val errors = mutableListOf<String>()
    val scope = CoroutineScope(UnconfinedTestDispatcher(scheduler) + CoroutineExceptionHandler { _, error -> errors += error.stackTraceToString() })
    val fixture = Fixture(s, family, symbols, scope)
    // Same host phases as the pinned ImageComposeScene, exposed separately so diagnostic
    // draws cannot call performFrame or wake animation waiters.
    registerSkikoComposeImplementation()
    val recomposer = FrameRecomposer(scope.coroutineContext)
    val scene = CanvasLayersComposeScene(recomposer, Density(1f), size = IntSize(s.viewportWidth, s.viewportHeight))
    val surface = Surface.makeRasterN32Premul(s.viewportWidth, s.viewportHeight)
    fun draw() = run {
        Snapshot.sendApplyNotifications(); scheduler.runCurrent()
        scene.measureAndLayout()
        surface.canvas.clear(org.jetbrains.skia.Color.TRANSPARENT)
        scene.draw(surface.canvas.asComposeCanvas())
        surface.makeImageSnapshot()
    }
    fun save(image: org.jetbrains.skia.Image, path: String) {
        val file = File(output, path); file.parentFile.mkdirs()
        image.encodeToData(EncodedImageFormat.PNG)?.use { file.writeBytes(it.bytes) } ?: error("PNG encoding failed")
    }
    val frames = mutableListOf<FrameObservation>()
    try {
        scene.setContent { fixture.render() }
        // Explicit pre-input mounting. No trigger-specific registration frame is inserted.
        recomposer.performFrame(998_000_000L); draw().close(); scheduler.runCurrent()
        recomposer.performFrame(999_000_000L); draw().close(); scheduler.runCurrent()
        var eventIndex = 0
        s.framesUs.forEachIndexed { index, time ->
            while (eventIndex < s.events.size && s.events[eventIndex].atUs <= time) {
                val e = s.events[eventIndex++]
                scheduler.advanceTimeBy(e.atUs / 1000 - scheduler.currentTime); scheduler.runCurrent()
                fixture.event(e, scene); scheduler.runCurrent()
            }
            scheduler.advanceTimeBy(time / 1000 - scheduler.currentTime); scheduler.runCurrent()
            recomposer.performFrame(1_000_000_000L + time * 1000)
            val image = draw()
            scheduler.runCurrent()
            val path = if (index in s.checkpoints) "frames/${index.toString().padStart(4, '0')}.png" else null
            if (path != null) save(image, path)
            image.close()
            val withoutText = if (path != null && "label" in fixture.texts && !fixture.detached) {
                val path = "without-text/${index.toString().padStart(4, '0')}.png"
                fixture.hideText = true
                draw().use { save(it, path) }
                fixture.hideText = false
                draw().close()
                path
            } else null
            frames += FrameObservation(index, time, path, fixture.nodes(),
                if (s.component == "motion") mapOf("value" to fixture.value.value.toDouble(), "velocity" to fixture.value.velocity.toDouble(),
                    "running" to if (fixture.value.isRunning) 1.0 else 0.0) else emptyMap(), fixture.clicks, fixture.enabled, withoutText)
        }
    } finally { scene.close(); recomposer.close(); surface.close(); scope.cancel(); scheduler.runCurrent() }
    val env = mapOf("platform" to System.getProperty("os.name").lowercase().let { if (it.contains("windows")) "windows" else it },
        "architecture" to System.getProperty("os.arch").let { if (it == "amd64") "x64" else it },
        "density" to "1", "fontScale" to "1", "colorSpace" to "sRGB", "fontSha256" to hash(font), "symbolSha256" to hash(symbol),
        "compose" to "1.13.0-alpha01", "jdk" to System.getProperty("java.version"), "coroutineTimeResolution" to "1ms",
        "dependencyLockSha256" to hash(File(root, "tests/VisualParity/reference/gradle.lockfile")),
        "verificationMetadataSha256" to hash(File(root, "tests/VisualParity/reference/gradle/verification-metadata.xml")),
        "classpath" to File(Fixture::class.java.protectionDomain.codeSource.location.toURI()).parentFile.listFiles()!!
            .filter { it.extension == "jar" }.sortedBy { it.name }.joinToString("\n") { "${it.name}:${hash(it)}" },
        "runnerJarSha256" to hash(File(Fixture::class.java.protectionDomain.codeSource.location.toURI())))
    val caps = mutableListOf("layout", "raster", "behavior", "text")
    if (s.component == "text" || s.component == "button" && s.content == "text") caps += "textLayers"
    if (s.component == "motion") caps += "motion"
    File(output, "observations.json").writeText(json.encodeToString(Capture("compose", hash(input),
        hash(File(root, "docs/upstream-baseline.json")), env, caps, frames, errors)))
    File(output, "trace.json").writeText(buildJsonObject {
        put("bootstrapUs", JsonArray(listOf(JsonPrimitive(-2000), JsonPrimitive(-1000))))
        put("events", json.encodeToJsonElement(s.events))
        put("frames", json.encodeToJsonElement(frames))
    }.toString())
    val files = output.walkTopDown().filter { it.isFile }.associate { it.relativeTo(output).invariantSeparatorsPath to hash(it) }
    File(output, "manifest.json").writeText(json.encodeToString(ArtifactManifest(1, "compose", files, env)))
    println("Captured ${s.id}: ${frames.size} frames, ${s.checkpoints.size} images")
}
