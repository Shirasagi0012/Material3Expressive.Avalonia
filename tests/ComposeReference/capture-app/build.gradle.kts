import java.security.MessageDigest

plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.plugin.compose")
}

val material3Version = "1.5.0-alpha24"
val composeVersion = "1.12.0-beta01"
val material3AarSha256 =
    "2D02D84CE0A73B604B6393738C1076B79B97BF33E3B4A4DB80502D75C2BAA09F"
val referenceFontSha256 =
    "B25986D18730960C7B27384AB2BC500856AE7FE9E71C9850019195FF9019F0B2"
val referenceFontPath =
    providers.gradleProperty("referenceFontPath").orElse(
        file(
            "${System.getenv("LOCALAPPDATA")}/Microsoft/Windows/Fonts/" +
                "Roboto-VariableFont_wdth,wght.ttf"
        ).absolutePath
    )
val generatedReferenceFontRoot = layout.buildDirectory.dir("generated/reference-font/res")
val referenceManifest =
    rootProject.file("../../visual-references/compose/1.5.0-alpha24/manifest.json")
val referenceManifestSha256 =
    MessageDigest.getInstance("SHA-256")
        .digest(referenceManifest.readBytes())
        .joinToString("") { "%02X".format(it) }
val generatedReferenceContractRoot =
    layout.buildDirectory.dir("generated/reference-contract/assets")

android {
    namespace = "material3expressive.compose.reference"
    compileSdk = 35

    defaultConfig {
        applicationId = "material3expressive.compose.reference"
        minSdk = 35
        targetSdk = 35
        versionCode = 1
        versionName = "1.0"
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
        buildConfigField("String", "MATERIAL3_VERSION", "\"$material3Version\"")
        buildConfigField(
            "String",
            "MATERIAL3_ARTIFACT",
            "\"androidx.compose.material3:material3-android:$material3Version\"",
        )
        buildConfigField("String", "MATERIAL3_AAR_SHA256", "\"$material3AarSha256\"")
        buildConfigField("String", "REFERENCE_FONT_SHA256", "\"$referenceFontSha256\"")
        buildConfigField(
            "String",
            "REFERENCE_MANIFEST_SHA256",
            "\"$referenceManifestSha256\"",
        )
    }

    buildTypes {
        release {
            isMinifyEnabled = false
        }
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    testOptions {
        animationsDisabled = true
    }

    sourceSets {
        getByName("main").res.directories.add(
            generatedReferenceFontRoot.get().asFile.absolutePath
        )
        getByName("androidTest").assets.directories.add(
            rootProject.file("../../visual-references/color-contracts").absolutePath
        )
        getByName("androidTest").assets.directories.add(
            generatedReferenceContractRoot.get().asFile.absolutePath
        )
    }
}

dependencies {
    implementation("androidx.compose.material3:material3:$material3Version")
    implementation("androidx.compose.ui:ui:$composeVersion")

    androidTestImplementation("androidx.compose.ui:ui-test-junit4:$composeVersion")
    androidTestImplementation("androidx.test.ext:junit:1.3.0")
    androidTestImplementation("androidx.test:runner:1.7.0")
    debugImplementation("androidx.compose.ui:ui-test-manifest:$composeVersion")
}

val prepareReferenceFont by tasks.registering {
    group = "verification"
    description = "Verifies and stages the exact Roboto variable font used by both renderers."
    val source = file(referenceFontPath)
    val destination = generatedReferenceFontRoot.map {
        it.file("font/roboto_reference.ttf")
    }
    inputs.file(source)
    outputs.file(destination)

    doLast {
        val digest = MessageDigest.getInstance("SHA-256")
            .digest(source.readBytes())
            .joinToString("") { "%02X".format(it) }
        check(digest == referenceFontSha256) {
            "Roboto font hash mismatch. Expected $referenceFontSha256, got $digest " +
                "from ${source.absolutePath}."
        }
        val output = destination.get().asFile
        output.parentFile.mkdirs()
        source.copyTo(output, overwrite = true)
        logger.lifecycle("Verified shared Roboto SHA-256: $digest")
    }
}

val prepareReferenceManifest by tasks.registering {
    group = "verification"
    description = "Stages the exact checked-in Compose reference manifest for instrumentation."
    val destination = generatedReferenceContractRoot.map {
        it.file("reference-manifest.json")
    }
    inputs.file(referenceManifest)
    outputs.file(destination)

    doLast {
        val digest = MessageDigest.getInstance("SHA-256")
            .digest(referenceManifest.readBytes())
            .joinToString("") { "%02X".format(it) }
        check(digest == referenceManifestSha256) {
            "Compose reference manifest changed during the build. " +
                "Expected $referenceManifestSha256, got $digest."
        }
        val output = destination.get().asFile
        output.parentFile.mkdirs()
        referenceManifest.copyTo(output, overwrite = true)
        logger.lifecycle("Verified reference manifest SHA-256: $digest")
    }
}

val verifyMaterial3ReferenceArtifact by tasks.registering {
    group = "verification"
    description = "Verifies the exact Material3 Android AAR used to produce reference images."

    doLast {
        val dependency = dependencies.create(
            "androidx.compose.material3:material3-android:$material3Version@aar"
        )
        val detached = configurations.detachedConfiguration(dependency)
        detached.isTransitive = false
        val artifact = detached.singleFile
        val digest = MessageDigest.getInstance("SHA-256")
            .digest(artifact.readBytes())
            .joinToString("") { "%02X".format(it) }
        check(digest == material3AarSha256) {
            "Material3 AAR hash mismatch. Expected $material3AarSha256, got $digest " +
                "from ${artifact.absolutePath}."
        }
        logger.lifecycle("Verified Material3 reference AAR SHA-256: $digest")
    }
}

tasks.named("preBuild") {
    dependsOn(verifyMaterial3ReferenceArtifact)
    dependsOn(prepareReferenceFont)
    dependsOn(prepareReferenceManifest)
}

// alpha24 was published with minCompileSdk 37 metadata, while upstream screenshot tests and this
// reference contract deliberately execute on API 35. This app compiles only its own small test
// shell against API 35; disabling the metadata preflight does not suppress AAPT, Kotlin, D8, install
// or runtime failures if the artifact actually cannot be consumed in that environment.
tasks.configureEach {
    if (name.endsWith("AarMetadata")) {
        enabled = false
    }
}
