# Architectuur — SVGViewer

Aanvulling op de [Epic](./Epic.md); beschrijft de code zoals die nu is.
Laatst bijgewerkt: milestone 2.

## Projectstructuur

```
SVGViewer/
├─ global.json                  .NET 8 SDK pin
├─ SVGViewer.sln
├─ build/
│  ├─ sync-obsidian.ps1         MD-docs naar de Obsidian-vault
│  └─ make-testdata.ps1         testmappen met SVG's genereren
├─ docs/                        Epic, work-breakdown, architectuur, handleidingen
└─ src/SVGViewer/
   ├─ App.xaml(.cs)             opstart: licentie, instellingen, taal
   ├─ MainWindow.xaml(.cs)      UI-shell (toolbar / tree / preview / statusbar)
   ├─ Localization/Loc.cs       resourcetoegang + runtime taalwissel
   ├─ Models/                   AppSettings, PreviewSize, FolderFilterMode
   ├─ Resources/                Strings.resx (NL), .en.resx, .de.resx
   ├─ Services/
   │  ├─ LicenseManager.cs      Syncfusion-key inlezen/registreren
   │  ├─ SettingsService.cs     voorkeuren als JSON in %AppData%
   │  ├─ DirectoryScanner.cs    veilige bestandssysteem-primitieven
   │  └─ SvgIndexService.cs     asynchrone drive-scan naar SVG-mappen
   └─ ViewModels/
      ├─ MainViewModel.cs       toolbar-status, tree opbouwen, commands
      ├─ DirectoryNodeViewModel.cs  één map, lazy children
      └─ Choices.cs             dropdown-items die taalwissel volgen
```

## Belangrijke ontwerpbeslissingen

### 1. Standaard WPF `TreeView` in plaats van Syncfusion

Syncfusion-licentiekeys zijn **versiegebonden**: een key die niet bij de
geïnstalleerde versie past, geeft bij elke start een trial-dialoog. De
benodigde functionaliteit (lazy-loading, markeren, filteren, templating) zit
volledig in de standaard `TreeView`. Daarom minder afhankelijkheden en geen
versiekoppeling.

`LicenseManager` is er wél en registreert de key via **reflectie**. Zodra een
Syncfusion-package wordt toegevoegd, werkt de registratie zonder codewijziging;
zonder package is het een no-op. Zo blijft de keuze omkeerbaar.

### 2. Twee filtermodi, twee scanstrategieën

| Modus | Strategie | Kosten |
|-------|-----------|--------|
| Volledige structuur | Lazy: submappen pas laden bij uitklappen | direct zichtbaar |
| Alleen mappen met SVG | Volledige drive-scan vooraf (async) | seconden tot minuten |

De gefilterde modus *moet* de hele schijf doorlopen: om een diepe map met SVG's
te tonen, moeten ook alle bovenliggende mappen zichtbaar blijven. Daarom houdt
`SvgFolderIndex` twee verzamelingen bij:

- `FoldersWithSvg` — mappen die zelf SVG's bevatten (deze worden gemarkeerd);
- `RelevantFolders` — die mappen **plus alle ouders**, zodat het pad ernaartoe
  zichtbaar blijft.

De scan is iteratief (met een `Stack`), niet recursief, zodat een diepe
mappenstructuur geen stack overflow kan geven. Hij draait op een achtergrond-
thread, rapporteert voortgang en is te annuleren.

### 3. Robuust tegen het bestandssysteem

Een volledige schijf bevat altijd mappen die niet leesbaar zijn. `DirectoryScanner`
vangt daarom per map fouten af en slaat over in plaats van te crashen. Verder:

- **reparse points** (junctions/symlinks) worden overgeslagen — die kunnen
  oneindige lussen veroorzaken;
- system- en hidden-mappen en bekende ruis (`$Recycle.Bin`,
  `System Volume Information`) worden genegeerd.

### 4. Lokalisatie zonder herstart

`Loc` is een singleton met een indexer, waar XAML op bindt:

```xml
Text="{Binding Source={x:Static loc:Loc.Instance}, Path=[LabelDrive]}"
```

`Loc.SetCulture()` wijzigt de UI-cultuur en vuurt `PropertyChanged` voor
`"Item[]"`. WPF beschouwt daarmee álle indexer-bindings als verouderd en
herlaadt ze — de taal wisselt dus direct, zonder herstart.

Nederlands is de **neutrale** taal (`NeutralResourcesLanguage`), dus die zit in
de hoofdassembly; Engels en Duits komen als satelliet-assemblies (`en\`, `de\`).

Teksten die in code worden samengesteld (zoals de statusregel) worden bewaard
als resourcekey + argumenten, zodat ze na een taalwissel opnieuw opgebouwd
kunnen worden in de nieuwe taal.

### 5. Waarom er één stukje code-behind is

`TreeView.SelectedItem` is read-only en dus niet bindbaar. De selectie wordt
daarom in `MainWindow.xaml.cs` doorgegeven aan de ViewModel. Dat is de enige
code-behind in het venster; al het andere gedrag zit in de ViewModels.

## Verificatie

De logica wordt gedekt door een xUnit-testproject (`tests/SVGViewer.Tests`), te
draaien met `dotnet test`. De tests maken hun eigen mappenstructuur aan in een
tijdelijke map en ruimen die weer op, dus ze hebben geen externe testdata nodig.

Gedekt: alleen `.svg` wordt geteld, markering van SVG-mappen inclusief het
relevant houden van oudermappen, beide filtermodi, onleesbare paden, annulering,
instellingen (inclusief corrupt bestand), alle drie de talen, en SVG-rendering.

Twee dingen die de tests bewust vastleggen omdat ze anders stil kapot gaan:

- **Rendering buiten de UI-thread.** De tests draaien zonder Dispatcher; als
  SharpVectors een UI-thread zou vereisen, falen ze meteen. Ze controleren ook
  dat elke image `IsFrozen` is — zonder freeze crasht de UI-thread erop.
- **Resourcedekking.** `build/check-resources.ps1` controleert dat nl/en/de
  dezelfde keys hebben en dat elke key die in XAML of C# wordt gebruikt bestaat.
  Ontbrekende keys vallen anders pas op als `!Key!` in de UI.

Bekend aandachtspunt: WPF-applicaties starten niet in een omgeving waar de
omgevingsvariabele `windir` ontbreekt (dan faalt WPF's interne font-cache op een
ongeldige URI). Dit raakt alleen niet-interactieve buildomgevingen, niet normaal
gebruik.

## SVG-previews (SE-3)

`SvgThumbnailService` rendert met SharpVectors naar een `DrawingImage`. Omdat dat
een **vector** is, volstaat één render voor alle previewgroottes: WPF schaalt hem
zonder kwaliteitsverlies. De previewgrootte bepaalt dus alleen de afmeting van de
`Image` in XAML, niet wat er gerenderd wordt.

- De cachesleutel is pad + wijzigingsdatum, dus een bewerkt bestand wordt
  automatisch opnieuw gerenderd.
- Parallellisme is begrensd op de helft van de processorkernen, zodat een map met
  honderden bestanden de machine niet plat legt.
- Bestandsdetails (naam, grootte, datum) worden direct gelezen en de lijst wordt
  meteen getoond; thumbnails druppelen daarna binnen. Een map voelt dus
  onmiddellijk responsief.
- Een onleesbare of kapotte SVG levert `null` op en wordt in de UI als
  "kan niet worden weergegeven" getoond in plaats van de app te laten crashen.
- In modus *Only details* wordt er niets gerenderd; er is dan alleen een lijst
  met naam, grootte en wijzigingsdatum.

### Zoombare viewer

Een klik op een thumbnail opent `Views/SvgZoomViewer`, een overlay over het hele
venster. Muiswiel zoomt rond de cursor, slepen verschuift, en er zijn knoppen
voor in-/uitzoomen, werkelijke grootte, passend en sluiten (ook Esc). Omdat de
bron een vector-`DrawingImage` is, blijft elk zoomniveau perfect scherp. Dit is
UI-gedrag zonder domeinlogica, dus het zit bewust in code-behind van het
UserControl in plaats van in een ViewModel.

### Padnormalisatie (belangrijke valkuil)

Een drive-root komt binnen als `C:\`. Wie daar de trailing backslash afknipt,
houdt `C:` over — en op Windows betekent `C:` *"de huidige map op schijf C:"*,
niet de root. Het gevolg was dat de tree de werkmap van de app toonde
(`bin\Debug\...`) in plaats van de schijf-root. `DirectoryScanner.NormalizeFolderPath`
lost dit centraal op: het verwijdert trailing separators, behalve bij een
drive-root, die juist `C:\` moet blijven. Alle padvergelijkingen en enumeraties
lopen via deze helper. Regressietests leggen het gedrag vast.

## Nog te doen

Zie [`UserStories.md`](./UserStories.md). Eerstvolgend: SE-4 (dubbelklik naar de
gekoppelde editor) en SE-6 (documentatie met screenshots, in-app help).
