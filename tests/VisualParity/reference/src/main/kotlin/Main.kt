package parity
import kotlinx.coroutines.runBlocking
import kotlinx.serialization.json.*
import java.io.File
fun main(args:Array<String>) {
    require(args.size in 3..4) { "case.json output-directory repository-root [--extra-images]" }
    val input=File(args[0]); val output=File(args[1]); val root=File(args[2]); require(!output.exists()); output.mkdirs()
    val metadata=json.decodeFromString<CaseMetadata>(input.readText()); require(metadata.schemaVersion==2)
    val baseline=json.parseToJsonElement(File(root,"docs/upstream-baseline.json").readText()).jsonObject
    check(baseline.getValue("composeMultiplatformCore").jsonObject.getValue("version").jsonPrimitive.content=="1.13.0-alpha01" &&
        baseline.getValue("composeMultiplatformCore").jsonObject.getValue("commit").jsonPrimitive.content=="a9e47e8aa04c95cfe572ab4297c770f3cbfb234e" &&
        baseline.getValue("androidx").jsonObject.getValue("commit").jsonPrimitive.content=="f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea") { "Producer source/version baseline differs." }
    val writer=CaptureWriter(metadata,input,output,root,args.size==4 && args[3]=="--extra-images")
    try { runBlocking { (Cases.entries[metadata.composeEntry] ?: error("Unknown Compose entry: ${metadata.composeEntry}"))(writer) } }
    catch(e:Exception) { writer.errors+=e.stackTraceToString() }
    val runner=File(Cases::class.java.protectionDomain.codeSource.location.toURI())
    val environment=mapOf("platform" to System.getProperty("os.name").lowercase().let { if(it.contains("windows")) "windows" else it },
        "architecture" to System.getProperty("os.arch").let { if(it=="amd64") "x64" else it },
        "density" to "1","fontScale" to "1","colorSpace" to "sRGB","fontSha256" to hash(File(root,"modules/MaterialTypography.Roboto/Assets/Roboto.ttf")),
        "compose" to "1.13.0-alpha01","jdk" to System.getProperty("java.version"),"coroutineTimeResolution" to "1ms",
        "dependencyLockSha256" to hash(File(root,"tests/VisualParity/reference/gradle.lockfile")),
        "verificationMetadataSha256" to hash(File(root,"tests/VisualParity/reference/gradle/verification-metadata.xml")),
        "runnerJarSha256" to hash(runner),"classpath" to runner.parentFile.listFiles()!!.filter { it.extension=="jar" }.sortedBy { it.name }.joinToString("\n") { "${it.name}:${hash(it)}" })
    writer.finish(environment); println("Captured ${metadata.id}: ${writer.samples.size} samples; ${writer.errors.size} errors")
}
