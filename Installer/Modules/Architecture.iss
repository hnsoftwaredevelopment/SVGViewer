[Setup]
#if TargetArchitecture == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#else
  #error SVG Viewer only supports TargetArchitecture=x64.
#endif
