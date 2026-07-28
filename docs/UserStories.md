# Work Breakdown — SVGViewer

Werkverdeling van de [Epic](./Epic.md) in sub-epics en user stories.
Elke user story is een eenheid van werk die apart wordt gecommit/gepusht.

Status-legenda: ☐ = todo · ◐ = bezig · ☑ = klaar

---

## SE-1 · Project & Infrastructuur

- ☑ **US-1.1** — Als ontwikkelaar wil ik een WPF-solution (.NET 8) met MVVM-opzet,
  zodat we een schone basis hebben. *(AC: solution buildt, `net8.0-windows`.)*
- ☑ **US-1.2** — Als ontwikkelaar wil ik een `.gitignore` die `syncfusionlicense.txt`
  uitsluit, zodat de licentie nooit gecommit wordt.
- ☑ **US-1.3** — Als ontwikkelaar wil ik Epic + work-breakdown in MD, zodat scope
  en planning vastliggen.
- ☑ **US-1.4** — Als ontwikkelaar wil ik een Obsidian-sync-script, zodat MD-docs
  automatisch in de vault verschijnen.
- ☑ **US-1.5** — Als ontwikkelaar wil ik een `LicenseManager` die de Syncfusion-key
  uit `syncfusionlicense.txt` leest en registreert (no-op als afwezig).
- ☑ **US-1.6** — Als ontwikkelaar wil ik een `SettingsService` (JSON in AppData)
  voor taal en previewgrootte.
- ☑ **US-1.7** — Als ontwikkelaar wil ik geautomatiseerde tests, zodat regressies
  zichtbaar worden bij elke wijziging. *(AC: `dotnet test` slaagt; dekking op
  bestandssysteem-scan, index, instellingen, lokalisatie en SVG-rendering.)*
- ☑ **US-1.8** — Als ontwikkelaar wil ik een controle op de resourcebestanden,
  zodat ontbrekende vertalingen niet pas in de UI als `!Key!` opvallen.

## SE-2 · Directory-navigatie

- ☑ **US-2.1** — Als gebruiker wil ik een drive kunnen kiezen uit een dropdown.
- ☑ **US-2.2** — Als gebruiker wil ik een TreeView van de gekozen drive en door
  mappen kunnen klikken (lazy-loading van submappen).
- ☑ **US-2.3** — Als gebruiker wil ik dat mappen met SVG's gemarkeerd worden
  (icoon/kleur/badge), zodat ik ze snel vind.
- ☑ **US-2.4** — Als gebruiker wil ik bovenaan kunnen kiezen: volledige structuur
  óf alleen mappen die SVG's bevatten.
- ☑ **US-2.5** — Als gebruiker wil ik dat het scannen asynchroon gebeurt, zodat de
  UI responsief blijft.

## SE-3 · SVG-preview

- ☑ **US-3.1** — Als gebruiker wil ik previews (thumbnails) zien van alle SVG's in
  de geselecteerde map.
- ☑ **US-3.2** — Als gebruiker wil ik de previewgrootte kiezen: Large / Medium /
  Small / Only details.
- ☑ **US-3.3** — Als gebruiker wil ik in "Only details" een lijst met naam, grootte
  en wijzigingsdatum in plaats van thumbnails.
- ☑ **US-3.4** — Als gebruiker wil ik dat uitsluitend `.svg`-bestanden getoond
  worden; alle andere bestanden blijven verborgen.
- ☑ **US-3.5** — Als ontwikkelaar wil ik thumbnails cachen voor snelheid.
  *(Vector-render, dus één render dient alle groottes; cache verloopt op
  wijzigingsdatum.)*
- ☑ **US-3.6** — Als gebruiker wil ik een preview groot kunnen zien in een popup
  die ik kan in- en uitzoomen. *(Geopend met **dubbelklik**; zoombare overlay met
  muiswiel, knoppen en slepen; blijft messcherp omdat het een vector is.)*
- ☑ **US-3.7** — Als gebruiker wil ik dat een drive-root (bijv. `C:\`) daadwerkelijk
  de root toont en niet de werkmap van de app. *(Regressie: `C:\` mag niet tot
  `C:` worden verkort; `NormalizeFolderPath` + 9 tests.)*

## SE-4 · SVG openen in editor

- ☑ **US-4.1** — Als gebruiker wil ik een SVG openen in de gekoppelde applicatie
  (bijv. Inkscape) via de Windows-shell. *(Via het contextmenu "Openen"; zie AD-2
  voor waarom dit niet meer op dubbelklik zit.)*
- ☑ **US-4.2** — Als gebruiker wil ik een nette melding als er geen app gekoppeld is.
- ☑ **US-4.3** — Als gebruiker wil ik via rechtermuisknop "Openen met…" / "Tonen in
  Verkenner" als extra opties.

## SE-5 · Meertaligheid

- ☑ **US-5.1** — Als ontwikkelaar wil ik `.resx`-resources: NL (default), EN, DE.
  *(Vooruitgetrokken naar M2 om herwerk van hardcoded teksten te voorkomen.)*
- ☑ **US-5.2** — Als gebruiker wil ik de taal in de toolbar wisselen; de UI werkt
  direct bij zonder herstart.
- ☑ **US-5.3** — Als gebruiker wil ik dat mijn taalkeuze bewaard blijft.
- ☑ **US-5.4** — Als ontwikkelaar wil ik dat er geen hardcoded UI-teksten zijn.

## SE-6 · Documentatie

- ☐ **US-6.1** — Als lezer wil ik een Engelse `README.md` met screenshots en een
  beknopte beschrijving.
- ☐ **US-6.2** — Als gebruiker wil ik user guides in NL/EN/DE met screenshots.
- ☐ **US-6.3** — Als gebruiker wil ik de help vanuit de app openen in mijn taal.
- ☐ **US-6.4** — Als team wil ik dat alle MD-docs in de Obsidian-vault staan.

## SE-7 · Afwerking

- ☐ **US-7.1** — App-icoon en over-dialoog.
- ☐ **US-7.2** — Foutafhandeling & logging.
- ☐ **US-7.3** — Handmatige testronde over alle features/talen.
- ☐ **US-7.4** — Echte screenshots vastleggen en in docs verwerken.

## SE-8 · Bestandsbeheer (backlog, nog niet ingepland)

Uit gebruik blijkt behoefte aan minimaal bestandsbeheer binnen de viewer.
Zie [AD-3](./Epic.md#ad-3--conflictafhandeling-bij-bestandsbewerkingen) voor de
gekozen aanpak bij naamconflicten en bevestigingen.

- ☐ **US-8.1** — Als gebruiker wil ik een SVG met slepen naar een andere map
  verplaatsen. *(Hiervoor is de enkele klik/drag gereserveerd.)*
- ☐ **US-8.2** — Als gebruiker wil ik via het contextmenu een SVG verwijderen,
  met bevestiging en bij voorkeur naar de prullenbak. De bevestiging heeft een
  "Niet meer tonen"-optie; weer inschakelbaar via Instellingen (SE-9).
- ☐ **US-8.3** — Als gebruiker wil ik via het contextmenu een SVG kopiëren
  (en plakken in een andere map).
- ☐ **US-8.4** — Als gebruiker wil ik via het contextmenu een SVG hernoemen.
- ☐ **US-8.5** — Als gebruiker wil ik een nieuwe map kunnen aanmaken.
- ☐ **US-8.6** — Als gebruiker wil ik dat de tree en preview zich verversen na een
  bestandsbewerking.
- ☐ **US-8.7** — Als gebruiker wil ik bij één bestand dat al bestaat, **elke keer
  opnieuw** gevraagd worden. *(Geen persistente "altijd overschrijven"; standaard
  is steeds vragen.)*
- ☐ **US-8.8** — Als gebruiker wil ik bij het verplaatsen/kopiëren van **meerdere**
  bestanden, per conflict de keuze: **Bestand overschrijven** · **Alle bestanden
  overschrijven** · **Dit bestand overslaan** · **Alle bestanden overslaan**.
  *(Volledige tekst voor duidelijkheid; knoplabels mogen korter met de volledige
  tekst als tooltip. "Alle …" geldt alleen binnen de lopende operatie, niet
  persistent.)*
- ☐ **US-8.9** — Als gebruiker wil ik meerdere bestanden kunnen selecteren voor
  verplaatsen/kopiëren/verwijderen. *(Voorwaarde voor US-8.8.)*

## SE-9 · Instellingen

- ☑ **US-9.1** — Als gebruiker wil ik een "Instellingen"-scherm waarin mijn
  voorkeuren bij elkaar staan. *(Modale dialoog via de knop "Instellingen".)*
- ☑ **US-9.2** — Als gebruiker wil ik mijn taal in Instellingen kiezen.
  *(Besluit: taalkeuze is **uit de toolbar gehaald** en staat nu alleen in
  Instellingen.)*
- ☑ **US-9.3** — Als gebruiker wil ik de verwijder-bevestiging aan/uit kunnen
  zetten. *(Bewaarde voorkeur `ConfirmBeforeDelete`; wordt door SE-8 gebruikt.)*
- ☑ **US-9.4** — Als gebruiker wil ik dat instellingen bewaard blijven.
  *(Via de bestaande `SettingsService`.)*

---

## Milestone-overzicht

| Milestone | Sub-epics | Commit-doel |
|-----------|-----------|-------------|
| M1 | SE-1 (deels), SE-6 (deels) | Skeleton + docs + repo-init |
| M2 | SE-2 | Directory-navigatie |
| M3 | SE-3 | SVG-preview + zoom-popup |
| M4 | SE-4 | SVG openen in editor |
| M5 | SE-5 | Meertaligheid (grotendeels in M2 gedaan) |
| M6 | SE-6, SE-7 | Documentatie compleet + afwerking |
| M7 | SE-9 | Instellingen-menu (taal + bevestigingen) |
| M8 | SE-8 | Bestandsbeheer |
