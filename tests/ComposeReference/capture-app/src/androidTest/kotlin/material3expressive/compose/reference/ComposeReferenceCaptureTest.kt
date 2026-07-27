package material3expressive.compose.reference

import android.graphics.Bitmap
import android.graphics.Color as AndroidColor
import android.os.Build
import android.os.ParcelFileDescriptor
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.Checkbox
import androidx.compose.material3.ElevatedButton
import androidx.compose.material3.ExperimentalMaterial3ExpressiveApi
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asAndroidBitmap
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.captureToImage
import androidx.compose.ui.test.junit4.v2.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.dp
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.filters.SdkSuppress
import androidx.test.platform.app.InstrumentationRegistry
import java.io.ByteArrayOutputStream
import java.security.MessageDigest
import java.time.Instant
import java.util.Locale
import org.json.JSONArray
import org.json.JSONObject
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
@SdkSuppress(minSdkVersion = 35, maxSdkVersion = 35)
class ComposeReferenceCaptureTest {
    @get:Rule val compose = createComposeRule()

    @Test
    fun capturePinnedButtonCases() {
        Locale.setDefault(Locale.US)
        val outputRoot =
            requireNotNull(
                InstrumentationRegistry.getInstrumentation().targetContext.getExternalFilesDir(null)
            ).resolve("compose-reference-capture")
        outputRoot.deleteRecursively()
        outputRoot.mkdirs()
        runShell("rm -rf $DeviceStagingRoot")
        runShell("mkdir -p $DeviceStagingRoot")

        var current by mutableStateOf(Cases.first())
        compose.setContent {
            CompositionLocalProvider(
                LocalDensity provides Density(density = 1f, fontScale = 1f),
                LocalLayoutDirection provides LayoutDirection.Ltr,
            ) {
                CaptureScene(current)
            }
        }

        val captured = JSONArray()
        for (visualCase in Cases) {
            compose.runOnIdle { current = visualCase }
            compose.waitForIdle()
            // A second idle boundary makes template/layout updates settle before raster capture.
            compose.runOnIdle {}
            val png =
                compose
                    .onNodeWithTag(CaptureTag)
                    .captureToImage()
                    .asAndroidBitmap()
                    .toPng()
            val relativeImage = "cases/${visualCase.id}.png"
            val output = outputRoot.resolve(relativeImage)
            output.parentFile?.mkdirs()
            output.writeBytes(png)
            stageForHost(relativeImage, png)
            captured.put(
                JSONObject()
                    .put("id", visualCase.id)
                    .put("image", relativeImage)
                    .put("width", visualCase.width)
                    .put("height", visualCase.height)
                    .put("sha256", png.sha256())
            )
        }

        val metadata =
            JSONObject()
                .put("schemaVersion", 2)
                .put("referenceSetId", "compose-material3-1.5.0-alpha24-api35-mdpi")
                .put("composeMaterial3Version", "1.5.0-alpha24")
                .put(
                    "sourceArtifact",
                    "androidx.compose.material3:material3-android:1.5.0-alpha24",
                )
                .put(
                    "sourceArtifactSha256",
                    "2D02D84CE0A73B604B6393738C1076B79B97BF33E3B4A4DB80502D75C2BAA09F",
                )
                .put("colorContract", ColorContract.metadata())
                .put(
                    "environment",
                    JSONObject()
                        .put("androidApi", Build.VERSION.SDK_INT)
                        .put("densityDpi", 160)
                        .put("fontScale", 1.0)
                        .put("locale", "en-US")
                        .put("layoutDirection", "ltr")
                        .put("fontFamily", "Roboto")
                        .put(
                            "fontFileName",
                            "Roboto-VariableFont_wdth,wght.ttf",
                        )
                        .put(
                            "fontFileSha256",
                            "B25986D18730960C7B27384AB2BC500856AE7FE9E71C9850019195FF9019F0B2",
                        )
                        .put("fontWeightPolicy", "preset-weights-only")
                        .put("emulatorProfile", "Pixel_9_Pro-api35-mdpi-override"),
                )
                .put("captureId", "pixel-9-pro-api35-${Instant.now()}")
                .put("capturedAtUtc", Instant.now().toString())
                .put("cases", captured)
        val metadataBytes = (metadata.toString(2) + "\n").toByteArray()
        outputRoot.resolve("capture.json").writeBytes(metadataBytes)
        stageForHost("capture.json", metadataBytes)
        check(captured.length() == Cases.size)
    }

    @OptIn(ExperimentalMaterial3ExpressiveApi::class)
    @Composable
    private fun CaptureScene(visualCase: CaptureCase) {
        val colorScheme = referenceColorScheme(visualCase.dark)
        MaterialTheme(colorScheme = colorScheme, typography = ReferenceTypography) {
            Box(
                modifier =
                    Modifier.requiredSize(visualCase.width.dp, visualCase.height.dp)
                        .background(MaterialTheme.colorScheme.surface)
                        .testTag(CaptureTag),
                contentAlignment = Alignment.Center,
            ) {
                when (visualCase.variant) {
                    Variant.FoundationPrimaryRoles -> PrimaryRoleSwatches()
                    Variant.CardFilled ->
                        Card(Modifier.requiredSize(160.dp, 80.dp)) {
                            Spacer(Modifier.fillMaxSize())
                        }
                    Variant.FabPrimarySmall ->
                        FloatingActionButton(onClick = {}) {
                            Spacer(Modifier.size(24.dp))
                        }
                    Variant.CheckboxSelected ->
                        Checkbox(checked = true, onCheckedChange = {})
                    Variant.TextFieldOutlined ->
                        OutlinedTextField(
                            value = "Text",
                            onValueChange = {},
                            label = { Text("Label") },
                            singleLine = true,
                            modifier = Modifier.requiredSize(240.dp, 56.dp),
                        )
                    Variant.NavigationBar ->
                        NavigationBar(
                            modifier = Modifier.requiredSize(360.dp, 80.dp),
                            windowInsets = WindowInsets(0, 0, 0, 0),
                        ) {
                            listOf("One", "Two", "Three").forEachIndexed { index, label ->
                                NavigationBarItem(
                                    selected = index == 0,
                                    onClick = {},
                                    icon = { Spacer(Modifier.size(24.dp)) },
                                    label = { Text(label) },
                                )
                            }
                        }
                    else -> MediumButton(visualCase)
                }
            }
        }
    }

    @Composable
    private fun PrimaryRoleSwatches() {
        val colors =
            listOf(
                MaterialTheme.colorScheme.primary,
                MaterialTheme.colorScheme.onPrimary,
                MaterialTheme.colorScheme.primaryContainer,
                MaterialTheme.colorScheme.onPrimaryContainer,
            )
        Row(Modifier.fillMaxSize()) {
            colors.forEach { color ->
                Box(Modifier.width(60.dp).fillMaxHeight().background(color))
            }
        }
    }

    private fun referenceColorScheme(dark: Boolean) =
        ColorContract.scheme(dark)

    @OptIn(ExperimentalMaterial3ExpressiveApi::class)
    @Composable
    private fun MediumButton(visualCase: CaptureCase) {
        val height = ButtonDefaults.MediumContainerHeight
        val modifier = Modifier.height(height)
        val shapes = ButtonDefaults.shapesFor(height)
        val padding = ButtonDefaults.contentPaddingFor(height)
        val content: @Composable () -> Unit = { Text("Button") }

        when (visualCase.variant) {
            Variant.FoundationPrimaryRoles,
            Variant.CardFilled,
            Variant.FabPrimarySmall,
            Variant.CheckboxSelected,
            Variant.TextFieldOutlined,
            Variant.NavigationBar ->
                error("Non-button scenes are rendered outside MediumButton.")
            Variant.Filled ->
                Button(
                    onClick = {},
                    shapes = shapes,
                    modifier = modifier,
                    enabled = visualCase.enabled,
                    contentPadding = padding,
                ) {
                    content()
                }
            Variant.Tonal ->
                FilledTonalButton(
                    onClick = {},
                    shapes = shapes,
                    modifier = modifier,
                    enabled = visualCase.enabled,
                    contentPadding = padding,
                ) {
                    content()
                }
            Variant.Elevated ->
                ElevatedButton(
                    onClick = {},
                    shapes = shapes,
                    modifier = modifier,
                    enabled = visualCase.enabled,
                    contentPadding = padding,
                ) {
                    content()
                }
            Variant.Outlined ->
                OutlinedButton(
                    onClick = {},
                    shapes = shapes,
                    modifier = modifier,
                    enabled = visualCase.enabled,
                    contentPadding = padding,
                ) {
                    content()
                }
            Variant.Text ->
                TextButton(
                    onClick = {},
                    shapes = shapes,
                    modifier = modifier,
                    enabled = visualCase.enabled,
                    contentPadding = padding,
                ) {
                    content()
                }
        }
    }

    private fun Bitmap.toPng(): ByteArray {
        val stream = ByteArrayOutputStream()
        check(compress(Bitmap.CompressFormat.PNG, 100, stream))
        return stream.toByteArray()
    }

    private fun ByteArray.sha256(): String =
        MessageDigest.getInstance("SHA-256").digest(this).joinToString("") { "%02X".format(it) }

    private fun stageForHost(relativePath: String, bytes: ByteArray) {
        check(relativePath.matches(Regex("[A-Za-z0-9._/-]+")) && ".." !in relativePath)
        val destination = "$DeviceStagingRoot/$relativePath"
        val parent = destination.substringBeforeLast('/')
        runShell("mkdir -p $parent")
        val pipes =
            InstrumentationRegistry.getInstrumentation()
                .uiAutomation
                .executeShellCommandRw("tee $destination")
        check(pipes.size == 2)
        ParcelFileDescriptor.AutoCloseOutputStream(pipes[1]).use { it.write(bytes) }
        ParcelFileDescriptor.AutoCloseInputStream(pipes[0]).use { it.readBytes() }
    }

    private fun runShell(command: String) {
        ParcelFileDescriptor.AutoCloseInputStream(
                InstrumentationRegistry.getInstrumentation().uiAutomation.executeShellCommand(command)
            )
            .use { it.readBytes() }
    }

    private data class CaptureCase(
        val id: String,
        val variant: Variant,
        val enabled: Boolean,
        val dark: Boolean,
        val width: Int = 240,
        val height: Int = 96,
    )

    private enum class Variant {
        FoundationPrimaryRoles,
        CardFilled,
        FabPrimarySmall,
        CheckboxSelected,
        TextFieldOutlined,
        NavigationBar,
        Filled,
        Tonal,
        Elevated,
        Outlined,
        Text,
    }

    private companion object {
        const val CaptureTag = "material3-expressive-reference"
        const val DeviceStagingRoot = "/data/local/tmp/material3expressive-compose-reference"
        val ReferenceRoboto =
            FontFamily(
                Font(R.font.roboto_reference, FontWeight.Normal),
                Font(R.font.roboto_reference, FontWeight.Medium),
                Font(R.font.roboto_reference, FontWeight.Bold),
            )
        val ReferenceTypography =
            Typography().let { defaults ->
                Typography(
                    labelLarge = defaults.labelLarge.copy(fontFamily = ReferenceRoboto),
                )
            }

        val Cases =
            listOf(
                CaptureCase(
                    "foundation/color/primary-roles/light",
                    Variant.FoundationPrimaryRoles,
                    enabled = true,
                    dark = false,
                ),
                CaptureCase(
                    "foundation/color/primary-roles/dark",
                    Variant.FoundationPrimaryRoles,
                    enabled = true,
                    dark = true,
                ),
                CaptureCase(
                    "card/filled/fixed/enabled/light",
                    Variant.CardFilled,
                    enabled = true,
                    dark = false,
                    width = 240,
                    height = 128,
                ),
                CaptureCase(
                    "fab/primary/small/enabled/light",
                    Variant.FabPrimarySmall,
                    enabled = true,
                    dark = false,
                    width = 160,
                    height = 120,
                ),
                CaptureCase(
                    "selection/checkbox/selected/light",
                    Variant.CheckboxSelected,
                    enabled = true,
                    dark = false,
                    width = 96,
                    height = 96,
                ),
                CaptureCase(
                    "textfield/outlined/populated/light",
                    Variant.TextFieldOutlined,
                    enabled = true,
                    dark = false,
                    width = 280,
                    height = 96,
                ),
                CaptureCase(
                    "navigation/bar/three-items/selected/light",
                    Variant.NavigationBar,
                    enabled = true,
                    dark = false,
                    width = 360,
                    height = 112,
                ),
                CaptureCase(
                    "button/filled/medium/enabled/light",
                    Variant.Filled,
                    enabled = true,
                    dark = false,
                ),
                CaptureCase(
                    "button/filled/medium/enabled/dark",
                    Variant.Filled,
                    enabled = true,
                    dark = true,
                ),
                CaptureCase(
                    "button/filled/medium/disabled/light",
                    Variant.Filled,
                    enabled = false,
                    dark = false,
                ),
                CaptureCase(
                    "button/filled/medium/disabled/dark",
                    Variant.Filled,
                    enabled = false,
                    dark = true,
                ),
                CaptureCase(
                    "button/tonal/medium/enabled/light",
                    Variant.Tonal,
                    enabled = true,
                    dark = false,
                ),
                CaptureCase(
                    "button/elevated/medium/enabled/light",
                    Variant.Elevated,
                    enabled = true,
                    dark = false,
                ),
                CaptureCase(
                    "button/outlined/medium/enabled/light",
                    Variant.Outlined,
                    enabled = true,
                    dark = false,
                ),
                CaptureCase(
                    "button/text/medium/enabled/light",
                    Variant.Text,
                    enabled = true,
                    dark = false,
                ),
            )
    }

    private object ColorContract {
        private const val AssetName = "tonal-spot-spec2021-phone-seed-6750a4.json"
        private const val ExpectedSha256 =
            "F5EEA9067538EA66A70A3C9197136C13FD330B46E9D5DF484D1BCA4BC23DE39C"
        private val bytes by lazy {
            val context = InstrumentationRegistry.getInstrumentation().context
            context.assets.open(AssetName).use { it.readBytes() }.also {
                val actual =
                    MessageDigest.getInstance("SHA-256")
                        .digest(it)
                        .joinToString("") { byte -> "%02X".format(byte) }
                check(actual == ExpectedSha256) {
                    "Color contract hash mismatch. Expected $ExpectedSha256, got $actual."
                }
            }
        }
        private val document by lazy {
            JSONObject(bytes.toString(Charsets.UTF_8))
        }

        fun metadata() =
            JSONObject()
                .put("id", "tonal-spot-spec2021-phone-seed-6750a4")
                .put("seed", "#6750A4")
                .put("variant", "TonalSpot")
                .put("specVersion", "Spec2021")
                .put("platform", "Phone")
                .put(
                    "manifest",
                    "visual-references/color-contracts/$AssetName",
                )
                .put("sha256", ExpectedSha256)
                .put(
                    "materialColorUtilitiesCommit",
                    "05f04c94555fc8fbaa3f85ea3c899ad948e955cd",
                )

        fun scheme(dark: Boolean) =
            if (dark) {
                darkColorScheme(
                    primary = role("Primary", dark),
                    onPrimary = role("OnPrimary", dark),
                    primaryContainer = role("PrimaryContainer", dark),
                    onPrimaryContainer = role("OnPrimaryContainer", dark),
                    secondary = role("Secondary", dark),
                    onSecondary = role("OnSecondary", dark),
                    secondaryContainer = role("SecondaryContainer", dark),
                    onSecondaryContainer = role("OnSecondaryContainer", dark),
                    tertiary = role("Tertiary", dark),
                    onTertiary = role("OnTertiary", dark),
                    tertiaryContainer = role("TertiaryContainer", dark),
                    onTertiaryContainer = role("OnTertiaryContainer", dark),
                    background = role("Background", dark),
                    onBackground = role("OnBackground", dark),
                    surface = role("Surface", dark),
                    onSurface = role("OnSurface", dark),
                    surfaceVariant = role("SurfaceVariant", dark),
                    onSurfaceVariant = role("OnSurfaceVariant", dark),
                    outline = role("Outline", dark),
                    outlineVariant = role("OutlineVariant", dark),
                    inverseSurface = role("InverseSurface", dark),
                    inverseOnSurface = role("InverseOnSurface", dark),
                    inversePrimary = role("InversePrimary", dark),
                    surfaceTint = role("SurfaceTint", dark),
                    surfaceContainerLowest = role("SurfaceContainerLowest", dark),
                    surfaceContainerLow = role("SurfaceContainerLow", dark),
                    surfaceContainer = role("SurfaceContainer", dark),
                    surfaceContainerHigh = role("SurfaceContainerHigh", dark),
                    surfaceContainerHighest = role("SurfaceContainerHighest", dark),
                    surfaceBright = role("SurfaceBright", dark),
                    surfaceDim = role("SurfaceDim", dark),
                )
            } else {
                lightColorScheme(
                    primary = role("Primary", dark),
                    onPrimary = role("OnPrimary", dark),
                    primaryContainer = role("PrimaryContainer", dark),
                    onPrimaryContainer = role("OnPrimaryContainer", dark),
                    secondary = role("Secondary", dark),
                    onSecondary = role("OnSecondary", dark),
                    secondaryContainer = role("SecondaryContainer", dark),
                    onSecondaryContainer = role("OnSecondaryContainer", dark),
                    tertiary = role("Tertiary", dark),
                    onTertiary = role("OnTertiary", dark),
                    tertiaryContainer = role("TertiaryContainer", dark),
                    onTertiaryContainer = role("OnTertiaryContainer", dark),
                    background = role("Background", dark),
                    onBackground = role("OnBackground", dark),
                    surface = role("Surface", dark),
                    onSurface = role("OnSurface", dark),
                    surfaceVariant = role("SurfaceVariant", dark),
                    onSurfaceVariant = role("OnSurfaceVariant", dark),
                    outline = role("Outline", dark),
                    outlineVariant = role("OutlineVariant", dark),
                    inverseSurface = role("InverseSurface", dark),
                    inverseOnSurface = role("InverseOnSurface", dark),
                    inversePrimary = role("InversePrimary", dark),
                    surfaceTint = role("SurfaceTint", dark),
                    surfaceContainerLowest = role("SurfaceContainerLowest", dark),
                    surfaceContainerLow = role("SurfaceContainerLow", dark),
                    surfaceContainer = role("SurfaceContainer", dark),
                    surfaceContainerHigh = role("SurfaceContainerHigh", dark),
                    surfaceContainerHighest = role("SurfaceContainerHighest", dark),
                    surfaceBright = role("SurfaceBright", dark),
                    surfaceDim = role("SurfaceDim", dark),
                )
            }

        private fun role(name: String, dark: Boolean): Color {
            val roles = document.getJSONObject("roles")
            val value = roles.getJSONObject(name).getString(if (dark) "dark" else "light")
            return Color(AndroidColor.parseColor(value))
        }
    }
}
