namespace FpsGodPc.Core.Localization;

public sealed partial class LocalizationService
{
    public string TweakName(string id) => id switch
    {
        "win-game-mode" => T("Enable Game Mode", "เปิด Game Mode"),
        "win-power-high" => T("High Performance Power Plan", "Power Plan ประสิทธิภาพสูง"),
        "win-visual-fx" => T("Disable Visual Effects", "ปิด Visual Effects"),
        "win-game-dvr" => T("Disable Xbox Game Bar DVR", "ปิด Xbox Game Bar DVR"),
        "win-telemetry" => T("Disable Telemetry Services", "ปิดบริการ Telemetry"),
        "win-fullscreen-opt" => T("Disable Fullscreen Optimizations", "ปิด Fullscreen Optimizations"),
        "win-bg-apps" => T("Limit Background Apps", "จำกัดแอปพื้นหลัง"),
        "win-disable-power-saving" => T("Disable All Power Saving", "ปิดการประหยัดพลังงานทั้งหมด"),
        "win-priority-26" => T("Win32 Priority Separation (0x26)", "Win32 Priority Separation (0x26)"),
        "win-mmcss-latency" => T("MMCSS Gaming Profile", "โปรไฟล์ MMCSS สำหรับเกม"),
        "win-system-ini-fps" => T("system.ini Latency Profile", "โปรไฟล์ system.ini ลดหน่วง"),
        "gpu-shader-cache" => T("Clear DirectX Shader Cache", "ล้างแคช DirectX Shader"),
        "gpu-max-perf" => T("Prefer Maximum Performance", "ตั้งการ์ดจอประสิทธิภาพสูงสุด"),
        "gpu-low-latency" => T("Low Latency Mode", "โหมด Low Latency"),
        "gpu-hags-advisor" => ExpertGuideTitle("gpu-hags-advisor"),
        "gpu-power-limit" => T("GPU Power Limit", "จำกัดพลังงาน GPU"),
        "gpu-clock-offset" => T("GPU Clock Offset", "ปรับคล็อก GPU"),
        "cpu-game-priority" => T("Game Process High Priority", "ลำดับความสำคัญเกมสูง"),
        "cpu-core-parking" => T("Disable CPU Core Parking", "ปิด CPU Core Parking"),
        "cpu-timer-res" => T("Timer Resolution (Gaming)", "Timer Resolution (เกม)"),
        "cpu-undervolt" => ExpertGuideTitle("cpu-undervolt"),
        "cpu-power-limit" => T("CPU Power Limit (PL1/PL2)", "จำกัดพลังงาน CPU (PL1/PL2)"),
        "net-dns-flush" => T("Flush DNS Cache", "ล้างแคช DNS"),
        "net-adapter-power" => T("Disable Adapter Power Saving", "ปิดการประหยัดพลังงาน Adapter"),
        "net-nagle" => T("Disable Nagle's Algorithm", "ปิด Nagle's Algorithm"),
        "net-throttling" => T("Network Throttling Index", "ปรับ Network Throttling Index"),
        "adv-fan-curve" => T("Fan Curve Tuning", "ปรับเส้นโค้งพัดลม"),
        "adv-ram-standby" => T("Standby Memory Cleaner", "ล้าง Standby Memory"),
        "adv-vbs-warn" => ExpertGuideTitle("adv-vbs-warn"),
        "adv-vbs-disable" => T("Disable Memory Integrity", "ปิด Memory Integrity"),
        "adv-bios-xmp" => ExpertGuideTitle("adv-bios-xmp"),
        _ => id
    };

    public string TweakDescription(string id) => id switch
    {
        "win-game-mode" => T("Prioritizes game processes and reduces background interruptions.", "ให้ความสำคัญกับเกมและลดการรบกวนจากพื้นหลัง"),
        "win-power-high" => T("Switches to High Performance so CPU and GPU stay responsive.", "สลับเป็น High Performance ให้ CPU/GPU ตอบสนองเร็ว"),
        "win-visual-fx" => T("Turns off animations and transparency to free CPU/GPU cycles.", "ปิดแอนิเมชันและความโปร่งใสเพื่อลดภาระ CPU/GPU"),
        "win-game-dvr" => T("Stops background recording overhead that can hurt FPS.", "หยุดการบันทึกพื้นหลังที่กิน FPS"),
        "win-telemetry" => T("Reduces background data collection services.", "ลดบริการเก็บข้อมูลพื้นหลัง"),
        "win-fullscreen-opt" => T("Can lower input lag in some titles.", "อาจลด input lag ในบางเกม"),
        "win-bg-apps" => T("Prevents non-essential UWP apps from running in the background.", "ป้องกันแอป UWP ที่ไม่จำเป็นทำงานพื้นหลัง"),
        "win-disable-power-saving" => T("Max performance on active plan.", "ประสิทธิภาพสูงสุดบนแผนที่ใช้อยู่"),
        "win-priority-26" => T("Gaming CPU scheduler profile (0x26).", "โปรไฟล์ scheduler CPU สำหรับเกม (0x26)"),
        "win-mmcss-latency" => T("System Responsiveness 0, Games task high priority.", "System Responsiveness 0, งาน Games ลำดับสูง"),
        "win-system-ini-fps" => T("Applies [386Enh] time-slice latency tweaks.", "ใช้การปรับ time-slice ใน [386Enh]"),
        "gpu-shader-cache" => T("Removes stale shader cache that can cause stutter.", "ลบแคช shader เก่าที่ทำให้กระตุก"),
        "gpu-max-perf" => T("Sets NVIDIA/AMD power management to maximum performance.", "ตั้งการจัดการพลังงาน NVIDIA/AMD เป็นสูงสุด"),
        "gpu-low-latency" => T("Enables driver low-latency path where supported.", "เปิดเส้นทาง low-latency ของไดรเวอร์"),
        "gpu-hags-advisor" => ExpertGuideSummary("gpu-hags-advisor"),
        "gpu-power-limit" => T("Adjusts power limit via vendor SDK.", "ปรับลิมิตพลังงานผ่าน SDK ผู้ผลิต"),
        "gpu-clock-offset" => T("Core/memory offset tuning.", "ปรับ offset คอร์/เมมโมรี"),
        "cpu-game-priority" => T("Raises active game process priority while session runs.", "เพิ่มลำดับความสำคัญเกมขณะเล่น"),
        "cpu-core-parking" => T("Keeps all cores awake for lower latency.", "ให้ทุกคอร์ทำงานเพื่อลดหน่วง"),
        "cpu-timer-res" => T("Requests 0.5ms timer while gaming.", "ขอ timer 0.5ms ขณะเล่นเกม"),
        "cpu-undervolt" => ExpertGuideSummary("cpu-undervolt"),
        "cpu-power-limit" => T("Wraps vendor tooling for power limits.", "ใช้เครื่องมือผู้ผลิตสำหรับลิมิตพลังงาน"),
        "net-dns-flush" => T("Clears resolver cache.", "ล้างแคช DNS resolver"),
        "net-adapter-power" => T("Prevents NIC from sleeping.", "ป้องกันการ์ดเครือข่ายเข้าโหมด sleep"),
        "net-nagle" => T("May reduce latency in some online games.", "อาจลดหน่วงในเกมออนไลน์บางเกม"),
        "net-throttling" => T("Tunes Windows multimedia network throttling.", "ปรับ network throttling ของ Windows"),
        "adv-fan-curve" => T("Manual fan curve via vendor tools.", "ปรับเส้นโค้งพัดลมด้วยเครื่องมือผู้ผลิต"),
        "adv-ram-standby" => T("Flushes standby list on demand.", "ล้าง standby list ตามต้องการ"),
        "adv-vbs-warn" => ExpertGuideSummary("adv-vbs-warn"),
        "adv-vbs-disable" => T("Can improve FPS on some CPUs.", "อาจเพิ่ม FPS บาง CPU"),
        "adv-bios-xmp" => ExpertGuideSummary("adv-bios-xmp"),
        _ => string.Empty
    };

    public string LocalizeBoostDisplayName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return name switch
        {
            "Safe Boost" => BoostName("safe"),
            "Competitive Boost" => BoostName("competitive"),
            "Extreme Boost" => BoostName("extreme"),
            "Expert Guide" => BoostName("expert"),
            _ => name
        };
    }
}
