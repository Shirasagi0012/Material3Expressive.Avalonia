plugins {
    kotlin("jvm") version "2.3.20"
    id("org.jetbrains.compose") version "1.13.0-alpha01"
    id("org.jetbrains.kotlin.plugin.compose") version "2.3.20"
    kotlin("plugin.serialization") version "2.3.20"
    application
}
dependencies {
    implementation(compose.desktop.currentOs)
    implementation("org.jetbrains.compose.material3:material3:1.13.0-alpha01")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-test:1.9.0")
    implementation("org.jetbrains.kotlinx:kotlinx-serialization-json:1.8.1")
    // The first paired environment is Windows x64. Keep the same Skiko version, but avoid
    // fetching unused Linux/macOS native binaries from Desktop's new all-platform bundle.
    runtimeOnly("org.jetbrains.skiko:skiko-awt-runtime-windows-x64:0.152.0-alpha02")
}
configurations.configureEach { exclude(group = "org.jetbrains.skiko", module = "skiko-awt-runtime-all") }
dependencyLocking { lockAllConfigurations(); lockMode.set(LockMode.STRICT) }
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
