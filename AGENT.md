# Repository Workflow Rules

## Project Scope

Falling Words is a Windows-only .NET 8 WPF app. Keep the app focused on Windows Live Captions words falling into a physics overlay.

- Windows Live Captions is the only text source.
- Do not add translation, LLM, transcript history, cloud API, analytics, or custom speech recognition unless explicitly requested.
- Preserve local-only behavior. Caption text and settings should not be sent to remote services.
- Keep Windows 11 as the supported target unless the project scope changes.

## Branch And Commit Rules

Keep each change scoped to one coherent task.

- Inspect `git status --short` before staging or committing.
- Stage exact files for the current change.
- Do not include generated output, unrelated dirty files, local screenshots, or temporary artifacts.
- Use concise commit titles that describe the behavior or artifact changed.
- For non-trivial commits, include a body with:

```text
Summary:
- ...

Validation:
- ...
```

Do not use temporary branch names, tool prefixes, or vague titles such as `update`, `fix`, `wip`, or `changes`.

## Build And Test Rules

Use the narrowest meaningful test while developing, then run the full suite before claiming completion for code changes.

Default commands:

```powershell
dotnet build FallingWords.sln --no-restore
dotnet test FallingWords.sln --no-restore
```

If the environment uses the repo-local SDK path:

```powershell
$dotnet = Join-Path $env:USERPROFILE '.dotnet-codex-sdk\dotnet.exe'
& $dotnet build FallingWords.sln --no-restore
& $dotnet test FallingWords.sln --no-restore
```

Publish check:

```powershell
dotnet publish src\FallingWords\FallingWords.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Run publish verification when changes affect packaging, startup, app icons, project files, runtime dependencies, or release output.

## Testing Expectations

Bug fixes require a regression test whenever practical.

- Caption diffing, tokenization, queues, and stabilization belong in `tests\FallingWords.Tests`.
- Window collision, visible top-edge resolution, monitor filtering, and fullscreen/top-edge exclusion need focused tests.
- Settings defaults, persistence, migration behavior, and localization changes need tests.
- Physics changes should be covered by deterministic unit tests when possible.
- UI XAML changes should be manually checked when layout, labels, named controls, tray behavior, or overlay state changes.

If a test or manual check cannot be run, state that clearly in the final response, commit body, or PR description. Do not describe skipped checks as passing.

## UI And Localization Rules

Visible UI text must stay aligned with runtime behavior.

- Add or update both English and Korean strings.
- Keep labels short and user-facing.
- Do not add explanatory UI text that describes implementation internals.
- If a setting is removed from UI, remove matching tray/menu exposure unless there is a clear reason to keep it.
- Keep the settings window and overlay behavior separable. The overlay should not interfere with capturing the settings window when idle.

## Live Captions Rules

Live Captions integration is fragile because it depends on Windows UI Automation.

- Keep the adapter defensive against missing automation elements.
- Avoid assuming a stable Windows Live Captions visual tree beyond what is verified.
- Do not spawn historical caption text after clearing words or restarting.
- Preserve caption stabilization delay so partially corrected Live Captions text does not immediately spawn stale words.
- Restore Live Captions only if this app hid it.

## Overlay And Physics Rules

The overlay should be visible only when needed.

- Avoid rendering when there are no active words and no pending spawned words.
- Preserve default click-through behavior.
- Treat window-top platforms as visible-surface physics only. Hidden, minimized, owned, tool, fullscreen, maximized, cloaked, and off-monitor windows must not create floating platforms.
- Top-edge-attached windows may occlude windows behind them, but should not become platforms.
- Keep physics stable over pixel-perfect glyph collision. Rectangular word bodies are acceptable.
- Enforce max active words by fading out the oldest active words when necessary.

## Documentation Rules

Keep public documentation concise and current.

- Main README stays English.
- Localized README files live under `docs\readme`.
- Korean README: `docs\readme\README.ko.md`.
- Keep language links at the top of README files.
- Documentation screenshots belong under `docs\assets\screenshots` only when they are intentional and safe to publish.
- Do not include private desktop content, private captions, account names, tokens, or full local user paths in documentation screenshots.

## Artifact And Large File Rules

Generated files do not belong in git.

Do not commit:

- `artifacts/`
- `bin/`
- `obj/`
- publish output folders
- installers unless explicitly requested as release assets
- logs
- temporary screenshots
- local settings files

Documentation screenshots under `docs/assets/` are allowed when they are intentional, current, and reasonably sized.

## Security And Privacy Rules

Never commit secrets or local credentials.

- Do not commit API keys, tokens, `.env` files with real credentials, or local-only overrides.
- Redact local account names and full local paths from public docs and screenshots.
- Do not add telemetry or network transmission of captions without an explicit project decision.
- Treat caption text as private user data.

## Safety Rules

- Never overwrite unrelated user changes.
- Never use destructive git commands such as `git reset --hard` or `git checkout --` unless the user explicitly requests that exact operation.
- Prefer exact file edits and exact staging.
- Use `apply_patch` for manual source or documentation edits.
- For Windows file operations, prefer native PowerShell cmdlets and avoid destructive shell composition.

## Final Response Rules

Final responses must reflect actual work and actual verification.

- Mention changed files when useful.
- Report only commands that were actually run.
- State skipped checks and why they were skipped.
- For packaging work, include the artifact path and launch or smoke-check evidence.
- For docs-only work, a content/link check is usually enough unless code or project files changed.
