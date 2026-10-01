package parity
import kotlinx.serialization.*
import kotlinx.serialization.json.*
import java.io.File
import java.security.MessageDigest
class CaptureWriter(val metadata:CaseMetadata, val input:File, val output:File, val root:File, val extraImages:Boolean=false) {
    inline fun <reified T> readConfig():T = json.decodeFromJsonElement<T>(metadata.config)
    val samples=mutableListOf<Sample>(); val trace=mutableListOf<TraceEvent>(); val errors=mutableListOf<String>()
    fun record(timeUs:Long, operation:String, detail:String="", data:JsonElement?=null) { trace+=TraceEvent(trace.size,timeUs,operation,detail,data) }
    fun submit(sample:Sample) { samples+=json.decodeFromString<Sample>(json.encodeToString(sample)); record(sample.timeUs,"sample",sample.id,buildJsonObject { put("sampleId",sample.id) }) }
    fun finish(environment:Map<String,String>) {
        File(output,"observations.json").writeText(json.encodeToString(Capture(2,"compose",metadata.id,hash(input),
            hash(File(root,"docs/upstream-baseline.json")),environment,samples,trace,errors)))
        val files=output.walkTopDown().filter { it.isFile }.associate { it.relativeTo(output).invariantSeparatorsPath to hash(it) }
        File(output,"manifest.json").writeText(json.encodeToString(ArtifactManifest(2,"compose",files,environment)))
    }
}
