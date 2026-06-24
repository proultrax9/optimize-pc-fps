using System.Text.RegularExpressions;

namespace FpsGodPc.Core.Localization;

public sealed partial class LocalizationService
{
    private static readonly Regex SnapshotCreatedRegex = new(@"^Snapshot #(\d+) created\.$", RegexOptions.Compiled);
    private static readonly Regex SnapshotRestoredRegex = new(@"^Snapshot #(\d+) restored\.$", RegexOptions.Compiled);
    private static readonly Regex WatcherProfileRegex = new(@"^Watcher (enabled|disabled) for (.+)\.$", RegexOptions.Compiled);
    private static readonly Regex BoostAppliedRegex = new(@"^(.+) applied successfully \((\d+) tweaks\)\.$", RegexOptions.Compiled);
    private static readonly Regex BoostPartialRegex = new(@"^(.+): (\d+) applied, (\d+) failed\. Some tweaks need Administrator\.$", RegexOptions.Compiled);
    private static readonly Regex WatcherAppliedRegex = new(@"^Applied (\d+) tweaks for (.+)\.$", RegexOptions.Compiled);
    private static readonly Regex WatcherRevertedRegex = new(@"^Reverted tweaks for (.+)\.$", RegexOptions.Compiled);
    private static readonly Regex ProfileAppliedRegex = new(@"^Applied (\d+) tweaks for (.+)\.$", RegexOptions.Compiled);
    private static readonly Regex ProfileRevertedRegex = new(@"^Reverted tweaks for (.+)\.$", RegexOptions.Compiled);

    public string LocalizePendingReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return string.Empty;
        }

        return reason switch
        {
            "Moderate tweak - confirm stability after reboot" =>
                T("Moderate tweak — confirm stability after reboot", "ทวีคปานกลาง — ยืนยันความเสถียรหลังรีบูต"),
            "Advanced GPU tweak - confirm thermals and stability" =>
                T("Advanced GPU tweak — confirm thermals and stability", "ทวีค GPU ขั้นสูง — ตรวจอุณหภูมิและความเสถียร"),
            _ => reason
        };
    }

    public string LocalizePresentMonStatus(string message) => message switch
    {
        "PresentMon not found. Place PresentMon.exe in %LOCALAPPDATA%\\fps-god-pc\\ or PATH. Telemetry fallback will be used." =>
            T("PresentMon not found. Place PresentMon.exe in %LOCALAPPDATA%\\fps-god-pc\\ or PATH. Telemetry fallback will be used.",
              "ไม่พบ PresentMon — วาง PresentMon.exe ใน %LOCALAPPDATA%\\fps-god-pc\\ หรือ PATH จะใช้เทเลเมทรีแทน"),
        "PresentMon detected — FPS metrics available." =>
            T("PresentMon detected — FPS metrics available.", "พบ PresentMon — วัด FPS ได้"),
        _ => message
    };

    public string LocalizeWatcherEvent(string? message) => LocalizeResult(message ?? string.Empty);

    public string LocalizeResult(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        var exact = message switch
        {
            "Safety settings saved." => T("Safety settings saved.", "บันทึกการตั้งค่าความปลอดภัยแล้ว"),
            "Crash watchdog dismissed." => T("Crash watchdog dismissed.", "ปิด Crash watchdog แล้ว"),
            "Tweaks confirmed as stable - marked known-good." =>
                T("Tweaks confirmed as stable — marked known-good.", "ยืนยันทวีคเสถียร — ทำเครื่องหมาย known-good"),
            "Pending tweaks reverted." => T("Pending tweaks reverted.", "ย้อนทวีคที่ค้างแล้ว"),
            "Restored from snapshot." => T("Restored from snapshot.", "คืนค่าจาก snapshot แล้ว"),
            "No pending revert." => T("No pending revert.", "ไม่มีทวีคที่รอย้อน"),
            "All applied tweaks have been reverted." => T("All applied tweaks have been reverted.", "ย้อนทวีคที่ใช้อยู่ทั้งหมดแล้ว"),
            "All applied tweaks reverted." => T("All applied tweaks reverted.", "ย้อนทวีคทั้งหมดแล้ว"),
            "Advisor guide - no system changes applied." =>
                T("Advisor guide — no system changes applied.", "คู่มือที่ปรึกษา — ไม่มีการเปลี่ยนระบบ"),
            "Already applied." => T("Already applied.", "ใช้อยู่แล้ว"),
            "Advisor - nothing to revert." => T("Advisor — nothing to revert.", "ที่ปรึกษา — ไม่มีอะไรให้ย้อน"),
            "Tweak was not applied." => T("Tweak was not applied.", "ทวีคยังไม่ได้ใช้"),
            "Safe tweak - no hardware limits changed." =>
                T("Safe tweak — no hardware limits changed.", "ทวีคปลอดภัย — ไม่เปลี่ยนขีดจำกัดฮาร์ดแวร์"),
            "Game Mode enabled." => T("Game Mode enabled.", "เปิด Game Mode แล้ว"),
            "High Performance power plan activated." => T("High Performance power plan activated.", "เปิด Power Plan ประสิทธิภาพสูงแล้ว"),
            "Visual effects set to best performance." => T("Visual effects set to best performance.", "ตั้ง Visual Effects เป็นประสิทธิภาพสูงสุด"),
            "Xbox Game Bar DVR disabled." => T("Xbox Game Bar DVR disabled.", "ปิด Xbox Game Bar DVR แล้ว"),
            "DNS cache flushed." => T("DNS cache flushed.", "ล้างแคช DNS แล้ว"),
            "Game Mode reverted." => T("Game Mode reverted.", "ย้อน Game Mode แล้ว"),
            "Power plan restored to Balanced." => T("Power plan restored to Balanced.", "คืน Power Plan เป็น Balanced"),
            "Standby memory flush requested." => T("Standby memory flush requested.", "ขอล้าง Standby memory แล้ว"),
            "Game Bar / DVR disabled." => T("Game Bar / DVR disabled.", "ปิด Game Bar / DVR แล้ว"),
            "Fullscreen optimizations hint applied." => T("Fullscreen optimizations hint applied.", "ใส่คำแนะนำ Fullscreen optimizations แล้ว"),
            "Core parking disabled." => T("Core parking disabled.", "ปิด Core parking แล้ว"),
            "Background apps trimmed." => T("Background apps trimmed.", "ลดแอปพื้นหลังแล้ว"),
            "Nagle disabled. Reboot recommended." => T("Nagle disabled. Reboot recommended.", "ปิด Nagle แล้ว — แนะนำรีบูต"),
            "Network throttling disabled." => T("Network throttling disabled.", "ปิด Network throttling แล้ว"),
            "Gaming services preset enabled." => T("Gaming services preset enabled.", "เปิดชุดบริการสำหรับเกมแล้ว"),
            "Timer resolution requested for this session." => T("Timer resolution requested for this session.", "ขอ Timer resolution สำหรับเซสชันนี้"),
            "GPU metrics sampled." => T("GPU metrics sampled.", "บันทึกค่า GPU แล้ว"),
            "Power plan reverted." => T("Power plan reverted.", "ย้อน Power plan แล้ว"),
            "Tweak removed from active list." => T("Tweak removed from active list.", "ลบทวีคออกจากรายการที่ใช้แล้ว"),
            "Game Bar / DVR re-enabled." => T("Game Bar / DVR re-enabled.", "เปิด Game Bar / DVR อีกครั้ง"),
            "Background trim session ended." => T("Background trim session ended.", "จบการลดแอปพื้นหลัง"),
            "Nagle settings reverted." => T("Nagle settings reverted.", "ย้อนการตั้งค่า Nagle แล้ว"),
            "Network throttling restored." => T("Network throttling restored.", "คืน Network throttling แล้ว"),
            "Gaming services preset reverted." => T("Gaming services preset reverted.", "ย้อนชุดบริการเกมแล้ว"),
            "Timer resolution resets when app exits." => T("Timer resolution resets when app exits.", "Timer resolution รีเซ็ตเมื่อปิดแอป"),
            "GPU monitoring session cleared." => T("GPU monitoring session cleared.", "ล้างเซสชันตรวจ GPU แล้ว"),
            "Telemetry services disabled (requires admin)." => T("Telemetry services disabled (requires admin).", "ปิดบริการ Telemetry แล้ว (ต้องใช้ admin)"),
            "Fullscreen optimizations disabled globally." => T("Fullscreen optimizations disabled globally.", "ปิด Fullscreen optimizations ทั้งระบบ"),
            "Background apps limited." => T("Background apps limited.", "จำกัดแอปพื้นหลังแล้ว"),
            "DirectX / GPU shader cache cleared." => T("DirectX / GPU shader cache cleared.", "ล้างแคช DirectX / GPU shader แล้ว"),
            "CPU core parking disabled." => T("CPU core parking disabled.", "ปิด CPU core parking แล้ว"),
            "Network adapter power saving disabled." => T("Network adapter power saving disabled.", "ปิดการประหยัดพลังงาน Adapter แล้ว"),
            "Nagle's algorithm disabled on network interfaces." => T("Nagle's algorithm disabled on network interfaces.", "ปิด Nagle's algorithm บน network interfaces"),
            "Network throttling index tuned." => T("Network throttling index tuned.", "ปรับ Network throttling index แล้ว"),
            "Standby memory trimmed." => T("Standby memory trimmed.", "ล้าง Standby memory แล้ว"),
            _ => null
        };

        if (exact is not null)
        {
            return exact;
        }

        if (SnapshotCreatedRegex.Match(message) is { Success: true } created)
        {
            return T($"Snapshot #{created.Groups[1].Value} created.", $"สร้าง Snapshot #{created.Groups[1].Value} แล้ว");
        }

        if (SnapshotRestoredRegex.Match(message) is { Success: true } restored)
        {
            return T($"Snapshot #{restored.Groups[1].Value} restored.", $"คืนค่า Snapshot #{restored.Groups[1].Value} แล้ว");
        }

        if (WatcherProfileRegex.Match(message) is { Success: true } watcher)
        {
            var enabled = watcher.Groups[1].Value == "enabled";
            var id = watcher.Groups[2].Value;
            return enabled
                ? T($"Watcher enabled for {id}.", $"เปิด Watcher สำหรับ {id}")
                : T($"Watcher disabled for {id}.", $"ปิด Watcher สำหรับ {id}");
        }

        if (BoostAppliedRegex.Match(message) is { Success: true } boostOk)
        {
            var name = LocalizeBoostDisplayName(boostOk.Groups[1].Value);
            var count = boostOk.Groups[2].Value;
            return T($"{name} applied successfully ({count} tweaks).", $"ใช้ {name} สำเร็จ ({count} ทวีค)");
        }

        if (BoostPartialRegex.Match(message) is { Success: true } boostPartial)
        {
            var name = LocalizeBoostDisplayName(boostPartial.Groups[1].Value);
            return T(
                $"{name}: {boostPartial.Groups[2].Value} applied, {boostPartial.Groups[3].Value} failed. Some tweaks need Administrator.",
                $"{name}: ใช้ได้ {boostPartial.Groups[2].Value}, ล้มเหลว {boostPartial.Groups[3].Value} — บางทวีคต้องใช้ Administrator");
        }

        if (message.StartsWith("Unknown tweak:", StringComparison.OrdinalIgnoreCase))
        {
            return T(message, $"ไม่รู้จักทวีค: {message["Unknown tweak:".Length..].Trim()}");
        }

        if (message.StartsWith("Profile not found:", StringComparison.OrdinalIgnoreCase))
        {
            return T(message, $"ไม่พบโปรไฟล์: {message["Profile not found:".Length..].Trim()}");
        }

        if (WatcherAppliedRegex.Match(message) is { Success: true } watcherApplied)
        {
            var count = watcherApplied.Groups[1].Value;
            var name = watcherApplied.Groups[2].Value;
            return T($"Applied {count} tweaks for {name}.", $"ใช้ {count} ทวีคสำหรับ {name}");
        }

        if (WatcherRevertedRegex.Match(message) is { Success: true } watcherReverted)
        {
            var name = watcherReverted.Groups[1].Value;
            return T($"Reverted tweaks for {name}.", $"ย้อนทวีคสำหรับ {name}");
        }

        if (ProfileAppliedRegex.Match(message) is { Success: true } profileApplied)
        {
            var count = profileApplied.Groups[1].Value;
            var name = profileApplied.Groups[2].Value;
            return T($"Applied {count} tweaks for {name}.", $"ใช้ {count} ทวีคสำหรับ {name}");
        }

        if (ProfileRevertedRegex.Match(message) is { Success: true } profileReverted)
        {
            var name = profileReverted.Groups[1].Value;
            return T($"Reverted tweaks for {name}.", $"ย้อนทวีคสำหรับ {name}");
        }

        if (message.StartsWith("Partial rollback:", StringComparison.OrdinalIgnoreCase))
        {
            return T(message, $"ย้อนบางส่วน: {message["Partial rollback:".Length..].Trim()}");
        }

        return message;
    }
}
