# MapWizard

osu! beatmap toolset for Windows, Linux, and macOS, built with C#/.NET 10 and Avalonia.

[![GitHub release](https://img.shields.io/github/v/release/maotovisk/MapWizard?style=flat-square)](https://github.com/maotovisk/MapWizard/releases)
![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Linux%20%7C%20macOS-blue?style=flat-square)
![Framework](https://img.shields.io/badge/.NET-10.0-blueviolet?style=flat-square)
[![Repo](https://img.shields.io/badge/GitHub-maotovisk%2FMapWizard-black?style=flat-square)](https://github.com/maotovisk/MapWizard)
[![Discord](https://img.shields.io/badge/Discord-join-5865F2?style=flat-square&logo=discord&logoColor=white)](https://mapwizard.maot.dev/discord)

## Current Tools

- HitSound Copier
- Metadata Manager
- Combo Colour Studio
- Map Picker
- HitSound Editor (beta)
- Map Cleaner

## Projects

- `MapWizard.Desktop`: Avalonia desktop app (main application).
- `MapWizard.Theme`: Custom Avalonia theme for the app.
- `MapWizard.Tools`: core tooling logic.
- `MapWizard.CLI`: soon-to-be command-line interface for the app.
- `MapWizard.Tests`: test suite.

## Requirements

- .NET SDK `10.0.0` or later (see `global.json`).
- Velopack CLI (`vpk`) for release packaging.

## Run from Source

```bash
git clone https://github.com/maotovisk/MapWizard.git
cd MapWizard
dotnet restore
dotnet build
dotnet run --project MapWizard.Desktop
```

Optional software rendering fallback:

```bash
dotnet run --project MapWizard.Desktop -- --software-rendering
```

Or set the environment variable:

```bash
MAPWIZARD_FORCE_SOFTWARE_RENDERING=1 dotnet run --project MapWizard.Desktop
```

## Tests

```bash
dotnet test MapWizard.Tests/MapWizard.Tests.csproj
```

## Release Builds

Packaging scripts are in `MapWizard.Desktop/`:

- `build-linux.sh`
- `build-osx.sh`
- `build-win.sh`
- `build-win.bat`

`MapWizard.Desktop/Assets/mapwizard.svg` is the source for the app logo and all
packaged icons. After changing it, regenerate the PNG, ICO, and ICNS files with
`python3 MapWizard.Desktop/Assets/generate-app-icons.py` (requires
`rsvg-convert` and Pillow) before building a release.

## Config and Data Paths

Settings file: `MainSettings.ini`

- Windows: `%APPDATA%\MapWizard\MainSettings.ini`
- macOS: `~/Library/Application Support/MapWizard/MainSettings.ini`
- Linux: `$XDG_CONFIG_HOME/MapWizard/MainSettings.ini` (fallback: `~/.config/MapWizard/MainSettings.ini`)

Combo Colour Studio local projects:

- Windows: `%APPDATA%\MapWizard\ComboColourStudio\projects.json`
- macOS: `~/Library/Application Support/MapWizard/ComboColourStudio/projects.json`
- Linux: `$XDG_DATA_HOME/MapWizard/ComboColourStudio/projects.json` (fallback: `~/.local/share/MapWizard/ComboColourStudio/projects.json`)

## Credits and Special Thanks

- [OliBomby's Mapping Tools](https://github.com/olibomby/mapping_tools) for inspiration.
- The original [Map Wizard](https://github.com/maotovisk/map-wizard) (Tauri/Svelte implementation).
- [osu! file format docs](https://osu.ppy.sh/help/wiki/osu!_File_Formats).
- [ppy/osu](https://github.com/ppy/osu) for reference.
- [OsuMemoryDataProvider](https://github.com/Piotrekol/ProcessMemoryDataFinder) (Windows memory reader dependency).
- [hwsmm/cosutrainer](https://github.com/hwsmm/cosutrainer) for Linux osu! memory reading reference.

## Contributing

Issues and pull requests are welcome. For questions, help, and updates, join the
[Discord](https://mapwizard.maot.dev/discord).
