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

## Bestanden openen (SE-4)

`FileOpenService` biedt drie acties, elk via de Windows-shell en elk faalt veilig
met een `FileActionOutcome` in plaats van een exception:

- **Openen in gekoppelde app** — `Process.Start` met `UseShellExecute`. Ontbreekt
  er een koppeling, dan gooit Windows `Win32Exception` met code 1155
  (`ERROR_NO_ASSOCIATION`); die wordt vertaald naar `NoAssociation` en de
  gebruiker krijgt een nette melding met de suggestie "Openen met…".
- **Openen met…** — dezelfde start met verb `openas`, wat de Windows-kiezer toont.
- **Tonen in Verkenner** — `explorer.exe /select,"<pad>"`.

Het starten van processen zit achter `IShellLauncher`, zodat de beslissingslogica
(welke actie, welke argumenten, hoe fouten worden gemapt) getest wordt met een
nep-launcher, zonder dat er echte programma's opstarten.

### Interactie: enkele vs. dubbele klik

**Dubbelklik** op een thumbnail of detailregel opent de zoom-preview. In de
detailweergave is nog geen thumbnail gerenderd, dus die wordt bij dubbelklik
alsnog op dat moment gerenderd. **Openen in de editor** en de overige
bestandsacties zitten in het **contextmenu** (rechtermuisknop). De **enkele klik**
is bewust vrijgelaten voor toekomstig bestandsbeheer (slepen/selecteren, SE-8).
Zie [AD-2](./Epic.md#ad-2--dubbelklik-opent-de-preview-niet-de-editor) voor de
onderbouwing. Dit is puur UI-gedrag en zit daarom in de code-behind, niet in een
ViewModel.

## Mappenmarkering & progressieve scan (SE-7)

De tree is in "Alles"-modus meteen zichtbaar en laadt lazy (alleen uitgeklapte
mappen worden echt aangemaakt). Tegelijk draait een achtergrondscan die een
`SvgFolderIndex` vult. Die index is **thread-safe** (concurrent dictionaries),
zodat de UI-thread markeringen kan lezen terwijl de scanthread nog schrijft.

Twee soorten markering:

- **Directe SVG-map** — blauw, vet, met aantal. De telling komt direct van schijf
  bij het aanmaken van de node, dus een map met SVG's is meteen gemarkeerd zodra
  hij verschijnt (geen wachten op de scan).
- **Bovenliggende map** (`IsAncestorOfSvg`) — blauw, normaal gewicht, zonder
  aantal. Dit betekent "leidt naar SVG's dieper in de boom" en komt uit de index;
  het vult zich **progressief** terwijl de scan vordert.

De markeringen worden **pull-based** bijgewerkt: de scan bouwt geen tree-knopen
(200k knopen live bouwen zou te duur zijn en flikkeren), maar vult alleen de
index. Op scanvoortgang en bij voltooiing loopt `RefreshMarkings()` over de reeds
gerealiseerde (uitgeklapte) knopen en herberekent hun ancestor-status — goedkoop,
want alleen zichtbare knopen worden geraakt. De repaint is gethrottled (~elke
400 ms) zodat het vloeiend blijft. De statusbar toont ondertussen de voortgang en
de scan is annuleerbaar; bij een nieuwe schijf-/filterkeuze wordt de lopende scan
netjes afgebroken.

### "Alleen SVG": progressief en toevoegend

Ook de gefilterde weergave bouwt nu mee tijdens de scan. Omdat relevantie alleen
maar aangroeit (een map die naar een SVG leidt blijft dat), is de opbouw puur
**toevoegend**: zodra de scan een SVG-map ontdekt, zorgt `SvgOnlyTreeBuilder` dat
het pad van de root naar die map bestaat (ontbrekende tussenmappen worden
aangemaakt en gesorteerd ingevoegd). Er wordt nooit iets weggehaald of herschikt,
dus geen geflikker. De schijf-root is meteen zichtbaar zodat het paneel nooit leeg
oogt. Deze knopen worden expliciet gebouwd (geen lazy placeholders) en halen hun
telling uit de index in plaats van van schijf, zodat er geen schijf-I/O op de
UI-thread plaatsvindt tijdens het invoegen.

De viewer start altijd in **Volledig**, zodat je meteen structuur ziet; "Alleen
SVG" is een keuze per sessie en wordt niet bewaard.

## Instellingen (SE-9)

De taalkeuze is uit de toolbar gehaald en zit nu in een modaal
**Instellingen**-scherm (`Views/SettingsWindow`), samen met de aan/uit-schakelaar
voor de verwijder-bevestiging (`ConfirmBeforeDelete`, gebruikt door SE-8).

`SettingsViewModel` past elke wijziging direct toe en bewaart die via
`SettingsService`. Omdat de taalwissel nu buiten het hoofdvenster gebeurt, luistert
`MainViewModel` naar `Loc.CultureChanged` om zelf-samengestelde teksten
(statusregel, bestandsgroottes/datums) opnieuw op te bouwen; de XAML-bindingen
verversen al via het bestaande `Item[]`-signaal van `Loc`.

## Nog te doen

Zie [`UserStories.md`](./UserStories.md). Eerstvolgend: SE-6 — documentatie met
echte screenshots, in-app help in de gekozen taal, en de afwerking (SE-7).


## In-app help (SE-6)

De Help-knop in de toolbar opent de **quick reference** van de **actieve taal** in
een klein **venster binnen de app** (`Views/HelpWindow`) — geen browser, zodat de
gebruiker de app niet verlaat. De drie bestanden
(`docs/user-guide/QuickReference.<taal>.md`) worden als inhoud meegeleverd naast de
`.exe` (map `Help/`). `HelpService` kiest het bestand voor de huidige taal (met
terugval op nl) en leest de Markdown. `MarkdownParser` (WPF-vrij, dus goed te
testen) parseert de kleine Markdown-subset — koppen, alinea's, opsommingen en
inline `**vet**`, `*cursief*` en `` `code` `` — naar een klein model, en
`MarkdownToFlowDocument` rendert dat model naar een WPF `FlowDocument` dat in een
`FlowDocumentScrollViewer` wordt getoond. De titelbalk-X, Esc en de Sluiten-knop
sluiten alleen dit venster.

## App-icoon (SE-7)

Het venster- en taakbalkicoon worden bij het starten geladen uit
`Assets\appicon.ico` (via `Content` meegekopieerd naar de uitvoermap); ontbreekt
het bestand, dan valt de app terug op het standaardicoon. Het `.exe`-icoon
(`<ApplicationIcon>`) wordt **conditioneel** meegebouwd (`Condition="Exists(...)"`),
zodat de build blijft werken zolang de illustratie nog niet in `Assets\` staat en
automatisch activeert zodra dat wel zo is.


## Over-dialoog (SE-7)

De over-dialoog (`Views/AboutWindow`) toont het applicatielogo, het versienummer
(uit de assembly) en een korte beschrijving, plus een donkere strook met het
HN-Software-logo. Beide logo's zijn ingebakken SVG-resources die via
`SvgResourceImage` (SharpVectors → `DrawingImage`) worden geladen; ontbreekt het
publisher-logo, dan verbergt de dialoog die strook netjes. De dialoog opent modaal
via de knop **Over** in de toolbar. Alle teksten zijn gelokaliseerd (nl/en/de).


### Titelbalk-icoon op donkere thema's

De titelbalk en de taakbalk delen normaal één icoon (`Window.Icon`); Windows tekent
het alleen kleiner in de titelbalk. Op een donker, thema-gekleurd titelbalk valt een
transparant logo weg. Daarom overschrijft `TitleBarIconFixer` na
`OnSourceInitialized` alléén het **kleine** icoon via `WM_SETICON` (ICON_SMALL): het
app-logo op een witte achtergrond. Het **grote** icoon (taakbalk/Alt-Tab) blijft het
transparante `Window.Icon`. De native icon-handle wordt bij het sluiten opgeruimd.


## Weergavekeuze als icoonmenu (SE-7)

De previewgrootte kiest de gebruiker via een Explorer-achtige *segmented control*
in de toolbar: vier flat toggle-knoppen (RadioButtons met een eigen template) met
vector-iconen — één groot vlak (Grote iconen), 2×2 (Middelgrote), 3×3 (Kleine) en
drie balken (Details). De actieve knop licht op in de accentkleur. Ze binden
two-way via `EnumToBooleanConverter` aan de nieuwe `PreviewSize`-enum-property op
`MainViewModel`, die de bestaande gelokaliseerde keuze (`SelectedPreviewSize`)
aanstuurt. De icoonkleur volgt de `Foreground` van de knop, zodat de actieve staat
vanzelf meekleurt.


## Toolbar-iconen (SE-7)

Vernieuwen, Annuleren, Instellingen, Help en Over zijn flat icoon-knoppen. De
glyphs komen uit het Windows-symboolfont **Segoe MDL2 Assets** (⚙ Instellingen
`E713`, ↻ Vernieuwen `E72C`, ✕ Annuleren `E711`, ❓ Help `E897`, ℹ Over `E946`),
gedeeld via de `ToolIconButton`-stijl: transparant met een lichte hover, en
accentkleur bij indrukken. De tekst zit als gelokaliseerde tooltip op elke knop.


## Taalkeuze met vlaggen (SE-7)

De taalkeuze in het Instellingen-scherm toont per taal een vlag naast de naam. De
vlaggen zijn zelfgetekende SVG's (NL, DE en een vereenvoudigde Union Jack voor
Engels) onder `Assets\flags\`, ingebakken als WPF-resource. `LanguageChoice` heeft
een `Flag`-property die de bijbehorende vlag eenmalig via `SvgResourceImage` laadt
(op basis van de cultuurcode). De ComboBox gebruikt een `ItemTemplate` met een klein
omkaderd vlagje plus de taalnaam; dat geldt zowel voor de lijst als voor de selectie.


## Eén scan per schijf, gedeeld door beide views (SE-7)

De mappenscan hoort bij de **schijf**, niet bij de view. `MainViewModel` houdt één
gedeelde `SvgFolderIndex` (`_index`) per gekozen schijf bij. `StartScanAsync` maakt
een verse index en start één achtergrondscan (`SvgIndexService.BuildIndexAsync`);
`ProjectView` bouwt `RootNodes` voor het huidige filter uit die index, zónder te
scannen. Van filter wisselen roept alleen `ProjectView` aan — nooit een nieuwe scan.
Terwijl de scan loopt werkt `RefreshActiveView` (throttled) de actieve view bij:
markeringen verversen in "Alles", of `SvgOnlyTreeBuilder.Sync` in "Alleen SVG".

Zo begint de scan niet opnieuw als de gebruiker halverwege van view wisselt; een
lopende scan blijft beide projecties vullen. Alleen een schijfwissel of Vernieuwen
start een nieuwe scan (oude wordt geannuleerd). Annuleren stopt de scan maar behoudt
het tot dan gevondene, en markeert dat als "klaar" zodat view-wissels het hergebruiken.


## Foutafhandeling & logging (SE-7)

Onverwachte fouten worden centraal afgevangen in `App`: `DispatcherUnhandledException`
(UI-thread) logt de fout, toont een nette, gelokaliseerde melding met de logmap en
houdt de app in de lucht (`Handled = true`); `AppDomain.UnhandledException` en
`TaskScheduler.UnobservedTaskException` loggen fouten van niet-UI-threads en
achtergrondtaken. De `Logger` (in `Services`) is bewust licht en dependency-vrij:
thread-safe, best-effort (logging mag de app nooit laten crashen), schrijft naar
`%AppData%\SVGViewer\logs\app.log` en roteert naar `app.prev.log` zodra het bestand
~1 MB passeert. Bestaande stille `catch`-blokken (o.a. `SvgResourceImage`,
`TitleBarIconFixer`, het openen van de handleiding) loggen nu ook, zodat problemen
bij gebruikers traceerbaar zijn zonder de werking te verstoren.


## Preview in een eigen venster (SE-3 / SE-7)

De zoom-preview opent in een los venster (`Views/SvgZoomWindow`) dat de bestaande
`SvgZoomViewer`-UserControl host, in plaats van een overlay in het hoofdvenster.
Reden: bij een overlay sluit de titelbalk-X van Windows de hele applicatie, terwijl
gebruikers die knop intuïtief gebruiken om de preview te sluiten. Als eigen venster
sluit de X (en Esc, en de Sluiten-knop in de viewer) alleen de preview. Het venster
wordt niet-modaal geopend met de hoofdvenster als eigenaar; `SvgZoomViewer.Close()`
sluit simpelweg zijn host-venster via `Window.GetWindow(this)`.


## Bestandsbeheer: verwijderen (SE-8)

Bestandsacties lopen via `FileOperationService` (achter `IFileOperationService`),
dat uitkomsten teruggeeft (`Success` / `FileNotFound` / `Failed`) en fouten logt in
plaats van te gooien. Verwijderen gaat naar de **prullenbak** (omkeerbaar) via
`Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile` met `RecycleOption.SendToRecycleBin`
en zonder Windows-eigen bevestiging (`UIOption.OnlyErrorDialogs`).

De bevestiging is onze eigen, gelokaliseerde dialoog (`Views/ConfirmDeleteWindow`,
achter `IDeleteConfirmer`) met een **"Niet meer vragen"**-optie die
`ConfirmBeforeDelete` (SE-9) uitzet; staat die uit, dan wordt niet gevraagd. Na een
succesvolle verwijdering haalt `MainViewModel` het bestand uit de previewlijst,
werkt de SVG-telling van de map in de index bij (`SvgFolderIndex.SetSvgCount`) en
ververst de markeringen — een gerichte refresh zonder volledige herscan.


## Bestandsbeheer: hernoemen (SE-8)

`FileOperationService.Rename(path, newName, overwrite)` hernoemt binnen dezelfde map
en geeft uitkomsten terug: `Success`, `FileNotFound`, `InvalidName` (lege naam of
ongeldige tekens), `TargetExists` (doelnaam bestaat al en `overwrite` is false) en
`Failed`. De naam-invoer gaat via `Views/RenameFileWindow` (achter `IRenameDialog`),
dat de extensie als vaste suffix toont en behoudt, zodat het resultaat altijd een
geldige bestandsnaam van hetzelfde type is.

Conform [AD-3]/US-8.7 wordt bij één bestand **elke keer** om bevestiging gevraagd als
de doelnaam al bestaat (geen persistente "altijd overschrijven"). Die ja/nee-vraag
loopt via `IUserNotifier.Confirm`. Na een succesvolle hernoeming herlaadt
`MainViewModel` de preview van de geselecteerde map, zodat de nieuwe naam en
thumbnail verschijnen; de telling verandert niet.


## Bestandsbeheer: nieuwe map (SE-8)

`FileOperationService.CreateFolder(parentPath, name)` maakt een submap en geeft
`Success`, `InvalidName`, `TargetExists` (naam al in gebruik) of `Failed` terug. De
actie zit in het rechtsklik-menu van een **map in de boom** (`INewFolderDialog` /
`Views/NewFolderWindow` voor de naam). Na aanmaken herleest de betreffende node zijn
kinderen via `DirectoryNodeViewModel.ReloadChildren` en klapt open, zodat de nieuwe
map meteen zichtbaar is — opnieuw een gerichte verversing zonder herscan. In het
filter "Alleen SVG" verschijnt een lege nieuwe map pas zodra er SVG's in staan
(consistent met de bedoeling van dat filter).


## Bestandsbeheer: kopiëren/plakken (SE-8)

Kopiëren gebruikt het **Windows-klembord** (achter `IFileClipboard`), zodat
bestanden ook van/naar de Verkenner werken. "Kopiëren" op een SVG zet het bestand op
het klembord; "Plakken" op een map-node kopieert het klembord-bestand erin via
`FileOperationService.Copy`. Conform US-8.7 wordt bij een naamconflict in de doelmap
**elke keer** om overschrijven gevraagd (`IUserNotifier.Confirm`); plakken in de
eigen map van het bestand maakt bewust een uniek `(2)`-duplicaat in plaats van te
vragen. Na een geslaagde plak-actie werkt `MainViewModel` de telling/markering van
de doelmap bij en herlaadt, als dat de geselecteerde map is, de preview.
