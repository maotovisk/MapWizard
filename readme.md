# MapWizard

osu! beatmap toolset for Windows, Linux, and macOS, built with C#/.NET 10 and Avalonia.

[![GitHub release](https://img.shields.io/github/v/release/maotovisk/MapWizard?style=flat-square)](https://github.com/maotovisk/MapWizard/releases)
![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Linux%20%7C%20macOS-blue?style=flat-square)
![Framework](https://img.shields.io/badge/.NET-10.0-blueviolet?style=flat-square)
[![Repo](https://img.shields.io/badge/GitHub-maotovisk%2FMapWizard-black?style=flat-square)](https://github.com/maotovisk/MapWizard)
[![Discord](https://img.shields.io/badge/Discord-join-5865F2?style=flat-square&logo=discord&logoColor=white)](https://mapwizard.maot.dev/discord)

## Install

### Linux and macOS:

You can install MapWizard by running the following command:

```bash
curl -fsSL https://mapwizard.maot.dev/install | bash
```

or grab the [latest release](https://github.com/maotovisk/MapWizard/releases/latest) manually.

### Windows

You can install MapWizard by downloading and running the `.exe` installer from the
[latest release](https://github.com/maotovisk/MapWizard/releases/latest).

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
- NativeAOT toolchain for release builds:
  - Windows: Visual Studio 2022 with the "Desktop development with C++" workload.
  - Linux: `clang` and zlib headers (`sudo apt-get install clang zlib1g-dev` on Debian/Ubuntu).
  - macOS: Xcode command line tools.

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
