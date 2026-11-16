# LiveCue

LiveCue is an experimental Windows WPF autocue application for live events.

**Status:** source prototype v0.6, not yet compiled or validated on Windows.

## Build

The Windows GitHub Actions workflow builds `LiveCue.csproj` and publishes a self-contained Windows x64 executable as a workflow artifact. The project source files must be uploaded to the repository root before a build can succeed.

## Test safety

Do not rely on this application for a live production until the Windows build, dual-monitor behavior, scrolling, recovery and controller safety tests have passed.
