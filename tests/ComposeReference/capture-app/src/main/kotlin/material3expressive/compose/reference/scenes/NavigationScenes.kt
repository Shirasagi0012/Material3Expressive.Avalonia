// Implementation-source basis: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/{ListItem,ListItemDefaults}.kt
// and Scaffold.kt, plus tokens/{ListTokens,ReorderListTokens,StateTokens}.kt (VERSION 29.0.0).
// The executable scenes below intentionally use only
// interaction overloads present in the pinned Compose Multiplatform Material3 1.12.0-alpha03 / Jetpack
// Material3 1.5.0-alpha22 reference. Alpha22 does not expose alpha25's non-interactive
// `ListItem(content = ...)` overload, so InteractionMode.None is parity unavailable. Their explicit
// 56/72/88dp sizes also keep the alpha25 expressive text-line-height flag out of this executable
// comparison; that newer behavior is likewise parity unavailable. Alpha22 Scaffold has the same
// slot geometry and resulting draw order as alpha25, but alpha25 makes each slot z-index explicit.

package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.DragInteraction
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExperimentalMaterial3ExpressiveApi
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationRail
import androidx.compose.material3.NavigationRailItem
import androidx.compose.material3.ListItem
import androidx.compose.material3.ListItemDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.PrimaryTabRow
import androidx.compose.material3.PrimaryScrollableTabRow
import androidx.compose.material3.FabPosition
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SecondaryTabRow
import androidx.compose.material3.SegmentedListItem
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRowDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.LayoutDirection
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry
import material3expressive.compose.reference.probes.ShadowProbeSpec
import kotlinx.coroutines.yield

@OptIn(ExperimentalMaterial3ExpressiveApi::class)
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
    register(
        scrollableTabWideCase("navigation/tab/primary/scrollable/all-items/selected-last/light"),
        ReferenceScene { probes -> ScrollableTabs(probes, width = 600.dp) },
    )
    register(
        listItemCase(
            id = "listitem/click/enabled/light",
            requiredNodes = listItemTextNodes("Settings"),
        ),
        ReferenceScene { probes ->
            ListItem(
                onClick = {},
                modifier = listItemModifier(probes, 56.dp),
                content = { ListItemText("Settings", "headline", probes) },
            )
        },
    )
    register(
        listItemCase(
            id = "listitem/click/disabled/light",
            requiredNodes = listItemTextNodes("Offline"),
        ),
        ReferenceScene { probes ->
            ListItem(
                onClick = {},
                enabled = false,
                modifier = listItemModifier(probes, 56.dp),
                content = { ListItemText("Offline", "headline", probes) },
            )
        },
    )
    register(
        listItemCase(
            id = "listitem/single-selection/selected-disabled/light",
            requiredNodes = listItemTextNodes("Default network"),
        ),
        ReferenceScene { probes ->
            ListItem(
                selected = true,
                onClick = {},
                enabled = false,
                modifier = listItemModifier(probes, 56.dp),
                content = { ListItemText("Default network", "headline", probes) },
            )
        },
    )
    register(
        listItemCase(
            id = "listitem/click/dragged/light",
            requiredNodes = listItemTextNodes("Reorder item", shadow = true),
        ),
        ReferenceScene { probes -> DraggedListItem(probes, enabled = true) },
    )
    register(
        listItemCase(
            id = "listitem/click/disabled-dragged/light",
            requiredNodes = listItemTextNodes("Reorder item", shadow = true),
        ),
        ReferenceScene { probes -> DraggedListItem(probes, enabled = false) },
    )
    register(
        listItemCase(
            id = "listitem/click/two-line/slots/light",
            height = 72.dp,
            requiredNodes = listOf(
                RequiredNodeSpec(id = "component", kind = "container"),
                RequiredNodeSpec(id = "leading", kind = "container", parentId = "component"),
                RequiredNodeSpec(id = "headline", kind = "text", parentId = "component", text = "Network"),
                RequiredNodeSpec(id = "supporting", kind = "text", parentId = "component", text = "Wi-Fi, mobile data"),
                RequiredNodeSpec(id = "trailing", kind = "text", parentId = "component", text = "On"),
            ),
        ),
        ReferenceScene { probes ->
            ListItem(
                onClick = {},
                modifier = listItemModifier(probes, 72.dp),
                leadingContent = {
                    Spacer(Modifier.size(24.dp).then(probes.geometry("leading")))
                },
                trailingContent = { ListItemText("On", "trailing", probes) },
                supportingContent = { ListItemText("Wi-Fi, mobile data", "supporting", probes) },
                content = { ListItemText("Network", "headline", probes) },
            )
        },
    )
    // Compose ListItem accepts composable slot lambdas rather than Avalonia-style string content.
    // Keep the same alpha22 Text nodes and 72dp Click overload here; the matching Avalonia case
    // deliberately reaches them through ContentPresenter-generated TextBlocks.
    register(
        listItemCase(
            id = "listitem/click/two-line/string-slots/light",
            height = 72.dp,
            requiredNodes = listOf(
                RequiredNodeSpec(id = "component", kind = "container"),
                RequiredNodeSpec(id = "leading", kind = "container", parentId = "component"),
                RequiredNodeSpec(id = "headline", kind = "text", parentId = "component", text = "Network"),
                RequiredNodeSpec(id = "supporting", kind = "text", parentId = "component", text = "Wi-Fi, mobile data"),
                RequiredNodeSpec(id = "trailing", kind = "text", parentId = "component", text = "On"),
            ),
        ),
        ReferenceScene { probes ->
            ListItem(
                onClick = {},
                modifier = listItemModifier(probes, 72.dp),
                leadingContent = {
                    Spacer(Modifier.size(24.dp).then(probes.geometry("leading")))
                },
                trailingContent = { ListItemText("On", "trailing", probes) },
                supportingContent = { ListItemText("Wi-Fi, mobile data", "supporting", probes) },
                content = { ListItemText("Network", "headline", probes) },
            )
        },
    )
    register(
        listItemCase(
            id = "listitem/click/three-line/overline-supporting/light",
            height = 88.dp,
            requiredNodes = listOf(
                RequiredNodeSpec(id = "component", kind = "container"),
                RequiredNodeSpec(id = "overline", kind = "text", parentId = "component", text = "Storage"),
                RequiredNodeSpec(id = "headline", kind = "text", parentId = "component", text = "Cloud backup"),
                RequiredNodeSpec(id = "supporting", kind = "text", parentId = "component", text = "Back up photos and videos"),
            ),
        ),
        ReferenceScene { probes ->
            ListItem(
                onClick = {},
                modifier = listItemModifier(probes, 88.dp),
                overlineContent = { ListItemText("Storage", "overline", probes) },
                supportingContent = { ListItemText("Back up photos and videos", "supporting", probes) },
                content = { ListItemText("Cloud backup", "headline", probes) },
            )
        },
    )
    register(
        listItemCase(
            id = "listitem/single-selection/selected/light",
            requiredNodes = listItemTextNodes("Default network"),
        ),
        ReferenceScene { probes ->
            ListItem(
                selected = true,
                onClick = {},
                modifier = listItemModifier(probes, 56.dp),
                content = { ListItemText("Default network", "headline", probes) },
            )
        },
    )
    register(
        listItemCase(
            id = "listitem/single-selection/selected/dark",
            dark = true,
            requiredNodes = listItemTextNodes("Default network"),
        ),
        ReferenceScene { probes ->
            ListItem(
                selected = true,
                onClick = {},
                modifier = listItemModifier(probes, 56.dp),
                content = { ListItemText("Default network", "headline", probes) },
            )
        },
    )
    register(
        listItemCase(
            id = "listitem/multi-selection/selected/light",
            requiredNodes = listItemTextNodes("Sync over Wi-Fi"),
        ),
        ReferenceScene { probes ->
            ListItem(
                checked = true,
                onCheckedChange = {},
                modifier = listItemModifier(probes, 56.dp),
                content = { ListItemText("Sync over Wi-Fi", "headline", probes) },
            )
        },
    )
    // Both segmented APIs exist in the pinned alpha22 executable reference. Keep this matrix
    // intentionally narrow: the three-row scene covers first/middle/last geometry plus selected
    // state precedence, while the selected one-row dark scene independently covers the single-row
    // layout and dark segmentedColors() resolution. Resting single-shape corners are locked by the
    // deterministic Avalonia structural tests because Surface-on-Surface has no visible edge.
    register(
        segmentedListCase(
            id = "listitem/segmented/three-items/selected-middle/light",
            labels = listOf("Account", "Notifications", "Privacy"),
        ),
        ReferenceScene { probes ->
            SegmentedList(
                probes = probes,
                labels = listOf("Account", "Notifications", "Privacy"),
                selectedIndex = 1,
            )
        },
    )
    register(
        segmentedListCase(
            id = "listitem/segmented/single-selected/dark",
            labels = listOf("Appearance"),
            dark = true,
        ),
        ReferenceScene { probes ->
            SegmentedList(
                probes = probes,
                labels = listOf("Appearance"),
                selectedIndex = 0,
            )
        },
    )
    register(
        scaffoldCase("scaffold/bars-fab-snackbar/light"),
        ReferenceScene { probes ->
            ScaffoldScene(probes)
        },
    )
    register(
        scaffoldCase(
            id = "scaffold/end-overlay/rtl/dark",
            dark = true,
            requiredNodes = listOf(
                RequiredNodeSpec(id = "component", kind = "container"),
                RequiredNodeSpec(id = "body", kind = "container", parentId = "component"),
                RequiredNodeSpec(id = "padded-body", kind = "container", parentId = "component"),
                RequiredNodeSpec(id = "bottom-bar", kind = "container", parentId = "component"),
                RequiredNodeSpec(id = "fab", kind = "container", parentId = "component"),
            ),
        ),
        ReferenceScene { probes ->
            CompositionLocalProvider(LocalLayoutDirection provides LayoutDirection.Rtl) {
                ScaffoldScene(
                    probes = probes,
                    fabPosition = FabPosition.EndOverlay,
                    showTopBar = false,
                    showSnackbar = false,
                )
            }
        },
    )
}

private fun scaffoldCase(
    id: String,
    dark: Boolean = false,
    requiredNodes: List<RequiredNodeSpec> = listOf(
        RequiredNodeSpec(id = "component", kind = "container"),
        RequiredNodeSpec(id = "body", kind = "container", parentId = "component"),
        RequiredNodeSpec(id = "padded-body", kind = "container", parentId = "component"),
        RequiredNodeSpec(id = "top-bar", kind = "container", parentId = "component"),
        RequiredNodeSpec(id = "bottom-bar", kind = "container", parentId = "component"),
        RequiredNodeSpec(id = "snackbar", kind = "container", parentId = "component"),
        RequiredNodeSpec(id = "fab", kind = "container", parentId = "component"),
    ),
) = CaseContract(
    id = id,
    viewportWidthDp = 360,
    viewportHeightDp = 400,
    dark = dark,
    requiredNodes = requiredNodes,
)

@Composable
private fun ScaffoldScene(
    probes: ProbeRegistry,
    fabPosition: FabPosition = FabPosition.End,
    showTopBar: Boolean = true,
    showSnackbar: Boolean = true,
) {
    Scaffold(
        modifier = Modifier.requiredSize(360.dp, 400.dp).then(probes.geometry("component")),
        contentWindowInsets = WindowInsets(10.dp, 12.dp, 14.dp, 16.dp),
        floatingActionButtonPosition = fabPosition,
        topBar = {
            if (showTopBar) {
                Box(
                    Modifier
                        .requiredSize(360.dp, 48.dp)
                        .background(MaterialTheme.colorScheme.primaryContainer)
                        .then(probes.geometry("top-bar")),
                )
            }
        },
        bottomBar = {
            Box(
                Modifier
                    .requiredSize(360.dp, 80.dp)
                    .background(MaterialTheme.colorScheme.secondaryContainer)
                    .then(probes.geometry("bottom-bar")),
            )
        },
        snackbarHost = {
            if (showSnackbar) {
                Box(
                    Modifier
                        .requiredSize(240.dp, 48.dp)
                        .background(MaterialTheme.colorScheme.inverseSurface)
                        .then(probes.geometry("snackbar")),
                )
            }
        },
        floatingActionButton = {
            Box(
                Modifier
                    .requiredSize(56.dp, 56.dp)
                    .background(MaterialTheme.colorScheme.tertiaryContainer)
                    .then(probes.geometry("fab")),
            )
        },
    ) { contentPadding ->
        Box(
            Modifier
                .fillMaxSize()
                .background(MaterialTheme.colorScheme.surface)
                .then(probes.geometry("body")),
        ) {
            Box(Modifier.padding(contentPadding)) {
                Box(
                    Modifier
                        .fillMaxSize()
                        .background(MaterialTheme.colorScheme.surfaceVariant)
                        .then(probes.geometry("padded-body")),
                )
            }
        }
    }
}

private fun listItemCase(
    id: String,
    height: Dp = 56.dp,
    dark: Boolean = false,
    requiredNodes: List<RequiredNodeSpec>,
) = CaseContract(
    id = id,
    viewportWidthDp = 360,
    viewportHeightDp = height.value.toInt() + 24,
    dark = dark,
    requiredNodes = requiredNodes,
)

private fun listItemTextNodes(headline: String, shadow: Boolean = false) = listOf(
    RequiredNodeSpec(id = "component", kind = if (shadow) "shadow" else "container"),
    RequiredNodeSpec(id = "headline", kind = "text", parentId = "component", text = headline),
)

private fun listItemModifier(probes: ProbeRegistry, height: Dp): Modifier =
    Modifier.requiredSize(360.dp, height).then(probes.geometry("component"))

private fun segmentedListCase(
    id: String,
    labels: List<String>,
    dark: Boolean = false,
) = CaseContract(
    id = id,
    viewportWidthDp = 360,
    viewportHeightDp = labels.size * 56 + (labels.size - 1) * 2 + 24,
    dark = dark,
    requiredNodes = listOf(
        RequiredNodeSpec(id = "component", kind = "container"),
        *labels.flatMapIndexed { index, label ->
            listOf(
                RequiredNodeSpec(
                    id = "item-$index",
                    kind = "container",
                    parentId = "component",
                ),
                RequiredNodeSpec(
                    id = "headline-$index",
                    kind = "text",
                    parentId = "item-$index",
                    text = label,
                ),
            )
        }.toTypedArray(),
    ),
)

@Composable
private fun SegmentedList(
    probes: ProbeRegistry,
    labels: List<String>,
    selectedIndex: Int? = null,
) {
    val count = labels.size
    Column(
        modifier = Modifier
            .requiredSize(360.dp, (count * 56 + (count - 1) * 2).dp)
            .then(probes.geometry("component")),
        verticalArrangement = Arrangement.spacedBy(ListItemDefaults.SegmentedGap),
    ) {
        labels.forEachIndexed { index, label ->
            val modifier = Modifier
                .requiredSize(360.dp, 56.dp)
                .then(probes.geometry("item-$index"))
            val content = @Composable {
                ListItemText(label, "headline-$index", probes)
            }
            if (index == selectedIndex) {
                SegmentedListItem(
                    selected = true,
                    onClick = {},
                    shapes = ListItemDefaults.segmentedShapes(index = index, count = count),
                    modifier = modifier,
                    content = content,
                )
            } else {
                SegmentedListItem(
                    onClick = {},
                    shapes = ListItemDefaults.segmentedShapes(index = index, count = count),
                    modifier = modifier,
                    content = content,
                )
            }
        }
    }
}

@Composable
private fun ListItemText(
    text: String,
    id: String,
    probes: ProbeRegistry,
    color: Color = Color.Unspecified,
) {
    Text(
        text = text,
        color = color,
        modifier = probes.text(id),
        onTextLayout = probes.onTextLayout(id),
    )
}

@Composable
private fun DraggedListItem(probes: ProbeRegistry, enabled: Boolean) {
    val interactionSource = remember { MutableInteractionSource() }
    // AndroidX alpha25 ListTokens maps a dragged row to CornerLarge (16dp) and Elevation.Level4
    // (8dp). The alpha22 executable implementation has the same external DragInteraction path,
    // so record the renderer-level contract while the scene verifies its pixels.
    probes.recordShadow(
        "component",
        ShadowProbeSpec(
            shapeKind = "roundedRectangle",
            cornerRadiusDp = 16f,
            elevationDp = 8f,
        ),
    )
    ListItem(
        onClick = {},
        enabled = enabled,
        modifier = listItemModifier(probes, 56.dp),
        interactionSource = interactionSource,
        // onTextLayout is not reinvoked when only the inherited content color changes. Use the
        // source-resolved terminal color explicitly so the structural paint probe describes the
        // same settled frame as the screenshot: ReorderListTokens.ItemLabelTextColor.
        content = {
            ListItemText(
                "Reorder item",
                "headline",
                probes,
                // ListItemColors resolves disabled before dragged. Its disabled color is already
                // present at the first composition, while only the enabled dragged target needs
                // an explicit terminal probe color.
                color = if (enabled) MaterialTheme.colorScheme.onTertiaryContainer else Color.Unspecified,
            )
        },
    )

    // MutableInteractionSource has no replay, and Compose does not expose a subscription-ready
    // signal for its internal collector. The parent effect yields so the child collector starts
    // and suspends on the shared flow before this single Start is emitted. Unlike retrying Start
    // on later render frames, that preserves the final state layer instead of restarting its fade.
    LaunchedEffect(interactionSource) {
        yield()
        interactionSource.emit(DragInteraction.Start())
    }
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
        RequiredNodeSpec(id = "tab-2", kind = "container", parentId = "component"),
        RequiredNodeSpec(id = "tab-3", kind = "container", parentId = "component"),
        RequiredNodeSpec(id = "label-2", kind = "text", parentId = "component", text = "Reviews"),
        RequiredNodeSpec(id = "label-3", kind = "text", parentId = "component", text = "Related items"),
    ),
)

private fun scrollableTabWideCase(id: String) = CaseContract(
    id = id, viewportWidthDp = 600, viewportHeightDp = 80, dark = false,
    requiredNodes = listOf(
        RequiredNodeSpec(id = "component", kind = "container"),
        RequiredNodeSpec(id = "indicator", kind = "container", parentId = "component"),
        *scrollableTabNodes().toTypedArray(),
        RequiredNodeSpec(id = "label-0", kind = "text", parentId = "component", text = "Overview"),
        RequiredNodeSpec(id = "label-1", kind = "text", parentId = "component", text = "Specifications"),
        RequiredNodeSpec(id = "label-2", kind = "text", parentId = "component", text = "Reviews"),
        RequiredNodeSpec(id = "label-3", kind = "text", parentId = "component", text = "Related items"),
    ),
)

private fun scrollableTabNodes() = List(4) { index ->
    RequiredNodeSpec(id = "tab-$index", kind = "container", parentId = "component")
}

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
private fun ScrollableTabs(probes: ProbeRegistry, width: Dp = 240.dp) {
    val selectedTabIndex = 3
    PrimaryScrollableTabRow(
        selectedTabIndex = selectedTabIndex,
        modifier = Modifier.requiredSize(width, 48.dp).then(probes.geometry("component")),
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
                modifier = probes.geometry("tab-$index"),
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
