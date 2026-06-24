using FpsGodPc.Core.Models;

namespace FpsGodPc.Core.Localization;

public sealed partial class LocalizationService
{
    public string ExpertGuideWarning(string id) => id switch
    {
        "cpu-undervolt" => T(
            "Do one step at a time. If games crash or you get BSOD, revert the last change immediately.",
            "ทำทีละขั้น ถ้าเกมแครชหรือ BSOD ให้ย้อนค่าล่าสุดทันที"),
        "adv-bios-xmp" => T(
            "Enabling XMP/EXPO is usually safe with QVL RAM, but unstable profiles can cause boot loops.",
            "เปิด XMP/EXPO มักปลอดภัยกับ RAM QVL แต่โปรไฟล์ไม่เสถียรอาจ boot loop"),
        _ => string.Empty
    };

    public string ExpertGuideRisk(string risk) => risk switch
    {
        "Safe" => Risk(RiskTier.Safe),
        "Medium" => T("Medium", "ปานกลาง"),
        _ => risk
    };

    public IReadOnlyList<string> ExpertGuideSteps(string id) => id switch
    {
        "gpu-hags-advisor" =>
        [
            T("Open Settings → System → Display → Graphics → Default graphics settings.", "เปิด Settings → System → Display → Graphics → Default graphics settings"),
            T("Find “Hardware-accelerated GPU scheduling” and note if it is On or Off.", "ดู “Hardware-accelerated GPU scheduling” ว่าเปิดหรือปิด"),
            T("NVIDIA RTX 20-series and newer: try ON for lower latency in DX12 games.", "NVIDIA RTX 20 ขึ้นไป: ลองเปิดเพื่อลดหน่วงในเกม DX12"),
            T("Restart the PC after changing HAGS, then run the same benchmark twice.", "รีสตาร์ทหลังเปลี่ยน HAGS แล้วรันเบนช์มาร์กเดิม 2 ครั้ง"),
            T("Keep whichever setting gives better 1% lows — not just average FPS.", "เลือกค่าที่ให้ 1% low ดีกว่า — ไม่ใช่แค่ FPS เฉลี่ย"),
        ],
        "cpu-undervolt" =>
        [
            T("Download your motherboard vendor tool (Intel XTU, AMD Ryzen Master, or BIOS offset).", "ดาวน์โหลดเครื่องมือผู้ผลิตเมนบอร์ด (Intel XTU, AMD Ryzen Master หรือ offset ใน BIOS)"),
            T("Run a 15-minute stress test at stock settings and note max temperature.", "รัน stress test 15 นาทีที่ค่า stock แล้วจดอุณหภูมิสูงสุด"),
            T("Lower CPU core voltage offset by −5 mV (or one small step in BIOS).", "ลด CPU core voltage offset ลง −5 mV (หรือทีละน้อยใน BIOS)"),
            T("Re-test the same game or Cinebench — watch for crashes or WHEA errors.", "ทดสอบเกมหรือ Cinebench เดิม — สังเกตแครชหรือ WHEA error"),
            T("Repeat small steps until stable, then stop. Do not chase maximum undervolt.", "ทำซ้ำทีละน้อยจนเสถียรแล้วหยุด อย่าไล่ undervolt สูงสุด"),
            T("Save a BIOS profile or export settings before closing the tool.", "บันทึกโปรไฟล์ BIOS หรือ export ก่อนปิดเครื่องมือ"),
        ],
        "adv-vbs-warn" =>
        [
            T("Open Windows Security → Device security → Core isolation details.", "เปิด Windows Security → Device security → Core isolation details"),
            T("Check if Memory integrity is On or Off.", "ดูว่า Memory integrity เปิดหรือปิด"),
            T("Run your main game with it ON and note average + 1% low FPS.", "เล่นเกมหลักแบบเปิด แล้วจด FPS เฉลี่ย + 1% low"),
            T("If FPS is significantly lower, consider turning Memory integrity OFF.", "ถ้า FPS ต่ำมาก อาจพิจารณาปิด Memory integrity"),
            T("Only disable if you accept reduced security — not recommended on daily drivers.", "ปิดเมื่อยอมรับความปลอดภัยลดลง — ไม่แนะนำเครื่องใช้ประจำ"),
            T("Reboot after any change and re-test the same scene for a fair comparison.", "รีบูตหลังเปลี่ยนแล้วทดสอบฉากเดิมเพื่อเปรียบเทียบ"),
        ],
        "adv-bios-xmp" =>
        [
            T("Open Task Manager → Performance → Memory and note the current speed (MHz).", "เปิด Task Manager → Performance → Memory แล้วดูความเร็ว (MHz)"),
            T("Compare with the speed printed on your RAM stick label (e.g. 3200, 6000).", "เทียบกับความเร็วบนสติกเกอร์ RAM (เช่น 3200, 6000)"),
            T("Reboot into BIOS (Del/F2) → find XMP (Intel) or EXPO (AMD) profile.", "รีบูตเข้า BIOS (Del/F2) → หาโปรไฟล์ XMP (Intel) หรือ EXPO (AMD)"),
            T("Enable the rated profile, save, and boot back into Windows.", "เปิดโปรไฟล์ตามที่ระบุ บันทึก แล้วบูตกลับ Windows"),
            T("Re-check Task Manager memory speed and run a quick stability test.", "ตรวจความเร็ว RAM ใน Task Manager อีกครั้งและทดสอบความเสถียรสั้นๆ"),
        ],
        _ => []
    };
}
