[Files]
; Laat Inno Setup bestaande bestanden bijwerken. Verwijder tijdens een upgrade
; geen algemene bestandsmaskers: vooral het tijdelijk verwijderen van de .exe
; kan bestaande pins in Start en de taakbalk ongeldig maken.
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
