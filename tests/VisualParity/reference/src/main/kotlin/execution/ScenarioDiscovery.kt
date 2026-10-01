package parity

import java.io.File
import java.lang.reflect.InvocationTargetException
import java.lang.reflect.Method
import java.lang.reflect.Modifier
import java.util.jar.JarFile
import kotlin.coroutines.Continuation
import kotlin.coroutines.intrinsics.suspendCoroutineUninterceptedOrReturn
import kotlinx.serialization.Serializable

@Target(AnnotationTarget.CLASS)
@Retention(AnnotationRetention.RUNTIME)
annotation class ParitySuite(val id: String)

@Target(AnnotationTarget.FUNCTION)
@Retention(AnnotationRetention.RUNTIME)
annotation class ParityScenario(val id: String)

@Serializable
data class ScenarioIdentity(val suite: String, val scenario: String)

class DiscoveredScenario(val identity: ScenarioIdentity, private val fixture: Class<*>, private val method: Method) {
    suspend fun execute(writer: CaptureWriter): Unit = suspendCoroutineUninterceptedOrReturn { continuation ->
        require(writer.metadata.suite == identity.suite && writer.metadata.scenario == identity.scenario)
        try {
            // Pass through the caller's coroutine continuation; no nested runBlocking or new scheduler.
            method.invoke(fixture.getConstructor().newInstance(), writer, continuation)
        } catch (error: InvocationTargetException) {
            throw error.targetException
        }
    }
}

object ScenarioDiscovery {
    val artifact: File get() = File(ScenarioDiscovery::class.java.protectionDomain.codeSource.location.toURI())

    fun discover(): Map<ScenarioIdentity, DiscoveredScenario> {
        // Only the producer's own jar is inspected, never the dependency classpath.
        val loader = ScenarioDiscovery::class.java.classLoader
        val types = JarFile(artifact).use { jar ->
            jar.entries().asSequence().map { it.name }
                .filter { it.endsWith(".class") && !it.contains('$') && it.startsWith("parity/") }
                .sorted().map { Class.forName(it.removeSuffix(".class").replace('/', '.'), false, loader) }.toList()
        }
        val suites = mutableSetOf<String>()
        val scenarios = linkedMapOf<ScenarioIdentity, DiscoveredScenario>()
        val id = Regex("^[a-z0-9][a-z0-9.-]*$")
        for (type in types) {
            val suite = type.getAnnotation(ParitySuite::class.java) ?: continue
            require(id.matches(suite.id) && suites.add(suite.id)) { "Invalid or duplicate suite: ${suite.id}" }
            require(Modifier.isPublic(type.modifiers) && !Modifier.isAbstract(type.modifiers) && type.typeParameters.isEmpty()) {
                "Suite requires a public concrete class: ${type.name}"
            }
            type.getConstructor()
            var count = 0
            for (method in type.declaredMethods) {
                val scenario = method.getAnnotation(ParityScenario::class.java) ?: continue
                val identity = ScenarioIdentity(suite.id, scenario.id)
                require(id.matches(scenario.id) && !scenarios.containsKey(identity)) { "Invalid or duplicate scenario: $identity" }
                require(Modifier.isPublic(method.modifiers) && !Modifier.isStatic(method.modifiers) && method.typeParameters.isEmpty()
                    && method.returnType == Any::class.java
                    && method.parameterTypes.contentEquals(arrayOf(CaptureWriter::class.java, Continuation::class.java))) {
                    "Scenario must be a public suspend instance method accepting CaptureWriter: ${type.name}.${method.name}"
                }
                scenarios[identity] = DiscoveredScenario(identity, type, method)
                count++
            }
            require(count > 0) { "Suite has no scenarios: ${suite.id}" }
        }
        return scenarios
    }
}
