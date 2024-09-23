# MapWizard 2

A tool that aims to allow mappers replicate the hitsounds of a beatmap difficulty to other difficulties on the set, manage metadata and more. This is a rewrite of the original [Map Wizard](https://github.com/maotovisk/map-wizard), previously made in Tauri and Svelte, now being rewritten in .NET 8 and Avalonia.

<br/>

<p align="right"><b>Current Status</b>: First public release, working on feedback.</p>

## Project Goals

- Fully cross-platform;
- Replicate hitsounds from a difficulty to other beatmaps;
- Metadata manager funcionality;
- Map cleaner funcionality.

## Requirements

- .NET 8 or above

## Usage

1. Clone the repository

```bash
git clone https://github.com/maotovisk/MapWizard.git
```

2. Run the project

```bash
dotnet run --project MapWizard.Desktop
```

## Roadmap

- Implement the basic beatmap parser
- Implement the hitsound copier
- Implement the metadata manager
- Implement the map cleaner

## References

- [Mapping Tools](https://github.com/olibomby/mapping_tools) by [OliBomby](https://github.com/olibomby) for the whole idea of the project and such a great toolset for mapping.
- [Map Wizard](https://github.com/maotovisk/map-wizard) by [me](https://github.com/maotovisk) for the first implementation of the hitsound copier and metadata management stuff.
- [OsuParsers](https://github.com/mrflashstudio/OsuParsers) by [mrflashstudio](https://github.com/mrflashstudio) for being a very complete .osu file parser and very good reference for the new parser.
- [osu! File Formats](https://osu.ppy.sh/help/wiki/osu!_File_Formats) for the .osu file format documentation.
- [osu!](https://github.com/ppy/osu) for the osu! lazer project, which is a great reference for the new parser.

## Contributing

Feel free to contribute to this project. Any help is welcome.
