plugins {
    kotlin("jvm") version "2.3.20"
    id("org.jetbrains.compose") version "1.13.0-alpha01"
    id("org.jetbrains.kotlin.plugin.compose") version "2.3.20"
    kotlin("plugin.serialization") version "2.3.20"
    application
}
// Keep the existing Windows lock intact; native artifacts and locks are platform-specific.
val referencePlatform = when {
    System.getProperty("os.name").startsWith("Windows") && System.getProperty("os.arch") == "amd64" -> "windows-x64"
    System.getProperty("os.name") == "Linux" && System.getProperty("os.arch") == "amd64" -> "linux-x64"
    else -> error("VisualParity reference supports Windows x64 and Linux x64 only.")
}
dependencies {
    implementation(compose.desktop.currentOs)
    implementation("org.jetbrains.compose.material3:material3:1.13.0-alpha01")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-test:1.9.0")
    implementation("org.jetbrains.kotlinx:kotlinx-serialization-json:1.8.1")
    // Already pinned on the producer runtime path. Dialog keyboard scenarios use the
    // real desktop BackNavigationEventInput, whose supertype must be visible at compile time.
    compileOnly("androidx.navigationevent:navigationevent-desktop:1.1.1") { isTransitive = false }
    // Fetch only the matching native runtime at the pinned version.
    runtimeOnly("org.jetbrains.skiko:skiko-awt-runtime-$referencePlatform:0.152.0-alpha02")
}
configurations.configureEach { exclude(group = "org.jetbrains.skiko", module = "skiko-awt-runtime-all") }
dependencyLocking {
    lockAllConfigurations()
    lockMode.set(LockMode.STRICT)
    if (referencePlatform == "linux-x64") lockFile.set(file("gradle-linux-x64.lockfile"))
}
application { mainClass = "parity.MainKt" }
java { sourceCompatibility = JavaVersion.VERSION_17; targetCompatibility = JavaVersion.VERSION_17 }
kotlin { compilerOptions {
    jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17)
    freeCompilerArgs.addAll("-opt-in=androidx.compose.material3.ExperimentalMaterial3ExpressiveApi",
        "-opt-in=androidx.compose.material3.ExperimentalMaterial3Api",
        "-opt-in=androidx.compose.ui.ExperimentalComposeUiApi")
} }
tasks.register("resolveLocks") {
    // Dev/hot-reload configurations are not part of the producer and pull unrelated tools.
    doLast { listOf("compileClasspath", "runtimeClasspath").forEach { configurations.getByName(it).resolve() } }
}
