package parity
import kotlinx.serialization.*
import kotlinx.serialization.json.*
import java.io.File
import java.security.MessageDigest
@Serializable data class CaseMetadata(val schemaVersion:Int, val id:String, val title:String, val family:String, val composeEntry:String, val avaloniaEntry:String)
@Serializable data class Measurement(val name:String, val category:String, val kind:String, val unit:String, val space:String,
    val definition:String, val source:String, val status:String="measured", val numbers:List<Double>?=null, val strings:List<String>?=null, val reason:String?=null, val boolean:Boolean?=null)
@Serializable data class ProbePoint(val id:String, val role:String, val parentId:String?, val measurements:List<Measurement>)
@Serializable data class RasterObservation(val id:String, val image:String, val pixels:String, val width:Int, val height:Int,
    val format:String="straight-srgba8", val space:String="scene-px", val contribution:String="full")
@Serializable data class Sample(val id:String, val timeUs:Long, val stage:String, val points:List<ProbePoint>, val rasters:List<RasterObservation>, val frameTimeUs:Long?=null)
@Serializable data class TraceEvent(val sequence:Int, val timeUs:Long, val operation:String, val detail:String)
@Serializable data class Capture(val schemaVersion:Int, val runner:String, val caseId:String, val metadataSha256:String, val baselineSha256:String,
    val environment:Map<String,String>, val samples:List<Sample>, val trace:List<TraceEvent>, val errors:List<String>)
@Serializable data class ArtifactManifest(val schemaVersion:Int, val runner:String, val files:Map<String,String>, val environment:Map<String,String>)
val json = Json { prettyPrint=true; encodeDefaults=true }
fun hash(file:File) = MessageDigest.getInstance("SHA-256").digest(file.readBytes()).joinToString("") { "%02x".format(it) }
fun number(name:String, value:Double, unit:String="dip", space:String="none", category:String="properties", definition:String=name, source:String="runtime") =
    Measurement(name,category,"number",unit,space,definition,source,numbers=listOf(value))
fun vector(name:String, kind:String, values:List<Double>, unit:String, space:String, category:String, definition:String=name, source:String="runtime") =
    Measurement(name,category,kind,unit,space,definition,source,numbers=values)
fun text(name:String, value:String, category:String="properties", definition:String=name, source:String="runtime") =
    Measurement(name,category,"string","none","none",definition,source,strings=listOf(value))
fun flag(name:String, value:Boolean, category:String="properties", definition:String=name, source:String="runtime") =
    Measurement(name,category,"boolean","none","none",definition,source,boolean=value)
fun unavailable(name:String, category:String, kind:String, unit:String, space:String, reason:String, definition:String=name) =
    Measurement(name,category,kind,unit,space,definition,"reader","unsupported",reason=reason)
class CaptureWriter(val metadata:CaseMetadata, val input:File, val output:File, val root:File, val extraImages:Boolean=false) {
    val samples=mutableListOf<Sample>(); val trace=mutableListOf<TraceEvent>(); val errors=mutableListOf<String>()
    fun record(timeUs:Long, operation:String, detail:String="") { trace+=TraceEvent(trace.size,timeUs,operation,detail) }
    fun submit(sample:Sample) { samples+=json.decodeFromString<Sample>(json.encodeToString(sample)); record(sample.timeUs,"sample",sample.id) }
    fun finish(environment:Map<String,String>) {
        File(output,"observations.json").writeText(json.encodeToString(Capture(2,"compose",metadata.id,hash(input),
            hash(File(root,"docs/upstream-baseline.json")),environment,samples,trace,errors)))
        val files=output.walkTopDown().filter { it.isFile }.associate { it.relativeTo(output).invariantSeparatorsPath to hash(it) }
        File(output,"manifest.json").writeText(json.encodeToString(ArtifactManifest(2,"compose",files,environment)))
    }
}
