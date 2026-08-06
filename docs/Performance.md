# Scanprestaties meten

De app gebruikt één geoptimaliseerde scanroute: één ongesorteerde enumeratie per
map telt SVG-bestanden en verzamelt submappen tegelijk. Dit maakt de boom tijdens
het scannen beschikbaar, terwijl de scan op de achtergrond verdergaat.

De eerdere vergelijking op dezelfde pc liet zien waarom deze route is gekozen:
op `C:\` met 420.260 mappen duurde zij gemiddeld 33,02 seconden, tegenover 92,63
seconden voor de oude aanpak (64,4% sneller). Die oude scanroute is daarna uit de
applicatie verwijderd, zodat er geen ongebruikte, afwijkende logica meer hoeft te
worden onderhouden.

Meet op de daadwerkelijke schijf, bijvoorbeeld de werklaptop:

```powershell
.\build\Measure-ScanPerformance.ps1 -Path 'C:\' -Iterations 3
```

Het script start een kleine .NET 8-benchmarkhost en voert per iteratie een verse
scan met de huidige applicatielogica uit. Per run rapporteert het de verstreken
tijd, het aantal gescande mappen en mappen per seconde, gevolgd door het
gemiddelde. Daardoor werkt het zowel vanuit Windows PowerShell 5.1 als PowerShell
7+. Het wijzigt niets op de gemeten schijf.

`-Iterations 1` is geschikt voor een snelle indicatie. Gebruik `-Iterations 3`
voor een betrouwbaarder gemiddelde; dit scant de doelmap drie keer en kan bij
430.000 mappen dus geruime tijd duren. Metingen na de eerste run profiteren vaak
van de Windows-bestandssysteemcache. Vergelijk daarom alleen metingen die op
dezelfde machine, schijf en zo rustig mogelijk moment zijn uitgevoerd.
