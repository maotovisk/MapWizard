# MapWizard

Cross-platform osu! beatmap utility suite built with C#/.NET 10 and Avalonia.

[![GitHub release](https://img.shields.io/github/v/release/maotovisk/MapWizard?style=flat-square)](https://github.com/maotovisk/MapWizard/releases)
![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Linux%20%7C%20macOS-blue?style=flat-square)
![Framework](https://img.shields.io/badge/.NET-10.0-blueviolet?style=flat-square)
[![Repo](https://img.shields.io/badge/GitHub-maotovisk%2FMapWizard-black?style=flat-square)](https://github.com/maotovisk/MapWizard)

## Current Features

- HitSound Copier
- Metadata Manager
- Combo Colour Studio
- Map Picker
- Theme/settings management
- Update stream + updater integration

## Projects

- `MapWizard.Desktop`: Avalonia desktop app.
- `MapWizard.Tools`: core tooling logic.
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

Or:

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

## Config and Data Paths

Settings file: `MainSettings.ini`

- Windows: `%APPDATA%\MapWizard\MainSettings.ini`
- macOS: `~/Library/Application Support/MapWizard/MainSettings.ini`
- Linux: `$XDG_CONFIG_HOME/MapWizard/MainSettings.ini` (fallback: `~/.config/MapWizard/MainSettings.ini`)

Combo Colour Studio local projects:

- Windows: `%APPDATA%\MapWizard\ComboColourStudio\projects.json`
- macOS: `~/Library/Application Support/MapWizard/ComboColourStudio/projects.json`
- Linux: `$XDG_DATA_HOME/MapWizard/ComboColourStudio/projects.json` (fallback: `~/.local/share/MapWizard/ComboColourStudio/projects.json`)

## Credits

- [OliBomby's Mapping Tools](https://github.com/olibomby/mapping_tools) for inspiration.
- The original [Map Wizard](https://github.com/maotovisk/map-wizard) (Tauri/Svelte implementation).
- [osu! file format docs](https://osu.ppy.sh/help/wiki/osu!_File_Formats).
- [ppy/osu](https://github.com/ppy/osu) for reference.
- [OsuMemoryDataProvider](https://github.com/Piotrekol/ProcessMemoryDataFinder) (Windows memory reader dependency).
- [hwsmm/cosutrainer](https://github.com/hwsmm/cosutrainer) for Linux osu! memory reading reference.

## Contributing

Issues and pull requests are welcome.
