package material3expressive.compose.reference.capture

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.key
import androidx.compose.ui.Alignment
import androidx.compose.ui.ImageComposeScene
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.TextUnitType
import androidx.compose.ui.unit.dp
import kotlinx.serialization.json.Json
import material3expressive.compose.reference.*
import material3expressive.compose.reference.probes.ProbeRegistry
import org.jetbrains.skia.EncodedImageFormat
import java.nio.file.Path
import java.security.MessageDigest
import kotlin.io.path.createDirectories
import kotlin.io.path.readBytes
import kotlin.io.path.writeBytes
import kotlin.io.path.writeText

private const val RENDER_SCALE = 2.0f

class ExpectedProducer(
    private val outputDir: Path,
    private val colorContract: ReferenceColorContract,
    private val typography: ReferenceTypography,
    private val caseRegistry: ReferenceCaseRegistry,
) {
    private val json = Json {
        prettyPrint = true
        encodeDefaults = true
        ignoreUnknownKeys = true
    }

    fun produce(caseIds: List<String>): ExpectedManifest {
        val expectedDir = outputDir.resolve("expected")
        val casesDir = expectedDir.resolve("cases")
        casesDir.createDirectories()

        val fileEntries = mutableListOf<ManifestFileEntry>()
        val caseEntries = mutableListOf<ManifestCaseEntry>()

        for (caseId in caseIds) {
            val scene = caseRegistry.get(caseId)
                ?: error("No scene registered for case '$caseId'.")
            val contract = caseRegistry.contract(caseId)
                ?: error("No contract for case '$caseId'.")

            val scale = 2.0f
            val widthPx = (contract.viewportWidthDp * scale).toInt()
            val heightPx = (contract.viewportHeightDp * scale).toInt()

            val probeRegistry = ProbeRegistry()
            val colorScheme = if (contract.dark) {
                colorContract.darkScheme()
            } else {
                colorContract.lightScheme()
            }

            val imageScene = ImageComposeScene(
                width = widthPx,
                height = heightPx,
                density = Density(density = scale, fontScale = 1.0f),
            )

            imageScene.setContent {
                CompositionLocalProvider(
                    LocalDensity provides Density(density = scale, fontScale = 1.0f),
                    LocalLayoutDirection provides LayoutDirection.Ltr,
                ) {
                    MaterialTheme(
                        colorScheme = colorScheme,
                        typography = typography.materialTypography,
                    ) {
                        Surface(color = MaterialTheme.colorScheme.surface) {
                            Box(
                                modifier = Modifier
                                    .requiredSize(contract.viewportWidthDp.dp, contract.viewportHeightDp.dp),
                                contentAlignment = Alignment.Center,
                            ) {
                                key(caseId) {
                                    scene.content(probeRegistry)
                                }
                            }
                        }
                    }
                }
            }

            val caseDir = casesDir.resolve(caseId)
            caseDir.createDirectories()

            imageScene.render().close()
            val skiaImage = imageScene.render()
            val (pngSha256, layoutSha256) = try {
                val pngBytes = skiaImage.encodeToData(EncodedImageFormat.PNG, 100)?.bytes
                    ?: error("Failed to encode image to PNG for case '$caseId'.")
                val pngPath = caseDir.resolve("image.png")
                pngPath.writeBytes(pngBytes)

                val captureResult = buildCaptureResult(caseId, contract, probeRegistry, widthPx, heightPx)
                val layoutJson = json.encodeToString(CaptureResult.serializer(), captureResult)
                val layoutPath = caseDir.resolve("layout.json")
                layoutPath.writeText(layoutJson)

                sha256(pngBytes) to sha256(layoutPath.readBytes())
            } finally {
                skiaImage.close()
                imageScene.close()
            }

            fileEntries.add(
                ManifestFileEntry(
                    path = "expected/cases/$caseId/image.png",
                    sha256 = pngSha256,
                    width = widthPx,
                    height = heightPx,
                )
            )
            fileEntries.add(
                ManifestFileEntry(
                    path = "expected/cases/$caseId/layout.json",
                    sha256 = layoutSha256,
                    width = 0,
                    height = 0,
                )
            )
            caseEntries.add(
                ManifestCaseEntry(
                    id = caseId,
                    imagePath = "cases/$caseId/image.png",
                    layoutPath = "cases/$caseId/layout.json",
                    imageSha256 = pngSha256,
                    layoutSha256 = layoutSha256,
                )
            )
        }

        val manifest = ExpectedManifest(
            composeMultiplatformPluginVersion = "1.12.0-beta02",
            composeRuntimeVersion = "1.12.0-beta02",
            composeMaterial3Version = "1.12.0-alpha03",
            jetpackMaterial3Lineage = "Jetpack Compose Material3 1.5.0-alpha22",
            kotlinVersion = KotlinVersion.CURRENT.toString(),
            jdkVersion = System.getProperty("java.version"),
            skikoVersion = System.getProperty("skiko.version") ?: "0.150.1",
            os = System.getProperty("os.name"),
            architecture = System.getProperty("os.arch"),
            density = RENDER_SCALE,
            fontScale = 1.0f,
            locale = "en-US",
            layoutDirection = "ltr",
            fontFileName = "Roboto-VariableFont_wdth,wght.ttf",
            fontFileSha256 = ReferenceTypography.expectedFontSha256,
            themeIdentifier = colorContract.manifestId,
            themeSha256 = colorContract.manifestSha256,
            caseCatalogSha256 = caseRegistry.catalogSha256(),
            viewportScale = RENDER_SCALE,
            files = fileEntries,
            cases = caseEntries,
        )

        val manifestPath = expectedDir.resolve("manifest.json")
        manifestPath.writeText(json.encodeToString(ExpectedManifest.serializer(), manifest))

        return manifest
    }

    private fun buildCaptureResult(
        caseId: String,
        contract: CaseContract,
        probes: ProbeRegistry,
        widthPx: Int,
        heightPx: Int,
    ): CaptureResult {
        val missingNodes = contract.requiredNodes
            .filter { probes.geometryBounds(it.id) == null }
            .map { it.id }
        check(missingNodes.isEmpty()) {
            "Capture for '$caseId' did not report required node(s): ${missingNodes.joinToString()}."
        }

        val nodes = contract.requiredNodes.map { spec ->
            val geom = checkNotNull(probes.geometryBounds(spec.id))
            val textLayout = captureTextLayout(caseId, spec, geom, probes.textLayoutResult(spec.id))

            CaptureNode(
                id = spec.id,
                kind = spec.kind,
                parentId = spec.parentId,
                boundsInRootPx = material3expressive.compose.reference.BoundsPx(
                    left = geom.left, top = geom.top, right = geom.right, bottom = geom.bottom
                ),
                visible = true,
                textLayout = textLayout,
            )
        }

        return CaptureResult(
            caseId = caseId,
            coordinateSpace = CoordinateSpace(scale = RENDER_SCALE),
            viewportPx = ViewportPx(width = widthPx, height = heightPx),
            nodes = nodes,
        )
    }

    private fun captureTextLayout(
        caseId: String,
        spec: RequiredNodeSpec,
        geometry: material3expressive.compose.reference.probes.BoundsPx,
        result: TextLayoutResult?,
    ): CaptureTextLayout? {
        if (spec.kind != "text") return null
        if (spec.text != null) {
            checkNotNull(result) { "Capture for '$caseId' did not report text layout for '${spec.id}'." }
        }
        result ?: return null

        val text = result.layoutInput.text.text
        if (spec.text != null) {
            check(text == spec.text) {
                "Text probe '${spec.id}' in '$caseId' reported '$text', expected '${spec.text}'."
            }
        }

        val lines = (0 until result.lineCount).map { index ->
            val localLeft = result.getLineLeft(index)
            val localRight = result.getLineRight(index)
            val localTop = result.getLineTop(index)
            val localBottom = result.getLineBottom(index)
            val localBaseline = result.getLineBaseline(index)
            CaptureTextLine(
                index = index,
                startUtf16 = result.getLineStart(index),
                endUtf16 = result.getLineEnd(index, false),
                visibleEndUtf16 = result.getLineEnd(index, true),
                leftPx = geometry.left + localLeft,
                rightPx = geometry.left + localRight,
                topPx = geometry.top + localTop,
                bottomPx = geometry.top + localBottom,
                baselinePx = geometry.top + localBaseline,
                ellipsized = result.isLineEllipsized(index),
                boundsInTextPx = material3expressive.compose.reference.BoundsPx(
                    left = localLeft,
                    top = localTop,
                    right = localRight,
                    bottom = localBottom,
                ),
                baselineInTextPx = localBaseline,
            )
        }
        val characters = (0 until text.length).map { offset ->
            val bounds = result.getBoundingBox(offset)
            CaptureCharacterBox(
                utf16Offset = offset,
                codeUnit = text[offset].code.toString(16).padStart(4, '0').uppercase(),
                lineIndex = result.getLineForOffset(offset),
                boundsInRootPx = material3expressive.compose.reference.BoundsPx(
                    left = geometry.left + bounds.left,
                    top = geometry.top + bounds.top,
                    right = geometry.left + bounds.right,
                    bottom = geometry.top + bounds.bottom,
                ),
                boundsInTextPx = material3expressive.compose.reference.BoundsPx(
                    left = bounds.left,
                    top = bounds.top,
                    right = bounds.right,
                    bottom = bounds.bottom,
                ),
            )
        }

        return CaptureTextLayout(
            text = text,
            firstBaselineInRootPx = geometry.top + result.firstBaseline,
            lastBaselineInRootPx = geometry.top + result.lastBaseline,
            lineCount = result.lineCount,
            overflowEllipsis = lines.any { it.ellipsized },
            lines = lines,
            characterBoxes = characters,
            drawOriginInRootPx = PointPx(geometry.left, geometry.top),
            firstBaselineInTextPx = result.firstBaseline,
            lastBaselineInTextPx = result.lastBaseline,
            styleSignature = if (isCustomDrawnTextFieldLabel(caseId, spec)) {
                null
            } else {
                captureTextStyleSignature(result.layoutInput.style)
            },
        )
    }

    private fun isCustomDrawnTextFieldLabel(caseId: String, spec: RequiredNodeSpec): Boolean =
        caseId.startsWith("textfield/") && spec.id == "label"

    private fun captureTextStyleSignature(style: TextStyle): CaptureTextStyleSignature {
        val fontSizePx = style.fontSize.toPhysicalPxOrNull()
        return CaptureTextStyleSignature(
            requestedFontFamily = ReferenceTypography.expectedFontFamily,
            resolvedFontFamily = ReferenceTypography.expectedFontFamily,
            pinnedFontFileSha256 = ReferenceTypography.expectedFontSha256,
            fontWeight = style.fontWeight?.weight ?: 400,
            fontSizePx = fontSizePx,
            lineHeightPx = style.lineHeight.toPhysicalPxOrNull(fontSizePx),
            letterSpacingPx = style.letterSpacing.toPhysicalPxOrNull(fontSizePx),
        )
    }

    private fun TextUnit.toPhysicalPxOrNull(fontSizePx: Float? = null): Float? = when (type) {
        TextUnitType.Sp -> value * RENDER_SCALE
        TextUnitType.Em -> fontSizePx?.let { value * it }
        else -> null
    }

    private fun sha256(bytes: ByteArray): String =
        MessageDigest.getInstance("SHA-256")
            .digest(bytes)
            .joinToString("") { "%02X".format(it) }
}
