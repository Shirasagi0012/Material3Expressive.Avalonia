import java.security.MessageDigest

plugins {
    kotlin("jvm")
    id("org.jetbrains.compose")
    id("org.jetbrains.kotlin.plugin.compose")
    kotlin("plugin.serialization")
    application
}

val composeMaterial3Version = "1.12.0-alpha03"
val referenceFontSha256 =
    "B25986D18730960C7B27384AB2BC500856AE7FE9E71C9850019195FF9019F0B2"
val referenceFontPath =
    providers.gradleProperty("referenceFontPath").orElse(
        file(
            "${System.getenv("LOCALAPPDATA")}/Microsoft/Windows/Fonts/" +
                "Roboto-VariableFont_wdth,wght.ttf"
        ).absolutePath
    )

dependencies {
    implementation(compose.desktop.currentOs)
    implementation("org.jetbrains.compose.material3:material3:$composeMaterial3Version")
    implementation("org.jetbrains.kotlinx:kotlinx-serialization-json:1.8.1")
}

dependencyLocking {
    lockAllConfigurations()
}

application {
    mainClass = "material3expressive.compose.reference.MainKt"
    applicationDefaultJvmArgs = listOf("-Dm3e.referenceFontPath=${referenceFontPath.get()}")
}

java {
    toolchain {
        languageVersion = JavaLanguageVersion.of(17)
    }
}

kotlin {
    compilerOptions {
        freeCompilerArgs.addAll(
            "-opt-in=androidx.compose.material3.ExperimentalMaterial3Api",
            "-opt-in=androidx.compose.material3.ExperimentalMaterial3ExpressiveApi",
            "-opt-in=androidx.compose.ui.ExperimentalComposeUiApi",
            "-opt-in=androidx.compose.foundation.ExperimentalFoundationApi",
        )
    }
}

val verifyReferenceFont by tasks.registering {
    group = "verification"
    description = "Verifies the exact Roboto variable font file."

    doLast {
        val source = file(referenceFontPath)
        check(source.isFile) { "Reference Roboto font not found: ${source.absolutePath}" }
        val digest = MessageDigest.getInstance("SHA-256")
            .digest(source.readBytes())
            .joinToString("") { "%02X".format(it) }
        check(digest == referenceFontSha256) {
            "Roboto font hash mismatch. Expected $referenceFontSha256, got $digest " +
                "from ${source.absolutePath}."
        }
        logger.lifecycle("Verified shared Roboto SHA-256: $digest")
    }
}

tasks.named("compileKotlin") {
    dependsOn(verifyReferenceFont)
}
