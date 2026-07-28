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
| F13 | Licentiesleutel nooit in de repo (zie AD-1) | SE-1 |

## 5. Architectuur & technologie

- **Framework:** .NET 8 (`net8.0-windows`), WPF, C#.
- **Patroon:** MVVM met `CommunityToolkit.Mvvm` (source generators voor
  `ObservableProperty` / `RelayCommand`).
- **SVG-rendering:** `SharpVectors.Reloaded` — rendert SVG naar WPF
  `DrawingImage`/`DrawingGroup`, ideaal voor thumbnails.
- **UI-componenten:** standaard WPF-controls (zie besluit AD-1).
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

## 11. Besluiten (ADR)

### AD-1 · Standaard WPF-controls i.p.v. Syncfusion
**Datum:** milestone 2 · **Status:** aangenomen

Er is een Syncfusion-licentie beschikbaar (versie **34.x**), maar voor de
functionaliteit van deze applicatie biedt Syncfusion geen meerwaarde:

- Lazy-loading, `HierarchicalDataTemplate` en markeren via `DataTrigger`s zijn
  volledig te realiseren met de standaard WPF `TreeView`.
- Minder externe afhankelijkheden en geen licentie nodig om te kunnen bouwen.
- De applicatie draait bij elke gebruiker, ook zonder licentiebestand.

**Gevolg:** de app gebruikt standaard WPF-controls. `syncfusionlicense.txt`
blijft in `.gitignore` staan zodat een aanwezige sleutel nooit in de repo
terechtkomt.

**Herziening:** mocht een specifiek onderdeel (bijv. een virtualiserende
thumbnail-grid in SE-3) meetbaar beter zijn met Syncfusion, dan wordt een
`LicenseManager` toegevoegd die de sleutel uit `syncfusionlicense.txt` leest.
Gebruik in dat geval Syncfusion WPF **34.x**-pakketten, passend bij de licentie.

### AD-2 · Dubbelklik opent de preview, niet de editor
**Datum:** milestone 4 (herzien) · **Status:** aangenomen

De oorspronkelijke eis F8 zei: dubbelklik opent de gekoppelde editor. Bij gebruik
bleek een andere indeling prettiger, en de gebruiker heeft die gevraagd:

- **Dubbelklik** op een thumbnail of lijstregel → opent de **zoom-preview**.
- **Openen in de editor** (en "Openen met…", "Tonen in Verkenner") → via het
  **contextmenu** (rechtermuisknop).
- **Enkele klik** → bewust vrijgelaten, gereserveerd voor toekomstig
  bestandsbeheer (slepen om te verplaatsen, selecteren) — zie SE-8.

**Reden:** één klik plus dubbelklik op hetzelfde element vereiste een uitgestelde
enkele-klik met de dubbelkliktijd, wat de preview traag liet aanvoelen en in de
praktijk niet betrouwbaar opende. Dubbelklik = preview is direct en voorspelbaar;
editor-acties zijn expliciet in het contextmenu.

### AD-3 · Conflictafhandeling bij bestandsbewerkingen
**Datum:** milestone 4 (vooruit vastgelegd) · **Status:** aangenomen · **Scope:** SE-8/SE-9

Voor het toekomstige bestandsbeheer geldt:

- **Eén bestand bestaat al** → **altijd opnieuw vragen**. Er komt bewust géén
  persistente "altijd overschrijven"-instelling; overschrijven is per keer een
  expliciete keuze.
- **Meerdere bestanden verplaatsen/kopiëren** → per conflict vier keuzes:
  *Bestand overschrijven*, *Alle bestanden overschrijven*, *Dit bestand overslaan*,
  *Alle bestanden overslaan*. De "Alle …"-keuzes gelden **alleen binnen die ene
  operatie**, niet persistent. Volledige teksten voor duidelijkheid; knoplabels
  mogen korter met de volledige tekst als tooltip.
- **Verwijderen** → met bevestiging, bij voorkeur naar de prullenbak. Deze
  bevestiging mág wél uitgezet worden ("Niet meer tonen") en is daarna weer aan te
  zetten via het Instellingen-menu (SE-9). Dit verschilt bewust van overschrijven:
  verwijderen naar de prullenbak is terug te draaien, overschrijven niet.

## 12. Werkverdeling

Zie [`UserStories.md`](./UserStories.md) voor de sub-epics en user stories.
Zie [`Architecture.md`](./Architecture.md) voor de technische uitwerking en de
onderbouwing van de gemaakte keuzes.
