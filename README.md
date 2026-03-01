# ScatMan
<p align="center">
  <img src="https://raw.githubusercontent.com/paralaxsd/ScatMan/main/images/logo.png"
       width="300"
       alt="ScatMan logo" />
</p>

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

```bash
scatman members NAudio.Wasapi 2.2.1 NAudio.CoreAudioApi.WasapiCapture
```

```
NAudio.CoreAudioApi.WasapiCapture — 9 public member(s)

events
  event EventHandler<StoppedEventArgs> RecordingStopped

methods
  void Dispose()
  void StartRecording()
  void StopRecording()

properties
  bool IsWaveFormatSupported(WaveFormat waveFormat)
  MMDevice MmDevice { get; }
  WaveFormat WaveFormat { get; set; }
```

---

### `ctors` — list constructors of a type

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

## Roadmap

| Command | Description | Status |
|---|---|---|
| `ctors <pkg> <ver> <type>` | List public constructors | ✅ done |
| `types <pkg> <ver>` | List all public types (optional `--namespace`) | ✅ done |
| `members <pkg> <ver> <type>` | List all public members of a type | ✅ done |
| `search <pkg> <ver> <query>` | Search types and members by name | 🔲 todo |
| `serve` | Expose everything as an MCP stdio server | 🔲 todo (Phase 2) |

---

## Architecture

- **ScatMan.Core** — NuGet download + `MetadataLoadContext` inspection. No UI. Usable as a standalone library.
- **ScatMan.Cli** — `dotnet tool`, Spectre.Console.Cli for argument parsing and output.
- **ScatMan.Mcp** — MCP stdio server (Phase 2), also powered by Core.
