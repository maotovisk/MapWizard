# MapWizard 2

**A powerful osu! beatmap editing tool** for transferring hitsounds between difficulties, managing metadata, auto-generating combo colors, and cleaning beatmaps.  

✨ *Rewritten in C# (.NET 9 + Avalonia) for better performance and cross-platform support*  

[![GitHub release](https://img.shields.io/github/v/release/maotovisk/MapWizard?style=flat-square)](https://github.com/maotovisk/MapWizard/releases)
![Platforms](https://img.shields.io/badge/platforms-Windows%20|%20Linux%20|%20macOS-blue?style=flat-square)
![Status](https://img.shields.io/badge/status-Working%20on%20Map%20Cleaner-yellow?style=flat-square)

---

## 🎯 Features & Goals

- **Cross-platform** (Linux-first, with Windows & macOS support)  
- **Hitsound Copier** - Transfer hitsounds between difficulties  
- **Metadata Manager** - Edit beatmap metadata efficiently  
- **Auto Combo Colors** - Generate colors from background images  
- **Map Cleaner** - Remove unused files and optimize beatmaps  

---

## ⚙️ Installation

### 📦 Pre-built Packages
- **Windows/Linux**: [Download from Releases](https://github.com/maotovisk/MapWizard/releases)  
  *(Auto-updates via [Velopack](https://velopack.io/))*  
- **Arch Linux**: Available via [AUR](https://aur.archlinux.org/packages/mapwizard-git)  
  ```bash
  yay -S mapwizard-git
  ```

### 🔧 Build from Source
1. Clone the repository:
   ```bash
   git clone https://github.com/maotovisk/MapWizard.git
   ```
2. Run the project:
   ```bash
   dotnet run --project MapWizard.Desktop
   ```

**Requirements**: .NET 9 or later

---

## 🗺️ Development Roadmap

| Status | Feature |
|--------|---------|
| ✅ | Basic beatmap parser |
| ✅ | Hitsound copier |
| ✅ | Metadata manager |
| 🚧 | Map cleaner |
| ⏳ | Combo color generator |

---

## 🙏 Acknowledgments

Special thanks to:
- [OliBomby's Mapping Tools](https://github.com/olibomby/mapping_tools) for inspiration  
- The original [Map Wizard](https://github.com/maotovisk/map-wizard) (Tauri/Svelte)  
- [osu! File Formats](https://osu.ppy.sh/help/wiki/osu!_File_Formats) documentation  
- [ppy/osu](https://github.com/ppy/osu) (Lazer reference implementation)  

---

## 🤝 Contributing

Contributions welcome! Feel free to open issues or submit PRs.  

---

*✨ Happy mapping!*  

<p align="center">
  <sub>Created with ❤️ by <a href="https://github.com/maotovisk">maotovisk</a></sub>
</p>
