# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. Please chat with me in German and let's be on a first name basis, but still code in US-English. Remember my personal C# style. For other languages the same principles apply.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs. Validate manually.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

**Verification & Validation**:
- **Discuss features before commit**: Once a feature is implemented, present the results and wait for manual validation/approval from the user before finalizing the commit.
- **Reproduce bugs**: Always reproduce reported issues before applying a fix.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style (see [.github/CLAUDE.md](.github/CLAUDE.md) for code style conventions).
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

# Code Style & Philosophy

## Core Principle: Code is Read

**The 10:1 Principle**: You read code 10x more than you write it. Therefore, optimize for the **reader**, not the writer.

When you design a file, class, or function, ask: *"Can someone understand this on the first read?"* If the answer is no, simplify.

## Structure: Abstraction First

Every class should tell a story: what it does (public interface) before how it does it (private details).

**Section order within a class** (use commented section headers):
1. **INTERNAL TYPES** — Nested types
2. **FIELDS** — State
3. **EVENTS** — Public event declarations
4. **PROPERTIES** — Public contract
5. **CONSTRUCTORS** — Setup
6. **METHODS** — Public first, private below. Within the private methods, order by caller-before-callee: a method should appear before the methods it calls. A reader scanning top-to-bottom follows the logic without jumping back up.

**Sort order within each section**:
- Visibility: `public` → `internal` → `protected` → `private`
- Modifier: instance before `static`
- Fields only: `readonly` before mutable

**Why**: When you open a file, you first see "what is this class for?" Only if you need details do you scroll down. Readers rarely need them.

## Type Design: Sealed by Default

**Most types should be `sealed`.**

Why? Inheritance creates invisible complexity—readers must think about subclasses, subclass authors must think about what's safe to override. **Sealed types signal**: "This is self-contained. No surprises."

**When to open inheritance**: Only when design *requires* substitution (interfaces for DI, abstract base for shared implementation).

## Functions: Small, Named, Focused

A 50-line function requires readers to hold ~50 lines of context. Five 10-line functions with clear names let readers understand one step at a time. **Rule**: If a method does more than one logical thing, extract. Good names are self-documenting.

## Control Flow: Early Returns

Early returns reduce cognitive load by removing nesting. Guard clauses first (preconditions), then happy path:

```csharp
if (op is null) return false;
if (op.Ports.Count == 0) return false;
return true;  // Clear, unindented happy path
```

## Modern C#: Noise Reduction

Modern C# features reduce visual noise, which reduces cognitive load:
- **`var`**: Removes type repetition
- **Pattern matching**: Replaces nested conditionals
- **Records**: Immutability + automatic equality
- **Primary constructors**: Concise initialization

Less noise = easier to read.

## Default by Omission

Omit keywords that match the compiler default — they add visual noise without information:
- **No `private`** on fields and methods (it's the default)
- **No `internal`** on classes and interfaces (it's the default)
- **Target-typed `new()`** or `var` instead of repeating the type
- **Expression-bodied members** (`=>`) for single-expression logic

## Naming

- **Private fields**: `_camelCase` (signals "internal state")
- **Public/Protected**: `PascalCase` (no underscore)
- **Methods**: Verb-noun (`LoadProject`, `ValidateSchema`)

Names communicate intent at a glance. Avoid cryptic names that require searching definitions.

## Git Conventions

### Commits: Tell the Story

```
Add frame-scoped logging for operator execution

Allows diagnostics to trace per-frame state changes.
Uses InterpolatedStringHandler to avoid allocations.
```

Git log is *documentation*. Future you will read commits to understand *why* code exists. "Add X" tells the story; "fix bugs" tells nothing.

**Rule**: First line 50 chars max, imperative mood, one logical change per commit. Each line in the commit description should be no longer than 72 characters.

### Branches

- `feature/description` — New feature
- `fix/description` — Bug fix
- `refactor/area` — Cleanup
- `docs/topic` — Documentation

## Code Quality Principles

### SOLID

- **Single Responsibility**: One reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Interfaces are properly substitutable
- **Interface Segregation**: Don't force unused methods on implementers
- **Dependency Inversion**: Depend on abstractions, not concretions

If a class has 5 responsibilities, readers must understand 5 things. If it has 1, they understand 1.

## 5. Environment & Communication
- **Shell**: The terminal environment is Windows PowerShell. Use `;` as a command separator instead of `&&`.
- **Language**: While code, comments, and commit messages MUST be in English, the chat interaction should follow the language used by the user when starting the thread.

### MCP Tooling

Two MCP tools are available that replace slow filesystem/reflection-based API discovery:

**ScatMan** — NuGet package API explorer (use instead of grepping DLLs or browsing XML docs):
- `mcp__ScatMan__search` — find types/members by name across a package (e.g. "focus" → finds `FocusChanged`)
- `mcp__ScatMan__get_members` — list all members of a type with full signatures
- Package ID comes from the `.csproj` `<PackageReference>` entry; version from `Directory.Packages.props` or the `.csproj`.

**ScatGirl** — Roslyn-based C# symbol search for *this codebase* (use instead of Grep for structured code queries):
- `mcp__ScatGirl__find_declarations` — find where a type/method/interface is declared
- `mcp__ScatGirl__find_members` — list all members of a type with line numbers (no file read needed)
- `mcp__ScatGirl__find_references` — find all usages; `kind: invocation` filters to call sites only
- Works on raw source, no build required. C# only.

### DRY & YAGNI
- **DRY**: If you copy-paste code 3x, extract it. Duplication forces readers to keep multiple versions in sync mentally.
- **YAGNI**: Don't add abstractions "just in case." Add when you need it, refactor then.