using FpsGodPc.Core.Models;

namespace FpsGodPc.Core.Tweaks;

public static class ExpertGuideCatalog
{
    public static IReadOnlyList<ExpertGuide> All { get; } =
    [
        new()
        {
            Id = "gpu-hags-advisor",
            Title = "HAGS Status Check",
            Risk = "Safe",
            Summary = "Hardware Accelerated GPU Scheduling can help or hurt FPS depending on your GPU and driver.",
            Steps =
            [
                new() { Text = "Open Settings → System → Display → Graphics → Default graphics settings." },
                new() { Text = "Find “Hardware-accelerated GPU scheduling” and note if it is On or Off." },
                new() { Text = "NVIDIA RTX 20-series and newer: try ON for lower latency in DX12 games." },
                new() { Text = "Restart the PC after changing HAGS, then run the same benchmark twice." },
                new() { Text = "Keep whichever setting gives better 1% lows — not just average FPS." },
            ],
        },
        new()
        {
            Id = "cpu-undervolt",
            Title = "CPU Undervolt Guide",
            Risk = "Medium",
            Summary = "Undervolting lowers heat and can improve boost clocks. Wrong values cause crashes.",
            Warning = "Do one step at a time. If games crash or you get BSOD, revert the last change immediately.",
            Steps =
            [
                new() { Text = "Download your motherboard vendor tool (Intel XTU, AMD Ryzen Master, or BIOS offset)." },
                new() { Text = "Run a 15-minute stress test at stock settings and note max temperature." },
                new() { Text = "Lower CPU core voltage offset by −5 mV (or one small step in BIOS)." },
                new() { Text = "Re-test the same game or Cinebench — watch for crashes or WHEA errors." },
                new() { Text = "Repeat small steps until stable, then stop. Do not chase maximum undervolt." },
                new() { Text = "Save a BIOS profile or export settings before closing the tool." },
            ],
        },
        new()
        {
            Id = "adv-vbs-warn",
            Title = "VBS / Core Isolation Check",
            Risk = "Safe",
            Summary = "Virtualization-based security can cost 5–15% FPS on some CPUs. Disabling reduces protection.",
            Steps =
            [
                new() { Text = "Open Windows Security → Device security → Core isolation details." },
                new() { Text = "Check if Memory integrity is On or Off." },
                new() { Text = "Run your main game with it ON and note average + 1% low FPS." },
                new() { Text = "If FPS is significantly lower, consider turning Memory integrity OFF." },
                new() { Text = "Only disable if you accept reduced security — not recommended on daily drivers." },
                new() { Text = "Reboot after any change and re-test the same scene for a fair comparison." },
            ],
        },
        new()
        {
            Id = "adv-bios-xmp",
            Title = "XMP / EXPO Advisor",
            Risk = "Medium",
            Summary = "RAM may run below its rated speed if XMP/EXPO is disabled in BIOS.",
            Warning = "Enabling XMP/EXPO is usually safe with QVL RAM, but unstable profiles can cause boot loops.",
            Steps =
            [
                new() { Text = "Open Task Manager → Performance → Memory and note the current speed (MHz)." },
                new() { Text = "Compare with the speed printed on your RAM stick label (e.g. 3200, 6000)." },
                new() { Text = "Reboot into BIOS (Del/F2) → find XMP (Intel) or EXPO (AMD) profile." },
                new() { Text = "Enable the rated profile, save, and boot back into Windows." },
                new() { Text = "Re-check Task Manager memory speed and run a quick stability test." },
            ],
        },
    ];
}
