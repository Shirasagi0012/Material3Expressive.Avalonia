package material3expressive.compose.reference

import kotlinx.serialization.Serializable

@Serializable
data class ExpectedManifest(
    val schemaVersion: Int = 1,
    val composeMultiplatformPluginVersion: String,
    val composeRuntimeVersion: String,
    val composeMaterial3Version: String,
    val jetpackMaterial3Lineage: String,
    val kotlinVersion: String,
    val jdkVersion: String,
    val skikoVersion: String,
    val os: String,
    val architecture: String,
    val density: Float,
    val fontScale: Float,
    val locale: String,
    val layoutDirection: String,
    val fontFileName: String,
    val fontFileSha256: String,
    val themeIdentifier: String,
    val themeSha256: String,
    val caseCatalogSha256: String,
    val captureSchemaVersion: Int = 1,
    val viewportScale: Float,
    val files: List<ManifestFileEntry>,
    val cases: List<ManifestCaseEntry>,
)

@Serializable
data class ManifestFileEntry(
    val path: String,
    val sha256: String,
    val width: Int,
    val height: Int,
)

@Serializable
data class ManifestCaseEntry(
    val id: String,
    val imagePath: String,
    val layoutPath: String,
    val imageSha256: String,
    val layoutSha256: String,
)

@Serializable
data class CaptureResult(
    val schemaVersion: Int = 1,
    val caseId: String,
    val coordinateSpace: CoordinateSpace,
    val viewportPx: ViewportPx,
    val nodes: List<CaptureNode>,
)

@Serializable
data class CoordinateSpace(
    val unit: String = "physical-pixel",
    val origin: String = "root-top-left",
    val xDirection: String = "right",
    val yDirection: String = "down",
    val scale: Float = 2.0f,
)

@Serializable
data class ViewportPx(
    val width: Int,
    val height: Int,
)

@Serializable
data class CaptureNode(
    val id: String,
    val kind: String,
    val parentId: String? = null,
    val boundsInRootPx: BoundsPx,
    val clipBoundsInRootPx: BoundsPx? = null,
    val visible: Boolean = true,
    val regionExpansionPx: RegionExpansion? = null,
    val textLayout: CaptureTextLayout? = null,
)

@Serializable
data class BoundsPx(
    val left: Float,
    val top: Float,
    val right: Float,
    val bottom: Float,
)

@Serializable
data class RegionExpansion(
    val left: Float,
    val top: Float,
    val right: Float,
    val bottom: Float,
)

@Serializable
data class CaptureTextLayout(
    val text: String,
    val encoding: String = "utf-16",
    val firstBaselineInRootPx: Float,
    val lastBaselineInRootPx: Float,
    val lineCount: Int,
    val overflowEllipsis: Boolean = false,
    val lines: List<CaptureTextLine>,
    val characterBoxes: List<CaptureCharacterBox>,
)

@Serializable
data class CaptureTextLine(
    val index: Int,
    val startUtf16: Int,
    val endUtf16: Int,
    val visibleEndUtf16: Int,
    val leftPx: Float,
    val rightPx: Float,
    val topPx: Float,
    val bottomPx: Float,
    val baselinePx: Float,
    val ellipsized: Boolean = false,
)

@Serializable
data class CaptureCharacterBox(
    val utf16Offset: Int,
    val codeUnit: String,
    val lineIndex: Int,
    val boundsInRootPx: BoundsPx,
)
