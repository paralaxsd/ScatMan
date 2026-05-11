# Gemini CLI Mandates - ScatGirl Project

> **Rule #0: ABSOLUTELY NO AUTOMATIC COMMITS.**
> NEVER stage or commit changes without explicit user feedback and approval. Always present small, verifiable steps and wait for a clear "Go" (or "Commit") from the user. This is the highest priority operational mandate.

> **Rule #1**: Adhere to all development, architectural, and naming conventions defined in the `CLAUDE.md` files in this repository.

## Primary Sources of Truth

- **Git Conventions**: Use the commit message format and branch naming rules specified in [`./CLAUDE.md`](./CLAUDE.md).
- **Rich Git Commit Style**: Do not use prefixes like 'feat:' or 'fix:'. Use descriptive titles. Always provide a detailed body explaining the "what" and "why" (using bullet points for complex changes). End every commit with a `Co-Authored-By: Gemini CLI <noreply@google.com>` tag to show LLM pride.

## Operational Instructions

1. **Research Phase**: Always check if a relevant `CLAUDE.md` exists in the subdirectory you are working in (e.g., `src/Packages/CLAUDE.md`).
2. **Strategy Phase (No-Surprise Checkpoint)**: For any task affecting >2 files or involving architectural decisions, stop after the Strategy phase. Present a concise summary of the plan and wait for explicit user approval before proceeding to Execution.
3. **Implementation**: Ensure new classes are `sealed` by default. Use modern C# features konsequent:
    - Prefer `records` for data containers (DTOs, Events).
    - Use `primary constructors` to minimize boilerplate.
    - Use collection expressions and target-typed `new()`.
4. **Anti-Generic Naming**: Avoid generic suffixes like `Manager`, `Helper`, or `Utility`. Choose precise domain terms (e.g., `OperatorSwapper` instead of `OperatorManager`) that describe exact responsibility.
5. **Code-Narrative & Readability**: Adhere to the **Step-Down Rule**. Organize methods so that a method appears before the methods it calls (caller-before-callee). A reader scanning top-to-bottom should follow the logic without jumping back up. Prioritize human readability and the reduction of cognitive load over concise AI-generated blocks.
6. **Clean Code & Guard Clauses**: Avoid deep nesting (Pyramids of Doom) by consistently using early exits and guard clauses. Always extract side effects and deep logic from core methods into well-named helper methods, which must be sorted strictly below the caller according to the Step-Down rule.
7. **Minimalism (Default by Omission)**:
   - **Rule**: Omit any keyword or expression that matches the compiler's default behavior.
     - Omit `private` for fields and methods.
     - Omit `internal` for classes and interfaces.
     - Use target-typed `new()` or `var` to avoid redundant type declarations.
     - Prefer expression-bodied members (`=>`) for concise logic.
5. **Class Organization & Sorting**:
   - **Sections**: Group members into commented sections: `FIELDS`, `PROPERTIES`, `EVENTS`, `STRUCTORS`, `METHODS`.
   - **Ordering**:
     1. Sort by visibility: `public` -> `internal` -> `protected` -> `private`.
     2. Sort by modifier: **Instance before Static**.
     3. (Fields only) **Readonly before Mutable**.
   - **SRP over Sections**: If a class becomes too large (requiring nested sub-sections), refactor it into smaller, focused classes instead.
6. **Verification**: Before finishing a task, cross-check against the "Code Review Checklist" in [`./CLAUDE.md`](./CLAUDE.md).

---
