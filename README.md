# Desktop Buddy

A small pixel cat that lives on the Windows taskbar: it walks, naps, and nudges you to take breaks.

Plan: [Desktop Buddy: Project Plan](https://claude.ai/code/artifact/2b44736d-a7fd-40a8-88b8-2615c187930f)

## Status

Phase 1: a living buddy. The cat walks, sits, blinks, naps when you're away, reacts when you click it, and can be dragged around. It has a tray icon, settings, and an option to start with Windows. See [the phase 1 test checklist](docs/phase-1-test-checklist.md).

Phase 2: a useful, livelier buddy. It scratches, plays with a yarn ball, follows and pounces on the mouse, looks at windows that open or flash, and hangs by the scruff or belly depending on where you grab it. It also gives break reminders, holds up a focus timer, and hurries and sweats when the PC is busy. See [the phase 2 test checklist](docs/phase-2-test-checklist.md).

## Build

Requires the .NET 8 SDK on Windows.

```
dotnet run --project src/DesktopBuddy
dotnet test tests/DesktopBuddy.Core.Tests
```

Every pull request also builds a ready-to-run `DesktopBuddy.exe`, attached to the build run as an artifact.

## How it works

- `DesktopBuddy.Core` holds the platform-free parts, all unit tested: the `BehaviorEngine` state machine (walk, sit, nap, petted, dragged, falling), the frame picker in `CatAnimation`, the pixel frames in `CatPixels`, and the settings store.
- `TaskbarTracker` reads the taskbar's position with `SHAppBarMessage` and the `Shell_TrayWnd` window rectangle (for auto-hide).
- `MainWindow` draws and moves the cat. It is a transparent, topmost tool window placed with `SetWindowPos` in physical pixels, so DPI scaling can't skew it. Its transparent pixels are click-through.
- `FullscreenDetector` hides the cat for fullscreen games, video and presentations.
- `TrayIcon` (WinForms `NotifyIcon`), `SettingsWindow`, and `StartupRegistration` (the current user's `Run` registry key) make up the rest of the app.
