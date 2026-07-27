# Epic — SVGViewer

> Status: In ontwikkeling · Eigenaar: HN Software Development · Laatst bijgewerkt: milestone 1

## 1. Visie

SVGViewer is een Windows-desktopapplicatie (C# / WPF, .NET 8) waarmee een
gebruiker snel door de mappenstructuur van een schijf kan navigeren en in één
oogopslag ziet waar SVG-bestanden staan. Mappen met SVG's worden gemarkeerd,
previews worden getoond in een instelbare grootte, en met een dubbelklik opent
het bestand in de gekoppelde editor (bijv. Inkscape). De applicatie is volledig
meertalig (Nederlands, Engels, Duits).

## 2. Doelen

- Snel visueel terugvinden van SVG-bestanden op een schijf.
- Mappen die SVG's bevatten duidelijk markeren in een boomstructuur.
- Alleen relevante bestanden tonen: uitsluitend `.svg`.
- Previews met instelbare grootte (Large / Medium / Small / Only details).
- Openen/bewerken van een SVG met de standaard gekoppelde applicatie.
- Meertalige UI én documentatie (NL default, EN, DE).

## 3. Niet-doelen (voor nu)

- Geen ingebouwde SVG-editor (bewerken gebeurt in de externe app).
- Geen conversie naar andere formaten.
- Geen netwerk-/clouddrives-specifieke features (lokale/aangekoppelde drives).
- Geen macOS/Linux-versie (WPF is Windows-only).

## 4. Functionele eisen

| # | Eis | Sub-epic |
|---|-----|----------|
| F1 | Selecteer een drive uit de beschikbare drives | SE-2 |
| F2 | TreeView van de gekozen drive; door mappen klikken | SE-2 |
| F3 | Mappen met SVG's worden gemarkeerd | SE-2 |
| F4 | Filter bovenaan: volledige structuur óf alleen mappen met SVG's | SE-2 |
| F5 | Preview van SVG's in de geselecteerde map | SE-3 |
| F6 | Dropdown grootte: Large / Medium / Small / Only details | SE-3 |
| F7 | Alleen `.svg` tonen, overige bestanden verbergen | SE-3 |
| F8 | Dubbelklik op SVG opent gekoppelde editor | SE-4 |
| F9 | Meertalig NL/EN/DE via `.resx`, runtime omschakelbaar | SE-5 |
| F10 | Voorkeuren (taal, previewgrootte) worden bewaard | SE-5 |
| F11 | In-app help in de gekozen taal | SE-6 |
| F12 | README (EN) + user guides (NL/EN/DE) met screenshots | SE-6 |
| F13 | Optioneel Syncfusion; licentie uit `syncfusionlicense.txt` | SE-1 |

## 5. Architectuur & technologie

- **Framework:** .NET 8 (`net8.0-windows`), WPF, C#.
- **Patroon:** MVVM met `CommunityToolkit.Mvvm` (source generators voor
  `ObservableProperty` / `RelayCommand`).
- **SVG-rendering:** `SharpVectors.Reloaded` — rendert SVG naar WPF
  `DrawingImage`/`DrawingGroup`, ideaal voor thumbnails.
- **UI-componenten:** Syncfusion WPF (o.a. `SfTreeView`) indien licentie
  aanwezig; anders val terug op de standaard WPF `TreeView`.
- **Lokalisatie:** satelliet-assemblies uit `.resx` (`Strings.resx` = NL default,
  `Strings.en.resx`, `Strings.de.resx`) + runtime `CultureInfo`-switch.
- **Instellingen:** JSON in `%AppData%\SVGViewer\settings.json`.
- **Bestandssysteem:** asynchroon scannen; UI blijft responsief bij grote mappen.

## 6. UI-ontwerp (globaal)

```
+------------------------------------------------------------------+
| [Drive v]   Filter: (o) Alles ( ) Alleen SVG   Grootte:[Large v] |  <- toolbar
|             Taal:[NL v]                              [? Help]     |
+---------------------+--------------------------------------------+
|  TreeView           |  Preview-grid (WrapPanel met thumbnails)   |
|  C:\                |  +-------+  +-------+  +-------+            |
|   ├ Projecten  *    |  | svg   |  | svg   |  | svg   |            |
|   │  └ Iconen   *   |  |thumb  |  |thumb  |  |thumb  |            |
|   └ Documenten      |  | naam  |  | naam  |  | naam  |            |
|  (* = bevat SVG's)  |  +-------+  +-------+  +-------+            |
+---------------------+--------------------------------------------+
| Statusbar: pad · aantal SVG's · geselecteerde taal               |
+------------------------------------------------------------------+
```

## 7. Lokalisatie-aanpak

- Alle zichtbare teksten via `Strings.*.resx`; geen hardcoded strings.
- Standaardtaal (neutraal) = Nederlands.
- Taalkeuze in de toolbar wisselt `Thread.CurrentUICulture` at runtime en
  herlaadt gebonden teksten (via een `LocalizationManager` met `INotifyPropertyChanged`).
- Documentatie (`UserGuide.<taal>.md`) wordt in-app geopend op basis van de
  actieve taal.

## 8. Documentatie-aanpak

- `README.md` (Engels) in de repo-root, met screenshots uit `docs/images/`.
- `docs/user-guide/UserGuide.nl|en|de.md` — eindgebruikershandleiding per taal.
- `docs/Epic.md` (dit document) en `docs/UserStories.md` — werkverdeling.
- Alle MD-docs worden per milestone gekopieerd naar de Obsidian-vault:
  `...\Obsidian\Development\HNSoftwareDevelopment\SVGViewer\`
  via `build\sync-obsidian.ps1`.

## 9. Repo- & commit-workflow

- Repo: https://github.com/hnsoftwaredevelopment/SVGViewer
- Branch: `main`.
- Per user story of bereikte milestone: commit + push.
- `syncfusionlicense.txt` staat in `.gitignore` en wordt nooit gecommit.
- Conventionele commit-berichten: `feat:`, `docs:`, `chore:`, `fix:`.

## 10. Risico's & mitigatie

| Risico | Mitigatie |
|--------|-----------|
| Grote mappen → trage scan | Async scannen, lazy-loading van tree-nodes |
| Complexe SVG's → trage render | Thumbnails cachen, render op achtergrond-thread |
| Syncfusion-licentie ontbreekt | Nette fallback naar standaard WPF-controls |
| Preview-SDK (.NET 10) op machine | `global.json` pint op .NET 8 |

## 11. Werkverdeling

Zie [`UserStories.md`](./UserStories.md) voor de sub-epics en user stories.
