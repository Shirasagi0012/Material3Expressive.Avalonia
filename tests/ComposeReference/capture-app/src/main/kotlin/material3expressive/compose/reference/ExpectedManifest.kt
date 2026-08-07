package material3expressive.compose.reference

import kotlinx.serialization.Serializable

@Serializable
data class ExpectedManifest(
    val schemaVersion: Int = 3,
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
    val captureSchemaVersion: Int = 10,
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
    val schemaVersion: Int = 10,
    val caseId: String,
    val coordinateSpace: CoordinateSpace,
    val viewportPx: ViewportPx,
    val nodes: List<CaptureNode>,
    val motion: CaptureMotion? = null,
)

@Serializable
data class CaptureMotion(
    val schemaVersion: Int = 1,
    val scenarioId: String,
    val checkpointId: String,
    val origin: String = "first-controlled-motion-frame",
    val timeUnit: String = "100ns-ticks",
    val durationScale: Float = 1.0f,
    val elapsedTicks: Long,
    val events: List<CaptureMotionEvent>,
    val deliveryFrameTicks: List<Long>,
    val captureFrameIndex: Int,
    val scheduleSha256: String,
    val clockCoverage: CaptureMotionClockCoverage,
)

@Serializable
data class CaptureMotionEvent(
    val atTicks: Long,
    val action: String,
)

@Serializable
data class CaptureMotionClockCoverage(
    val composeFrameClock: String = "controlled",
    val composeCoroutineClock: String,
    val avaloniaMotionCoordinator: String = "controlled",
    val avaloniaTransition: String = "forbidden",
    val avaloniaComposition: String = "forbidden",
    val inputTimestamp: String = "insensitive",
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
    /** The observed semantic focus state when the corresponding node explicitly contracts it. */
    val focused: Boolean? = null,
    /** Fully resolved Skia m150 shadow inputs for renderer-level elevation probes. */
    val shadow: CaptureShadow? = null,
)

@Serializable
data class CaptureShadow(
    val skiaBehaviorVersion: String = "m150",
    val shapeKind: String,
    val shapeBoundsLocalDp: BoundsPx,
    val occluderBoundsDevicePx: BoundsPx,
    val cornerRadiiDp: CornerRadiiPx,
    val localToDevice: AffineTransformPx,
    val elevationPx: Float,
    val ambientColorArgb: String,
    val spotColorArgb: String,
    val ambientAlpha: Float,
    val spotAlpha: Float,
    val opacity: Float,
    val transparentOccluder: Boolean,
    val lightPositionPx: Point3Px,
    val lightRadiusPx: Float,
    val flags: List<String>,
    val clipBoundsInRootPx: BoundsPx? = null,
    val occluderDrawn: Boolean,
)

@Serializable
data class CornerRadiiPx(
    val topLeft: Float,
    val topRight: Float,
    val bottomRight: Float,
    val bottomLeft: Float,
)

@Serializable
data class AffineTransformPx(
    val m11: Float,
    val m12: Float,
    val m21: Float,
    val m22: Float,
    val m31: Float,
    val m32: Float,
)

@Serializable
data class Point3Px(val x: Float, val y: Float, val z: Float)

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
    val drawOriginInRootPx: PointPx? = null,
    val firstBaselineInTextPx: Float? = null,
    val lastBaselineInTextPx: Float? = null,
    val styleSignature: CaptureTextStyleSignature? = null,
    val paintSignature: CaptureTextPaintSignature? = null,
)

@Serializable
data class PointPx(
    val x: Float,
    val y: Float,
)

@Serializable
data class CaptureTextStyleSignature(
    val requestedFontFamily: String,
    val resolvedFontFamily: String,
    val pinnedFontFileSha256: String? = null,
    val fontWeight: Int,
    val fontSizePx: Float? = null,
    val lineHeightPx: Float? = null,
    val letterSpacingPx: Float? = null,
)

@Serializable
data class CaptureTextPaintSignature(
    val kind: String,
    val foregroundArgb: String? = null,
    val opacity: Float = 1f,
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
    val boundsInTextPx: BoundsPx? = null,
    val baselineInTextPx: Float? = null,
)

@Serializable
data class CaptureCharacterBox(
    val utf16Offset: Int,
    val codeUnit: String,
    val lineIndex: Int,
    val boundsInRootPx: BoundsPx,
    val boundsInTextPx: BoundsPx? = null,
)
