# Release Workflow

User preferences for this release (may be empty): $ARGUMENTS

Follow these steps exactly — do NOT skip ahead:

1. Run `git status` to verify no uncommitted changes.
2. Find the most recent git tag with `git describe --tags --abbrev=0`.
3. Run `git log <last-tag>..HEAD --oneline` to list all commits since that tag.
4. **Filter for significant changes only** (features, fixes, breaking changes, deprecations). Skip chores, docs, typos, refactoring unless user notes otherwise.
5. Summarize filtered changes as a concise bullet list, grouped by area (core / cli / mcp / build / docs).
6. Propose a CHANGELOG.md entry with:
   - Header: `## [X.Y.Z] - YYYY-MM-DD` (date today, version will be set by nbgv)
   - Bullet points: only the significant changes from step 5
   - If user passed preferences, apply them (additional notes, highlights, which commits to include/exclude)
7. Propose a commit message (imperative, short summary of release).
8. **STOP. Wait for explicit user approval** ("release", "yes", "ok", etc.) before proceeding.
9. Only after approval:
   - Create the commit with the proposed message.
   - Run `nbgv tag` to automatically assign version and create annotated tag.
   - Update CHANGELOG.md with the proposed entry at the top.
   - Run `git add CHANGELOG.md` and `git commit --amend` to add changelog to the same commit.
10. **Never push** unless the user explicitly says so.
11. **Never use `--no-verify`** or skip hooks.
