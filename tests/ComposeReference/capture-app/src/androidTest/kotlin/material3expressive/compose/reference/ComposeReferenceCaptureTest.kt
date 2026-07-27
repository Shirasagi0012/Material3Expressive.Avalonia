package material3expressive.compose.reference

import android.graphics.Bitmap
import android.os.ParcelFileDescriptor
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.key
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.asAndroidBitmap
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.captureToImage
import androidx.compose.ui.test.junit4.v2.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.dp
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.filters.SdkSuppress
import androidx.test.platform.app.InstrumentationRegistry
import java.io.ByteArrayOutputStream
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
    fun capturePinnedCases() {
        Locale.setDefault(Locale.US)
        ReferenceTypographyContract.validate()
        val manifest = ReferenceManifestContract.read()
        val cases = ReferenceCaseRegistry.bind(manifest.cases)
        val outputRoot =
            requireNotNull(
                InstrumentationRegistry.getInstrumentation().targetContext.getExternalFilesDir(null)
            ).resolve("compose-reference-capture")
        outputRoot.deleteRecursively()
        outputRoot.mkdirs()
        runShell("rm -rf $DeviceStagingRoot")
        runShell("mkdir -p $DeviceStagingRoot")

        var currentIndex by mutableStateOf(0)
        compose.setContent {
            CompositionLocalProvider(
                LocalDensity provides Density(density = 1f, fontScale = 1f),
                LocalLayoutDirection provides LayoutDirection.Ltr,
            ) {
                CaptureScene(cases[currentIndex])
            }
        }

        val captured = JSONArray()
        for ((index, visualCase) in cases.withIndex()) {
            compose.runOnIdle { currentIndex = index }
            compose.waitForIdle()
            // A second idle boundary makes component/layout updates settle before raster capture.
            compose.runOnIdle {}
            visualCase.scene.prepare(compose)
            compose.waitForIdle()
            compose.runOnIdle {}
            visualCase.scene.validate(compose)
            assertComponentInsideViewport(visualCase.contract)
            val png =
                compose
                    .onNodeWithTag(CaptureTag)
                    .captureToImage()
                    .asAndroidBitmap()
                    .toPng()
            val relativeImage = visualCase.contract.image
            val output = outputRoot.resolve(relativeImage)
            output.parentFile?.mkdirs()
            output.writeBytes(png)
            stageForHost(relativeImage, png)
            captured.put(
                JSONObject()
                    .put("id", visualCase.contract.id)
                    .put("image", relativeImage)
                    .put("width", visualCase.contract.width)
                    .put("height", visualCase.contract.height)
                    .put("sha256", png.sha256Hex())
            )
        }

        val capturedAt = Instant.now()
        val metadata =
            JSONObject()
                .put("schemaVersion", manifest.schemaVersion)
                .put("referenceSetId", manifest.referenceSetId)
                .put("composeMaterial3Version", manifest.composeMaterial3Version)
                .put("sourceArtifact", manifest.sourceArtifact)
                .put("sourceArtifactSha256", manifest.sourceArtifactSha256)
                .put("referenceManifestSha256", manifest.sha256)
                .put("colorContract", ReferenceColorContract.metadata())
                .put("environment", manifest.environmentMetadata())
                .put("captureId", "pixel-9-pro-api35-$capturedAt")
                .put("capturedAtUtc", capturedAt.toString())
                .put("cases", captured)
        val metadataBytes = (metadata.toString(2) + "\n").toByteArray()
        outputRoot.resolve("capture.json").writeBytes(metadataBytes)
        stageForHost("capture.json", metadataBytes)
        check(captured.length() == manifest.cases.size)
    }

    @Composable
    private fun CaptureScene(visualCase: BoundReferenceCase) {
        val contract = visualCase.contract
        MaterialTheme(
            colorScheme = ReferenceColorContract.scheme(contract.dark),
            typography = ReferenceTypographyContract.Typography,
        ) {
            Box(
                modifier =
                    Modifier.requiredSize(contract.width.dp, contract.height.dp)
                        .background(MaterialTheme.colorScheme.surface)
                        .testTag(CaptureTag),
                contentAlignment = Alignment.Center,
            ) {
                key(contract.id) {
                    visualCase.scene.content()
                }
            }
        }
    }

    private fun assertComponentInsideViewport(contract: ReferenceCaseContract) {
        val bounds = compose.onNodeWithTag(ComponentTag).fetchSemanticsNode().boundsInRoot
        check(bounds.width > 0 && bounds.height > 0) {
            "Case '${contract.id}' rendered an empty component."
        }
        check(
            bounds.left >= 0f &&
                bounds.top >= 0f &&
                bounds.right <= contract.width.toFloat() &&
                bounds.bottom <= contract.height.toFloat()
        ) {
            "Case '${contract.id}' component bounds $bounds escape " +
                "${contract.width}x${contract.height}."
        }
    }

    private fun Bitmap.toPng(): ByteArray {
        val stream = ByteArrayOutputStream()
        check(compress(Bitmap.CompressFormat.PNG, 100, stream))
        return stream.toByteArray()
    }

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

    private companion object {
        const val DeviceStagingRoot = "/data/local/tmp/material3expressive-compose-reference"
    }
}
