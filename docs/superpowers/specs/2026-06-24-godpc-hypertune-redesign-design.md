# FPS Optimize GOD PC — HyperTune Structural Redesign (Design Spec)

**Date:** 2026-06-24
**Status:** Approved direction, pending spec review
**Scope:** Visual/structural redesign of the WPF View layer of `FPS Optimize GOD PC`. Make the UX/UI **faithfully** resemble the **HyperTune** desktop app (CompReady), across the **whole app**.

---

## 1. Goal & Decisions

The previous pass copied HyperTune's **colors + fonts** only; the **structure/layout is unchanged**, so the app still reads as generic stacked-card WPF. This redesign changes the **structure**: app shell, navigation, page composition, and a library of HyperTune-style signature controls.

Decisions (confirmed with user):
- **Fidelity:** Faithful re-creation of HyperTune's structure (not merely "inspired").
- **Scope:** Whole app in one pass (executed foundation-first internally).
- **Priority component:** Radial gauges + OC knobs (most-wanted), plus top-bar shell, live graphs, Game Hub.
- **Assets:** Draw **all visuals as vector** in XAML (no bitmaps lifted from HyperTune). No IP risk.
- **Reference:** Use the installed HyperTune app + the decompiled artifacts (below) as the visual reference. `codex` (GPT-5.x) is used to author the visual XAML for the flagship pages (Dashboard, Optimize, Game Hub, Settings).

### Non-goals
- No changes to business logic, services, or `FpsGodPc.Core` behavior.
- No copying of HyperTune's proprietary bitmaps, hardware renders, game banners, or logo.
- No login/licensing/paywall system (HyperTune has one; we do not replicate it).
- No new app features beyond what existing pages already do — this is a re-skin/restructure, not new functionality.

---

## 2. Reference Material

- **Installed app:** `C:\Users\User\AppData\Local\Programs\CompReady\HyperTune` (WPF .NET 8, version 2.1.0.25).
- **Decompiled artifacts (scratchpad):** `…/scratchpad/hypertune-decompiled/`
  - `resources/fonts/interdisplay/*`, `resources/fonts/switzer/*` — fonts (Inter Display, Switzer; both free/OFL — OK to use).
  - `resources/images/**` — proprietary art (hardware renders, game banners, login art). **Reference only; do not ship.**
  - `*.baml` (87 files) — compiled XAML for every screen/control; names enumerate the full IA.

### HyperTune design DNA (extracted)
- **Canvas:** near-black `#0E1011`; surfaces layer up in charcoal (`#131516` → `#1A1B1D` → `#1D2021`).
- **Accent:** mint-green `#13F287`, used heavily including **green gradient/radial glows** (e.g., hero bloom, beams behind hardware).
- **Status:** red `#E1251B`/`#FF4555`, orange `#FF9F18`, yellow `#FFF44F`; minor accents lavender `#C5CBF9`, magenta/premium `#733CFF`.
- **Type:** **Inter Display** (display/headings/metrics) + **Switzer** (body), Cascadia Code (mono).
- **Shell:** **top navigation bar with tab buttons** (`ActiveTab`/`ActiveTabStyle`), brand left, notifications/tray right; large content area; **no left sidebar**.
- **Signature controls:** semicircular **radial gauge** (vector `DrawingImage` + rotating bar), **OC knob**, **temperature bar**, **live graph**, **gradient big button**, **feature toggle**.
- **Sections (from `resources/images/` + BAML):** dashboard, optimize (CPU/GPU/Memory/Network with skeuomorphic hardware visuals), gamehub, customize, cleaner, disktools, benchmark + leaderboard, restore, settings, help, welcome.

> The current `Themes/Colors.xaml` already matches this palette and exposes glow/gloss brushes (`HeroGlowBrush`, `AccentGlowBrush`, `CardSheenBrush`, `AccentGradientBrush`). We extend, not replace, these tokens.

---

## 3. Current App (baseline)

- **Stack:** WPF .NET 8, MVVM (CommunityToolkit.Mvvm). Projects: `FpsGodPc.App` (Views/Controls/Themes), `FpsGodPc.ViewModels`, `FpsGodPc.Core`.
- **Shell:** custom `WindowChrome` title bar (40px) + **left sidebar 220px** (`NavigationSections`) + content.
- **Pages (12):** Dashboard, Benchmark, Scanner, Tweaks, Boost, Profiles, Safety, Cleaner, Restore, Games, Network, Settings (`Themes/ViewTemplates.xaml`, `Views/*.xaml`).
- **Controls:** `Card`, `BoostCard`, `MetricTile`, `TweakRow` — all flat sheen-cards with drop shadows.
- **Gap:** flat, single-column stacked layouts; uniform cards; no gauges/graphs/hardware visuals/top-bar; weak visual hierarchy.

**Preserve:** all ViewModels and their bindings, commands, and navigation data. The redesign **rewires the Views** to those same ViewModels; where a new layout needs data the VM doesn't expose, prefer adding read-only VM members over changing logic.

---

## 4. Target Architecture

### 4.1 Shell (`MainWindow.xaml`)
Replace sidebar with a **top bar** (merges the custom title bar):

```
[●logo HYPERTUNE-style wordmark]   [Dashboard][Optimize][Game Hub][Benchmark][Cleaner][Restore][Settings] …   [🔔][status pill][— □ ✕]
─────────────────────────────────────────────────────────────────────────────────────────────────────────────
[optional sub-nav tabs for pages that have subpages: e.g. Optimize ▸ General · Selective · CPU OC · Memory · Network]
─────────────────────────────────────────────────────────────────────────────────────────────────────────────
   CONTENT (full width) with top HeroGlow bloom
```
- Top bar height ~56px, `SidebarBrush` (`#0B0D0E`) bg, bottom 1px `BorderBrush`. Keep `WindowChrome` (drag, min/max/close, 6px resize).
- **`TopBarTab`** control: pill/tab with `IsActive` → green text + 2px green underline/indicator + soft bg; hover = `SurfaceAltBrush`. Font Switzer 13 Medium.
- Right cluster: notifications button (reuse existing notification VM if any, else placeholder), a status/score pill, window buttons (close hover red `#CCC0392B`).
- **Sub-nav** row appears only for pages with subpages; bound to the page VM's sub-sections.
- Navigation: convert `NavigationSections` (sidebar groups) into top-bar tabs (flatten primary items to tabs; secondary items become sub-nav or overflow "More" menu). Keep the same page-switching mechanism/`Frame`/ContentControl + DataTemplates.

### 4.2 Design system additions (`Themes/`)
- **Type ramp** (add styles): `DisplayXL` 36 / `DisplayL` 27 / `DisplayM` 20 (Inter Display, Bold/SemiBold); `H1` 16 / `H2` 15 (Inter Display SemiBold); `Body` 13 (Switzer); `Caption` 12; `Label` 10–11 uppercase tracked (Switzer Bold, MutedBrush).
- **Glow/gloss:** reuse `HeroGlowBrush`, `AccentGlowBrush`, `CardSheenBrush`; add a green **beam** brush (vertical linear, transparent→`#1F4836`→transparent) for behind-hardware accents, and a soft outer **drop-glow** effect resource for active gauges (green `BlurEffect`/`DropShadowEffect`).
- **Radii/spacing:** keep card 14–15, control 9–12; standard gaps 8/12/16/20/28.

### 4.3 Component library (`Controls/`) — to build
Each is a reusable `UserControl`/templated control, fully vector, themed via tokens, bindable.

| Control | Purpose | Visual / key props |
|---|---|---|
| **RadialGauge** | CPU/GPU/RAM/temp value | Semicircular vector arc (track + value arc + rotating tick), center value + unit + label; `Value`, `Min`, `Max`, `Unit`, `Label`, `ArcBrush` (green/orange/red by threshold), green outer glow when high. |
| **OcKnob** | Overclock/offset dial | Rotary knob with tick ring, drag-to-rotate, numeric readout; `Value`, `Min`, `Max`, `Step`, `Suffix`. |
| **TempBar** | Temperature / linear meter | Horizontal/vertical gradient bar (green→orange→red) with marker; `Value`, `Max`, thresholds. |
| **LiveGraph** | FPS/temp/usage over time | Sparkline/area polyline with green fill gradient + grid; bind to an `ObservableCollection<double>` / ring buffer; smooth, lightweight (no heavy chart lib). |
| **GradientButton** | Primary CTA (Boost/Apply) | `AccentGradientBrush` fill, glossy, hover lift + glow; supports icon. |
| **FeatureToggle** | On/off feature row | HyperTune toggle: pill track, green "on" glow / red "off", label + description + risk badge. |
| **TopBarTab** | Primary nav tab | See 4.1. |
| **GameCard** | Game Hub tile | Banner area (vector/owned art) + game name + status + per-game "Optimize" CTA. |
| **HardwarePanel** | Optimize hub module | Container for a component (CPU/GPU/Memory/Network): vector hardware glyph + green beam + RadialGauge + key stats + FeatureToggles. |

Existing `Card`/`MetricTile`/`TweakRow`/`BoostCard` are refactored to sit inside the new layouts (or superseded by the new controls where appropriate).

### 4.4 Page redesigns (12)
- **Dashboard** *(flagship, codex)* — Hero header + glow; a row of **RadialGauges** (CPU/GPU/RAM, temps) + a **LiveGraph**; quick-action **GradientButton**s; compact system summary card. Multi-column grid, not a stack.
- **Optimize** *(flagship, codex)* — replaces **Boost + Tweaks**: a **hub** of **HardwarePanel**s (CPU / GPU / Memory / Network), each with hardware visual + gauge + FeatureToggles; "Selective features" section; sub-nav tabs. Boost presets become a prominent CTA strip.
- **Game Hub** *(flagship, codex)* — replaces **Games**: grid of **GameCard**s; selecting one opens a **per-game profile** subpage with that game's tweaks/profile.
- **Settings** *(flagship, codex)* — HyperTune-style settings with grouped sections, sub-nav, toggles, and tidy form rows (replaces the plain checkbox stack).
- **Benchmark** — run controls + **LiveGraph** results + **Leaderboard** sub-section.
- **Scanner / Safety / Cleaner / Restore / Network / Profiles** — re-laid out to HyperTune patterns (header + glow, multi-column cards, risk-color sub-nav, dense tables styled dark). These are authored by Claude/secondary agents using the same component library and the patterns established by the flagship pages.

---

## 5. Execution Plan (foundation-first, one pass)

Order (each builds on the prior so the whole app converges):
1. **Foundation** — type ramp + extra brushes/effects in `Themes/`; bundle/confirm Inter Display + Switzer fonts.
2. **Shell** — `MainWindow.xaml` top-bar + `TopBarTab` + sub-nav; migrate navigation off the sidebar.
3. **Component library** — RadialGauge, OcKnob, TempBar, LiveGraph, GradientButton, FeatureToggle, GameCard, HardwarePanel (with a small gallery/test harness page to verify visuals + build).
4. **Flagship pages via codex** — Dashboard, Optimize, Game Hub, Settings.
5. **Remaining pages** — Benchmark, Scanner, Safety, Cleaner, Restore, Network, Profiles.
6. **Integration & polish** — consistent spacing, motion/hover states, full `dotnet build`, run, visual parity check vs HyperTune.

### Tooling / multi-agent
- **Claude (orchestrator):** foundation, shell, component library, secondary pages, integration, build/run verification, coordinating codex.
- **codex (GPT-5.x):** authors the visual XAML for the 4 flagship pages against this spec + the live HyperTune reference. Runs in `workspace-write` within the project; output is reviewed and build-verified by Claude before acceptance.

---

## 6. Constraints & Verification

- **Platform:** WPF .NET 8; MVVM with CommunityToolkit; bindings to existing ViewModels preserved.
- **Build/run:** `dotnet build FpsGodPc.sln` must pass; app must launch and navigate all pages without binding errors.
- **No regressions:** every existing command/feature reachable; nav covers all 12 pages.
- **Fidelity check:** side-by-side against HyperTune — shell, palette, type, gauges, hub layout should read as the same family.
- **Performance:** LiveGraph/gauges must be lightweight (no per-frame allocations; cap update rate).
- **Assets:** 100% vector/owned; zero HyperTune bitmaps shipped.

---

## 7. Open Questions (none blocking)
- Exact set/order of top-bar tabs vs an overflow "More" menu (finalize during shell build against the live app).
- Whether to keep `Tweaks` as a sub-nav of `Optimize` or as its own tab (lean: sub-nav of Optimize, matching HyperTune).
