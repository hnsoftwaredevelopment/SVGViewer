; SVG Viewer-productconfiguratie. Houd AppId stabiel voor upgrades.

#ifndef ProductName
  #define ProductName "SVGViewer"
#endif
#ifndef ProductVersion
  #define ProductVersion "0.0.0"
#endif
#ifndef ProductPublisher
  #define ProductPublisher "© HN Software Development"
#endif
#ifndef ProductUrl
  #define ProductUrl "https://hnsoftwaredevelopment.nl/"
#endif
#ifndef AppId
  #define AppId "{{F03368A6-429E-4683-910D-F1DB7F54B380}"
#endif
#ifndef MainExecutable
  #define MainExecutable "SVGViewer.exe"
#endif
#ifndef PublishDir
  #define PublishDir "..\Builds\Publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\Builds\Installer"
#endif
#ifndef OutputBaseFilename
  #define OutputBaseFilename "SVGViewerSetup"
#endif

#define SetupIconFile "..\src\SVGViewer\Assets\appicon.ico"
#define WizardImageFile "Assets\developer-logo-wizard.bmp"
#define WizardSmallImageFile "Assets\app-logo-small.bmp"

; SVG Viewer is een machinebrede x64-installatie.
#define PrivilegesRequired "admin"
#define TargetArchitecture "x64"

; .NET Desktop Runtime 8.0.29, gecontroleerd op 6 augustus 2026.
#define DotNetDesktopRuntimeVersion "8.0.29"
#define DotNetDesktopRuntimeUrl "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.29/windowsdesktop-runtime-8.0.29-win-x64.exe"
#define DotNetDesktopRuntimeFileName "windowsdesktop-runtime-8.0.29-win-x64.exe"
#define DotNetDesktopRuntimeSha256 "c0ffa16efeb7ef3ac8100a6a9d7089d9c2904ee89f1815557a79a91be584f775"
