@echo off
echo Building self-contained PDF Annotator application...
echo.

dotnet publish src/PdfAnnotator.App/PdfAnnotator.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true

echo.
echo Publish completed. Check the output directory for the self-contained executable.
echo Default output location: src\PdfAnnotator.App\bin\Release\net8.0\win-x64\publish\
pause