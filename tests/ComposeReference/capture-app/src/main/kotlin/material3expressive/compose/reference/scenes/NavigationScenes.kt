package material3expressive.compose.reference.scenes

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
import androidx.compose.material3.PrimaryScrollableTabRow
import androidx.compose.material3.SecondaryTabRow
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRowDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.Dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

fun ReferenceCaseRegistry.registerNavigationScenes() {
    register(
        navCase("navigation/bar/three-items/selected/light"),
        ReferenceScene { probes -> NavBar(probes, selectedIndex = 0) },
    )
    register(
        navCase("navigation/bar/three-items/selected/dark", dark = true),
        ReferenceScene { probes -> NavBar(probes, selectedIndex = 0) },
    )
    register(
        navCase("navigation/bar/three-items/selected-second/light"),
        ReferenceScene { probes -> NavBar(probes, selectedIndex = 1) },
    )
    register(
        CaseContract(
            id = "navigation/rail/three-items/selected/light",
            viewportWidthDp = 128, viewportHeightDp = 312, dark = false,
            requiredNodes = navigationNodes(),
        ),
        ReferenceScene { probes -> NavRail(probes) },
    )
    register(
        CaseContract(
            id = "navigation/rail/three-items/selected/dark",
            viewportWidthDp = 128, viewportHeightDp = 312, dark = true,
            requiredNodes = navigationNodes(),
        ),
        ReferenceScene { probes -> NavRail(probes) },
    )
    register(
        tabCase("navigation/tab/primary/three-items/selected/light"),
        ReferenceScene { probes -> Tabs(probes, primary = true) },
    )
    register(
        tabCase("navigation/tab/primary/three-items/selected/dark", dark = true),
        ReferenceScene { probes -> Tabs(probes, primary = true) },
    )
    register(
        tabCase("navigation/tab/secondary/three-items/selected/light"),
        ReferenceScene { probes -> Tabs(probes, primary = false) },
    )
    register(
        tabCase("navigation/tab/secondary/three-items/selected/dark", dark = true),
        ReferenceScene { probes -> Tabs(probes, primary = false) },
    )
    register(
        scrollableTabCase("navigation/tab/primary/scrollable/selected-last/light"),
        ReferenceScene { probes -> ScrollableTabs(probes) },
    )
}

private fun navCase(id: String, dark: Boolean = false) = CaseContract(
    id = id, viewportWidthDp = 360, viewportHeightDp = 112, dark = dark,
    requiredNodes = navigationNodes(),
)

private fun tabCase(id: String, dark: Boolean = false) = CaseContract(
    id = id, viewportWidthDp = 360, viewportHeightDp = 80, dark = dark,
    requiredNodes = navigationNodes() + RequiredNodeSpec(id = "indicator", kind = "container", parentId = "component"),
)

private fun scrollableTabCase(id: String) = CaseContract(
    id = id, viewportWidthDp = 240, viewportHeightDp = 80, dark = false,
    requiredNodes = listOf(
        RequiredNodeSpec(id = "component", kind = "container"),
        RequiredNodeSpec(id = "indicator", kind = "container", parentId = "component"),
        RequiredNodeSpec(id = "label-2", kind = "text", parentId = "component", text = "Reviews"),
        RequiredNodeSpec(id = "label-3", kind = "text", parentId = "component", text = "Related items"),
    ),
)

private fun navigationNodes() = listOf(
    RequiredNodeSpec(id = "component", kind = "container"),
    RequiredNodeSpec(id = "label-0", kind = "text", parentId = "component", text = "One"),
    RequiredNodeSpec(id = "label-1", kind = "text", parentId = "component", text = "Two"),
    RequiredNodeSpec(id = "label-2", kind = "text", parentId = "component", text = "Three"),
)

@Composable
private fun NavBar(probes: ProbeRegistry, selectedIndex: Int) {
    NavigationBar(
        modifier = Modifier.requiredSize(360.dp, 80.dp).then(probes.geometry("component")),
        windowInsets = WindowInsets(0, 0, 0, 0),
    ) {
        listOf("One", "Two", "Three").forEachIndexed { index, label ->
            NavigationBarItem(
                selected = index == selectedIndex,
                onClick = {},
                icon = { Spacer(Modifier.size(24.dp)) },
                label = {
                    Text(
                        text = label,
                        modifier = probes.text("label-$index"),
                        onTextLayout = probes.onTextLayout("label-$index"),
                    )
                },
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun NavRail(probes: ProbeRegistry) {
    NavigationRail(
        modifier = Modifier.requiredSize(96.dp, 280.dp).then(probes.geometry("component")),
        windowInsets = WindowInsets(0, 0, 0, 0),
    ) {
        listOf("One", "Two", "Three").forEachIndexed { index, label ->
            NavigationRailItem(
                selected = index == 0,
                onClick = {},
                icon = { Spacer(Modifier.size(24.dp)) },
                label = {
                    Text(
                        text = label,
                        modifier = probes.text("label-$index"),
                        onTextLayout = probes.onTextLayout("label-$index"),
                    )
                },
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun Tabs(probes: ProbeRegistry, primary: Boolean) {
    val selectedTabIndex = 0
    val content: @Composable () -> Unit = {
        listOf("One", "Two", "Three").forEachIndexed { index, label ->
            Tab(
                selected = index == 0,
                onClick = {},
                text = {
                    Text(
                        text = label,
                        modifier = probes.text("label-$index"),
                        onTextLayout = probes.onTextLayout("label-$index"),
                    )
                },
            )
        }
    }
    val modifier = Modifier.requiredSize(360.dp, 48.dp).then(probes.geometry("component"))

    if (primary) {
        PrimaryTabRow(
            selectedTabIndex = selectedTabIndex,
            modifier = modifier,
            indicator = {
                TabRowDefaults.PrimaryIndicator(
                    modifier = Modifier.tabIndicatorOffset(selectedTabIndex, matchContentSize = true)
                        .then(probes.geometry("indicator")),
                    width = Dp.Unspecified,
                )
            },
        ) { content() }
    } else {
        SecondaryTabRow(
            selectedTabIndex = selectedTabIndex,
            modifier = modifier,
            indicator = {
                TabRowDefaults.SecondaryIndicator(
                    Modifier.tabIndicatorOffset(selectedTabIndex, matchContentSize = false)
                        .then(probes.geometry("indicator")),
                )
            },
        ) { content() }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ScrollableTabs(probes: ProbeRegistry) {
    val selectedTabIndex = 3
    PrimaryScrollableTabRow(
        selectedTabIndex = selectedTabIndex,
        modifier = Modifier.requiredSize(240.dp, 48.dp).then(probes.geometry("component")),
        edgePadding = 52.dp,
        minTabWidth = 90.dp,
        indicator = {
            TabRowDefaults.PrimaryIndicator(
                Modifier.tabIndicatorOffset(selectedTabIndex, matchContentSize = true)
                    .then(probes.geometry("indicator")),
                width = Dp.Unspecified,
            )
        },
    ) {
        listOf("Overview", "Specifications", "Reviews", "Related items").forEachIndexed { index, label ->
            Tab(
                selected = index == selectedTabIndex,
                onClick = {},
                text = {
                    Text(
                        text = label,
                        modifier = probes.text("label-$index"),
                        onTextLayout = probes.onTextLayout("label-$index"),
                    )
                },
            )
        }
    }
}
