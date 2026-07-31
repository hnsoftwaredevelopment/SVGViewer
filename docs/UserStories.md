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

- ☑ **US-6.1** — Als lezer wil ik een Engelse `README.md` met screenshots en een
  beknopte beschrijving. *(README met twee screenshots — hoofdvenster en preview —
  features, tech stack en getting-started.)*
- ☑ **US-6.2** — Als gebruiker wil ik beknopte documentatie per taal (NL/EN/DE).
  *(Bewust een korte **quick reference** i.p.v. een uitgebreide handleiding: de app
  is eenvoudig en zelfverklarend. Zie `docs/user-guide/QuickReference.<taal>.md`,
  ook geopend door de Help-knop.)*
- ☑ **US-6.3** — Als gebruiker wil ik de help vanuit de app openen in mijn taal.
  *(Help-knop opent de quick reference van de actieve taal — nl/en/de, met terugval
  op nl — gerenderd als WPF `FlowDocument` in een klein venster **binnen de app**,
  dus geen browser.)*
- ☐ **US-6.4** — Als team wil ik dat alle MD-docs in de Obsidian-vault staan.

## SE-7 · Afwerking & UI-makeover

### Afwerking
- ☑ **US-7.1** — App-icoon en over-dialoog. *(Icoon in titelbalk/taakbalk +
  `.exe`; startup-splash (WPF `SplashScreen`). Over-dialoog toont het app-logo, de
  versie en een korte beschrijving, met een donkere strook voor het HN-Software-
  logo (`Assets\devicon.svg`, de witte variant).)*
- ☑ **US-7.2** — Foutafhandeling & logging. *(Globale afvang van onverwachte
  fouten (UI-thread, achtergrondtaken, AppDomain) met een nette melding en verwijzing
  naar het logbestand; lichte, dependency-vrije `Logger` naar
  `%AppData%\SVGViewer\logs\app.log` met rotatie bij ~1 MB. Stille catch-blokken
  loggen nu ook.)*
- ☑ **US-7.3** — Handmatige testronde over alle features/talen. *(Uitgevoerd met
  `docs\TestChecklist.nl.md`; gevonden punten opgelost: taalwissel, "Openen met…",
  logging van fouten, witte preview-achtergrond, en de preview als los venster.)*
- ☑ **US-7.4** — Eén algemene screenshot van de app vastgelegd
  (`docs\images\Screenshot01.png`) en in de README verwerkt. *(Verdere screenshots
  niet nodig: de functionaliteit spreekt voor zich; de handleidingen blijven tekst.)*
- ☑ **US-7.13** — Preview opent in een **eigen venster** (popup) i.p.v. een overlay
  in het hoofdvenster. *(De titelbalk-X sluit nu alleen de preview, niet de hele
  app. De zoom-viewer wordt gehost in `SvgZoomWindow`.)*

### UI-makeover (iconen)
- ☑ **US-7.5** — Weergavekeuze (previewgrootte) als Explorer-achtig icoonmenu
  in plaats van een dropdown (Grote / Middelgrote / Kleine iconen · Details).
  *(Segmented control van vier flat toggle-knoppen met vector-iconen; de actieve
  keuze licht op. Bindt via een enum→bool-converter aan de previewgrootte.)*
- ☑ **US-7.6** — Tandwiel-icoon (⚙) voor Instellingen; iconen voor Vernieuwen en
  Annuleren. *(Flat icoon-knoppen met Segoe MDL2-glyphs — ⚙ Instellingen, ↻
  Vernieuwen, ✕ Annuleren, en ook ❓ Help en ℹ Over — met gelokaliseerde tooltips;
  kleuren mee met de accentkleur bij hover/klik.)*
- ☑ **US-7.7** — Talen in het Instellingen-scherm met vlaggen. *(Zelfgetekende
  SVG-vlaggen NL/EN/DE, ingebakken als resource; de taal-ComboBox toont vlag +
  naam via een ItemTemplate.)*

### Navigatie-verbeteringen
- ☑ **US-7.8** — Als gebruiker wil ik dat **bovenliggende** mappen van een
  SVG-map ook blauw gemarkeerd worden (zónder aantal), zodat ik in ingeklapte
  staat direct zie langs welke tak ik SVG's vind. *(Directe SVG-map: blauw + vet +
  aantal; bovenliggende map: blauw, normaal gewicht, geen aantal.)*
- ☑ **US-7.9** — Als gebruiker wil ik dat de mappenmarkeringen **progressief**
  verschijnen tijdens de achtergrondscan: de tree is meteen bruikbaar (lazy) en
  "licht op" naarmate de scan vordert. De statusbar met voortgang blijft.
- ☑ **US-7.10** — Als gebruiker wil ik dat ook de weergave **"Alleen SVG"**
  gaandeweg wordt opgebouwd: zodra een SVG-map wordt gevonden, verschijnt die tak
  (met bovenliggende mappen). Toevoegend, dus zonder geflikker.
- ☑ **US-7.11** — Als gebruiker wil ik dat de viewer altijd in **Volledig**
  opstart, zodat ik meteen structuur zie; de keuze "Alleen SVG" geldt per sessie
  en wordt niet bewaard.
- ☑ **US-7.12** — Als gebruiker wil ik dat er **één keer** wordt gescand per
  schijf, zodat ik tijdens een lopende scan van view kan wisselen (Alles ↔ Alleen
  SVG) **zonder dat de scan opnieuw begint**. *(De scan hoort bij de schijf, niet
  bij de view: één gedeelde index; van filter wisselen herprojecteert alleen. Een
  lopende scan blijft beide views vullen. Alleen bij schijfwissel/Vernieuwen wordt
  opnieuw gescand; Annuleren behoudt het tot dan gevondene.)*

## SE-8 · Bestandsbeheer (in uitvoering)

Uit gebruik blijkt behoefte aan minimaal bestandsbeheer binnen de viewer.
Zie [AD-3](./Epic.md#ad-3--conflictafhandeling-bij-bestandsbewerkingen) voor de
gekozen aanpak bij naamconflicten en bevestigingen.

- ☐ **US-8.1** — Als gebruiker wil ik een SVG met slepen naar een andere map
  verplaatsen. *(Hiervoor is de enkele klik/drag gereserveerd.)*
- ☑ **US-8.2** — Als gebruiker wil ik via het contextmenu een SVG verwijderen,
  met bevestiging en bij voorkeur naar de prullenbak. De bevestiging heeft een
  "Niet meer tonen"-optie; weer inschakelbaar via Instellingen (SE-9).
  *(Contextmenu "Verwijderen" → eigen gelokaliseerde bevestiging met "Niet meer
  vragen" (zet `ConfirmBeforeDelete` uit); verwijdert naar de prullenbak via
  `FileOperationService`; preview en markering/telling verversen meteen.)*
- ☐ **US-8.3** — Als gebruiker wil ik via het contextmenu een SVG kopiëren
  (en plakken in een andere map).
- ☑ **US-8.4** — Als gebruiker wil ik via het contextmenu een SVG hernoemen.
  *(Contextmenu "Hernoemen" → dialoog met de nieuwe naam (extensie blijft behouden);
  bestaat de doelnaam al dan wordt **elke keer** om overschrijven gevraagd (US-8.7);
  daarna ververst de preview. Ongeldige namen worden netjes gemeld.)*
- ☑ **US-8.5** — Als gebruiker wil ik een nieuwe map kunnen aanmaken.
  *(Rechtsklik op een map in de boom → "Nieuwe map" → naamdialoog; de submap wordt
  aangemaakt via `FileOperationService.CreateFolder` en de boom klapt open zodat de
  nieuwe map zichtbaar is. Bestaat de naam al of is 'ie ongeldig, dan een nette
  melding. In "Alleen SVG" verschijnt een lege nieuwe map pas als er SVG's in staan.)*
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
