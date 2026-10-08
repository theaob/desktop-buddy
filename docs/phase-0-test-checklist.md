# Phase 0: taskbar spike test checklist

Goal: prove a transparent window can stand on the Windows taskbar reliably before building anything else.
Gate to phase 1: every row below that applies to your setup passes.

## Get the build

1. Open the pull request, go to **Checks**, open the latest **build** run, and download the `DesktopBuddy-win-x64` artifact.
2. Unzip it and run `DesktopBuddy.exe`. It is unsigned, so Windows SmartScreen may warn: choose **More info**, then **Run anyway**.
3. Right-click the cat for **Diagnostics...**, **Pause walking** and **Exit**.

The app logs every change it sees to `%LOCALAPPDATA%\DesktopBuddy\buddy.log`. When a row fails, copy the diagnostics text (or attach the log) to the PR.

## Test matrix

| # | Setup | What to check | Pass? |
| --- | --- | --- | --- |
| 1 | Default taskbar, 100% scaling | Cat stands exactly on the top edge of the taskbar and walks its full width | |
| 2 | Display scaling 125%, 150%, 175% (Settings > Display > Scale) | Cat stays on the taskbar edge after each change, without restarting | |
| 3 | Auto-hide taskbar on | Cat drops to the screen edge when the taskbar hides and rides back up when it shows | |
| 4 | Two monitors | Cat stays on the primary monitor's taskbar; note what happens when you change which screen is primary | |
| 5 | Fullscreen video in a browser (press F) | Cat hides, then comes back when you leave fullscreen | |
| 6 | A fullscreen game | Cat hides while the game is focused | |
| 7 | PowerPoint slideshow, or a Teams/Zoom screen share | Cat hides during the slideshow | |
| 8 | Click and Alt+Tab | Clicking the cat doesn't steal focus from your current app; the cat never shows in Alt+Tab or the taskbar | |
| 9 | Clicking around the cat | The empty space around the cat passes clicks through to the window underneath | |
| 10 | Right-click menu | Menu opens, and closes when you click somewhere else | |
| 11 | Leave it running for an hour | Task Manager shows it near 0% CPU and under about 60 MB memory | |
| 12 | Taskbar on top or side (Windows 10 only) | Cat hangs under a top taskbar, sits beside a side one | |

## Known limits of the spike

- The cat is a placeholder drawn in code; real sprites come in phase 1.
- Only the primary monitor's taskbar is tracked.
- While an auto-hide taskbar is hidden, the cat covers a small part of the bottom edge where your mouse reveals the taskbar.
