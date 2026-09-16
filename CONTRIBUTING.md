# Contributing

Thanks for helping improve MingDynasty Open Toolkit.

## Before opening a change

- Keep runtime code independent of `UnityEngine` unless the change belongs in an explicitly optional adapter.
- Prefer small, focused pull requests with tests for public behavior.
- Do not add MingDynastyGame scenes, private game rules, historical content data, art, audio, fonts, models, or generated Unity folders.
- Do not add dependencies or copied code unless its license permits redistribution and the source is recorded.
- Run the standalone validation and, when a Unity installation is available, the package EditMode tests.

## Style

Use the existing C# style: explicit types, braces for control flow, deterministic ordering for snapshots and listings, and clear argument validation at public boundaries.

## Pull requests

Describe the behavior changed, the tests run, and any compatibility or licensing implications. Never include secrets or private project paths in an issue or pull request.
