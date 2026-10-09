# Desktop Buddy

A small pixel cat that lives on the Windows taskbar: it walks, naps, and nudges you to take breaks.

Plan: [Desktop Buddy: Project Plan](https://claude.ai/code/artifact/2b44736d-a7fd-40a8-88b8-2615c187930f)

## Status

Phase 0: a spike that proves a transparent window can stand on the taskbar. See [the test checklist](docs/phase-0-test-checklist.md).

## Build

Requires the .NET 8 SDK on Windows.

```
dotnet run --project src/DesktopBuddy
```

Every pull request also builds a ready-to-run `DesktopBuddy.exe`, attached to the build run as an artifact.

## How it works

- `TaskbarTracker` reads the taskbar's position with `SHAppBarMessage` and the `Shell_TrayWnd` window rectangle (for auto-hide).
- `MainWindow` is a transparent, topmost tool window placed with `SetWindowPos` in physical pixels, so DPI scaling can't skew it. Its transparent pixels are click-through.
- `FullscreenDetector` hides the cat for fullscreen games, video and presentations.
