using FpsGodPc.Core.Models;

namespace FpsGodPc.Core.Localization;

public sealed partial class LocalizationService
{
    private string _language = "en";

    public event Action? LanguageChanged;

    public string Language => _language;

    public void SetLanguage(string? language)
    {
        var next = string.Equals(language, "th", StringComparison.OrdinalIgnoreCase) ? "th" : "en";
        if (_language == next)
        {
            return;
        }

        _language = next;
        LanguageChanged?.Invoke();
    }

    public string T(string en, string th) => _language == "th" ? th : en;

    public string Nav(string key) => key switch
    {
        "dashboard" => T("Dashboard", "แดชบอร์ด"),
        "benchmark" => T("Benchmark", "เบนช์มาร์ก"),
        "scanner" => T("Scanner", "สแกนเนอร์"),
        "tweaks" => T("Tweaks", "ปรับแต่ง"),
        "boost" => T("Boost", "บูสต์"),
        "profiles" => T("Profiles", "โปรไฟล์เกม"),
        "safety" => T("Safety", "ความปลอดภัย"),
        "cleaner" => T("Cleaner", "ล้างระบบ"),
        "restore" => T("Rollback", "ย้อนกลับ"),
        "games" => T("Games", "เกม"),
        "network" => T("Network", "เครือข่าย"),
        "settings" => T("Settings", "ตั้งค่า"),
        _ => key
    };

    public string Section(string key) => key switch
    {
        "MONITOR" => T("MONITOR", "มอนิเตอร์"),
        "PERFORMANCE" => T("PERFORMANCE", "ประสิทธิภาพ"),
        "OPTIMIZE" => T("OPTIMIZE", "ปรับแต่ง"),
        "TOOLS" => T("TOOLS", "เครื่องมือ"),
        _ => key
    };

    public (string Title, string Subtitle) Page(string key) => key switch
    {
        "dashboard" => (T("Live Monitor", "มอนิเตอร์สด"), T("Real-time telemetry and optimization health.", "เทเลเมทรีเรียลไทม์และสุขภาพการปรับแต่ง")),
        "benchmark" => (T("Benchmark", "เบนช์มาร์ก"), T("Capture before/after performance snapshots.", "บันทึกผลประสิทธิภาพก่อน/หลัง")),
        "scanner" => (T("System Scanner", "สแกนระบบ"), T("Analyze optimization opportunities before applying tweaks.", "วิเคราะห์จุดปรับปรุงก่อนใช้ทวีค")),
        "tweaks" => (T("Tweaks", "ปรับแต่ง"), T("Fine-grained Windows, CPU, GPU, and network tuning.", "ปรับ Windows, CPU, GPU และเครือข่ายทีละรายการ")),
        "boost" => (T("Boost Presets", "ชุดบูสต์"), T("One-click bundles aligned with risk tolerance.", "ชุดปรับแต่งกดครั้งเดียวตามระดับความเสี่ยง")),
        "profiles" => (T("Profiles", "โปรไฟล์เกม"), T("Auto-apply tweak bundles for specific games.", "ใส่ทวีคอัตโนมัติตามเกม")),
        "safety" => (T("Safety", "ความปลอดภัย"), T("Snapshots, watchdog status, and guardian controls.", "Snapshot, watchdog และ Guardian")),
        "cleaner" => (T("Cleaner", "ล้างระบบ"), T("Remove stale files and caches that impact smoothness.", "ลบไฟล์และแคชที่ทำให้กระตุก")),
        "restore" => (T("Rollback Center", "ศูนย์ย้อนกลับ"), T("Create restore points and revert applied optimization sets.", "สร้างจุดคืนค่าและย้อนชุดปรับแต่ง")),
        "games" => (T("Games", "เกม"), T("Per-title launch settings, FPS caps, and profile hints.", "ตั้งค่าเปิดเกม, จำกัด FPS และคำแนะนำ")),
        "network" => (T("Network", "เครือข่าย"), T("Latency tools for DNS, adapter tuning, and ping tests.", "เครื่องมือลดหน่วง DNS, adapter และ ping")),
        "settings" => (T("Settings", "ตั้งค่า"), T("Application behavior, safety guardrails, and storage paths.", "พฤติกรรมแอป, ความปลอดภัย และที่เก็บข้อมูล")),
        _ => (key, string.Empty)
    };

    public string AdminBanner() =>
        T("Administrator permissions are required for some performance and safety actions.",
          "ต้องใช้สิทธิ์ Administrator สำหรับการปรับแต่ง registry และ power plan");

    public string RestartAsAdmin() => T("Restart as Administrator", "เปิดใหม่แบบ Administrator");

    public string WatchdogMessage() =>
        T("Previous session did not exit cleanly. Review Safety before applying extreme tweaks.",
          "เซสชันก่อนหน้าปิดไม่สมบูรณ์ — ตรวจหน้า Safety ก่อนใช้ทวีคระดับสูง");

    public string ConfirmTitle() => T("Confirm", "ยืนยัน");

    public string Category(string key) => key.ToLowerInvariant() switch
    {
        "windows" => T("Windows", "Windows"),
        "gpu" => T("GPU", "การ์ดจอ"),
        "cpu" => T("CPU", "ซีพียู"),
        "network" => T("Network", "เครือข่าย"),
        "advanced" => T("Advanced", "ขั้นสูง"),
        _ => key
    };

    public string Risk(RiskTier tier) => tier switch
    {
        RiskTier.Safe => T("Safe", "ปลอดภัย"),
        RiskTier.Moderate => T("Moderate", "ปานกลาง"),
        RiskTier.Advanced => T("Advanced", "ขั้นสูง"),
        _ => tier.ToString()
    };

    public string Enabled => T("Enabled", "เปิด");
    public string Disabled => T("Disabled", "ปิด");
    public string CreateSnapshot => T("Create Snapshot", "สร้าง Snapshot");
    public string RollbackAll => T("Rollback All", "ย้อนทั้งหมด");
    public string DismissWatchdog => T("Dismiss Watchdog", "ปิด Watchdog");
    public string SaveSettings => T("Save Settings", "บันทึกการตั้งค่า");

    public string BoostName(string id) => id switch
    {
        "safe" => T("Safe Boost", "บูสต์ปลอดภัย"),
        "competitive" => T("Competitive Boost", "บูสต์แข่งขัน"),
        "extreme" => T("Extreme Boost", "บูสต์สูงสุด"),
        "expert" => T("Expert Guide", "คู่มือ Expert"),
        _ => id
    };

    public string BoostDescription(string id) => id switch
    {
        "safe" => T("Safe baseline for daily gaming.", "พื้นฐานปลอดภัยสำหรับเล่นประจำ"),
        "competitive" => T("Latency-focused esports bundle.", "ชุดลดหน่วงสำหรับอีสปอร์ต"),
        "extreme" => T("Maximum performance with higher risk.", "ประสิทธิภาพสูงสุด ความเสี่ยงสูง"),
        "expert" => T("Advisor-only advanced tuning hints.", "คำแนะนำขั้นสูง (ที่ปรึกษา)"),
        _ => string.Empty
    };

    // Common grid headers
    public string GridId => T("ID", "รหัส");
    public string GridLabel => T("Label", "ชื่อ");
    public string GridTaken => T("Taken", "บันทึกเมื่อ");
    public string GridDuration => T("Duration", "ระยะเวลา");
    public string GridFps => T("FPS", "FPS");
    public string GridPct1Low => T("1% Low", "1% ต่ำ");
    public string GridCpuPct => T("CPU %", "CPU %");
    public string GridKnownGood => T("Known Good", "สถานะดี");
    public string GridSequence => T("#", "#");
    public string GridDescription => T("Description", "คำอธิบาย");
    public string GridCreated => T("Created", "สร้างเมื่อ");
    public string GridTweakId => T("Tweak", "ทวีค");
    public string GridAppliedAt => T("Applied At", "ใช้เมื่อ");

    // Dashboard
    public string DashboardPerformanceScore => T("Performance Score", "คะแนนประสิทธิภาพ");
    public string DashboardSafetyScore => T("Safety Score", "คะแนนความปลอดภัย");
    public string DashboardPowerPlan => T("Power Plan", "Power Plan");
    public string DashboardGameMode => T("Game Mode", "Game Mode");
    public string DashboardPerformanceDetail => T("Current active plan", "แผนพลังงานที่ใช้อยู่");
    public string DashboardSafetyDetail => T("Based on applied tweaks", "จากทวีคที่ใช้");
    public string DashboardPowerPlanDetail => T("Current active plan", "แผนพลังงานที่ใช้อยู่");
    public string DashboardGameModeDetail => T("Windows gaming state", "สถานะ Game Mode ของ Windows");
    public string DashboardLiveHardware => T("Live Hardware", "ฮาร์ดแวร์เรียลไทม์");
    public string DashboardSystemDetails => T("System Details", "รายละเอียดระบบ");
    public string MetricCpu => T("CPU", "CPU");
    public string MetricGpu => T("GPU", "GPU");
    public string MetricMemory => T("Memory", "หน่วยความจำ");
    public string MetricProcesses => T("Processes", "โปรเซส");
    public string MetricOperatingSystem => T("Operating System", "ระบบปฏิบัติการ");
    public string MetricStorage => T("Storage", "ที่เก็บข้อมูล");

    // Scanner
    public string ScannerRunScan => T("Run Scan", "เริ่มสแกน");
    public string ScannerFpsGain => T("FPS Gain", "FPS ที่ได้");
    public string ScannerLatencyGain => T("Latency Gain", "ลดหน่วง");
    public string ScannerStabilityRisk => T("Stability Risk", "ความเสี่ยงเสถียร");
    public string ScannerRecommended => T("Recommended", "แนะนำ");

    // Benchmark
    public string BenchmarkLabelField => T("Label", "ชื่อ");
    public string BenchmarkDuration => T("Duration (s)", "ระยะเวลา (วิ)");
    public string BenchmarkRun => T("Run Benchmark", "เริ่มเบนช์มาร์ก");
    public string BenchmarkDefaultLabel => T("Before tweaks", "ก่อนปรับแต่ง");
    public string BenchmarkSaved(string label, string source) =>
        T($"Captured benchmark \"{label}\" ({source}).", $"บันทึกเบนช์มาร์ก \"{label}\" ({source})");
    public string BenchmarkTelemetryNote => T(
        "FPS metrics require PresentMon. CPU/GPU averages captured.",
        "ต้องใช้ PresentMon สำหรับ FPS — บันทึกค่าเฉลี่ย CPU/GPU แทน");
    public string BenchmarkPresentMonStatus(string message) => LocalizePresentMonStatus(message);

    // Watcher / shell
    public string ShellLanguage => T("Language", "ภาษา");
    public string WatcherStatusActive(string profileId) =>
        T($"Watcher active: {profileId}", $"Watcher ทำงาน: {profileId}");
    public string WatcherStatusIdle => T("Watcher idle", "Watcher รอเกม");
    public string WatcherLastEvent(string evt) => T($"Last event: {evt}", $"เหตุการณ์ล่าสุด: {evt}");
    public string BootSafetyTitle => T("Safety Guardian", "Safety Guardian");

    // Onboarding
    public string OnboardingTitle => T("Safety Guardian", "Safety Guardian");
    public string OnboardingGotIt => T("Got it", "เข้าใจแล้ว");
    public IReadOnlyList<string> OnboardingBullets =>
    [
        T("Guardian tweaks create a SQLite snapshot before every apply.", "ทวีค Guardian สร้าง SQLite snapshot ก่อนทุกครั้ง"),
        T("Moderate/advanced tweaks start a confirm timer — keep or auto-revert.", "ทวีคระดับกลาง/สูงมีตัวจับเวลา — เก็บหรือย้อนอัตโนมัติ"),
        T("Run as Administrator for registry and power-plan tweaks.", "รันแบบ Administrator สำหรับ registry และ power plan"),
        T("Game watcher profiles auto-apply tweaks when a game launches.", "โปรไฟล์ Watcher ใส่ทวีคอัตโนมัติเมื่อเปิดเกม"),
    ];

    // Confirm timer
    public string ConfirmTimerTitle => T("Confirm stability", "ยืนยันความเสถียร");
    public string ConfirmTimerHint => T(
        "Moderate/advanced tweak applied. Keep if stable, or wait for auto-revert.",
        "ใช้ทวีคระดับกลาง/สูงแล้ว — เก็บถ้าเสถียร หรือจะย้อนอัตโนมัติ");
    public string ConfirmTimerCountdown(int seconds) => T($"{seconds}s remaining", $"เหลือ {seconds} วิ");

    // Games
    public string GamesApplyProfile => T("Apply profile tweaks", "ใช้ทวีคโปรไฟล์");
    public string GamesInstalled => T("Installed", "ติดตั้งแล้ว");
    public string GamesNotInstalled => T("Not detected", "ไม่พบติดตั้ง");

    // Benchmark compare
    public string BenchmarkCompareTitle => T("Latest comparison", "เปรียบเทียบล่าสุด");
    public string BenchmarkCompareFps(string before, string after, string delta) =>
        T($"FPS: {before} → {after} ({delta})", $"FPS: {before} → {after} ({delta})");
    public string BenchmarkCompareNone => T("Run two benchmarks to compare.", "รันเบนช์มาร์ก 2 ครั้งเพื่อเปรียบเทียบ");

    // Cleaner
    public string CleanerTempFiles => T("Temporary files", "ไฟล์ชั่วคราว");
    public string CleanerShaderCache => T("Shader cache", "แคช Shader");
    public string CleanerDnsCache => T("DNS cache", "แคช DNS");
    public string CleanerRecycleBin => T("Recycle bin", "ถังขยะ");
    public string CleanerRun => T("Run Cleaner", "เริ่มล้างระบบ");
    public string CleanerNoOptionsSelected() => T("No cleaner options selected.", "ไม่ได้เลือกตัวเลือกการล้าง");
    public string CleanerSuccess(double freedMb, int itemCount) =>
        T($"Cleaned {itemCount} item(s). Freed ~{freedMb:F1} MB.", $"ล้าง {itemCount} รายการ ปล่อยพื้นที่ ~{freedMb:F1} MB");

    // Network
    public string NetworkFlushDns => T("Flush DNS", "ล้าง DNS");
    public string NetworkTuneAdapter => T("Tune Adapter", "ปรับ Adapter");
    public string NetworkPingTest => T("Ping Test", "ทดสอบ Ping");
    public string NetworkHostPrefix => T("Host:", "โฮสต์:");
    public string NetworkLatencyPrefix => T("Latency:", "หน่วง:");
    public string NetworkPacketLossPrefix => T("Packet loss:", "สูญหาย:");
    public string NetworkNoTestYet => T("No test yet.", "ยังไม่ได้ทดสอบ");

    // Restore
    public string RestoreCreatePoint => T("Create Windows Restore Point", "สร้างจุดคืนค่า Windows");
    public string RestoreRollbackAll => T("Rollback All Tweaks", "ย้อนทวีคทั้งหมด");
    public string RestorePointsSection => T("Windows Restore Points", "จุดคืนค่า Windows");
    public string RestoreAppliedSection => T("Applied Tweaks", "ทวีคที่ใช้อยู่");
    public string RestoreLastBoostPrefix => T("Last boost:", "บูสต์ล่าสุด:");
    public string RestoreManualPointDescription => T("FPS Optimize GOD PC manual restore point", "จุดคืนค่าด้วยตนเอง FPS Optimize GOD PC");
    public string RestorePointCreated() => T("Restore point created.", "สร้างจุดคืนค่าแล้ว");
    public string RestorePointFailed(string detail) =>
        T($"Could not create restore point: {detail}", $"สร้างจุดคืนค่าไม่ได้: {detail}");
    public string RestoreRollbackConfirm => T("Rollback all active tweaks?", "ย้อนทวีคที่ใช้อยู่ทั้งหมดไหม?");

    // Safety
    public string SafetyGuardianTweaks => T("Guardian Tweaks", "ทวีค Guardian");
    public string SafetySnapshotsSection => T("Safety Snapshots", "Snapshot ความปลอดภัย");

    // Settings
    public string SettingsLanguage => T("Language", "ภาษา");
    public string SettingsCreateRestoreBeforeBoost => T("Create restore point before boost", "สร้างจุดคืนค่าก่อนบูสต์");
    public string SettingsConfirmExtremeTweaks => T("Confirm extreme tweaks", "ยืนยันทวีคระดับสูง");
    public string SettingsWatcherEnabled => T("Watcher enabled", "เปิด Watcher");
    public string SettingsBootAutoRevert => T("Boot auto-revert", "ย้อนอัตโนมัติเมื่อบูต");
    public string SettingsConfirmTimer => T("Confirm timer (sec)", "ตัวจับเวลายืนยัน (วิ)");
    public string SettingsAppDataPrefix => T("App data:", "ข้อมูลแอป:");
    public string SettingsGuardianDataPrefix => T("Guardian data:", "ข้อมูล Guardian:");
    public string SettingsVersionLabel => T("Version", "เวอร์ชัน");
    public string AppVersion => T("FPS Optimize GOD PC v0.1.1", "FPS Optimize GOD PC v0.1.1");

    // Profiles
    public string ProfilesApply => T("Apply", "ใช้");
    public string ProfilesWatcher => T("Watcher", "Watcher");

    // Boost
    public string BoostTweakCountFormat => T("{0} tweaks", "{0} ทวีค");
    public string BoostApplyPreset => T("Apply Preset", "ใช้ชุดบูสต์");
    public string BoostViewChecklist => T("View Checklist", "ดู Checklist");
    public string BoostExtremeWarning(string name) => T($"Warning: {name} applies high-risk tweaks.", $"คำเตือน: {name} ใช้ทวีคความเสี่ยงสูง");

    // Expert guide
    public string ExpertChecklistTitle => T("Expert Guide Checklist", "Checklist คู่มือ Expert");
    public string ExpertWaiveRisk => T("I accept the risk", "ยอมรับความเสี่ยง");
    public string ExpertMarkComplete => T("Mark guides complete", "ทำเครื่องหมายว่าทำครบ");
    public string ExpertClose => T("Close", "ปิด");
    public string ExpertRiskWaived => T("Expert risk waiver saved.", "บันทึกการยอมรับความเสี่ยงแล้ว");
    public string AdvisorOnlyHint => T("Advisor-only — follow the Expert Guide on the Boost page.", "ที่ปรึกษาเท่านั้น — ดูคู่มือ Expert ในหน้า Boost");
    public string AdminRequiredForTweak => T("Administrator rights required for this tweak.", "ทวีคนี้ต้องใช้สิทธิ์ Administrator");
    public string ConfirmEnableTweak(string name, string desc, string risk) =>
        T($"Enable {name}?\n\n{desc}\n\nRisk: {risk}", $"เปิด {name}?\n\n{desc}\n\nความเสี่ยง: {risk}");

    // Pending revert
    public string PendingRevertKeep => T("Keep tweaks", "เก็บทวีค");
    public string PendingRevertRevert => T("Revert now", "ย้อนทันที");
    public string PendingRevertBanner(string reason) =>
        T($"Stability check: {LocalizePendingReason(reason)}", $"ตรวจความเสถียร: {LocalizePendingReason(reason)}");

    public string ExpertGuideTitle(string id) => id switch
    {
        "gpu-hags-advisor" => T("HAGS Status Check", "ตรวจสถานะ HAGS"),
        "cpu-undervolt" => T("CPU Undervolt Guide", "คู่มือ Undervolt CPU"),
        "adv-vbs-warn" => T("VBS / Core Isolation Check", "ตรวจ VBS / Core Isolation"),
        "adv-bios-xmp" => T("XMP / EXPO Advisor", "คำแนะนำ XMP / EXPO"),
        _ => id
    };

    public string ExpertGuideSummary(string id) => id switch
    {
        "gpu-hags-advisor" => T(
            "Hardware Accelerated GPU Scheduling can help or hurt FPS depending on your GPU and driver.",
            "HAGS อาจช่วยหรือทำให้ FPS แย่ลง ขึ้นกับ GPU และไดรเวอร์"),
        "cpu-undervolt" => T(
            "Undervolting lowers heat and can improve boost clocks. Wrong values cause crashes.",
            "Undervolt ลดความร้อนและอาจเพิ่มคล็อก แต่ค่าผิดทำให้แครช"),
        "adv-vbs-warn" => T(
            "Virtualization-based security can cost 5–15% FPS on some CPUs. Disabling reduces protection.",
            "VBS อาจลด FPS 5–15% บาง CPU การปิดลดความปลอดภัย"),
        "adv-bios-xmp" => T(
            "RAM may run below its rated speed if XMP/EXPO is disabled in BIOS.",
            "RAM อาจทำงานต่ำกว่าที่ระบุถ้าไม่เปิด XMP/EXPO ใน BIOS"),
        _ => string.Empty
    };

    public string GamesFpsCapPrefix => T("FPS Cap:", "จำกัด FPS:");
    public string GamesPriorityPrefix => T("Priority:", "ลำดับความสำคัญ:");
    public string GamesLaunchOptionsPrefix => T("Launch Options:", "ตัวเลือกเปิดเกม:");
    public string GamesSelectHint => T("Select a game to view details.", "เลือกเกมเพื่อดูรายละเอียด");

    // Guardian tweak names
    public string GuardianTweakName(string id) => id switch
    {
        "safe-game-mode" => T("Enable Game Mode", "เปิด Game Mode"),
        "safe-power-high" => T("High Performance Power Plan", "Power Plan ประสิทธิภาพสูง"),
        "safe-visual-fx" => T("Best Performance Visual Effects", "Visual Effects ประสิทธิภาพสูงสุด"),
        "safe-standby-clean" => T("Flush Standby Memory", "ล้าง Standby Memory"),
        "safe-game-dvr-off" => T("Disable Game Bar / DVR", "ปิด Game Bar / DVR"),
        "safe-fso-hint" => T("Fullscreen Optimizations Hint", "คำแนะนำ Fullscreen Optimizations"),
        "safe-core-parking" => T("Disable Core Parking", "ปิด Core Parking"),
        "safe-bg-trim" => T("Trim Background Apps", "ลดแอปพื้นหลัง"),
        "mod-nagle-off" => T("Disable Nagle Algorithm", "ปิด Nagle Algorithm"),
        "mod-net-throttle-off" => T("Disable Network Throttling", "ปิด Network Throttling"),
        "mod-services-gaming" => T("Gaming Services Preset", "ชุดบริการสำหรับเกม"),
        "mod-timer-resolution" => T("1ms Timer Resolution", "Timer Resolution 1ms"),
        "adv-gpu-power" => T("GPU Monitoring (Advanced)", "ตรวจ GPU (ขั้นสูง)"),
        _ => id
    };

    // Scanner
    public string ScannerApplyRecommended => T("Apply recommended boost", "ใช้บูสต์ที่แนะนำ");
    public string ScanStabilityRisk(string risk) => risk switch
    {
        "High" => T("High", "สูง"),
        "Medium" => T("Medium", "ปานกลาง"),
        "Low" => T("Low", "ต่ำ"),
        _ => risk
    };

    public string ScanRecommendedMode(string presetId) => presetId switch
    {
        "safe" => BoostName("safe"),
        "competitive" => BoostName("competitive"),
        "extreme" => BoostName("extreme"),
        _ => presetId
    };

    public string ScanFpsGain(int score) => score >= 85
        ? T("+3-6%", "+3-6%")
        : score >= 70 ? T("+6-12%", "+6-12%") : T("+10-18%", "+10-18%");

    public string ScanLatencyGain(int score) => score >= 85
        ? T("-2-4 ms", "-2-4 ms")
        : score >= 70 ? T("-4-8 ms", "-4-8 ms") : T("-8-12 ms", "-8-12 ms");

    public string BootCrashWatchdog() =>
        T("Crash watchdog: previous session was dirty — rolled back pending tweaks.",
          "Crash watchdog: เซสชันก่อนหน้ามีปัญหา — ย้อนทวีคที่ค้างแล้ว");

    public string BootAutoRevert(string reason) =>
        T($"Boot auto-revert: {LocalizePendingReason(reason)}", $"ย้อนอัตโนมัติตอนบูต: {LocalizePendingReason(reason)}");

    public string SpecGateMessage(string message)
    {
        if (message.Contains("Safe tweak", StringComparison.OrdinalIgnoreCase))
        {
            return T("Safe tweak — no hardware limits changed.", "ทวีคปลอดภัย — ไม่เปลี่ยนขีดจำกัดฮาร์ดแวร์");
        }

        if (message.Contains("thermals are elevated", StringComparison.OrdinalIgnoreCase))
        {
            return T("Warning: thermals are elevated. Proceed only if stable.",
                "คำเตือน: อุณหภูมิสูง — ใช้ต่อเมื่อมั่นใจว่าเสถียร");
        }

        if (message.Contains("15s confirm timer", StringComparison.OrdinalIgnoreCase))
        {
            return T("Moderate tweak — snapshot created. 15s confirm timer will start.",
                "ทวีคปานกลาง — สร้าง snapshot แล้ว จะเริ่มตัวจับเวลา 15 วิ");
        }

        if (message.Contains("Blocked: GPU already hot", StringComparison.OrdinalIgnoreCase))
        {
            return T("Blocked: GPU already hot at stock. Applying limits risks crashes.",
                "บล็อก: GPU ร้อนอยู่แล้ว — ปรับลิมิตเสี่ยงแครช");
        }

        if (message.Contains("Advanced tweak", StringComparison.OrdinalIgnoreCase))
        {
            return T("Advanced tweak — 15s confirm timer. Snapshot saved.",
                "ทวีคขั้นสูง — ตัวจับเวลา 15 วิ Snapshot บันทึกแล้ว");
        }

        return message;
    }
}
