[English](README.md) · **ไทย**

# FPS Optimize GOD PC

**ตัวปรับแต่งเกมรวมทุกอย่าง** — แอป **WPF (.NET 8)** รวม FPS Unleashed + FPS Unlocker ในแอปเดียว

| | |
|---|---|
| **ชื่อผลิตภัณฑ์** | FPS Optimize GOD PC |
| **เวอร์ชัน** | 0.1.0 |
| **เทคโนโลยี** | WPF · .NET 8 · C# |
| **แพลตฟอร์ม** | Windows 10 / 11 (x64) |
| **โฟลเดอร์** | `FPS Optimize GOD PC/` |
| **ที่อยู่ใน workspace** | `Optimize PC/FPS Optimize GOD PC/` |

## ความต้องการของระบบ

- **Windows 10/11** (x64)
- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** — สำหรับ build จากซอร์ส
- **Administrator** — จำเป็นตอนรันแอป สำหรับ registry, power plan และทวีคระบบ

## จุดเด่น

### Performance (จาก Unleashed)
- ทวีค 31+ แบ่งหมวด Windows / GPU / CPU / Network / Advanced
- **Boost** — preset คลิกเดียว
- **Scanner** — สแกนหาจุดปรับปรุง
- Cleaner, Rollback, Games, Network

### Live Monitor + Safety (จาก Unlocker)
- Dashboard live CPU/GPU/RAM + **Game Watcher** (ใส่ทวีคอัตโนมัติเมื่อเปิดเกม)
- **PresentMon** — เบนช์มาร์ก FPS จริง (ถ้ามี `PresentMon.exe`)
- **มอนิเตอร์สด** — CPU/GPU %, อุณหภูมิ, RAM, จำนวน process
- **Performance Index** + **Safety Index** บน Dashboard
- **Spec Gate** ก่อนทวีกระดับกลาง/สูง
- **Snapshot SQLite** ก่อนทุกการเขียน Guardian
- ตัวจับเวลา 15 วินาที / ย้อนตอนบูต / Crash Watchdog
- **โปรไฟล์เกม + Watcher** — ใส่ทวีกเมื่อเปิดเกม
- **เบนช์มาร์ก** — PresentMon หรือ telemetry สำรอง
- **ภาษาไทย / English**
- **UI แบบ WPF** — ไฟล์ `.exe` เดียว ไม่ต้องติดตั้ง Node.js หรือ Rust

## ภาพหน้าจอ

> ตัวอย่าง — เพิ่มภาพใน `docs/screenshots/` แล้วลิงก์ที่นี่

| Dashboard | Tweaks | Safety |
|-----------|--------|--------|
| *(รอเพิ่มภาพ)* | *(รอเพิ่มภาพ)* | *(รอเพิ่มภาพ)* |

## ไฟล์แอป (.exe)

```
Optimize PC/FPS Optimize GOD PC/release/FPS Optimize GOD PC.exe
```

Build ใหม่: รัน `.\build.bat` ในโฟลเดอร์ `FPS Optimize GOD PC/`

## เริ่มใช้งาน (พัฒนา)

```powershell
cd "Optimize PC\FPS Optimize GOD PC"
dotnet build FpsGodPc.sln
dotnet run --project src\FpsGodPc.App\FpsGodPc.App.csproj
```

**พกพา:** `Run FPS Optimize GOD PC.bat` (ขอสิทธิ์ Administrator) หรือ `release\FPS Optimize GOD PC.exe`  
**Build release:** `.\build.bat` → `release\FPS Optimize GOD PC.exe` (single-file, self-contained win-x64)

> รันแบบ **Administrator** สำหรับ registry และ power plan

## โครงสร้าง solution

| โปรเจกต์ | หน้าที่ |
|----------|--------|
| `FpsGodPc.App` | WPF shell, views, themes |
| `FpsGodPc.ViewModels` | MVVM view models |
| `FpsGodPc.Services` | บริการ Windows/ระบบ |
| `FpsGodPc.Core` | โมเดล, logic ร่วม |

## เมนู

| กลุ่ม | หน้า |
|------|------|
| Monitor | Dashboard (Live), Benchmark |
| Performance | Scanner, Tweaks, Boost |
| Optimize | Profiles (Watcher), Safety |
| Tools | Cleaner, Rollback, Games, Network |

## PresentMon (ไม่บังคับ)

วาง `PresentMon.exe` ที่ `%LOCALAPPDATA%\fps-god-pc\`

## ข้อมูล

| เก็บอะไร | ที่อยู่ |
|----------|--------|
| ทวีคหลัก (Unleashed) | `%LOCALAPPDATA%\fps-god-pc\` |
| Guardian DB | `%LOCALAPPDATA%\fps-god-pc\guardian.db` |

## Tauri เวอร์ชันเก่า

สแต็ก Tauri 2 + React เก็บไว้ที่ `archive/tauri-legacy/` ใช้ WPF build ด้านบนสำหรับพัฒนาปัจจุบัน

## GitHub release

ดู [RELEASE.md](RELEASE.md) สำหรับ tag และอัปโหลด `FPS Optimize GOD PC.exe`

CI: `.github/workflows/build.yml` build อัตโนมัติเมื่อ push ไป `main`

ดู `CHANGELOG.md` และ `RELEASE.md` (รวม QA checklist)

## ลิขสิทธิ์

MIT — ดู [LICENSE](LICENSE)
