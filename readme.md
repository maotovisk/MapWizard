# MapWizard 2

**A modern tool for osu! beatmap editing**, designed to streamline your workflow with features like hitsound transfer, metadata management, auto combo color generation, and map cleaning.

> ✨ Rebuilt from the ground up in C# (.NET 9 + Avalonia) for speed, stability, and cross-platform support.

[![GitHub release](https://img.shields.io/github/v/release/maotovisk/MapWizard?style=flat-square)](https://github.com/maotovisk/MapWizard/releases)
![Platforms](https://img.shields.io/badge/platforms-Windows%20|%20Linux%20|%20macOS-blue?style=flat-square)
![Status](https://img.shields.io/badge/status-Map%20Cleaner%20in%20Progress-yellow?style=flat-square)

---

## 🎯 Features

- **Cross-platform** – First-class Linux support, works on Windows and macOS  
- **Hitsound Copier** – Quickly transfer hitsounds between difficulties  
- **Metadata Manager** – Edit and sync beatmap metadata with ease  
- **Auto Combo Colors** – Generate color schemes from background images  
- **Map Cleaner** – Remove unused files and tidy up your beatmaps  

---

## ⚙️ Installation

### 📦 Pre-built Binaries

- **Windows / Linux** – [Grab the latest release](https://github.com/maotovisk/MapWizard/releases)  
  *(Includes auto-update via [Velopack](https://velopack.io/))*  
- **Arch Linux** – Install via the AUR:  
  ```bash
  yay -S mapwizard-git
  ```

### 🔧 Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/maotovisk/MapWizard.git
   ```
2. Run the app:
   ```bash
   dotnet run --project MapWizard.Desktop
   ```

> Requires [.NET 9](https://dotnet.microsoft.com/) or later

---

## 🗺 Roadmap

| Status | Feature               |
|--------|-----------------------|
| ✅     | Beatmap parser         |
| ✅     | Hitsound copier        |
| ✅     | Metadata manager       |
| 🚧     | Map cleaner            |
| ⏳     | Combo color generator  |

---

## 🙌 Credits

With thanks to:

- [OliBomby's Mapping Tools](https://github.com/olibomby/mapping_tools) – for inspiration  
- The original [Map Wizard](https://github.com/maotovisk/map-wizard) (Tauri/Svelte version)  
- [osu! File Formats](https://osu.ppy.sh/help/wiki/osu!_File_Formats) – official documentation  
- [ppy/osu](https://github.com/ppy/osu) – for reference and structure  

---

## 🤝 Contribute

Contributions are welcome—feel free to open issues or submit pull requests!

---

<p align="center">
  <em>Happy mapping!</em><br/>
  <sub>Created with ❤️ by <a href="https://github.com/maotovisk">maotovisk</a></sub>
</p>
