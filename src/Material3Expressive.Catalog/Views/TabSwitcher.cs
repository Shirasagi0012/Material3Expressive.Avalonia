using Avalonia.Controls;
using Material3Expressive.Controls;

namespace Material3Expressive.Catalog.Views;

/// <summary>
/// Shows exactly one panel per tab. The catalog's pages are static XAML, so the wiring is a
/// visibility toggle rather than a data-bound content host.
/// </summary>
internal static class TabSwitcher
{
    public static void Wire(MaterialTabs tabs, params Control[] panels)
    {
        void Update(int index)
        {
            for (var i = 0; i < panels.Length; i++)
                panels[i].IsVisible = i == index;
        }

        foreach (var tab in tabs.Items)
        {
            tab.IsCheckedChanged += (_, _) =>
            {
                if (tab.IsChecked == true)
                    Update(tabs.Items.IndexOf(tab));
            };
        }

        Update(tabs.SelectedIndex);
    }
}
