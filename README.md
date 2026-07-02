<div align="center">

<img src="logo.png" alt="ClipKeep" width="160" />

# ClipKeep

### Keep everything you copy in a searchable memory.

**Never lose anything you copied again.**

[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6?logo=windows&logoColor=white)](https://github.com/blindxfish/clipkeep)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF%20%2F%20MVVM-2C3E50)](https://learn.microsoft.com/dotnet/desktop/wpf/)
![Local-first](https://img.shields.io/badge/local--first-yes-2ECC71)
![No cloud](https://img.shields.io/badge/cloud-none-E23B3B)
![No AI](https://img.shields.io/badge/AI-none-E23B3B)
![Release](https://img.shields.io/badge/release-v1.1.1-E23B3B)

</div>

---

**ClipKeep** is a Windows clipboard manager that stores, organizes, and searches everything you copy — text, links, code, and images. It runs quietly in the system tray and turns your clipboard into a fast, private, searchable archive.

It is **local-first**, **offline**, **fast**, and **privacy-focused**.

> 🔒 No cloud &nbsp;•&nbsp; 🧠 No AI &nbsp;•&nbsp; 👤 No account &nbsp;•&nbsp; 📡 No telemetry

---

## ✨ Features

### 📋 Capture everything
- Automatic clipboard monitoring via the native Win32 listener
- **Text, URLs, emails, phone numbers, file paths, colors, and code** — classified deterministically (no AI), with programming-language detection
- **Image clipboard history** — screenshots and copied bitmaps saved to disk with thumbnails
- Tracks the **source application** and window title for every clip
- Smart **duplicate detection** (content-hash) — re-copying bumps a counter instead of cluttering history

### 🔎 Find it instantly
- Full-text search powered by **SQLite FTS5** — results as you type
- Sidebar filters by type: All, Text, URLs, Code, Files, Images, ★ Favorites

### ⚡ Quick Paste
- Global hotkey **`Ctrl + Shift + V`** opens a lightweight popup at your cursor
- Type to filter, arrow keys to navigate, **Enter pastes straight into the app you were using**
- Favorites float to the top

### 🛡️ Privacy first
- **Sensitive-content detection** — credit cards (Luhn), IBANs, JWTs, API keys, and private keys are **never saved by default**
- **Application blacklist** — password managers (Bitwarden, 1Password, KeePass, …) are excluded out of the box
- Everything stays on your machine

### 🧰 Made to live in the tray
- Pause/resume monitoring, favorites, retention cleanup, launch-on-startup
- Configurable retention (Forever / 30 / 90 / 365 days) — **favorites are never auto-deleted**

---

## 📥 Download & Install

Grab the latest build from the [**Releases**](https://github.com/blindxfish/clipkeep/releases) page:

| Download | What it is |
|---|---|
| **`ClipKeep-Setup-1.1.1-x64.exe`** | Standard installer (x64) — Start Menu shortcut, uninstaller, per-user (no admin needed) |
| **`ClipKeep-1.1.1-x64-portable.exe`** | Portable single file — no install, just run it |

> Also available for **ARM64** and **x86** (`-arm64` / `-x86` in the file name), with SHA-256 checksums. ClipKeep is also coming to the **Microsoft Store**.

> ℹ️ Both are self-contained — **no .NET runtime required**.
>
> ⚠️ The builds aren't code-signed yet, so Windows SmartScreen may warn about an *"unknown publisher."* Click **More info → Run anyway**.

---

## 🚀 Usage

1. Launch ClipKeep — it lives in the **system tray**.
2. Copy things as you normally would. ClipKeep captures them automatically.
3. Press **`Ctrl + Shift + V`** anywhere for Quick Paste, or double-click the tray icon to open the main window.
4. Search, favorite ⭐, or delete clips from the main window.

---

## 🏗️ Tech Stack

- **C# / .NET 8**, **WPF** desktop app with the **MVVM** pattern (CommunityToolkit.Mvvm)
- **SQLite** + **FTS5** full-text search (raw `Microsoft.Data.Sqlite`)
- Win32 interop: `AddClipboardFormatListener`, `RegisterHotKey`, `SendInput`
- Tray icon via Hardcodet.NotifyIcon.Wpf
- Images stored on disk (PNG + JPEG thumbnails); only metadata in SQLite

---

## 🧩 Project Structure

```
ClipForge.sln
├── src/
│   ├── ClipForge.Core/            # Domain, classification, security, settings (no UI/infra deps)
│   ├── ClipForge.Infrastructure/  # SQLite database, repositories, Win32 APIs
│   ├── ClipForge.App/             # WPF views, view models, tray, hotkeys, clipboard capture
│   └── ClipForge.Tests/           # xUnit tests
└── installer/                     # Inno Setup script
```

---

## 🔧 Build from Source

**Requirements:** Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
# Clone
git clone https://github.com/blindxfish/clipkeep.git
cd clipkeep

# Build & test
dotnet build ClipForge.sln
dotnet test ClipForge.sln

# Run
dotnet run --project src/ClipForge.App
```

### Package (optional)

```bash
# Portable self-contained single-file exe
dotnet publish src/ClipForge.App -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -o dist/portable

# Installer (requires Inno Setup 6)
ISCC installer/ClipForge.iss
```

---

## 🗺️ Roadmap

**Done ✅** — tray app, text + image capture, classification, FTS search, favorites, Quick Paste, sensitive-content exclusion, app blacklist, settings, retention, installer.

**Planned 🔜** — OCR search for images, statistics dashboard, tags & collections, export/import & backup, portable mode, theming.

---

## 🔐 Privacy

ClipKeep is designed to keep your data yours. All clipboard history lives in a local SQLite database and image files under `%AppData%\ClipForge`. Nothing is ever sent anywhere — no cloud sync, no accounts, no telemetry. Sensitive content is filtered before it's ever written to disk.

---

<div align="center">

**ClipKeep** — *Fast like a utility. Organized like a database. Private like an offline tool.*

</div>
