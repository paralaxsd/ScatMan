# ScatMan [![ci](https://github.com/paralaxsd/ScatMan/actions/workflows/ci.yml/badge.svg)](https://github.com/paralaxsd/ScatMan/actions/workflows/ci.yml) [![CLI NuGet](https://img.shields.io/nuget/v/ScatMan.Cli.svg?color=blue)](https://www.nuget.org/packages/ScatMan.Cli) [![MCP NuGet](https://img.shields.io/nuget/v/ScatMan.Mcp.svg?color=blue)](https://www.nuget.org/packages/ScatMan.Mcp)
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
scatman types <package> <version> [--namespace <ns>]
```

```bash
scatman types NAudio.Wasapi 2.2.1 --namespace NAudio.CoreAudioApi
```

```
NAudio.Wasapi 2.2.1 [NAudio.CoreAudioApi] — 38 public type(s)

NAudio.CoreAudioApi
  class      AudioClient
  class      AudioCaptureClient
  class      AudioEndpointVolume
  class      MMDevice
  class      MMDeviceEnumerator
  class      WasapiCapture
  class      WasapiOut
  enum       AudioClientShareMode
  interface  IAudioSessionEventsHandler
  ...
```

---

### `members` — list public members of a type

```bash
scatman members <package> <version> <typeName>
```

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
| `get_versions` | List available versions of a package (`packageId`, `includePrerelease?`) |
| `get_types` | List all public types (`packageId`, `version`, `ns?`) |
| `get_members` | List all public members incl. constructors (`packageId`, `version`, `typeName`) |

---

## Roadmap

| Command | Description | Status |
|---|---|---|
| `versions <pkg>` | List available versions (optional `--pre`) | ✅ done |
| `ctors <pkg> <ver> <type>` | List public constructors | ✅ done |
| `types <pkg> <ver>` | List all public types (optional `--namespace`) | ✅ done |
| `members <pkg> <ver> <type>` | List all public members of a type | ✅ done |
| `search <pkg> <ver> <query>` | Search types and members by name | 🔲 todo |
| `serve` | Expose everything as an MCP stdio server | ✅ done (Phase 2) |

---

## Architecture

- **ScatMan.Core** — NuGet download + `MetadataLoadContext` inspection. No UI. Usable as a standalone library.
- **ScatMan.Cli** — `dotnet tool`, Spectre.Console.Cli for argument parsing and output.
- **ScatMan.Mcp** — MCP stdio server, powered by Core. Installable as `dotnet tool install -g ScatMan.Mcp`.
