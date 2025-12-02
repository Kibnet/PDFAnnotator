# API Reference

<cite>
**Referenced Files in This Document**   
- [IPdfService.cs](file://src/PdfAnnotator.Core/Services/IPdfService.cs)
- [ICsvService.cs](file://src/PdfAnnotator.Core/Services/ICsvService.cs)
- [IPresetService.cs](file://src/PdfAnnotator.Core/Services/IPresetService.cs)
- [IProjectService.cs](file://src/PdfAnnotator.Core/Services/IProjectService.cs)
- [PdfService.cs](file://src/PdfAnnotator.App/Services/PdfService.cs)
- [MainWindowViewModel.cs](file://src/PdfAnnotator.ViewModels/MainWindowViewModel.cs)
- [AnnotationViewModel.cs](file://src/PdfAnnotator.ViewModels/AnnotationViewModel.cs)
- [ExtractionViewModel.cs](file://src/PdfAnnotator.ViewModels/ExtractionViewModel.cs)
- [TableViewModel.cs](file://src/PdfAnnotator.ViewModels/TableViewModel.cs)
- [RelayCommand.cs](file://src/PdfAnnotator.ViewModels/RelayCommand.cs)
- [PdfProject.cs](file://src/PdfAnnotator.Core/Models/PdfProject.cs)
- [TableRow.cs](file://src/PdfAnnotator.Core/Models/TableRow.cs)
- [AnnotationPreset.cs](file://src/PdfAnnotator.Core/Models/AnnotationPreset.cs)
- [ExtractionPreset.cs](file://src/PdfAnnotator.Core/Models/ExtractionPreset.cs)
- [TableRowViewModel.cs](file://src/PdfAnnotator.ViewModels/TableRowViewModel.cs)
</cite>

## Table of Contents
1. [Introduction](#introduction)
2. [Service Interfaces](#service-interfaces)
   - [IPdfService](#ipdfservice)
   - [ICsvService](#icsvservice)
   - [IPresetService](#ipresetservice)
   - [IProjectService](#iprojectservice)
3. [ViewModel Classes](#viewmodel-classes)
   - [MainWindowViewModel](#mainwindowviewmodel)
   - [ExtractionViewModel](#extractionviewmodel)
   - [TableViewModel](#tableviewmodel)
   - [AnnotationViewModel](#annotationviewmodel)
4. [Data Models](#data-models)
5. [Command and Event Patterns](#command-and-event-patterns)
6. [Usage Examples](#usage-examples)
7. [Thread Safety and Async Patterns](#thread-safety-and-async-patterns)
8. [Lifecycle Considerations](#lifecycle-considerations)

## Introduction
This document provides comprehensive API documentation for the PDFAnnotator application, focusing on public interfaces in the Core project and ViewModel classes in the ViewModels project. The system is built using Avalonia UI framework with MVVM pattern, featuring asynchronous operations for PDF processing, text extraction, and annotation. The architecture separates concerns through service interfaces, with implementations handling PDF rendering, data persistence, and user interaction logic.

## Service Interfaces

### IPdfService
Defines core PDF processing operations including page rendering, text extraction, and annotation generation.

**Methods:**
- `Task<int> GetPageCountAsync(string path)`  
  Returns the total number of pages in the specified PDF file.  
  **Parameters:**  
  - `path`: Full path to the PDF file  
  **Returns:** Number of pages as integer  
  **Exceptions:** FileNotFoundException if file not found  
  **Purpose:** Enables navigation and page selection in UI components

- `Task<Bitmap> RenderPageAsync(string path, int page, int dpi)`  
  Renders a specific page of a PDF document as a bitmap image.  
  **Parameters:**  
  - `path`: Path to PDF file  
  - `page`: Page number (1-based)  
  - `dpi`: Resolution for rendering  
  **Returns:** Bitmap object of rendered page  
  **Exceptions:** ArgumentOutOfRangeException for invalid page numbers, InvalidOperationException for rendering failures  
  **Purpose:** Provides visual representation of PDF pages in the application

- `Task<List<TableRow>> ExtractTextAsync(string pdfPath, ExtractionPreset preset)`  
  Extracts text from specified regions of all pages in a PDF.  
  **Parameters:**  
  - `pdfPath`: Path to source PDF  
  - `preset`: Defines rectangular region (X0,Y0,X1,Y1) for text extraction  
  **Returns:** List of TableRow objects containing extracted text per page  
  **Purpose:** Implements region-based text extraction functionality

- `Task GenerateAnnotatedPdfAsync(string pdfPath, string outputPdfPath, List<TableRow> rows, AnnotationPreset preset)`  
  Generates a new PDF with annotations (text) added to specified positions.  
  **Parameters:**  
  - `pdfPath`: Source PDF path  
  - `outputPdfPath`: Destination path for annotated PDF  
  - `rows`: Data to be annotated (typically codes)  
  - `preset`: Styling and positioning parameters for annotations  
  **Purpose:** Creates output PDFs with embedded annotations based on user data

**Section sources**
- [IPdfService.cs](file://src/PdfAnnotator.Core/Services/IPdfService.cs#L8-L14)
- [PdfService.cs](file://src/PdfAnnotator.App/Services/PdfService.cs#L18-L178)

### ICsvService
Handles CSV file operations for data persistence.

**Methods:**
- `Task SaveCsvAsync(string path, List<TableRow> rows)`  
  Saves table data to CSV format.  
  **Parameters:**  
  - `path`: Output file path  
  - `rows`: Data to serialize  
  **Purpose:** Persists extracted/edited data to CSV format

- `Task<List<TableRow>> LoadCsvAsync(string path)`  
  Loads table data from CSV file.  
  **Parameters:**  
  - `path`: Input CSV file path  
  **Returns:** Deserialized list of TableRow objects  
  **Purpose:** Restores table data from persistent storage

**Section sources**
- [ICsvService.cs](file://src/PdfAnnotator.Core/Services/ICsvService.cs#L7-L11)
- [CsvService.cs](file://src/PdfAnnotator.Core/Services/CsvService.cs#L1-L25)

### IPresetService
Manages persistence of user-defined configuration presets.

**Methods:**
- `Task SaveExtractionPresetAsync(ExtractionPreset preset)`  
  Saves extraction region configuration.  
  **Parameters:** Coordinates (X0,Y0,X1,Y1) defining text extraction area

- `Task SaveAnnotationPresetAsync(AnnotationPreset preset)`  
  Saves annotation styling and positioning configuration.  
  **Parameters:** Text position, font, size, color, and rotation angle

- `Task<List<ExtractionPreset>> LoadAllExtractionPresetsAsync()`  
  Returns all available extraction presets.  
  **Purpose:** Enables preset selection in UI

- `Task<List<AnnotationPreset>> LoadAllAnnotationPresetsAsync()`  
  Returns all available annotation presets

- `Task<ExtractionPreset?> LoadExtractionPresetAsync(string path)`  
  Loads a specific extraction preset from file path

**Section sources**
- [IPresetService.cs](file://src/PdfAnnotator.Core/Services/IPresetService.cs#L7-L14)
- [PresetService.cs](file://src/PdfAnnotator.Core/Services/PresetService.cs#L1-L45)

### IProjectService
Handles complete project serialization.

**Methods:**
- `Task SaveProjectAsync(PdfProject project, string path)`  
  Saves entire project state including PDF reference, presets, and table data

- `Task<PdfProject> LoadProjectAsync(string path)`  
  Restores project state from serialized file  
  **Returns:** Fully reconstructed PdfProject object

**Section sources**
- [IProjectService.cs](file://src/PdfAnnotator.Core/Services/IProjectService.cs#L6-L10)
- [ProjectService.cs](file://src/PdfAnnotator.Core/Services/ProjectService.cs#L1-L30)

## ViewModel Classes

### MainWindowViewModel
Central coordinator managing application state and navigation between modes.

**Observable Properties:**
- `AppMode Mode`: Current application state (Extraction, Table, Annotation)
- `ExtractionViewModel Extraction`: Child VM for extraction functionality
- `TableViewModel Table`: Child VM for table editing
- `AnnotationViewModel Annotation`: Child VM for annotation workflow

**Commands:**
- `GoToTableCommand`: Switches to table editing mode
- `GoToAnnotationCommand`: Navigates to annotation mode with data synchronization
- `SaveProjectCommand`: Persists current project state
- `LoadProjectCommand`: Restores project from storage

**Events:**
- Coordinates workflow through event subscriptions from child ViewModels
- Implements data synchronization between components via event handlers

**Section sources**
- [MainWindowViewModel.cs](file://src/PdfAnnotator.ViewModels/MainWindowViewModel.cs#L21-L119)

### ExtractionViewModel
Manages PDF text extraction workflow with region selection.

**Observable Properties:**
- `string PdfPath`: Source PDF file path
- `int PageCount`: Total pages in current PDF
- `int CurrentPage`: Currently displayed page
- `Bitmap? PageBitmap`: Rendered page image
- `double X0, Y0, X1, Y1`: Extraction region coordinates
- `double SelectLeft, SelectTop, SelectWidth, SelectHeight`: UI selection overlay
- `ObservableCollection<ExtractionPreset> Presets`: Available extraction configurations
- `ExtractionPreset? SelectedPreset`: Active preset

**Commands:**
- `LoadPdfCommand`: Loads and renders PDF
- `ExtractTextCommand`: Executes text extraction in selected region
- `SavePresetCommand`: Saves current extraction parameters
- `ReloadPresetsCommand`: Refreshes preset list

**Events:**
- `TableUpdated`: Broadcasts extracted data to other components

**Section sources**
- [ExtractionViewModel.cs](file://src/PdfAnnotator.ViewModels/ExtractionViewModel.cs#L16-L195)

### TableViewModel
Handles tabular data editing and CSV persistence.

**Observable Properties:**
- `ObservableCollection<TableRowViewModel> Rows`: Editable table data
- Implements INotifyPropertyChanged for data binding

**Commands:**
- `SaveCsvCommand`: Exports data to CSV
- `LoadCsvCommand`: Imports data from CSV

**Events:**
- `RowsUpdated`: Notifies other components of data changes
- Automatically subscribes to property changes on individual rows

**Section sources**
- [TableViewModel.cs](file://src/PdfAnnotator.ViewModels/TableViewModel.cs#L16-L70)

### AnnotationViewModel
Manages PDF annotation workflow with visual preview.

**Observable Properties:**
- `string PdfPath`: Source PDF path
- `int PageCount`, `CurrentPage`: Navigation state
- `Bitmap? PageBitmap`: Rendered page for annotation
- `AnnotationPreset` properties: `TextX`, `TextY`, `FontSize`, `Angle`, `ColorHex`, `FontName`
- `ObservableCollection<AnnotationPreset> Presets`: Available annotation styles
- `ObservableCollection<TableRow> Rows`: Data to be annotated
- `SelectedCodePreview`: Current page's annotation code for display

**Commands:**
- `LoadPdfCommand`: Loads PDF for annotation
- `SavePresetCommand`: Saves current annotation style
- `ReloadPresetsCommand`: Refreshes preset list
- `SaveAnnotatedPdfCommand`: Generates final annotated PDF

**Section sources**
- [AnnotationViewModel.cs](file://src/PdfAnnotator.ViewModels/AnnotationViewModel.cs#L15-L194)

## Data Models
Core data structures used throughout the application:

**PdfProject**: Contains complete project state including name, source PDF, selected presets, and table data.

**TableRow**: Represents a single row of data with Page number, extracted FieldText, and user-entered Code.

**ExtractionPreset**: Defines rectangular region (X0,Y0,X1,Y1) for text extraction from PDF pages.

**AnnotationPreset**: Specifies annotation parameters including position (TextX, TextY), font properties, color, and rotation angle.

**Section sources**
- [PdfProject.cs](file://src/PdfAnnotator.Core/Models/PdfProject.cs#L5-L12)
- [TableRow.cs](file://src/PdfAnnotator.Core/Models/TableRow.cs#L3-L8)
- [ExtractionPreset.cs](file://src/PdfAnnotator.Core/Models/ExtractionPreset.cs#L3-L10)
- [AnnotationPreset.cs](file://src/PdfAnnotator.Core/Models/AnnotationPreset.cs#L3-L12)

## Command and Event Patterns
The application implements MVVM pattern with:

**RelayCommand**: Generic ICommand implementation enabling command binding in XAML views. Supports async execution and CanExecute evaluation.

**Event-Driven Communication**: 
- Child ViewModels publish events (TableUpdated, RowsUpdated)
- Parent ViewModels subscribe and coordinate state changes
- Enables loose coupling between components

**Data Binding**: All observable properties use Fody/PropertyChanged for automatic INotifyPropertyChanged implementation, enabling two-way binding in Avalonia XAML views.

**Section sources**
- [RelayCommand.cs](file://src/PdfAnnotator.ViewModels/RelayCommand.cs#L6-L24)
- [MainWindowViewModel.cs](file://src/PdfAnnotator.ViewModels/MainWindowViewModel.cs#L60-L62)
- [TableViewModel.cs](file://src/PdfAnnotator.ViewModels/TableViewModel.cs#L21-L22)

## Usage Examples
**Extracting Text from PDF Region:**
```csharp
var extractionVm = serviceProvider.GetService<ExtractionViewModel>();
extractionVm.PdfPath = "sample.pdf";
await extractionVm.LoadPdfAsync();
extractionVm.UpdateSelection(100, 200, 300, 250, 1000); // Define region
extractionVm.ExtractTextCommand.Execute(null);
// Extracted data available via TableUpdated event
```

**Generating Annotated PDF:**
```csharp
var annotationVm = serviceProvider.GetService<AnnotationViewModel>();
annotationVm.PdfPath = "source.pdf";
annotationVm.Rows = tableData;
annotationVm.TextX = 50;
annotationVm.TextY = 100;
annotationVm.FontSize = 14;
await annotationVm.SaveAnnotatedAsync();
```

**Saving Project State:**
```csharp
var mainVm = serviceProvider.GetService<MainWindowViewModel>();
mainVm._currentProject.Name = "MyProject";
await mainVm.SaveProjectAsync();
```

## Thread Safety and Async Patterns
All service operations are asynchronous to prevent UI blocking during I/O operations. The PdfService implements:
- Task.Run for CPU-intensive PDF operations
- Internal caching with lock synchronization (_cacheLock)
- Proper exception handling with logging
- Thread-safe property updates in ViewModels via INotifyPropertyChanged

## Lifecycle Considerations
- ViewModels are typically registered as scoped services
- Event subscriptions are established in constructors and automatically garbage collected
- File operations use proper using statements for resource disposal
- Caching is implemented for expensive PDF rendering operations
- Error handling includes both exception throwing and logging via ILogger