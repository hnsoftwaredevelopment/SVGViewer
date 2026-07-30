# SVG Viewer — Handmatige testchecklist (US-7.3)

Loop deze lijst af in de echte app. Vink af wat werkt, noteer afwijkingen.
Herhaal de kernpaden in **alle drie de talen** (NL/EN/DE).

- App-versie getest: `__________`
- Datum / tester: `__________`
- Windows-thema tijdens test (licht/donker): `__________`

> Tip: leg tijdens het testen meteen de screenshots vast voor US-7.4
> (PNG's in `docs\images\`): hoofdvenster (Volledig én Alleen SVG), preview met
> thumbnails, zoomviewer, Instellingen met vlaggen, en de Over-dialoog.

## 1. Opstarten
- [ ] Splashscreen verschijnt bij het starten en vervaagt zodra het venster laadt.
- [ ] De app opent in de **laatst gekozen taal**.
- [ ] De laatst gekozen schijf is voorgeselecteerd (indien eerder gekozen).
- [ ] Logbestand bestaat: `%AppData%\SVGViewer\logs\app.log` bevat een regel
      `... [INFO] SVG Viewer starting (v...)`. *(Bewijs dat logging werkt, zonder fout.)*

## 2. Venster- en taakbalk-icoon
- [ ] Titelbalk-icoon staat op een wit vlakje en is goed zichtbaar — test ook met
      een **donker** Windows-thema.
- [ ] Taakbalk-icoon toont het nieuwe logo (transparant), duidelijk herkenbaar.

## 3. Scannen & mappenmarkering (kernfunctionaliteit)
- [ ] Schijf kiezen → scan start, de boom is **meteen** bruikbaar (lazy),
      statusbalk toont voortgang (aantal mappen / met SVG).
- [ ] Mappen mét SVG's: **blauw + vet + aantal**. Bovenliggende mappen op de route:
      **blauw, normaal, zonder aantal**.
- [ ] **Tijdens** een lopende scan wisselen naar "Alleen SVG" en terug naar
      "Volledig" → de scan begint **niet** opnieuw; voortgang loopt door.
- [ ] **Annuleren** (✕) tijdens een scan → scan stopt, tot dan gevonden resultaat
      blijft staan; van view wisselen scant daarna **niet** opnieuw.
- [ ] **Vernieuwen** (↻) → start wél een nieuwe scan (en toont bewerkte bestanden vers).
- [ ] **Schijf wisselen** → start een nieuwe scan (oude wordt netjes afgebroken).
- [ ] "Alleen SVG" toont alleen relevante takken; op een schijf/tak zonder SVG's
      volgt een lege staat met de melding "geen SVG's gevonden".

## 4. Weergavegrootte (segmented control)
- [ ] Grote / Middelgrote / Kleine iconen → thumbnails schalen mee.
- [ ] Details → lijstweergave (geen thumbnails).
- [ ] De actieve knop licht op; tooltips kloppen in elke taal.

## 5. Preview & zoom
- [ ] Map met SVG's selecteren → thumbnails renderen correct.
- [ ] **Dubbelklik** op een SVG → zoomviewer opent (niet de editor).
- [ ] In-/uitzoomen in de viewer werkt; sluiten werkt.
- [ ] Lege map geselecteerd → nette "geen SVG's"-melding, geen thumbnails.

## 6. Contextmenu (rechtermuisknop op een SVG)
- [ ] "Openen in editor" opent het bestand in de gekoppelde app (bijv. Inkscape).
- [ ] "Openen met…" toont de Windows-dialoog.
- [ ] "Tonen in Verkenner" opent Verkenner met het bestand geselecteerd.
- [ ] Foutpad: verwijder/hernoem een bestand en probeer het te openen → nette
      melding (geen crash).

## 7. Instellingen (⚙)
- [ ] Taal wisselen NL → EN → DE: de **hele** UI vertaalt live (toolbar-tooltips,
      statusbalk, contextmenu, dialogen, bestandsgroottes/datums).
- [ ] Bij de taalkeuze staan de **vlaggen** naast de namen; de geselecteerde vlag klopt.
- [ ] "Bevestiging vragen voordat een bestand wordt verwijderd" onthoudt de stand.
- [ ] Gekozen taal blijft behouden **na herstart** van de app.

## 8. Help (❓) & Over (ℹ)
- [ ] Help opent de handleiding in de standaardbrowser, in de **juiste taal**
      (terugval op NL als een taalversie ontbreekt).
- [ ] Over toont het app-logo, het **versienummer**, de beschrijving en het
      HN-Software-logo op de donkere strook; "Sluiten" werkt.

## 9. Talen — kernpaden herhalen
- [ ] Herhaal §3–§8 kort in **English** en **Deutsch**; let op dat álle teksten,
      meldingen, groottes en datums in de gekozen taal staan (geen NL-restanten).

## 10. Logging verifiëren (zonder een echte crash)
- [ ] `app.log` groeit met een nieuwe `starting`-regel bij elke start.
- [ ] *(Optioneel, ongevaarlijk foutpad)* Hernoem tijdelijk een handleiding-bestand
      in de `Help`-map naast de `.exe` en klik op **Help** → je krijgt een nette
      melding én er verschijnt een `[WARN] Could not open the user guide` in `app.log`.
      Zet de naam daarna terug.
- [ ] *(Optioneel)* Controleer dat rotatie werkt: als `app.log` groot wordt,
      ontstaat `app.prev.log` (of vertrouw op de automatische test hiervoor).

## 11. Afsluiten
- [ ] De app sluit netjes; er blijft geen `SVGViewer`-proces hangen.

---
**Bevindingen / afwijkingen:**

| # | Onderdeel | Verwacht | Waargenomen | Taal | Status |
|---|-----------|----------|-------------|------|--------|
|   |           |          |             |      |        |
