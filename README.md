# Minimal Albion DPS Meter

A lightweight, real-time DPS meter for **Albion Online** built with WinUI 3 and .NET 9. Captures network packets to track damage, healing, taken damage, fame, and silver across your party — no game modification required.

![Windows](https://img.shields.io/badge/platform-Windows-blue)
![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)
![WinUI 3](https://img.shields.io/badge/WinUI-3-green)

## Features

- **Real-time tracking** — Damage, DPS, Healing, HPS, and Taken Damage per player
- **Fame tracking** — Total fame gained per session (includes premium bonus, zone multiplier, and satchel)
- **Combat Fame tracking** — Respec/combat credits earned when your gear is already at max spec
- **Silver tracking** — Net silver gained per session after guild tax
- **Compact overlay** — Shrink to a small always-visible window while playing
- **Always on top** — Pin the meter above all windows
- **Weapon icons** — Displays each player's main-hand weapon from the Albion render API
- **Sort options** — Sort by Damage, DPS, Heal, HPS, Name, or Taken Damage
- **Copy to clipboard** — One-click copy of the damage summary
- **Mica backdrop** — Modern Windows 11 look with a translucent background
- **No game modification** — Works by reading network packets passively

## Requirements

- **Windows 10/11** (build 22621+)
- **.NET 9.0 Runtime** — [Download here](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- **Npcap** (recommended) or raw sockets for packet capture
  - Download from [npcap.com](https://npcap.com/#download)
  - During install, check **"WinPcap API-compatible mode"**

## Installation & Running

### Quick Start (Recommended)

1. Download **AlbionDpsMeter-v1.0.0.2.zip** from the [latest release](https://github.com/akashi-sym/Minimal-Albion-Online-DPS-Meter/releases/latest)
2. Extract the zip to any folder
3. Run **`AlbionDpsMeter.exe`** — it's right in the root of the extracted folder
4. Launch **Albion Online** and enter a zone
5. The meter starts tracking automatically once you join a party

> **Note:** If tracking doesn't start, right-click `AlbionDpsMeter.exe` → **Run as Administrator**. This is required when using raw sockets without Npcap installed.

---

## Usage Tips

### Joining / Leaving a Party

The meter tracks only players in your party. Keep these in mind:

- **Start the meter before you zone in** — it needs to see the `NewCharacter` packet when players load into the map to register them. If you open the meter mid-session, use the tip below.
- **If party members are missing from the list** — leave the party and rejoin. This forces a fresh `PartyJoined` event and re-registers everyone.
- **If you join a party after the meter is already open** — the meter will pick up the `PartyJoined` event automatically. No restart needed.
- **Party member shows 0 damage** — they may have been in the zone before you opened the meter. Have them leave and rejoin the party to re-register.

### Moving to a New Map

- **Stats are NOT automatically reset on map change** (unless you enable Auto-reset).
- When you travel to a new zone, the meter continues accumulating from the previous session.
- To start fresh, click the **Reset** button (↺ icon in the toolbar) before entering the new zone.
- Fame and Silver continue to accumulate across zones — they represent your full session total since the last reset.

### Controls

| Button | Action |
|---|---|
| ▶ / ⏸ | Start or pause tracking |
| ↺ | Reset all stats (damage, heal, fame, silver) |
| 📋 | Copy damage summary to clipboard |
| 📌 | Toggle always-on-top |
| ⧉ | Toggle compact overlay mode |

### Stats Explained

| Stat | Description |
|---|---|
| **DMG** | Total damage dealt by each party member |
| **HEAL** | Total healing done |
| **TAKEN** | Total damage received |
| **FAME** | Total fame earned this session (with all bonuses applied) |
| **C.FAME** | Combat credits — fame converted to respec points when gear is already max spec |
| **SILVER** | Net silver looted this session after guild tax |

---

## Build from Source

```bash
git clone https://github.com/akashi-sym/Minimal-Albion-Online-DPS-Meter.git
cd Minimal-Albion-Online-DPS-Meter
dotnet build -c Release
```

Requires .NET 9 SDK and Windows App SDK 1.7.

---

## How It Works

The meter passively captures UDP packets on the network interface used by Albion Online. It decodes Photon protocol messages to extract combat and economy events and maps them to player entities in your party. No packets are modified or injected — the tool is read-only.

### Architecture

| Component | Description |
|---|---|
| **NetworkManager** | Captures packets via Npcap/Sockets and dispatches decoded Photon events |
| **TrackingController** | Orchestrates entity tracking and combat processing |
| **CombatController** | Aggregates per-player damage, heal, taken damage and session fame/silver |
| **EntityController** | Manages known players, party membership, and local player identity |
| **ItemController** | Maps item indices to names via [ao-bin-dumps](https://github.com/ao-data/ao-bin-dumps) |
| **ImageController** | Fetches and caches weapon icons from the Albion render API |

## Tech Stack

- **WinUI 3** (Windows App SDK 1.7) — UI framework, pure code-behind (no XAML)
- **CommunityToolkit.Mvvm** — MVVM source generators
- **Serilog** — Structured logging to file
- **Libpcap** — Network packet capture
- **StatisticsAnalysisTool.Network** — Photon protocol parsing

## Credits

Made by **Akashi**

## License

This project is for personal/educational use. Albion Online is a registered trademark of Sandbox Interactive GmbH.
