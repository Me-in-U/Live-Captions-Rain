# Contributing

Thanks for taking time to improve Live Captions Rain.

## Branch Workflow

Use a focused branch for each coherent change. Keep feature work, bug fixes, documentation updates, and cleanup changes separate when practical.

## Before Opening A Pull Request

Keep pull requests small enough to review. Avoid mixing unrelated refactors, generated output, formatting churn, and user-visible behavior changes.

For code changes, run:

```powershell
dotnet build LiveCaptionsRain.sln
dotnet test LiveCaptionsRain.sln
```

For release or packaging changes, also verify the published executable:

```powershell
dotnet publish src\LiveCaptionsRain\LiveCaptionsRain.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

## Development Notes

- UI text changes must update both English and Korean localization entries.
- Live Captions integration should remain local and Windows-only. Do not add remote caption, translation, LLM, or analytics services without a clear project decision.
- Do not commit generated installers, publish output, `artifacts`, local screenshots, logs, `bin`, `obj`, or user settings files.
- Keep window physics changes covered by tests where possible. Window visibility and occlusion bugs are easy to regress.
- Keep UI Automation code defensive because Windows Live Captions can change its automation tree after Windows updates.

## Pull Request Requirements

Pull requests should include:

- A short summary of the user-visible change.
- Risk or compatibility notes.
- Build and test evidence.
- Manual verification notes for overlay, tray, Live Captions, or window-physics changes.
- A note for any relevant test that could not be run.
