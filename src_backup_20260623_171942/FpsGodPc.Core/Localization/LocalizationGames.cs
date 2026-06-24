namespace FpsGodPc.Core.Localization;

public sealed partial class LocalizationService
{
    public string ProfileName(string id) => id switch
    {
        "cs2" => T("Counter-Strike 2", "Counter-Strike 2"),
        "valorant" => T("VALORANT", "VALORANT"),
        _ => id
    };

    public string GamePriority(string? priority) => priority switch
    {
        "High" => T("High", "สูง"),
        "Normal" => T("Normal", "ปกติ"),
        _ => priority ?? "—"
    };

    public string GameNote(string noteKey) => noteKey switch
    {
        "cs2-overlays" => T(
            "Disable overlays for lower frametime variance.",
            "ปิด overlay เพื่อลดความแปรผันของ frametime"),
        "valorant-latency" => T(
            "Keep driver latency mode to low/ultra depending on GPU vendor.",
            "ตั้งโหมด latency ของไดรเวอร์เป็น low/ultra ตามผู้ผลิต GPU"),
        _ => noteKey
    };

    public IReadOnlyList<string> GameNotesForProfile(string profileId) => profileId switch
    {
        "cs2" => [GameNote("cs2-overlays")],
        "valorant" => [GameNote("valorant-latency")],
        _ => []
    };
}
