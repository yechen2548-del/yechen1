# Open-source extraction audit

Audit scope: the local MingDynastyGame Unity project, inspected on 2026-09-17. The source project was not modified during extraction.

## Included modules

| Toolkit area | Source area | What it provides | Extraction action |
|---|---|---|---|
| Core infrastructure | `Assets/Game/Core/Runtime` | Commands, events, stable IDs, localization, logging, performance samples, seeded random, data registry, versioned saves, task scheduling, services, settings, and clock | Copied source; namespace changed to `MingDynasty.OpenToolkit.Core`; save content-pack terminology generalized to module terminology |
| Calendar/time | `Assets/Game/Simulation/Runtime/Time/GameTimeSystem.cs` | Configurable month/day calendar, seasons, time progression, pause, scale, and events | Copied source; namespace changed |
| Simulation ticks | `Assets/Game/Simulation/Runtime/Ticks/SimulationTickSystem.cs` | Fast/normal/slow tick accumulators, catch-up cap, and chunk-state LOD mapping | Copied source; namespace changed |
| Coordinates/chunks | `Assets/Game/Simulation/Runtime/World` | World/local/region/chunk coordinates, bounds, chunk registry, lifecycle transitions, focus radii, and state events | Copied selected source; removed terrain, population, and content-reference fields that belonged to the game |
| LOD scheduling | `Assets/Game/Simulation/Runtime/Phase83/Phase83SimulationLod.cs` | Distance-based LOD policy, rotating entity work budget, and chunk budget snapshot | Copied source; renamed Phase 8.3-specific public types to generic names |
| Inventory transactions | `Assets/Game/Simulation/Runtime/Resources/ResourceInventory.cs` | Capacity, reservations, transfer rollback, deterministic snapshots, and atomic multi-resource commit/refund | Copied source; removed game-specific resource tags, definition fields, and settlement adapter |

## Explicitly excluded

- `Presentation` runtime: depends on Unity/Input System and contains the game's UI, camera, visual, and scene integration.
- `Content` loaders and content packs: contain project-specific content contracts and asset references.
- Population, household, survival, building, infrastructure, military, economy, settlement, agriculture, and production simulations: these encode MingDynastyGame's core commercial design and cannot be safely generalized by mechanical copying.
- Pathfinding: coupled to the game's building registry, road network, world query, and project data definitions.
- Editor scene generators and acceptance launchers: project-specific development tooling tied to private scenes and phase workflows.
- All scenes, ScriptableObjects, prefabs, materials, textures, meshes, animations, audio, fonts, imported FBX files, archives, and external asset staging directories: not needed by the toolkit and/or not cleared for redistribution.
- Logs, test result dumps, Unity `Library/`, `Temp/`, `Logs/`, `Builds/`, and user settings: generated or machine-specific data.

## Security review

The included source and repository configuration were checked for common secret markers, credential file names, connection strings, private keys, API keys, tokens, and local secret files. No secret was found in the selected source. The parent project contains local paths and development logs; those files were not copied.

This is a repository hygiene review, not a guarantee that a future contributor cannot add a secret. See `SECURITY.md`.

## Copyright and license review

No third-party asset or binary was copied. The source project's asset records show a large external staging library, including packages with separate licenses and at least one candidate whose metadata required reconciliation; all of that material is excluded here. Unity itself is a host dependency and is not redistributed.

The code is treated as project-owned source under the maintainer's instruction. Before a public upload, the maintainer should confirm that every included source file was authored by or properly assigned to the repository owner and that no unrecorded contractor or third-party code was used.

## Verification status

- Source project location verified: yes.
- Copy, not move: yes; original project remains in place.
- Standalone C# compilation: not available in the source environment because no C# SDK/compiler was installed; a temporary harness was prepared but could not run.
- Unity EditMode tests: included but require a local Unity 2022.3 Editor run.
- GitHub Release: not created; remote publication status is represented by repository history rather than this source audit.
