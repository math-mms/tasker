# Tasker

Desktop task manager for Windows, with an optional Android companion app. Keep objectives with title, subtitle, and details; pin the active one to a floating overlay; customize colors and typography to match your setup.

## Features

### Windows (`Tasker/`)

- Create, edit, complete, and delete tasks (title, subtitle, description)
- Active / completed grouping in the main list
- Always-on-top **active task widget** with drag repositioning and click-through when the cursor is outside it
- System tray icon (minimize to tray instead of exiting)
- Appearance settings for app and widget (colors, fonts, opacity, size)
- Local persistence under `%LocalAppData%\Tasker\` (`todos.json`, `settings.json`)

### Android (`Tasker.Android/`)

- Native companion with the same indigo palette
- Add, open, edit, complete, restore, and delete objectives
- Separate active and cleared lists
- Local storage on the device (no sync with Windows yet)

## Requirements

| Platform | Needs |
|----------|--------|
| Windows  | [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer, Windows OS |
| Android  | Recent [Android Studio](https://developer.android.com/studio), JDK 17 |

## Quick start (Windows)

```bat
launch-tasker.cmd
```

Or manually:

```bat
dotnet build Tasker\Tasker.csproj -p:EnableWindowsTargeting=true
Tasker\bin\Debug\net8.0-windows\Tasker.exe
```

## Build Android

Open `Tasker.Android` in Android Studio, let Gradle sync, then run the `app` configuration on an emulator or device.

Targets Android API 35 (compile SDK 36). See [`Tasker.Android/README.md`](Tasker.Android/README.md) for details.

## Project layout

```
tasker/
├── Tasker/                 # WPF desktop app (.NET 8)
├── Tasker.Android/         # Native Android companion
└── launch-tasker.cmd       # Build + run Windows app
```

## Data

| App     | Location |
|---------|----------|
| Windows | `%LocalAppData%\Tasker\` |
| Android | App-private SharedPreferences (`tasker_android`) |

The two apps do **not** sync with each other today.

## License

No license file is included yet. Add one if you plan to distribute or accept contributions.
