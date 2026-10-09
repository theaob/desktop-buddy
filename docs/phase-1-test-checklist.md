# Phase 1: living buddy test checklist

Goal: the first version worth running every day.
Gate to phase 2: it runs a full workday at under 1% CPU and nothing below is broken.

Get the build the same way as in phase 0: download the `DesktopBuddy-win-x64` artifact from the pull request's latest **build** run. Close the phase 0 spike first.

| # | What to do | What should happen | Pass? |
| --- | --- | --- | --- |
| 1 | Watch it for a minute | Cat walks with a two-step gait, sits now and then, and blinks while sitting | |
| 2 | Click the cat | A heart bobs over its head for a moment, then it sits | |
| 3 | Drag the cat up and let go | It dangles while held, then falls back onto the taskbar and walks on from there | |
| 4 | Drag it and let go below the taskbar's top edge | It hops back up onto the taskbar | |
| 5 | Leave mouse and keyboard alone for 1 minute (set "Nap after" to 1 in Settings) | Cat lies down with a pulsing "z"; moving the mouse wakes it | |
| 6 | Tray icon (look in the hidden icons under ^ if needed) | Shows a tiny cat; right-click menu has Hide/Show, Pause, Start with Windows, Settings, Diagnostics, Exit | |
| 7 | Tray > Hide buddy, then Show buddy | Cat disappears and comes back | |
| 8 | Tray > Pause walking | Cat stops walking but still sits, blinks and naps | |
| 9 | Settings: change walking speed | Cat walks faster or slower right away; the value survives a restart | |
| 10 | Settings or tray: Start with Windows, then sign out and back in | Cat starts by itself | |
| 11 | Run DesktopBuddy.exe a second time | No second cat appears | |
| 12 | Right-click the cat | Menu with Settings, Pause, Hide, Diagnostics, Exit; it closes when you click elsewhere | |
| 13 | Everything from phase 0 still holds | Stands on the taskbar, hides in fullscreen, doesn't steal focus | |
| 14 | Leave it running for a workday | Task Manager shows it under 1% CPU | |
| 15 | With two monitors, drag the cat onto the other one and drop it | It stays there, walking on that monitor's taskbar (or its bottom edge if that monitor has no taskbar) | |
| 16 | Exit and start it again | It comes back on the monitor you last dropped it on | |
| 17 | Play a fullscreen video on the other monitor | The cat stays visible on its own monitor | |

Settings live in `%APPDATA%\DesktopBuddy\settings.json`; the log is `%LOCALAPPDATA%\DesktopBuddy\buddy.log`.
