# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. Please chat with me in German and let's be on a first name basis, but still code in US-English. Remember my personal C# style. For other languages the same principles apply.

## C# Style

- **Prefer conciseness**: `var`, expression-bodied members, file-scoped namespaces, primary constructors, pattern matching, collection expressions.
- **Visibility**: omit `private`; omit `internal` for nested types. Use `sealed` by default.
- **Member order**: fields → properties → constructors → methods; sorted public → internal → protected → private within each group; static members after all instance members. Fields: `_camelCase`; static/protected fields: `PascalCase`.
- **Prefer records** where appropriate; **prefer extension types** for utility helpers.
- **Minimize negations** in conditionals; prefer early returns.
- **Prefer functional expressions** (LINQ, etc.) over loops where readable.
- Max line length: **100 characters**.
- Use **strategic blank lines** to separate logical blocks, error handling, and return statements.

## MCP Server
- `builder.Logging.ClearProviders()` is required in Program.cs — default console logger writes to stdout and corrupts the JSON-RPC channel
- Register with `claude mcp add --scope user` — the VS Code extension's claude.exe subprocess only reads top-level `mcpServers`, not project-scoped entries

## Git
- **No conventional commits** — no `feat:`, `fix:`, `chore:` prefixes. Plain imperative subject line, capitalize first word.
