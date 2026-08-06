[InstallDelete]
; Alleen bestanden in de eigen applicatiemap opruimen tijdens een upgrade.
Type: files; Name: "{app}\*.exe"
Type: files; Name: "{app}\*.dll"
Type: files; Name: "{app}\*.pdb"
Type: files; Name: "{app}\*.deps.json"
Type: files; Name: "{app}\*.runtimeconfig.json"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
