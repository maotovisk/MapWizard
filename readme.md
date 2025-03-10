# MapWizard 2

A tool that aims to allow mappers copy the hitsounds of a beatmap difficulty to other difficulties on the set, manage metadata and more. This is a rewrite of the original [Map Wizard](https://github.com/maotovisk/map-wizard), previously made in Tauri and Svelte, now being rewritten in C# with .NET 9 and Avalonia.

<br/>

<p align="right"><b>Current Status</b>: Working on Map Cleaner</p>

## Project Goals

- Fully cross-platform (primary target is Linux, but should work on MacOS and Windows as well);
- Hitsound Copier;
- Metadata manager;
- Auto generate combo color from BG;
- Map cleaner.

## Requirements

- .NET 9 or above

## Installing
We provide pre-built binaries for Linux and Windows. You can download it from the [releases page](
https://github.com/maotovisk/MapWizard/releases). 
 
Thanks to the [Velopack](https://velopack.io/) project, it auto-updates and it's just a 1-click install.

You can also get it from the [AUR](https://aur.archlinux.org/packages/mapwizard-git) with the package `mapwizard-git`

## Building

1. Clone the repository

```bash
git clone https://github.com/maotovisk/MapWizard.git
```

2. Run the project

```bash
dotnet run --project MapWizard.Desktop
```

## Roadmap

- [x] Implement the basic beatmap parser
- [x] Implement the hitsound copier
- [x] Implement the metadata manager
- [ ] Implement the map cleaner
- [ ] Implement the combo color generator

## References

- [Mapping Tools](https://github.com/olibomby/mapping_tools) by [OliBomby](https://github.com/olibomby) for the whole idea of the project and such a great toolset for mapping.
- [Map Wizard](https://github.com/maotovisk/map-wizard) by [me](https://github.com/maotovisk) for the first implementation of the hitsound copier and metadata management stuff.
- [osu! File Formats](https://osu.ppy.sh/help/wiki/osu!_File_Formats) for the .osu file format documentation.
- [osu!](https://github.com/ppy/osu) for the osu! lazer project, which is a great reference for the new parser.

## Contributing

Feel free to contribute to this project. Any help is welcome.
