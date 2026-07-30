package material3expressive.compose.reference

import material3expressive.compose.reference.capture.ExpectedProducer
import material3expressive.compose.reference.scenes.registerButtonScenes
import material3expressive.compose.reference.scenes.registerAppBarScenes
import material3expressive.compose.reference.scenes.registerBottomSheetScenes
import material3expressive.compose.reference.scenes.registerComponentMotionScenes
import material3expressive.compose.reference.scenes.registerComponentScenes
import material3expressive.compose.reference.scenes.registerDividerScenes
import material3expressive.compose.reference.scenes.registerExposedDropdownMenuScenes
import material3expressive.compose.reference.scenes.registerFoundationScenes
import material3expressive.compose.reference.scenes.registerMotionProbeScenes
import material3expressive.compose.reference.scenes.registerNavigationScenes
import material3expressive.compose.reference.scenes.registerPickerInputScenes
import material3expressive.compose.reference.scenes.registerPullToRefreshBoxScenes
import material3expressive.compose.reference.scenes.registerSegmentedButtonScenes
import material3expressive.compose.reference.scenes.registerSnackbarScenes
import material3expressive.compose.reference.scenes.registerSwipeToDismissBoxScenes
import material3expressive.compose.reference.scenes.registerTextFieldScenes
import material3expressive.compose.reference.scenes.registerTypographyScenes
import java.nio.file.Path
import kotlin.io.path.Path
import kotlin.io.path.createDirectories
import kotlin.io.path.exists
import java.util.Locale

fun main(args: Array<String>) {
    val locale = Locale.forLanguageTag("en-US")
    Locale.setDefault(locale)
    Locale.setDefault(Locale.Category.FORMAT, locale)
    Locale.setDefault(Locale.Category.DISPLAY, locale)

    val parsed = parseArgs(args)
    val repositoryRoot = findRepositoryRoot()
    val requestedOutput = Path(parsed.output)
    val outputDir = if (requestedOutput.isAbsolute) requestedOutput else repositoryRoot.resolve(requestedOutput)
    outputDir.createDirectories()

    val colorContractPath = repositoryRoot.resolve(
        "visual-references/color-contracts/tonal-spot-spec2021-phone-seed-6750a4.json"
    )
    check(colorContractPath.exists()) {
        "Color contract not found: $colorContractPath"
    }

    val colorContract = ReferenceColorContract.load(colorContractPath)
    val typography = ReferenceTypography(referenceFontPath())
    val registry = ReferenceCaseRegistry().apply {
        registerFoundationScenes()
        registerMotionProbeScenes()
        registerTypographyScenes()
        registerComponentScenes()
        registerDividerScenes()
        registerComponentMotionScenes()
        registerButtonScenes()
        registerAppBarScenes()
        registerBottomSheetScenes()
        registerSegmentedButtonScenes()
        registerSnackbarScenes()
        registerTextFieldScenes()
        registerPickerInputScenes()
        registerNavigationScenes()
        registerExposedDropdownMenuScenes()
        registerSwipeToDismissBoxScenes()
        registerPullToRefreshBoxScenes()
    }

    val allIds = registry.allIds()
    val caseIds = if (parsed.cases.isEmpty()) {
        allIds
    } else {
        parsed.cases.flatMap { selector ->
            val matched = if (selector.endsWith("/*")) {
                val prefix = selector.removeSuffix("/*")
                allIds.filter { it.startsWith(prefix) }
            } else {
                listOf(selector)
            }
            check(matched.isNotEmpty()) { "No cases matched '$selector'." }
            matched
        }.distinct()
    }

    val producer = ExpectedProducer(outputDir, colorContract, typography, registry)
    val manifest = producer.produce(caseIds)

    println("Generated ${manifest.cases.size} expected case(s) in ${outputDir.resolve("expected")}")
    println("Manifest written with ${manifest.files.size} files.")
}

private fun referenceFontPath(): Path {
    val configured = System.getProperty("m3e.referenceFontPath")
    val fallback = "${System.getenv("LOCALAPPDATA")}/Microsoft/Windows/Fonts/" +
        "Roboto-VariableFont_wdth,wght.ttf"
    val path = Path(configured ?: fallback).toAbsolutePath()
    check(path.exists()) { "Reference font not found: $path" }
    return path
}

private data class Args(
    val output: String = "artifacts/compose-expected",
    val cases: List<String> = emptyList(),
)

private fun parseArgs(args: Array<String>): Args {
    var output = "artifacts/compose-expected"
    val cases = mutableListOf<String>()
    var i = 0
    while (i < args.size) {
        when (args[i]) {
            "--output" -> {
                output = args.getOrNull(++i) ?: error("--output requires a directory.")
            }
            "--case" -> {
                cases.add(args.getOrNull(++i) ?: error("--case requires an ID or prefix."))
            }
            else -> error("Unknown argument: ${args[i]}")
        }
        i++
    }
    return Args(output, cases)
}

private fun findRepositoryRoot(): Path {
    var dir = Path("").toAbsolutePath()
    while (true) {
        if (dir.resolve(".git").exists()) return dir
        val parent = dir.parent ?: error("No .git directory found in any ancestor.")
        dir = parent
    }
}
