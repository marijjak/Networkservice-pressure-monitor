# 🛰️ NetworkService — Pressure Infrastructure Monitor

[![.NET](https://img.shields.io/badge/.NET%20Framework-WPF-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Pattern](https://img.shields.io/badge/Architecture-MVVM-0F766E)](#architecture)
[![Status](https://img.shields.io/badge/Status-Coursework%20Project-blue)](#about)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](#license)

> A real-time WPF desktop application that simulates a **SCADA-style monitoring system** for pressure valves — built around drag-and-drop network mapping, live data visualization, and a full undo/redo command history.

---

## 📑 Table of Contents

- [About](#about)
- [Assigned Specification](#assigned-specification)
- [Key Features](#key-features)
- [Screenshots](#screenshots)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [Data Model](#data-model)
- [Communication Protocol](#communication-protocol)
- [Roadmap](#roadmap)
- [License](#license)
- [Acknowledgments](#acknowledgments)

---

## 🧭 About

**NetworkService** is a WPF MVVM client application built for the *Applied Software Engineering — Usability Engineering in Infrastructure Systems* course. It acts as a monitoring dashboard for industrial entities (think SCADA control panels), receiving live measurements over TCP from a companion simulator app (**MeteringSimulator**), logging them to disk, and visualizing them through an interactive, draggable network topology and a self-drawn real-time chart.

The whole experience — drag & drop, undo history, keyboard-first navigation, field-level validation — was designed around a specific **target-user persona**, simulating how a real usability-driven enterprise tool would behave for that audience.

## 🎯 Assigned Specification

This implementation follows one specific combination drawn from the course's task matrix:

| Axis | Code | Meaning |
|---|---|---|
| 👤 **Target group** | `CG4` | **Clerks** — long-time computer users, accustomed to accounting-style information systems. Need precise numeric/percentage data, shortcuts for frequent actions, and a global **Undo All**, plus a visible action **History** panel. |
| 🌡️ **Entity type** | `T1` | **Pressure in valves** — entities modeled as pressure gauges (`Cable sensor` / `Digital manometer`), valid range **5–16 MPa**. |
| 🔍 **Filter mode** | `P2` | **Combinable filter** — type `ComboBox` + ID comparison (`<`, `>`, `=`) via `RadioButton` + `TextBox`, plus a CG4-specific in-range/out-of-range filter, all combinable simultaneously. |
| 📊 **Chart style** | `G2` | **Bar chart over time** — hand-drawn rectangles (no chart library), both axes labeled, color-coded for valid vs. out-of-range readings. |

## ✨ Key Features

### 🖱️ Drag & Drop Network Map
- Drag entities from a type-grouped **TreeView** onto a 12-cell canvas grid (3×4).
- Re-arrange entities cell-to-cell, or drag them back into the TreeView to remove them from the map.
- **Shift+Click** two placed entities to draw (or remove) a connection line between them — lines re-render live as entities move.
- "Auto-arrange" button instantly places every unplaced entity into free slots.

### ↩️ Full Undo / Undo All
- Every mutating action (add, delete, move, connect/disconnect) pushes its own inverse onto a shared, application-wide undo stack.
- `Ctrl+Z` reverts the last action; `Ctrl+Shift+Z` rewinds the *entire* history in one go.
- A live **History** panel lists every action taken, newest first.

### 📊 Live, Self-Drawn Bar Chart
- No chart control involved — bars are plain `Rectangle` shapes positioned manually on a `Canvas`.
- Shows the **last 5 measurements** for the selected entity, refreshing automatically the instant new data arrives over TCP — no manual refresh needed.
- Out-of-range readings are color-coded distinctly from valid ones, with a labeled valid-range band overlay.

### 🧮 Combinable Filtering (P2 + CG4)
- Filter by entity **type**, by **ID** comparison (`<`, `>`, `=`), and by **measurement range** (in/out of bounds) — all three criteria apply together, not just one at a time.
- One-click reset returns the table to its unfiltered state.

### ✅ Per-Field Validation
- Inline error messages directly under each form field — no `MessageBox` anywhere in the app.
- Smart "touched" tracking: errors don't appear prematurely while a field is still untouched, then validate live afterward.

### ⌨️ Keyboard-First Navigation
- Global shortcuts for view switching, undo, and undo-all.
- Context-aware Enter/Escape/Delete behavior depending on what's open (form, confirmation modal, filter).
- `Ctrl+E` expands/collapses all TreeView groups — the app's equivalent of an Expander shortcut.

### 🔔 Toast Notifications & Confirmation Modals
- Custom, theme-consistent toast feedback for successful add/delete actions.
- A custom confirmation dialog (not `MessageBox`) gates every delete operation.

### 📡 Live TCP Ingestion + Logging
- A background TCP listener receives measurements pushed by `MeteringSimulator`, dispatches them safely to the UI thread, and appends every reading (timestamp, entity, value, validity) to a persistent `log.txt`.

## 🖼️ Screenshots

> _Add screenshots or a short GIF walkthrough here before publishing — e.g._

| Network Entities | Display Map | Measurement Graph |
|---|---|---|
| `docs/screenshot-entities.png` | `docs/screenshot-display.png` | `docs/screenshot-graph.png` |

## 🏗️ Architecture

The application strictly follows the **MVVM** pattern — every code-behind file (`*.xaml.cs`) contains nothing but `InitializeComponent()`. All interaction logic lives in `Behaviors` (attached properties that translate mouse/keyboard events into ViewModel commands) and `ViewModel` (the actual business logic), bound declaratively from XAML.

```
┌──────────────────────┐        TCP         ┌──────────────────────┐
│   MeteringSimulator   │ ─────────────────▶ │   NetworkService      │
│  (external producer)  │   "Objekat_N:Val"  │  TCP Listener thread  │
└──────────────────────┘                     └──────────┬───────────┘
                                                          │ Dispatcher.Invoke
                                                          ▼
                                              ┌──────────────────────┐
                                              │   Model (Ventil)      │
                                              │  + LogService (.txt)  │
                                              └──────────┬───────────┘
                                                          │ INotifyPropertyChanged /
                                                          │ ObservableCollection events
                                                          ▼
                              ┌───────────────────────────────────────────────┐
                              │                  ViewModels                    │
                              │  NetworkEntitiesVM · NetworkDisplayVM ·        │
                              │  MeasurementGraphVM · MainWindowVM (Undo/Nav)  │
                              └───────────────────┬─────────────────────────────┘
                                                   │ Data Binding
                                                   ▼
                              ┌───────────────────────────────────────────────┐
                              │                    Views (XAML)                │
                              │  + Behaviors (DragDrop / Keyboard / Chart /     │
                              │    Connections) — zero logic in code-behind     │
                              └───────────────────────────────────────────────┘
```

## 📂 Project Structure

```
NetworkService/
├── Behaviors/
│   ├── DragDropBehavior.cs       # TreeView ↔ Grid ↔ Grid drag & drop
│   ├── KeyboardBehavior.cs       # Global/contextual keyboard shortcuts
│   ├── ChartBehavior.cs          # Hand-drawn G2 bar chart rendering
│   └── ConnectionsBehavior.cs    # Connection-line drawing between entities
├── Commands/
│   └── RelayCommand.cs           # Generic ICommand implementation
├── Converters/
│   └── Converters.cs             # Bool↔Visibility and friends
├── Model/
│   ├── Ventil.cs                 # T1 entity (pressure valve)
│   └── EntityType.cs             # Entity type metadata (name + image)
├── ViewModel/
│   ├── BaseViewModel.cs
│   ├── MainWindowViewModel.cs    # Navigation, global Undo stack, TCP server
│   ├── NetworkEntitiesViewModel.cs   # Table, filtering, validation, CRUD
│   ├── NetworkDisplayViewModel.cs    # Grid, TreeView sync, connections
│   ├── MeasurementGraphViewModel.cs  # Selected entity + last-5 measurements
│   └── LogService.cs             # File logging + measurement stream
├── Views/
│   ├── NetworkEntitiesView.xaml(.cs)
│   ├── NetworkDisplayView.xaml(.cs)
│   └── MeasurementGraphView.xaml(.cs)
├── Images/                       # Per-type entity icons
├── MainWindow.xaml(.cs)
└── App.xaml(.cs)
```

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | WPF (.NET Framework) |
| Pattern | MVVM (hand-rolled — no MVVM toolkit) |
| Data Binding | Native WPF binding + `INotifyPropertyChanged` / `ObservableCollection` |
| Charts | Custom-drawn (`Canvas` + `Rectangle`/`Line`) — **no chart library** |
| Networking | Raw `TcpListener` / `TcpClient` (`System.Net.Sockets`) |
| Persistence | Plain-text append log (`log.txt`) |
| Commands | Custom `RelayCommand` (`ICommand`) |

## 🚀 Getting Started

### Prerequisites
- Windows + Visual Studio 
- .NET Framework (matching the project's target — see `NetworkService.csproj`)
- The companion **MeteringSimulator** project/executable

### Run it

```bash
git clone https://github.com/marijjak/networkservice-pressure-monitor.git
cd networkservice-pressure-monitor
# Open NetworkService.sln in Visual Studio
# Set NetworkService as the startup project, then press F5
```

The app starts its own TCP listener on launch; `MeteringSimulator` is auto-restarted whenever an entity is added or removed, so the simulator always knows the current entity count.

## ⌨️ Keyboard Shortcuts

| Shortcut | Action | Scope |
|---|---|---|
| `Alt + 1` / `2` / `3` | Navigate to Entities / Display / Graph | Global |
| `Ctrl + Z` | Undo last action | Global |
| `Ctrl + Shift + Z` | Undo **all** actions | Global (hidden on Graph view) |
| `Ctrl + Enter` | Add new entity | Entities view |
| `Enter` | Apply filter / confirm delete | Context-aware |
| `Esc` | Cancel form / cancel delete | Context-aware |
| `Delete` | Delete selected entity (ignored while typing) | Entities view |
| `Ctrl + E` | Expand/collapse all TreeView groups | Display view |
| `Shift + Click` (mouse) | Connect/disconnect two entities | Display view |

## 📐 Data Model

**T1 — Pressure in Valves**

| Field | Type | Notes |
|---|---|---|
| `Id` | `int` | Auto-generated, always unique (`max + 1`) |
| `Name` | `string` | Must be non-empty and unique |
| `Type` | `EntityType` | `Cable sensor` or `Digital manometer` |
| `LastMeasurement` | `double` | Valid range: **5–16 MPa** — anything else is out-of-range |

## 📡 Communication Protocol

```
NetworkService                MeteringSimulator
      │   "Need object count" ───────────▶
      │ ◀────────────────────── object count
      │
      │ ◀──────────────── "Objekat_N:Value"
      │   (parsed → index N, measured value)
```

Every incoming reading is dispatched to the UI thread, written to `log.txt` with a timestamp and validity flag, and pushed into an `ObservableCollection` that drives the live chart.

## 🗺️ Roadmap

- [ ] Add a `Title` + action button(s) to the toast notification component (currently single-string only)
- [ ] Persist saved filter presets across sessions
- [ ] Unit tests for `NetworkEntitiesViewModel` filtering logic
- [ ] Theming pass — extract a dedicated CG4 "control-room" color palette into a standalone resource dictionary

## 📄 License

Distributed under the MIT License. This is an academic coursework project — feel free to fork and adapt for your own learning.

## 🙏 Acknowledgments

- Course: *Applied Software Engineering — Usability Engineering in Infrastructure Systems* (FTN)
- Companion app: **MeteringSimulator** (measurement producer)
- Built solo as part of Predmetni zadatak 2 (PZ2)

---

<p align="center">Made with ☕ and a lot of <code>Ctrl+Z</code></p>
