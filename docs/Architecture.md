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

## Verificatie bij milestone 2

De logica is getest met een tijdelijk testharnas tegen een gegenereerde
mappenstructuur (`build/make-testdata.ps1`). Alle 30 controles slaagden:

- alleen `.svg` wordt geteld (`.txt`/`.png` worden genegeerd);
- markering van mappen met SVG's, inclusief het relevant maken van ouders;
- beide filtermodi tonen precies de juiste mappen;
- alle drie de talen resolven en wisselen correct;
- instellingen overleven een schrijf/lees-ronde.

Bekend aandachtspunt: WPF-applicaties starten niet in een omgeving waar de
omgevingsvariabele `windir` ontbreekt (dan faalt WPF's interne font-cache op een
ongeldige URI). Dit raakt alleen niet-interactieve buildomgevingen, niet normaal
gebruik.

## Nog te doen

Zie [`UserStories.md`](./UserStories.md). Eerstvolgend: SE-3 (SVG-previews met
instelbare grootte) en SE-4 (dubbelklik naar de gekoppelde editor).
