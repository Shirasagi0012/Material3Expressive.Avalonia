// Fixed AndroidX SnapFlingBehavior.kt / PagerSnapLayoutInfoProvider.kt (Apache-2.0).
namespace Material3Expressive.Components;

/// <summary>Post-scroll behavior. Default selects NoSnap for Uncontained and SingleAdvance otherwise.</summary>
public enum CarouselFlingMode { Default, SingleAdvance, MultiBrowse, NoSnap }
