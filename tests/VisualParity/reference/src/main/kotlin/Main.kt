package parity

import kotlinx.coroutines.runBlocking
import kotlinx.serialization.json.*
import kotlinx.serialization.encodeToString
import java.io.File

fun main(args: Array<String>) {
    val scenarios = ScenarioDiscovery.discover()
    if (args.contentEquals(arrayOf("--list-scenarios"))) {
        println(json.encodeToString(scenarios.keys.sortedWith(compareBy({ it.suite }, { it.scenario }))))
        return
    }
    val worker = args.size == 2 && args[0] == "--worker"
    require(worker || args.size in 3..4 && (args.size == 3 || args[3] == "--extra-images"))
    val root = File(if (worker) args[1] else args[2])
    val baseline = json.parseToJsonElement(File(root, "docs/upstream-baseline.json").readText()).jsonObject
    check(baseline.getValue("composeMultiplatformCore").jsonObject.getValue("version").jsonPrimitive.content == "1.13.0-alpha01" &&
        baseline.getValue("composeMultiplatformCore").jsonObject.getValue("commit").jsonPrimitive.content == "a9e47e8aa04c95cfe572ab4297c770f3cbfb234e" &&
        baseline.getValue("androidx").jsonObject.getValue("commit").jsonPrimitive.content == "f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea") { "Producer source/version baseline differs." }
    val runner = ScenarioDiscovery.artifact
    val dependencyLock = if (System.getProperty("os.name") == "Linux") "gradle-linux-x64.lockfile" else "gradle.lockfile"
    val environment = mapOf("platform" to System.getProperty("os.name").lowercase().let { if (it.contains("windows")) "windows" else it },
        "architecture" to System.getProperty("os.arch").let { if (it == "amd64") "x64" else it },
        "density" to ComposeHost.RenderScale.toString(), "fontScale" to "1", "colorSpace" to "sRGB",
        "fontSha256" to hash(File(root, "modules/MaterialTypography.Roboto/Assets/Roboto-Regular.ttf")),
        "fontMediumSha256" to hash(File(root, "modules/MaterialTypography.Roboto/Assets/Roboto-Medium.ttf")),
        "fontBoldSha256" to hash(File(root, "modules/MaterialTypography.Roboto/Assets/Roboto-Bold.ttf")),
        "compose" to "1.13.0-alpha01", "jdk" to System.getProperty("java.version"), "coroutineTimeResolution" to "1ms",
        "dependencyLockSha256" to hash(File(root, "tests/VisualParity/reference/$dependencyLock")),
        "verificationMetadataSha256" to hash(File(root, "tests/VisualParity/reference/gradle/verification-metadata.xml")),
        "runnerJarSha256" to hash(runner), "classpath" to runner.parentFile.listFiles()!!.filter { it.extension == "jar" }
            .sortedBy { it.name }.joinToString("\n") { "${it.name}:${hash(it)}" })

    fun capture(input: File, output: File, extraImages: Boolean) {
        require(!output.exists()); output.mkdirs()
        val metadata = json.decodeFromString<CaseMetadata>(input.readText())
        require(metadata.schemaVersion == 4) { "Case metadata schema 4 required." }
        val writer = CaptureWriter(metadata, input, output, root, extraImages)
        try {
            runBlocking { (scenarios[ScenarioIdentity(metadata.suite, metadata.scenario)]
                ?: error("Unknown Compose scenario: ${metadata.suite}/${metadata.scenario}")).execute(writer) }
        } catch (e: Exception) { writer.errors += e.stackTraceToString() }
        writer.finish(environment)
        check(writer.errors.isEmpty()) { "Capture failed; diagnostics retained in $output: ${writer.errors.joinToString("\n")}" }
    }

    if (!worker) {
        capture(File(args[0]), File(args[1]), args.size == 4)
        return
    }
    // Reserve stdout for one compact response per request, including any scene diagnostics.
    val responses = System.out
    System.setOut(System.err)
    val wire = Json(json) { prettyPrint = false }
    fun reply(value: JsonObject) { responses.println(wire.encodeToString(value)); responses.flush() }
    reply(buildJsonObject {
        put("scenarios", json.encodeToJsonElement(scenarios.keys.sortedWith(compareBy({ it.suite }, { it.scenario }))))
        put("environment", json.encodeToJsonElement(environment))
    })
    System.`in`.bufferedReader().useLines { lines ->
        for (line in lines) {
            try {
                val request = wire.parseToJsonElement(line).jsonObject
                capture(File(request.getValue("input").jsonPrimitive.content), File(request.getValue("output").jsonPrimitive.content),
                    request.getValue("extraImages").jsonPrimitive.boolean)
                reply(buildJsonObject { put("ok", true) })
            } catch (e: Exception) {
                reply(buildJsonObject { put("error", e.stackTraceToString()) })
                return // Do not reuse potentially contaminated framework state after a failed case.
            }
        }
    }
}
