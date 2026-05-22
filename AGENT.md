# Repository Workflow Rules

## Project Scope

Live Captions Rain is a Windows-only .NET 8 WPF app. Keep the app focused on Windows Live Captions words falling into a physics overlay.

- Windows Live Captions is the only text source.
- Do not add translation, LLM, transcript history, cloud API, analytics, or custom speech recognition unless explicitly requested.
- Preserve local-only behavior. Caption text and settings should not be sent to remote services.
- Keep Windows 11 as the supported target unless the project scope changes.

## Versioning

Use `Major.Minor.Patch`.

- `Major`: platform-wide changes, architecture-wide rewrites, breaking changes, or very large feature sets.
- `Minor`: one feature release. It may contain multiple accepted user-facing features.
- `Patch`: already-released version hotfix. It may contain one or more tightly related bug, security, packaging, or runtime fixes, but no new user-facing feature.

Version bumps must match the release scope.

- Do not use a patch release for new functionality.
- If a release contains new user-facing functionality, use the next minor version.
- If a release is only a hotfix for an already published release, use the next patch version.
- If release scope grows beyond the chosen version meaning, either move the extra work back to normal development or choose a correctly named version.

Keep release version surfaces in sync when they exist:

- Project/package version metadata in `.csproj` or shared build props.
- Git tag and GitHub release name.
- Release artifact name.
- Release notes.

## Changelog And Release Notes Rules

Release notes are required for every final release and release candidate.

- Record user-visible features, bug fixes, packaging changes, dependency/runtime changes, and known limitations.
- Keep release notes consistent with the version bump type.
- Do not mix unrelated feature summaries into a patch release note.
- Mention manual validation gaps explicitly.
- If `CHANGELOG.md` or versioned changelog files are introduced, update them in the same commit as the code or release change they describe.

Release notes should include:

- Version number.
- Short summary.
- Added, changed, fixed, and packaging/runtime sections when applicable.
- Validation commands and manual checks.
- Release artifact path or release asset name.

## Branch Model

`main` is the release branch. Use it only for final release integration.

Feature and normal fix work should happen off `main`, then return through a reviewed and verified integration flow. If a `develop` branch is introduced, use it as the ongoing integration branch for the next release.

Branch names:

- Feature branch format: `feature/<short-name>`.
- Bug fix branch format: `fix/<short-name>`.
- Release stabilization branch format: `v<major>.<minor>.<patch>-beta`.
- Emergency production hotfix format: `hotfix/v<major>.<minor>.<patch>-<short-name>`.

Do not use personal, tool, or automation prefixes in branch names. In particular, do not create branches with prefixes such as `codex/`.

Release flow when a `develop` branch exists:

1. Keep `main` at the latest stable release.
2. Keep `develop` as the shared integration branch for the next release.
3. Branch feature/fix work from `develop`.
4. Merge completed feature/fix branches back into `develop` after review and verification.
5. When `develop` is stable enough for release preparation, cut `v<major>.<minor>.<patch>-beta` from `develop`.
6. On beta, allow only stabilization work: bug fixes, docs, packaging, dependency lock fixes, release notes, and verification changes.
7. When beta is final-stable, promote the release to `main`.
8. After `main` receives the release, tag it as `v<major>.<minor>.<patch>` and publish the release artifact.
9. Sync the released `main` state back into `develop` if `develop` exists.

Do not add new feature scope directly to beta. New feature work after beta cut goes to the next development branch.

Branch cleanup rules:

- Never delete `main` or `develop`.
- Delete completed feature/fix branches after they are merged.
- Delete obsolete beta branches after the release is promoted to `main`, unless the user explicitly wants to keep them.
- Run `git fetch --prune origin` after remote branch deletion.
- Verify remaining branches with `git branch --all --verbose` and `git ls-remote --heads origin`.

## Main And Release Rules

`main` should stay clean, linear, and release-oriented.

- Do not commit experimental work directly to `main`.
- Do not use `main` as a feature integration branch.
- Do not merge planning-only branches into `main`.
- Do not leave merge commits that only expose temporary branch names.
- Prefer rebase, squash, or fast-forward history when it keeps history clearer.
- Only force-push `main` when explicitly requested and after verifying the target commit.

Before promoting a release to `main`, verify:

- .NET app tests pass.
- The app version or release name is correct.
- The app publishes successfully when packaging or release output changes.
- The published app launches or the release artifact is smoke-checked.
- Release notes describe the user-visible changes and validation.

## History Rewrite Rules

Treat public history rewrites as release operations.

- Rewrite `main` only when the user explicitly requests it.
- Use `--force-with-lease`, not plain force push.
- Capture the expected remote commit before force pushing.
- Verify local and remote branch pointers after the rewrite.
- Search rewritten history for unwanted branch names, tool prefixes, raw hashes, and obsolete merge commits.
- Do not leave merge commits that expose temporary branches in release history.
- Do not include planning-only commits in release history.

Before and after a history rewrite, check:

```powershell
git status --short --branch
git log --oneline --decorate --max-count=20
git rev-parse main origin/main
git branch --all --verbose
```

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
dotnet build LiveCaptionsRain.sln --no-restore
dotnet test LiveCaptionsRain.sln --no-restore
```

If the environment uses the repo-local SDK path:

```powershell
$dotnet = Join-Path $env:USERPROFILE '.dotnet-codex-sdk\dotnet.exe'
& $dotnet build LiveCaptionsRain.sln --no-restore
& $dotnet test LiveCaptionsRain.sln --no-restore
```

Publish check:

```powershell
dotnet publish src\LiveCaptionsRain\LiveCaptionsRain.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Run publish verification when changes affect packaging, startup, app icons, project files, runtime dependencies, or release output.

## Testing Expectations

Bug fixes require a regression test whenever practical.

- Caption diffing, tokenization, queues, and stabilization belong in `tests\LiveCaptionsRain.Tests`.
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
