# PDF Extract → Table → Annotate Tool

Рабочий прототип Avalonia (.NET 8) для выделения текста в PDF, редактирования таблицы и аннотирования с пресетами.

## Запуск

```bash
dotnet build
dotnet run --project src/PdfAnnotator.App
```

Требуется .NET 8 SDK. Все зависимости ставятся через NuGet при первой сборке (в песочнице сети может не быть — скачайте заранее при необходимости).

## Самостоятельная сборка (без установки .NET на целевой машине)

Для создания автономной сборки, которая может работать на компьютерах без установленного .NET, выполните:

Windows (cmd):
```bash
publish.bat
```

Windows (PowerShell):
```bash
.\publish.ps1
```

Или вручную:
```bash
dotnet publish src/PdfAnnotator.App/PdfAnnotator.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true
```

Результат будет находиться в: `src\PdfAnnotator.App\bin\Release\net8.0\win-x64\publish\`

## Сценарии
- Режим «Извлечение»: введите путь к PDF, откройте страницу, выделите прямоугольник мышью, сохраните пресет при необходимости, нажмите «Извлечь текст» → таблица заполняется.
- Режим «Таблица»: редактируйте Code, сохраняйте/загружайте CSV (`tables/latest.csv`). Строки синхронизируются с аннотированием.
- Режим «Аннотирование»: введите путь к PDF, выберите страницу, кликните позицию текста, настройте шрифт/цвет/угол, сохраните пресет, нажмите «Сохранить итоговый PDF» → файл `output/annotated.pdf`.

## Архитектура
- MVVM: ViewModels в `src/PdfAnnotator.App/ViewModels`, Views в `src/PdfAnnotator.App/Views`.
- Сервисы и доменные модели: `src/PdfAnnotator.Core`.
- DI: `Microsoft.Extensions.DependencyInjection`, регистрация в `AppBootstrapper`.
- Логирование в `logs/app.log` через простой файл-логгер.
- PDF абстракция: `IPdfService` (Docnet для рендера, PdfPig для текста, PdfSharpCore для записи).

## Тесты
```bash
dotnet test
```
Покрывают CsvService, PresetService, ProjectService.

## Пример данных
- Пресеты: сохраняются в `presets/extraction` и `presets/annotation` как JSON.
- Проекты: `projects/{name}.json`.
- CSV: `tables/latest.csv`.

## Ограничения текущей версии
- Рендер и выделение работают в координатах отображения, что может давать неточность при масштабировании.
- Аннотации добавляются в `output/annotated.pdf` с базовой поддержкой шрифтов/цветов.
- Нет встроенных диалогов открытия файлов: путь вводится вручную.