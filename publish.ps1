Write-Host "Building self-contained PDF Annotator application..." -ForegroundColor Green
Write-Host ""

dotnet publish src/PdfAnnotator.App/PdfAnnotator.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true

Write-Host ""
Write-Host "Publish completed. Check the output directory for the self-contained executable." -ForegroundColor Green
Write-Host "Default output location: src\PdfAnnotator.App\bin\Release\net8.0\win-x64\publish\" -ForegroundColor Yellow