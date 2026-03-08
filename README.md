# ScatMan [![test](https://github.com/paralaxsd/ScatMan/actions/workflows/test.yml/badge.svg)](https://github.com/paralaxsd/ScatMan/actions/workflows/test.yml) [![ScatMan.CLI NuGet](https://img.shields.io/nuget/v/ScatMan.Cli.svg?color=blue&label=ScatMan.Cli)](https://www.nuget.org/packages/ScatMan.Cli) [![ScatManMCP NuGet](https://img.shields.io/nuget/v/ScatMan.Mcp.svg?color=blue&label=ScatMan.Mcp)](https://www.nuget.org/packages/ScatMan.Mcp) [![ScatMan.Core NuGet](https://img.shields.io/nuget/v/ScatMan.Core.svg?color=blue&label=ScatMan.Core)](https://www.nuget.org/packages/ScatMan.Core) 
![ScatMan logo](https://raw.githubusercontent.com/paralaxsd/ScatMan/main/images/logo.png)

> *Improvising over unknown APIs since 2026.*

A good jazz musician doesn't read the whole score before playing — they listen, explore, respond.
ScatMan does the same with NuGet packages: drop it a package and a type name, and it riffs back
with everything you need to know. No throwaway reflection projects. No hunting through decompilers.
Just you and the API, trading phrases.

---

## Install

```bash
dotnet tool install --global ScatMan.Cli
```

---

## Commands

### `versions` — list available versions of a package

```bash
scatman versions <package> [--pre] [--head <n>]
```

```bash
scatman versions NAudio.Lame --pre
```

```
NAudio.Lame — 15 version(s)

prerelease
  (none)

stable
  2.1.0    2023-05-30
  2.0.1    2022-01-17
  2.0.0    2021-02-10
  ...
```

Without `--pre`, only stable versions are shown. With `--pre`, both sections are always displayed.
Use `--head N` to show only the N most recent versions; the header will read `N of total` so you always know how many exist.

---

### `types` — list all public types in a package

```bash
scatman types <package> <version> [--namespace <nsOrGlob>] [--filter <nameOrGlob>]
```

`<version>` also accepts aliases: `latest` and `latest-pre`.

```bash
scatman types NAudio.Wasapi 2.2.1 --namespace NAudio.CoreAudioApi --filter Capture
```

```
NAudio.Wasapi 2.2.1 [NAudio.CoreAudioApi] ~Capture — 2 public type(s)

NAudio.CoreAudioApi
  class      AudioCaptureClient
  class      WasapiCapture
```

`--namespace` supports exact namespace or glob pattern.
`--filter` supports glob pattern; plain text remains a case-insensitive substring match.

Glob syntax follows `Microsoft.Extensions.FileSystemGlobbing`.
Supported tokens: `*`, `?`, exact names, directory separators (`/`), and `**` for arbitrary directory depth. Character classes `[abc]` and alternation `{foo,bar}` are not supported.

Examples:
- `--namespace "ScatMan.*"`
- `--namespace "*.Core"`
- `--filter "*Capture*"`

---

### `search` — search types and members across a package

```bash
scatman search <package> <version> <queryOrGlob> [--namespace <nsOrGlob>]
```

`<version>` also accepts aliases: `latest` and `latest-pre`.

```bash
scatman search NAudio.Wasapi 2.2.1 GetDefaultCaptureDevice
```

```
NAudio.Wasapi 2.2.1 — search "GetDefaultCaptureDevice"

Types (0)
  (none)

Members (1)
  WasapiCapture
    method       static MMDevice GetDefaultCaptureDevice()
```

Cross-type search — useful when you know a method name exists somewhere in the package
but not which type owns it. Both type names and member names are searched.

`<query>` accepts glob patterns. Plain text is still treated as case-insensitive substring.

---

### `members` — list public members of a type

```bash
scatman members <package> <version> <typeName> [--kind <kind>]
```

`<version>` also accepts aliases: `latest` and `latest-pre`.

`--kind` filters by member type: `constructor`, `method`, `property`, `field`, or `event` (case-insensitive).

Long signatures are automatically expanded to one parameter per line when they would exceed the terminal width.

```bash
scatman members NAudio.Wasapi 2.2.1 NAudio.CoreAudioApi.WasapiCapture
```

```
NAudio.CoreAudioApi.WasapiCapture — 13 public member(s)

constructors
  .ctor()
  .ctor(MMDevice captureDevice)
  .ctor(MMDevice captureDevice, bool useEventSync)
  .ctor(
    MMDevice captureDevice,
    bool useEventSync,
    int audioBufferMillisecondsLength)

events
  event EventHandler<WaveInEventArgs> DataAvailable
  event EventHandler<StoppedEventArgs> RecordingStopped

methods
  void Dispose()
  static MMDevice GetDefaultCaptureDevice()
  void StartRecording()
  void StopRecording()

properties
  CaptureState CaptureState { get; }
  AudioClientShareMode ShareMode { get; set; }
  WaveFormat WaveFormat { get; set; }
```

---

### `ctors` — list constructors of a type

> Convenience alias — constructors are also included as the first group in `members`.

```bash
scatman ctors <package> <version> <typeName>
```

`<version>` also accepts aliases: `latest` and `latest-pre`.

```bash
scatman ctors NAudio.Wasapi 2.2.1 NAudio.CoreAudioApi.WasapiCapture
```

```
NAudio.CoreAudioApi.WasapiCapture — 4 public constructor(s)

  .ctor()
  .ctor(MMDevice captureDevice)
  .ctor(MMDevice captureDevice, bool useEventSync)
  .ctor(MMDevice captureDevice, bool useEventSync, int audioBufferMillisecondsLength)
```

---

### `diff` — compare API between two versions

```bash
scatman diff <package> <version1> <version2> [--type <typeNameOrFull>]
```

`<version1>` and `<version2>` also accept aliases: `latest` and `latest-pre`.

`--type` restricts the diff to a single type (full or simple name).

```bash
scatman diff Serilog 3.1.1 4.0.0
```

```
Serilog — 3.1.1 → 4.0.0

BREAKING CHANGES
  - method  Serilog.Core.ILogEventSink.void Emit(LogEvent logEvent)

DEPRECATIONS
  ! property  Serilog.Log.string OutputTemplate { get; set; }

ADDITIONS
  + type  Serilog.Core.IBatchedLogEventSink
  + method  Serilog.Core.ILogEventSink.Task EmitBatchAsync(IEnumerable<LogEvent> batch)
```

Results are grouped by severity:
- **BREAKING CHANGES** — removed types, removed members, and members whose signature changed
- **DEPRECATIONS** — members newly marked `[Obsolete]` compared to the previous version
- **ADDITIONS** — added types and added members

```bash
scatman diff Serilog 3.1.1 4.0.0 --type LoggerConfiguration
```

---

### `sources` — list configured package sources

```bash
scatman sources
```

Lists all enabled package sources from your `nuget.config` hierarchy (project → user → machine).

```
Configured Package Sources

Name                               URL
nuget.org                          https://api.nuget.org/v3/index.json
internal-nexus                     https://nexus.company.com/nuget/v3/index.json
```

Use source names or URLs with the `--source` option (see below).

---

## Package Sources

All commands support a `--source` option to specify which NuGet package source to use:

```bash
scatman types <package> <version> --source <sourceNameOrUrl>
scatman versions <package> --source <sourceNameOrUrl>
scatman members <package> <version> <type> --source <sourceNameOrUrl>
scatman search <package> <version> <query> --source <sourceNameOrUrl>
scatman ctors <package> <version> <type> --source <sourceNameOrUrl>
scatman diff <package> <version1> <version2> --source <sourceNameOrUrl>
```

`<sourceNameOrUrl>` can be:
- A **source name** from `scatman sources` — e.g. `nuget.org`, `internal-nexus`
- A **full V3 NuGet index URL** — e.g. `https://api.nuget.org/v3/index.json`

If `--source` is omitted, defaults to `nuget.org`.

Examples:
```bash
# Use a named source from nuget.config
scatman types NAudio.Wasapi 2.2.1 --source internal-nexus

# Use a URL directly
scatman versions Newtonsoft.Json --source https://myget.org/F/myorg/api/v3/index.json

# Resolve "latest" from a custom source
scatman types Newtonsoft.Json latest --source internal-nexus
```

> **Note:** Your system's `nuget.config` file must be properly configured with authentication for private sources.
> The `--source` option respects the same credentials and settings that `dotnet` CLI tools use.

---

All commands support `--json` for machine-readable output.
Packages and their transitive dependencies are cached in `~/.scatman/cache/` after the first download.

---

## MCP Server

ScatMan ships as an **MCP stdio server** — use it directly from Claude Code, Claude Desktop, or any MCP-compatible client without ever opening a terminal.

```bash
dotnet tool install --global ScatMan.Mcp
```

### Claude Code

```bash
claude mcp add --scope user -t stdio ScatMan scatman-mcp
```

> `--scope user` is required so the server is available globally, not just within one project directory.

### Claude Desktop

`~/.claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "scatman": {
      "command": "scatman-mcp"
    }
  }
}
```

### Available tools

| Tool | Description |
|---|---|
| `get_versions` | List available versions of a package (`packageId`, `includePrerelease?`, `source?`) |
| `get_types` | List all public types (`packageId`, `version`, `ns?`, `filter?`, `source?`) |
| `search` | Search types and members by name (`packageId`, `version`, `query`, `ns?`, `source?`) |
| `get_members` | List all public members incl. constructors (`packageId`, `version`, `typeName`, `kind?`, `source?`) |
| `get_diff` | Compare API between two versions — added/removed types and members (`packageId`, `version1`, `version2`, `typeName?`, `source?`) |

**Version aliases:** For tools with a `version` parameter, aliases are supported:
`latest` (latest stable, fallback to latest if no stable exists) and `latest-pre`.

**Package sources:** All tools support an optional `source` parameter (source name or URL) to query from custom NuGet sources.
If omitted, defaults to nuget.org. See [Package Sources](#package-sources) section above.

For MCP tools with `ns`, `filter`, or `query`, glob syntax follows
`Microsoft.Extensions.FileSystemGlobbing` (`*`, `?`, `**`, exact names, `/`). Character classes `[abc]` and alternation `{foo,bar}` are not supported.

---

## Roadmap

| Command | Description | Status |
|---|---|---|
| `versions <pkg>` | List available versions (optional `--pre`) | ✅ done |
| `ctors <pkg> <ver> <type>` | List public constructors | ✅ done |
| `types <pkg> <ver>` | List all public types (optional `--namespace`) | ✅ done |
| `members <pkg> <ver> <type>` | List all public members of a type | ✅ done |
| `search <pkg> <ver> <query>` | Search types and members by name | ✅ done |
| `serve` | Expose everything as an MCP stdio server | ✅ done (Phase 2) |
| `diff <pkg> <v1> <v2>` | Detect added/removed types and members between versions | ✅ done |

---

## Architecture

- **ScatMan.Core** — NuGet download + `MetadataLoadContext` inspection. No UI. Usable as a standalone library.
- **ScatMan.Cli** — `dotnet tool`, Spectre.Console.Cli for argument parsing and output.
- **ScatMan.Mcp** — MCP stdio server, powered by Core. Installable as `dotnet tool install -g ScatMan.Mcp`.
