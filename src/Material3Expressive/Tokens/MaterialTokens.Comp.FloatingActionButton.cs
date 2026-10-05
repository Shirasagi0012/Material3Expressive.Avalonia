using Avalonia;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class ToggleFab
        {
            public static readonly MaterialTokenKey<double> InitialCornerRadius = new("md.comp.ToggleFab.InitialCornerRadius");
            public static readonly MaterialTokenKey<double> MediumInitialCornerRadius = new("md.comp.ToggleFab.MediumInitialCornerRadius");
            public static readonly MaterialTokenKey<double> LargeInitialCornerRadius = new("md.comp.ToggleFab.LargeInitialCornerRadius");
            public static readonly MaterialTokenKey<Color> InitialContainerColor = new("md.comp.ToggleFab.InitialContainerColor");
            public static readonly MaterialTokenKey<Color> FinalContainerColor = new("md.comp.ToggleFab.FinalContainerColor");
            public static readonly MaterialTokenKey<Color> InitialIconColor = new("md.comp.ToggleFab.InitialIconColor");
            public static readonly MaterialTokenKey<Color> FinalIconColor = new("md.comp.ToggleFab.FinalIconColor");
            public static readonly MaterialTokenKey<SpringSpec> CheckedMotion = new("md.comp.ToggleFab.CheckedMotion");
        }
        public static class ExtendedFab
        {
            public static readonly MaterialTokenKey<SpringSpec> WidthMotion = new("md.comp.ExtendedFab.WidthMotion");
            public static readonly MaterialTokenKey<SpringSpec> AlphaMotion = new("md.comp.ExtendedFab.AlphaMotion");
        }
        public static class Fab
        {
            public static readonly MaterialTokenKey<float> HiddenScale = new("md.comp.Fab.HiddenScale");
            public static readonly MaterialTokenKey<RelativePoint> VisibilityAlignment = new("md.comp.Fab.VisibilityAlignment");
            public static readonly MaterialTokenKey<SpringSpec> VisibilityScaleMotion = new("md.comp.Fab.VisibilityScaleMotion");
            public static readonly MaterialTokenKey<SpringSpec> VisibilityAlphaMotion = new("md.comp.Fab.VisibilityAlphaMotion");
        }
        public static class FabBaseline
        {
            public static readonly MaterialTokenKey<double> ContainerWidth = new("md.comp.FabBaseline.ContainerWidth");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.FabBaseline.ContainerHeight");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.FabBaseline.ContainerShape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.FabBaseline.IconSize");
        }
        public static class FabSmall
        {
            public static readonly MaterialTokenKey<double> ContainerWidth = new("md.comp.FabSmall.ContainerWidth");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.FabSmall.ContainerHeight");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.FabSmall.ContainerShape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.FabSmall.IconSize");
        }
        public static class FabMedium
        {
            public static readonly MaterialTokenKey<double> ContainerWidth = new("md.comp.FabMedium.ContainerWidth");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.FabMedium.ContainerHeight");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.FabMedium.ContainerShape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.FabMedium.IconSize");
        }
        public static class FabLarge
        {
            public static readonly MaterialTokenKey<double> ContentIconSize = new("md.comp.FabLarge.ContentIconSize");
            public static readonly MaterialTokenKey<double> ContainerWidth = new("md.comp.FabLarge.ContainerWidth");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.FabLarge.ContainerHeight");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.FabLarge.ContainerShape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.FabLarge.IconSize");
        }
        public static class FabPrimaryContainer
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.FabPrimaryContainer.ContainerColor");
            public static readonly MaterialTokenKey<double> ContainerElevation = new("md.comp.FabPrimaryContainer.ContainerElevation");
            public static readonly MaterialTokenKey<double> PressedContainerElevation = new("md.comp.FabPrimaryContainer.PressedContainerElevation");
            public static readonly MaterialTokenKey<double> FocusedContainerElevation = new("md.comp.FabPrimaryContainer.FocusedContainerElevation");
            public static readonly MaterialTokenKey<double> HoveredContainerElevation = new("md.comp.FabPrimaryContainer.HoveredContainerElevation");
        }
        public static class ExtendedFabPrimary
        {
            public static readonly MaterialTokenKey<double> MinimumWidth = new("md.comp.ExtendedFabPrimary.MinimumWidth");
            public static readonly MaterialTokenKey<double> IconLeadingSpace = new("md.comp.ExtendedFabPrimary.IconLeadingSpace");
            public static readonly MaterialTokenKey<SpringSpec> ExpandWidthMotion = new("md.comp.ExtendedFabPrimary.ExpandWidthMotion");
            public static readonly MaterialTokenKey<SpringSpec> CollapseWidthMotion = new("md.comp.ExtendedFabPrimary.CollapseWidthMotion");
            public static readonly MaterialTokenKey<SpringSpec> ExpandAlphaMotion = new("md.comp.ExtendedFabPrimary.ExpandAlphaMotion");
            public static readonly MaterialTokenKey<SpringSpec> CollapseAlphaMotion = new("md.comp.ExtendedFabPrimary.CollapseAlphaMotion");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.ExtendedFabPrimary.ContainerHeight");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.ExtendedFabPrimary.ContainerShape");
            public static readonly MaterialTokenKey<double> LeadingSpace = new("md.comp.ExtendedFabPrimary.LeadingSpace");
            public static readonly MaterialTokenKey<double> TrailingSpace = new("md.comp.ExtendedFabPrimary.TrailingSpace");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.ExtendedFabPrimary.IconLabelSpace");
            public static readonly MaterialTokenKey<TextStyle> LabelTextStyle = new("md.comp.ExtendedFabPrimary.LabelTextStyle");
        }
        public static class ExtendedFabSmall
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.ExtendedFabSmall.ContainerHeight");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.ExtendedFabSmall.ContainerShape");
            public static readonly MaterialTokenKey<double> LeadingSpace = new("md.comp.ExtendedFabSmall.LeadingSpace");
            public static readonly MaterialTokenKey<double> TrailingSpace = new("md.comp.ExtendedFabSmall.TrailingSpace");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.ExtendedFabSmall.IconLabelSpace");
            public static readonly MaterialTokenKey<TextStyle> LabelTextStyle = new("md.comp.ExtendedFabSmall.LabelTextStyle");
        }
        public static class ExtendedFabMedium
        {
            public static readonly MaterialTokenKey<double> ContentIconLabelSpace = new("md.comp.ExtendedFabMedium.ContentIconLabelSpace");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.ExtendedFabMedium.ContainerHeight");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.ExtendedFabMedium.ContainerShape");
            public static readonly MaterialTokenKey<double> LeadingSpace = new("md.comp.ExtendedFabMedium.LeadingSpace");
            public static readonly MaterialTokenKey<double> TrailingSpace = new("md.comp.ExtendedFabMedium.TrailingSpace");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.ExtendedFabMedium.IconLabelSpace");
            public static readonly MaterialTokenKey<TextStyle> LabelTextStyle = new("md.comp.ExtendedFabMedium.LabelTextStyle");
        }
        public static class ExtendedFabLarge
        {
            public static readonly MaterialTokenKey<double> ContentIconLabelSpace = new("md.comp.ExtendedFabLarge.ContentIconLabelSpace");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.ExtendedFabLarge.ContainerHeight");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.ExtendedFabLarge.ContainerShape");
            public static readonly MaterialTokenKey<double> LeadingSpace = new("md.comp.ExtendedFabLarge.LeadingSpace");
            public static readonly MaterialTokenKey<double> TrailingSpace = new("md.comp.ExtendedFabLarge.TrailingSpace");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.ExtendedFabLarge.IconLabelSpace");
            public static readonly MaterialTokenKey<TextStyle> LabelTextStyle = new("md.comp.ExtendedFabLarge.LabelTextStyle");
        }
        public static class FabMenu
        {
            public static readonly MaterialTokenKey<double> ItemIconSize = new("md.comp.FabMenu.ItemIconSize");
            public static readonly MaterialTokenKey<TextStyle> LabelTextStyle = new("md.comp.FabMenu.LabelTextStyle");
            public static readonly MaterialTokenKey<Color> ItemContainerColor = new("md.comp.FabMenu.ItemContainerColor");
            public static readonly MaterialTokenKey<FloatingActionButtonMenuAlignment> MenuAlignment = new("md.comp.FabMenu.MenuAlignment");
            public static readonly MaterialTokenKey<SpringSpec> ItemWidthMotion = new("md.comp.FabMenu.ItemWidthMotion");
            public static readonly MaterialTokenKey<SpringSpec> ItemAlphaMotion = new("md.comp.FabMenu.ItemAlphaMotion");
            public static readonly MaterialTokenKey<SpringSpec> StaggerMotion = new("md.comp.FabMenu.StaggerMotion");
            public static readonly MaterialTokenKey<double> ItemHeight = new("md.comp.FabMenu.ItemHeight");
            public static readonly MaterialTokenKey<CornerRadius> ItemShape = new("md.comp.FabMenu.ItemShape");
            public static readonly MaterialTokenKey<double> ItemSpacing = new("md.comp.FabMenu.ItemSpacing");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.FabMenu.ContentPadding");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.FabMenu.IconLabelSpace");
            public static readonly MaterialTokenKey<double> ButtonBottomPadding = new("md.comp.FabMenu.ButtonBottomPadding");
            public static readonly MaterialTokenKey<double> ButtonBetweenSpace = new("md.comp.FabMenu.ButtonBetweenSpace");
            public static readonly MaterialTokenKey<double> HorizontalPadding = new("md.comp.FabMenu.HorizontalPadding");
            public static readonly MaterialTokenKey<double> CloseButtonSize = new("md.comp.FabMenu.CloseButtonSize");
            public static readonly MaterialTokenKey<double> CloseIconSize = new("md.comp.FabMenu.CloseIconSize");
        }
    }
}
