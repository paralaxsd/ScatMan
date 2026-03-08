# Changelog

All notable changes to this project will be documented in this file.
This project adheres to [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [v0.1.36] – 2026-03-08

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
