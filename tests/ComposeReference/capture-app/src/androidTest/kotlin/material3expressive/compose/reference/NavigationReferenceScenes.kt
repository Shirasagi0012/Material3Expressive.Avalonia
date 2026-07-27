package material3expressive.compose.reference

import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationRail
import androidx.compose.material3.NavigationRailItem
import androidx.compose.material3.PrimaryTabRow
import androidx.compose.material3.SecondaryTabRow
import androidx.compose.material3.Tab
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp

internal val NavigationReferenceScenes =
    mapOf<String, ReferenceScene>(
        "navigation/bar/three-items/selected/light" to referenceScene {
            ReferenceNavigationBar(selectedIndex = 0)
        },
        "navigation/bar/three-items/selected/dark" to referenceScene {
            ReferenceNavigationBar(selectedIndex = 0)
        },
        "navigation/bar/three-items/selected-second/light" to referenceScene {
            ReferenceNavigationBar(selectedIndex = 1)
        },
        "navigation/rail/three-items/selected/light" to referenceScene {
            ReferenceNavigationRail()
        },
        "navigation/rail/three-items/selected/dark" to referenceScene {
            ReferenceNavigationRail()
        },
        "navigation/tab/primary/three-items/selected/light" to referenceScene {
            ReferenceTabs(primary = true)
        },
        "navigation/tab/primary/three-items/selected/dark" to referenceScene {
            ReferenceTabs(primary = true)
        },
        "navigation/tab/secondary/three-items/selected/light" to referenceScene {
            ReferenceTabs(primary = false)
        },
        "navigation/tab/secondary/three-items/selected/dark" to referenceScene {
            ReferenceTabs(primary = false)
        },
    )

@Composable
private fun ReferenceNavigationBar(selectedIndex: Int) {
    NavigationBar(
        modifier = Modifier.requiredSize(360.dp, 80.dp).testTag(ComponentTag),
        windowInsets = WindowInsets(0, 0, 0, 0),
    ) {
        listOf("One", "Two", "Three").forEachIndexed { index, label ->
            NavigationBarItem(
                selected = index == selectedIndex,
                onClick = {},
                icon = { Spacer(Modifier.size(24.dp)) },
                label = { Text(label) },
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ReferenceNavigationRail() {
    NavigationRail(
        modifier = Modifier.requiredSize(96.dp, 280.dp).testTag(ComponentTag),
        windowInsets = WindowInsets(0, 0, 0, 0),
    ) {
        listOf("One", "Two", "Three").forEachIndexed { index, label ->
            NavigationRailItem(
                selected = index == 0,
                onClick = {},
                icon = { Spacer(Modifier.size(24.dp)) },
                label = { Text(label) },
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ReferenceTabs(primary: Boolean) {
    val content: @Composable () -> Unit = {
        listOf("One", "Two", "Three").forEachIndexed { index, label ->
            Tab(
                selected = index == 0,
                onClick = {},
                text = { Text(label) },
            )
        }
    }
    val modifier = Modifier.requiredSize(360.dp, 48.dp).testTag(ComponentTag)

    if (primary) {
        PrimaryTabRow(selectedTabIndex = 0, modifier = modifier) {
            content()
        }
    } else {
        SecondaryTabRow(selectedTabIndex = 0, modifier = modifier) {
            content()
        }
    }
}
