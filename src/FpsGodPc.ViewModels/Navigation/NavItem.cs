namespace FpsGodPc.App.Navigation;

public sealed record NavItem(string Key, string Label, string Section);

public static class NavigationCatalog
{
    public static IReadOnlyList<NavItem> Items { get; } =
    [
        new("dashboard", "Dashboard", "MONITOR"),
        new("benchmark", "Benchmark", "MONITOR"),
        new("scanner", "Scanner", "PERFORMANCE"),
        new("tweaks", "Tweaks", "PERFORMANCE"),
        new("boost", "Boost", "PERFORMANCE"),
        new("profiles", "Profiles", "OPTIMIZE"),
        new("safety", "Safety", "OPTIMIZE"),
        new("cleaner", "Cleaner", "TOOLS"),
        new("restore", "Rollback", "TOOLS"),
        new("games", "Games", "TOOLS"),
        new("network", "Network", "TOOLS"),
        new("settings", "Settings", "FOOTER"),
    ];
}
