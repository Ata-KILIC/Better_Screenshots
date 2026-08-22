# Better Screenshots

Better Screenshots is a compact, local-first Windows 10/11 screenshot companion. It intentionally does not replace the Snipping Tool or run at sign-in: launch it when you want its shortcuts, and exit it when you are done.

## What it does

- Region capture with a frozen desktop, selection dimensions, pixel magnifier, Escape cancellation, and DPI-aware virtual-desktop coordinates.
- `Shift + PrintScreen` captures a region; `Ctrl + Shift + PrintScreen` captures text with local Windows OCR and puts Unicode text directly on the clipboard.
- Two post-capture modes only: **Save + Clipboard** (default) and **Clipboard Only**.
- Local deterministic smart names based on the active application, window title, and on-demand OCR; ordinary PNG files remain the complete screenshot history.
- A lightweight on-demand editor with shapes, arrow, pen, highlighter, text, numbered markers, crop, undo/redo, blur, pixelate, and permanent solid redaction.
- Tray actions for region, fullscreen, active-window and delayed capture; open-folder, Copy Path, Open With, and a short-lived preview are included.

No screenshot content is uploaded, no analytics are present, and no service, scheduled task, registry startup entry, startup shortcut, keyboard hook, or residual helper process is created.

## Requirements

- Windows 10 version 2004 (build 19041) or later, x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build
- A Windows OCR language pack for Copy Text and OCR-assisted naming (installable from Windows Settings if unavailable)

## Build and run

```powershell
dotnet build BetterScreenshots.sln
dotnet run --project src\BetterScreenshots\BetterScreenshots.csproj
dotnet test BetterScreenshots.sln
```

The launch window is the settings page. The tray icon confirms that the utility is currently active. Closing the settings window with **X**, or choosing **Exit Better Screenshots** from the tray, unregisters the global shortcuts, removes the tray icon, and terminates the process.

## Defaults and storage

- Screenshot: `Shift + PrintScreen`
- Copy Text: `Ctrl + Shift + PrintScreen`
- Save behavior: **Save + Clipboard**
- Save folder: `Pictures\Better Screenshots`
- Settings: `%LocalAppData%\BetterScreenshots\settings.json`

The application never registers `PrintScreen` or `Win + Shift + S`, so Windows’ own screenshot behavior remains unchanged. Shortcut conflicts are shown when settings are saved; select another shortcut in that case.
