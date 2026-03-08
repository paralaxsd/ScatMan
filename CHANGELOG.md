# Changelog

All notable changes to this project will be documented in this file.
This project adheres to [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [v0.1.48] – 2026-03-08

### Added
- New `scatman diff <pkg> <v1> <v2>` command to compare the public API between two versions
- `--type` option on `diff` to restrict the comparison to a single type
- `ApiDiffer` class in `ScatMan.Core` — compares types and members across two assembly sets
- `TypeDiff` and `ApiDiff` records in `ScatMan.Core` for structured diff results
- `ChangedMember` record for members whose signature changed (same name, different signature)
- Severity grouping in formatted output: **BREAKING CHANGES** / **DEPRECATIONS** / **ADDITIONS**
- Detection of signature changes: single-overload members with the same name but different signature are reported as changed rather than removed + added
- Detection of newly deprecated members: members that gained `[Obsolete]` in v2 are reported under Deprecations
- `IsObsolete` flag on `MemberDescriptor` — populated for all member kinds (constructor, method, property, field, event)
- `get_diff` MCP tool (`packageId`, `version1`, `version2`, `typeName?`, `source?`)
- `--source` support on `diff` command (consistent with all other commands)
- 9 new unit tests for `ApiDiffer` and `IsObsolete` detection

### Changed
- `MemberDescriptor` extended with `IsObsolete` property (default `false`, non-breaking)
- `TypeDiff` extended with `Changed` and `Deprecated` member lists
- All `TypeInspector` format methods now detect and propagate `[ObsoleteAttribute]`

### Documentation
- README `diff` section updated with severity-grouped output example
- MCP tools table updated with `get_diff` entry

---

## [v0.1.45] – 2026-03-08

### Added
- New `scatman sources` command to list all configured package sources from `nuget.config` hierarchy
- `--source <name|url>` option on all CLI commands (types, members, search, ctors, versions)
- `PackageSourceResolver` class for discovering and resolving NuGet sources
- Optional `source` parameter on all MCP tools (get_types, get_members, search, get_versions)
- Support for internal NuGet repositories (Nexus, Artifactory, Azure Artifacts, MyGet, etc.)
- Direct V3 NuGet index URL support as alternative to source names

### Changed
- `PackageDownloader` now accepts optional `sourceUrl` parameter (defaults to nuget.org)
- `NuGetRegistrationClient.GetVersionsAsync` now accepts optional `sourceUrl` parameter
- When custom source is provided, uses `NuGet.Protocol.PackageMetadataResource` for version discovery
- `PackageVersionResolver` now threads `sourceUrl` through version resolution

### Documentation
- README updated with `sources` command documentation
- Added "Package Sources" section with examples for named sources and URLs
- MCP tools table updated to show `source?` parameter

### Testing
- 13 new unit tests for `PackageSourceResolver`:
  - Source enumeration and nuget.org verification
  - Null/empty/whitespace handling
  - Source name resolution (case-insensitive)
  - Direct URL resolution (HTTP/HTTPS)
  - Error handling for invalid sources, malformed URLs, unsupported schemes
- All 90 tests passing (77 existing + 13 new)

---

## [v0.1.35] – 2026-03-06

### Added
- XML documentation extraction for overloaded constructors
- Unit tests for TypeInspector including constructor summary extraction

### Fixed
- Bug in XmlDocumentationProvider: constructor overloads now get correct summaries
- Documentation and test coverage aligned with actual glob pattern support

---
Older changes are not tracked yet.
