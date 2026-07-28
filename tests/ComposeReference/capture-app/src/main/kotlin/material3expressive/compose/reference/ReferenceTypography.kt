package material3expressive.compose.reference

import androidx.compose.material3.Typography
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.platform.Font
import java.nio.file.Path

class ReferenceTypography(fontPath: Path) {
    private val fontFile = fontPath.toFile()

    val roboto: FontFamily = FontFamily(
        Font(fontFile, FontWeight.Normal),
        Font(fontFile, FontWeight.Medium),
        Font(fontFile, FontWeight.Bold),
    )

    private val defaults = Typography()

    val materialTypography: Typography = defaults.copy(
        displayLarge = defaults.displayLarge.withReferenceFont(),
        displayMedium = defaults.displayMedium.withReferenceFont(),
        displaySmall = defaults.displaySmall.withReferenceFont(),
        headlineLarge = defaults.headlineLarge.withReferenceFont(),
        headlineMedium = defaults.headlineMedium.withReferenceFont(),
        headlineSmall = defaults.headlineSmall.withReferenceFont(),
        titleLarge = defaults.titleLarge.withReferenceFont(),
        titleMedium = defaults.titleMedium.withReferenceFont(),
        titleSmall = defaults.titleSmall.withReferenceFont(),
        bodyLarge = defaults.bodyLarge.withReferenceFont(),
        bodyMedium = defaults.bodyMedium.withReferenceFont(),
        bodySmall = defaults.bodySmall.withReferenceFont(),
        labelLarge = defaults.labelLarge.withReferenceFont(),
        labelMedium = defaults.labelMedium.withReferenceFont(),
        labelSmall = defaults.labelSmall.withReferenceFont(),
        displayLargeEmphasized = defaults.displayLargeEmphasized.withReferenceFont(),
        displayMediumEmphasized = defaults.displayMediumEmphasized.withReferenceFont(),
        displaySmallEmphasized = defaults.displaySmallEmphasized.withReferenceFont(),
        headlineLargeEmphasized = defaults.headlineLargeEmphasized.withReferenceFont(),
        headlineMediumEmphasized = defaults.headlineMediumEmphasized.withReferenceFont(),
        headlineSmallEmphasized = defaults.headlineSmallEmphasized.withReferenceFont(),
        titleLargeEmphasized = defaults.titleLargeEmphasized.withReferenceFont(),
        titleMediumEmphasized = defaults.titleMediumEmphasized.withReferenceFont(),
        titleSmallEmphasized = defaults.titleSmallEmphasized.withReferenceFont(),
        bodyLargeEmphasized = defaults.bodyLargeEmphasized.withReferenceFont(),
        bodyMediumEmphasized = defaults.bodyMediumEmphasized.withReferenceFont(),
        bodySmallEmphasized = defaults.bodySmallEmphasized.withReferenceFont(),
        labelLargeEmphasized = defaults.labelLargeEmphasized.withReferenceFont(),
        labelMediumEmphasized = defaults.labelMediumEmphasized.withReferenceFont(),
        labelSmallEmphasized = defaults.labelSmallEmphasized.withReferenceFont(),
    )

    private fun androidx.compose.ui.text.TextStyle.withReferenceFont() =
        copy(fontFamily = roboto)

    companion object {
        const val expectedFontFamily = "Roboto"
        const val expectedFontSha256 =
            "B25986D18730960C7B27384AB2BC500856AE7FE9E71C9850019195FF9019F0B2"
    }
}
