# SVGViewer

A Windows desktop application (C# / WPF, .NET 8) to **browse a drive, find SVG
files fast, and preview them** — with folders that contain SVGs clearly marked,
adjustable preview sizes, and one double‑click to open a file in your associated
editor (e.g. Inkscape). The interface is fully multilingual (Dutch, English,
German).

## Features

- **Drive & folder navigation** — pick a drive and click through a tree view.
- **SVG folder highlighting** — folders that contain `.svg` files are marked so
  you can jump straight to them.
- **Structure filter** — show the full folder tree, or only folders that contain
  SVG files.
- **Preview sizes** — Large, Medium, Small, or Only details (list view).
- **SVG‑only** — all non‑SVG files are hidden.
- **Edit externally** — double‑click an SVG to open it in the associated app.
- **Multilingual UI** — Dutch (default), English, German, switchable at runtime.
- **In‑app help** — user guide opens in the currently selected language.

## Screenshots

Main window:

![SVG Viewer main window](docs/images/Screenshot01.png)

Preview window (zoom & pan):

![SVG preview window](docs/images/Screenshot02.png)

## Tech stack

| Area | Choice |
|------|--------|
| Framework | .NET 8 (`net8.0-windows`), WPF |
| Pattern | MVVM (`CommunityToolkit.Mvvm`) |
| SVG rendering | `SharpVectors.Reloaded` |
| Optional UI | Syncfusion WPF (license read from a local file) |
| Localization | `.resx` satellite assemblies (nl / en / de) |

## Documentation

- Quick reference: [`docs/user-guide/`](docs/user-guide/) — a short overview in NL / EN / DE (also opened by the in‑app Help button).
- Design & planning: [`docs/Epic.md`](docs/Epic.md) and
  [`docs/UserStories.md`](docs/UserStories.md).

## License

© HN Software Development. All rights reserved.
