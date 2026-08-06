# SVG Viewer installer

`Main.iss` is het enige compilatiepunt. Productwaarden staan in `Product.iss`; technische onderdelen zijn onder `Modules` opgesplitst.

## Runtimebeleid

SVG Viewer is framework-dependent en vereist de .NET 8 Desktop Runtime voor x64. De runtime is vastgezet op versie 8.0.29 met SHA-256-controle. Wijzig URL, bestandsnaam en hash altijd gezamenlijk en test daarna installatie, upgrade en uninstall in een schone VM.

## Build

```powershell
.\Installer\Build-Installer.ps1 -InnoCompilerPath 'C:\Program Files\Inno Setup 7\ISCC.exe'
```

De build schrijft uitsluitend naar `Builds\Publish`, `Builds\Installer` en `Builds\Release`.
De pipeline weigert bewust een Inno Setup 6-compiler, ook wanneer die lokaal beschikbaar is.

Controleer een gegenereerde installer met:

```powershell
.\Installer\Test-Installer.ps1 -InstallerPath .\Builds\Release\SVGViewerSetup-<versie>.exe
```
