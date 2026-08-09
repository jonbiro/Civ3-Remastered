# Original Civilization III files

Civ3 Remastered does not commit or redistribute the original Civilization III game data.

The application treats a user's existing **Civilization III Complete** installation as a runtime dependency for Classic mode. The remaster repository contains the engine, compatibility code, tests, and replacement/open assets only.

## What the application expects

Select the root directory of an installed or extracted Civilization III Complete file tree. A valid root contains, at minimum:

```text
Civilization III Complete/
├── Conquests/
│   ├── conquests.biq
│   ├── Art/
│   │   └── buttonsFINAL.pcx
│   └── Text/
│       └── PediaIcons.txt
├── Art/                 # base-game assets, when present
├── Text/                # base-game text, when present
└── civ3PTW/             # Play the World assets, when present
```

The validator resolves these paths case-insensitively because the original files were authored for Windows but Civ3 Remastered also runs on case-sensitive filesystems.

## Archive/disc images versus an install directory

CD/DVD image files and compressed disc-image archives are **installation media**, not the runtime asset directory. They must first be installed or extracted into a normal Civilization III Complete file tree.

Civ3 Remastered's folder picker can be pointed either at the actual install root or at a nearby wrapper folder created by an extractor. It searches a small, bounded number of nested directories and stores the normalized root after validation.

## Environment variable

For development and automated testing, set `CIV3_HOME` to the install directory (or a nearby wrapper directory):

```bash
export CIV3_HOME="/path/to/Sid Meier's Civilization III Complete"
```

An invalid `CIV3_HOME` is ignored rather than overriding a valid auto-detected installation.

## Validation boundary

A directory is accepted only when the remaster can locate all three of these essential files:

1. `Conquests/conquests.biq` — default Conquests rules/data.
2. `Conquests/Text/PediaIcons.txt` (or base `Text/PediaIcons.txt`) — Civilopedia asset mappings.
3. `Conquests/Art/buttonsFINAL.pcx` (or base `Art/buttonsFINAL.pcx`) — representative original UI art.

This is intentionally stronger than checking for a single marker file. It prevents a partial disc, installer directory, or unrelated folder from being saved as the active Civ III installation.

## Repository rule

Do not add original Civilization III binaries, music, sound, art, BIQ/SAV fixtures derived from private game files, or other publisher-owned assets to the public repository. Compatibility fixtures should be synthetic whenever possible. Private/local fixtures may be used during development without being committed.
