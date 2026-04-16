# Minimal Albion DPS Meter

A lightweight, real-time DPS meter for **Albion Online** built with WinUI 3 and .NET 9. Captures network packets to track damage, healing, and taken damage across your party — no game modification required.

![Windows](https://img.shields.io/badge/platform-Windows-blue)
![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)
![WinUI 3](https://img.shields.io/badge/WinUI-3-green)

## Features

- **Real-time tracking** — Damage, DPS, Healing, HPS, and Taken Damage per player
- **Compact overlay** — Shrink to a small always-visible window while playing
- **Always on top** — Pin the meter above all windows
- **Weapon icons** — Displays each player's main-hand weapon from the Albion render API
- **Sort options** — Sort by Damage, DPS, Heal, HPS, Name, or Taken Damage
- **Copy to clipboard** — One-click copy of the damage summary
- **Auto-reset** — Optionally reset stats before each combat encounter
- **Mica backdrop** — Modern Windows 11 look with a translucent background
- **No game modification** — Works by reading network packets passively

## Requirements

- **Windows 10/11** (build 22621+)
- **.NET 9.0** runtime
- **Npcap** (recommended) or raw sockets for packet capture
  - Download Npcap from [npcap.com](https://npcap.com/#download)
  - Install with **"WinPcap API-compatible mode"** checked

## Getting Started

### Build from source

```bash
git clone https://github.com/akashi-sym/AlbionDpsMeter.git
cd AlbionDpsMeter
dotnet build
```

### Run

```bash
dotnet run --project AlbionDpsMeter
```

> **Note:** You may need to run as Administrator for raw socket packet capture. Npcap does not require elevation.

## How It Works

The meter passively captures UDP packets on the network interface used by Albion Online. It decodes Photon protocol messages to extract combat events (damage dealt, healing done, damage taken) and maps them to player entities in your party. No packets are modified or injected — the tool is read-only.

### Architecture

| Component | Description |
|---|---|
| **NetworkManager** | Captures packets via Npcap/Sockets and dispatches decoded Photon events |
| **TrackingController** | Orchestrates entity tracking and combat processing |
| **CombatController** | Aggregates per-player damage, heal, and taken damage stats |
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
