# Self-Contained Application Build Instructions

This document explains how to build a self-contained version of the PDF Annotator application that can run on computers without .NET installed.

## Prerequisites

- .NET 8 SDK installed on the build machine
- Windows OS (the current configuration targets win-x64)

## Build Process

### Using the provided scripts

1. **Using Command Prompt:**
   ```cmd
   publish.bat
   ```

2. **Using PowerShell:**
   ```powershell
   .\publish.ps1
   ```

### Manual build command

If you prefer to run the command manually:

```bash
dotnet publish src/PdfAnnotator.App/PdfAnnotator.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true
```

## Output Location

The self-contained executable will be created at:
```
src\PdfAnnotator.App\bin\Release\net8.0\win-x64\publish\
```

The main executable will be named `PdfAnnotator.App.exe` and will contain all necessary dependencies to run on a Windows machine without .NET installed.

## Deployment

To deploy the application to another machine:
1. Copy the entire contents of the publish folder
2. Place it on the target machine
3. Run `PdfAnnotator.App.exe` directly without needing to install .NET

## Technical Details

The self-contained build includes the following settings in the project file:

- `<PublishSingleFile>true</PublishSingleFile>` - Bundles the application into a single executable file
- `<SelfContained>true</SelfContained>` - Includes the .NET runtime with the application
- `<RuntimeIdentifier>win-x64</RuntimeIdentifier>` - Targets Windows 64-bit systems
- `<PublishReadyToRun>true</PublishReadyToRun>` - Improves startup performance
- `<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>` - Ensures native libraries are properly included

These settings ensure that all dependencies are bundled with the application, making it completely portable.