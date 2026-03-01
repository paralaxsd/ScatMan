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
# not yet on NuGet — clone and run locally for now
dotnet run --project src/ScatMan.Cli -- <command>
```

---

## What works

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
  .ctor(MMDevice captureDevice, Boolean useEventSync)
  .ctor(MMDevice captureDevice, Boolean useEventSync, Int32 audioBufferMillisecondsLength)
```

Add `--json` to get machine-readable output:

```bash
scatman ctors NAudio.Wasapi 2.2.1 NAudio.CoreAudioApi.WasapiCapture --json
```

```json
{
  "package": "NAudio.Wasapi",
  "version": "2.2.1",
  "typeName": "NAudio.CoreAudioApi.WasapiCapture",
  "constructors": [
    { "parameters": [] },
    { "parameters": [{ "name": "captureDevice", "typeName": "MMDevice" }] }
  ]
}
```

Packages are cached in `~/.scatman/cache/` after the first download.

---

## Roadmap

| Command | Description | Status |
|---|---|---|
| `ctors <pkg> <ver> <type>` | List public constructors | ✅ done |
| `types <pkg> <ver>` | List all public types (optional `--namespace`) | 🔲 todo |
| `members <pkg> <ver> <type>` | List all public members of a type | 🔲 todo |
| `search <pkg> <ver> <query>` | Search types and members by name | 🔲 todo |
| `serve` | Expose everything as an MCP stdio server | 🔲 todo (Phase 2) |

All commands support `--json`.

---

## Architecture

- **ScatMan.Core** — NuGet download + `MetadataLoadContext` inspection. No UI. Usable as a standalone library.
- **ScatMan.Cli** — `dotnet tool`, Spectre.Console.Cli for argument parsing and output.
- **ScatMan.Mcp** — MCP stdio server (Phase 2), also powered by Core.
