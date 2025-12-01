<!--
  Назначение: краткие, практичные инструкции для AI-ассистентов (Copilot-подобных) по этому репозиторию.
  Держите коротко, но достаточно конкретно — ссылки на ключевые файлы и примеры команд.
-->

# PDFAnnotator — инструкция для ассистента (на русском)

Ниже — сжатая и практичная справка, которая помогает быстро понять архитектуру, конвенции и опасные места в проекте.

## Быстрый запуск (нужен .NET 8 SDK)
- Сборка: `dotnet build`
- Запуск UI: `dotnet run --project src/PdfAnnotator.App`
- Тесты: `dotnet test`

Если сборка скачивает пакеты — NuGet сделает это при первой сборке. В оффлайн-окружениях подготовьте зависимости заранее.

## Крупная картина / архитектура
- Приложение — Avalonia MVVM (desktop) в `src/PdfAnnotator.App`.
  - Views: `src/PdfAnnotator.App/Views/`
  - ViewModels: `src/PdfAnnotator.App/ViewModels/`
- Бизнес-логика и I/O — в `src/PdfAnnotator.Core` (модели и сервисы).
- DI и регистрация сервисов — в `src/PdfAnnotator.App/Services/AppBootstrapper.cs` (используйте этот файл для добавления новых сервисов / ViewModel).
- Логирование — файл-логгер в `src/PdfAnnotator.App/Logging/` сохраняет в `logs/app.log`.

## Ключевые файлы и что в них смотреть (конкретно)
- `src/PdfAnnotator.App/Services/PdfService.cs`
  - Рендер страниц (Docnet), извлечение текста (PdfPig) и запись аннотаций (PdfSharpCore).
  - Имеется кеш рендера `_renderCache`. Обратите внимание на преобразования координат и инверсию Y при генерации PDF.
- `src/PdfAnnotator.Core/Services/CsvService.cs`
  - CSV использует `;` как разделитель и заголовок обязателен: `page;field_text;code`.
  - Для разбора используется регулярное выражение, которое учитывает кавычки; при изменении CSV — сохраните совместимость заголовка и экранирования.
- `src/PdfAnnotator.Core/Services/PresetService.cs`
  - Сохранение/загрузка пресетов как JSON в `presets/extraction` и `presets/annotation`.
- `src/PdfAnnotator.Core/Services/ProjectService.cs`
  - Проекты хранятся в `projects/{name}.json`. `LoadProjectAsync` кидает `FileNotFoundException`, и после десериализации гарантирует, что `Rows` != null.
- `src/PdfAnnotator.App/ViewModels/MainWindowViewModel.cs`
  - Центральная оркестрация: есть режимы приложения (Extraction / Table / Annotation).
  - Синхронизация происходит через события: `Extraction.TableUpdated -> Table.SetRows`, `Table.RowsUpdated -> Annotation.SetRows`.

## Специфические конвенции и подводные камни
- Проект таргетит `net8.0`; убедитесь, что у окружения установлен .NET 8 SDK.
- CSV: строго `;` и UTF-8. Заголовок должен быть ровно `page;field_text;code` (проверяется в `CsvService.LoadCsvAsync`).
- Preset/Project JSON: сериализация через `JsonSerializer` с `WriteIndented` — формат простой, каталоги создаются заранее (см. `ProjectService`/`PresetService`).
- PDF: PdfPig даёт координаты в типичных PDF-поинтах; PdfSharpCore рисует в другом контексте —следите за Y-инверсией и единицами измерения при переносе координат (см. `GenerateAnnotatedPdfAsync`).
- Пакет `UglyToad.PdfPig` в файле проекта использует кастомную версию (`1.7.0-custom-5`) — будьте осторожны при обновлении.

## Как добавить новый сервис или ViewModel (практический чеклист)
1. Если логика относится к домену (CSV, проекты, пресеты) — добавьте интерфейс и реализацию в `src/PdfAnnotator.Core/Services`.
2. Зарегистрируйте сервис в `AppBootstrapper.Configure()` (напр. `services.AddSingleton<IFoo, Foo>();`).
3. Для ViewModel: добавьте класс в `src/PdfAnnotator.App/ViewModels`, зарегистрируйте в `AppBootstrapper` и добавьте соответствующий View (`*.axaml`) в `Views/`.
4. Напишите быстрые unit-тесты в `tests/PdfAnnotator.Tests` (см. тесты для `CsvService`, `PresetService`, `ProjectService` — они дают стиль assertion и временные файлы).

## Работа с I/O (CSV / presets / projects)
- Всюду используются явные исключения: отсутствующий файл -> `FileNotFoundException`, некорректные данные -> `InvalidDataException`.
- При сохранении всегда создавайте каталог: `Directory.CreateDirectory(...)` — это стандартный паттерн в сервисах проекта.

## Тесты и отладка
- Запуск всех тестов: `dotnet test` (в корне репозитория или в папке `tests`).
- Для быстрой локальной проверки core-логики запускайте тесты из `tests/PdfAnnotator.Tests` — они изолированы от UI.
- Отладка UI: `dotnet run --project src/PdfAnnotator.App` и подключение дебаггера (VS/JetBrains/VSCode с .NET debugger).

## Частые ошибки / рекомендации
- Не меняйте заголовок CSV и формат кавычек/экранирования без миграции данных — `CsvService` строго проверяет заголовок.
- При правке `PdfService` проверьте, что `RenderPageAsync` и `ExtractTextAsync` остаются потокобезопасными и не ломают `_renderCache`.
- Если меняете пресеты/проекты — добавьте миграцию или версионирование формата, иначе старые JSON-файлы могут перестать парситься.

Если нужно, могу расширить эту инструкцию примерами PR (конкретные изменения кода и тесты) или добавить чеклист для проверки корректности координат при аннотировании PDF.

