# MingDynasty Open Toolkit

MingDynasty Open Toolkit is an open-source collection of reusable C# simulation and infrastructure components extracted and generalized from the development of MingDynastyGame, an ongoing historical city-building and society simulation project.

The goal of this repository is to provide lightweight, reusable building blocks for simulation-heavy games and applications while keeping the original game's proprietary gameplay, art, assets, and content separate.

## Features

The toolkit currently includes reusable components related to:

- Simulation tick management
- Game time systems
- World coordinates
- Chunk lifecycle management
- Simulation LOD
- Resource inventory
- Reservation and resource handling
- Versioned save infrastructure
- General simulation utilities

The project is currently in early development and will continue to evolve as reusable systems are extracted, generalized, tested, and documented.

## Project Background

MingDynastyGame is a historical city-building and society simulation project involving population, production, logistics, economy, infrastructure, warfare, and large-scale simulation.

During development, several general-purpose systems were created that may also be useful for other simulation projects. This repository provides selected reusable components without publishing the main game's proprietary source code or assets.

## Requirements

- .NET / C#
- Some modules are designed to remain independent of Unity where possible.
- Individual module requirements are documented as the project evolves.

## Usage

Browse the source modules and documentation included in this repository.

Each component is intended to be reusable independently where practical. More examples and integration documentation will be added as development continues.

## Development Status

**Early Development**

This project is actively maintained and is expected to change as APIs are refined, additional tests are added, and more reusable components are generalized.

## Roadmap

- Improve documentation and usage examples
- Expand automated testing
- Improve module independence
- Add additional reusable simulation utilities
- Improve performance validation
- Add integration examples
- Continue security and code-quality reviews

## Contributing

Issues, suggestions, bug reports, and contributions are welcome.

Please provide clear reproduction steps when reporting bugs and explain the intended use case when proposing new functionality.

## Security

If you discover a potential security issue, please report it responsibly rather than publicly disclosing sensitive details before a fix is available.

## License

See the LICENSE file in this repository for licensing information.

## Disclaimer

This repository contains selected reusable and generalized components only.

It does not contain the complete MingDynastyGame source code, proprietary gameplay systems, commercial assets, third-party licensed assets, or private project resources.
