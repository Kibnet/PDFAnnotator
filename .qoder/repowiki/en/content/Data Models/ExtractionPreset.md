# ExtractionPreset Entity Documentation

<cite>
**Referenced Files in This Document**
- [ExtractionPreset.cs](file://src/PdfAnnotator.Core/Models/ExtractionPreset.cs)
- [Example.json](file://presets/extraction/Example.json)
- [ExtractionViewModel.cs](file://src/PdfAnnotator.ViewModels/ExtractionViewModel.cs)
- [PdfService.cs](file://src/PdfAnnotator.App/Services/PdfService.cs)
- [PresetService.cs](file://src/PdfAnnotator.Core/Services/PresetService.cs)
- [ExtractionView.axaml.cs](file://src/PdfAnnotator.App/Views/ExtractionView.axaml.cs)
- [README.md](file://README.md)
</cite>

## Table of Contents
1. [Introduction](#introduction)
2. [Entity Definition](#entity-definition)
3. [Properties Overview](#properties-overview)
4. [Coordinate System and PDF Space](#coordinate-system-and-pdf-space)
5. [JSON Serialization Format](#json-serialization-format)
6. [Storage and Management](#storage-and-management)
7. [Usage in Text Extraction Workflow](#usage-in-text-extraction-workflow)
8. [UI Integration and Selection](#ui-integration-and-selection)
9. [Coordinate Transformation Process](#coordinate-transformation-process)
10. [Workflow for Creating and Applying Presets](#workflow-for-creating-and-applying-presets)
11. [Best Practices and Considerations](#best-practices-and-considerations)
12. [Troubleshooting Guide](#troubleshooting-guide)

## Introduction

The ExtractionPreset entity serves as a fundamental component in the PDFAnnotator application, enabling repeatable and consistent text extraction from specific rectangular regions within PDF documents. It defines a precise area on a PDF page using coordinate boundaries that can be saved, loaded, and reused across different extraction operations.

This entity is crucial for automating text extraction tasks where the same document structure appears consistently across multiple files, allowing users to capture predefined regions without manual reselection for each document.

## Entity Definition

The ExtractionPreset is implemented as a simple data transfer object containing four essential properties that define a rectangular extraction area:

```mermaid
classDiagram
class ExtractionPreset {
+string Name
+double X0
+double Y0
+double X1
+double Y1
+getName() string
+setName(name) void
+getX0() double
+setX0(value) void
+getY0() double
+setY0(value) void
+getX1() double
+setX1(value) void
+getY1() double
+setY1(value) void
}
class ExtractionViewModel {
-IPdfService _pdfService
-IPresetService _presetService
+ExtractionPreset SelectedPreset
+ObservableCollection~ExtractionPreset~ Presets
+double X0
+double Y0
+double X1
+double Y1
+UpdateSelection(startX, startY, endX, endY, imageHeight) void
+ExtractAsync() Task
+SavePresetAsync() Task
+ApplyPreset() void
}
class PdfService {
+ExtractTextAsync(pdfPath, preset) Task~TableRow[]~
-IsInside(word, preset) bool
}
ExtractionViewModel --> ExtractionPreset : "manages"
PdfService --> ExtractionPreset : "uses for extraction"
ExtractionViewModel --> PdfService : "delegates extraction"
```

**Diagram sources**
- [ExtractionPreset.cs](file://src/PdfAnnotator.Core/Models/ExtractionPreset.cs#L3-L10)
- [ExtractionViewModel.cs](file://src/PdfAnnotator.ViewModels/ExtractionViewModel.cs#L18-L196)
- [PdfService.cs](file://src/PdfAnnotator.App/Services/PdfService.cs#L100-L126)

**Section sources**
- [ExtractionPreset.cs](file://src/PdfAnnotator.Core/Models/ExtractionPreset.cs#L1-L11)

## Properties Overview

### Name Property
- **Type**: `string`
- **Purpose**: Unique identifier for the preset
- **Default Value**: Empty string (`string.Empty`)
- **Usage**: Used for preset identification, display, and file naming
- **Constraints**: Must be unique within the preset collection

### Coordinate Properties (X0, Y0, X1, Y1)
These four properties define the rectangular boundary of the extraction area:

#### X0 (Horizontal Minimum)
- **Type**: `double`
- **Purpose**: Leftmost x-coordinate of the rectangle
- **Range**: Typically corresponds to PDF page width in points (72 DPI)
- **Constraint**: Must be less than or equal to X1

#### Y0 (Vertical Minimum)
- **Type**: `double`
- **Purpose**: Bottommost y-coordinate of the rectangle
- **Range**: Typically corresponds to PDF page height in points (72 DPI)
- **Constraint**: Must be less than or equal to Y1

#### X1 (Horizontal Maximum)
- **Type**: `double`
- **Purpose**: Rightmost x-coordinate of the rectangle
- **Range**: Typically corresponds to PDF page width in points (72 DPI)
- **Constraint**: Must be greater than or equal to X0

#### Y1 (Vertical Maximum)
- **Type**: `double`
- **Purpose**: Topmost y-coordinate of the rectangle
- **Range**: Typically corresponds to PDF page height in points (72 DPI)
- **Constraint**: Must be greater than or equal to Y0

**Section sources**
- [ExtractionPreset.cs](file://src/PdfAnnotator.Core/Models/ExtractionPreset.cs#L5-L9)

## Coordinate System and PDF Space

The ExtractionPreset operates within the PDF coordinate system, which follows specific conventions:

### PDF Coordinate System Characteristics
- **Origin**: Bottom-left corner of the PDF page
- **Units**: Points (1 point = 1/72 inch)
- **Orientation**: X-axis increases rightward, Y-axis increases upward
- **Page Dimensions**: Typically 612×792 points for US Letter size (8.5×11 inches)

### Coordinate Transformation Process

The application handles coordinate conversion between different systems:

```mermaid
flowchart TD
Start([Mouse Selection]) --> CalculateBounds["Calculate Selection Bounds<br/>X0 = min(startX, endX)<br/>X1 = max(startX, endX)<br/>Y0 = imageHeight - max(startY, endY)<br/>Y1 = imageHeight - min(startY, endY)"]
CalculateBounds --> PDFCoords["Convert to PDF Coordinates<br/>Bottom-left Origin"]
PDFCoords --> ValidateBounds["Validate Bounds<br/>Ensure X0 ≤ X1 and Y0 ≤ Y1"]
ValidateBounds --> StorePreset["Store in ExtractionPreset"]
StorePreset --> ExtractText["Use in Text Extraction"]
ExtractText --> FilterWords["Filter Words Inside Rectangle<br/>word.Left ≥ X0 AND<br/>word.Right ≤ X1 AND<br/>word.Bottom ≥ Y0 AND<br/>word.Top ≤ Y1"]
FilterWords --> OutputResults["Return Extracted Text"]
```

**Diagram sources**
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L106-L117)
- [PdfService.cs](file://src\PdfAnnotator.App\Services\PdfService.cs#L158-L162)

### Coordinate Validation
The system ensures coordinate validity through several mechanisms:
- **Boundary Checking**: X0 ≤ X1 and Y0 ≤ Y1 constraints
- **Range Validation**: Coordinates must fall within PDF page dimensions
- **Type Safety**: All coordinates use `double` precision for accuracy

**Section sources**
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L106-L117)
- [PdfService.cs](file://src\PdfAnnotator.App\Services\PdfService.cs#L158-L162)

## JSON Serialization Format

The ExtractionPreset is serialized to JSON format for persistent storage and cross-platform compatibility. The serialization follows a straightforward structure:

### Standard JSON Schema
```json
{
  "name": "PresetName",
  "x0": 100.0,
  "y0": 100.0,
  "x1": 300.0,
  "y1": 200.0
}
```

### JSON Property Mapping
| JSON Property | C# Property | Type | Description |
|---------------|-------------|------|-------------|
| `name` | `Name` | string | Unique preset identifier |
| `x0` | `X0` | double | Left boundary coordinate |
| `y0` | `Y0` | double | Bottom boundary coordinate |
| `x1` | `X1` | double | Right boundary coordinate |
| `y1` | `Y1` | double | Top boundary coordinate |

### Serialization Configuration
The JSON serialization uses the following configuration:
- **Formatting**: Indented for readability
- **Property Naming**: Lowercase with hyphens (camelCase)
- **Precision**: Full double precision maintained
- **Validation**: Automatic type checking during deserialization

**Section sources**
- [Example.json](file://presets/extraction/Example.json#L1-L8)
- [PresetService.cs](file://src\PdfAnnotator.Core\Services\PresetService.cs#L11-L14)

## Storage and Management

### Directory Structure
Extraction presets are stored in the `presets/extraction/` directory with the following organization:

```
presets/
└── extraction/
    ├── Example.json          # Sample preset file
    ├── InvoiceTemplate.json  # Business template preset
    ├── FormLayout.json       # Form extraction preset
    └── CustomRegion.json     # User-defined preset
```

### File Naming Convention
- **Pattern**: `{preset-name}.json`
- **Naming Rules**: Alphanumeric characters, spaces, and special characters allowed
- **Uniqueness**: Names must be unique within the directory
- **Default Naming**: Generated timestamps for unnamed presets

### Storage Operations

```mermaid
sequenceDiagram
participant UI as "Extraction UI"
participant VM as "ExtractionViewModel"
participant PS as "PresetService"
participant FS as "File System"
UI->>VM : Save Preset Request
VM->>VM : Create ExtractionPreset Object
VM->>PS : SaveExtractionPresetAsync(preset)
PS->>FS : Create Directory (if needed)
PS->>FS : Serialize to JSON
PS->>FS : Write to {name}.json
FS-->>PS : Confirmation
PS-->>VM : Save Complete
VM->>VM : Reload Presets
VM->>UI : Update Preset List
```

**Diagram sources**
- [PresetService.cs](file://src\PdfAnnotator.Core\Services\PresetService.cs#L16-L20)
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L134-L147)

**Section sources**
- [PresetService.cs](file://src\PdfAnnotator.Core\Services\PresetService.cs#L8-L20)

## Usage in Text Extraction Workflow

### Integration with PdfService

The ExtractionPreset serves as the primary input parameter for the text extraction process:

```mermaid
sequenceDiagram
participant User as "User Interface"
participant VM as "ExtractionViewModel"
participant PS as "PdfService"
participant PDF as "PDF Document"
participant Word as "Word Processor"
User->>VM : Select Extraction Area
VM->>VM : Update Coordinates (X0, Y0, X1, Y1)
User->>VM : Click Extract Button
VM->>VM : Create ExtractionPreset
VM->>PS : ExtractTextAsync(pdfPath, preset)
PS->>PDF : Open PDF Document
loop For Each Page
PS->>PDF : Get Page Content
PS->>Word : Filter Words by Rectangle
Note over Word : word.Left ≥ X0 AND<br/>word.Right ≤ X1 AND<br/>word.Bottom ≥ Y0 AND<br/>word.Top ≤ Y1
PS->>PS : Collect Matching Text
end
PS-->>VM : Return Extracted Rows
VM-->>User : Display Results in Table
```

**Diagram sources**
- [PdfService.cs](file://src\PdfAnnotator.App\Services\PdfService.cs#L100-L126)
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L119-L132)

### Text Filtering Algorithm

The core filtering mechanism uses a spatial intersection algorithm:

```csharp
private static bool IsInside(Word word, ExtractionPreset preset)
{
    var rect = word.BoundingBox;
    return rect.Left >= preset.X0 && 
           rect.Right <= preset.X1 && 
           rect.Bottom >= preset.Y0 && 
           rect.Top <= preset.Y1;
}
```

This algorithm ensures that only words completely contained within the specified rectangle are extracted.

**Section sources**
- [PdfService.cs](file://src\PdfAnnotator.App\Services\PdfService.cs#L100-L126)
- [PdfService.cs](file://src\PdfAnnotator.App\Services\PdfService.cs#L158-L162)

## UI Integration and Selection

### Preset Selection Interface

The user interface provides intuitive preset management through the ExtractionViewModel:

```mermaid
classDiagram
class ExtractionViewModel {
+ObservableCollection~ExtractionPreset~ Presets
+ExtractionPreset SelectedPreset
+string SelectedPresetName
+LoadPresetsAsync() Task
+LoadPresetFromFileAsync(path) Task
+ApplyPreset() void
+SavePresetAsync() Task
}
class ExtractionView {
+ComboBox PresetSelector
+Button SavePresetButton
+Button LoadPresetButton
+OnLoadPresetClick(sender, e) void
}
class PresetService {
+LoadAllExtractionPresetsAsync() Task~ExtractionPreset[]~
+LoadExtractionPresetAsync(path) Task~ExtractionPreset~
+SaveExtractionPresetAsync(preset) Task
}
ExtractionView --> ExtractionViewModel : "binds to"
ExtractionViewModel --> PresetService : "uses"
```

**Diagram sources**
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L55-L62)
- [ExtractionView.axaml.cs](file://src\PdfAnnotator.App\Views\ExtractionView.axaml.cs#L107-L137)

### Selection Process

1. **Preset Loading**: Available presets are loaded from the `presets/extraction/` directory
2. **Selection**: Users select a preset from the dropdown menu
3. **Application**: Selected preset coordinates are applied to the extraction area
4. **Validation**: Coordinates are validated against current PDF page dimensions

### Dynamic Preset Management

The system supports real-time preset operations:
- **Loading**: Presets can be loaded from JSON files
- **Saving**: Current extraction area becomes a named preset
- **Reloading**: Preset list refreshes automatically
- **Deletion**: Not directly supported (requires file system manipulation)

**Section sources**
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L168-L196)
- [ExtractionView.axaml.cs](file://src\PdfAnnotator.App\Views\ExtractionView.axaml.cs#L107-L137)

## Coordinate Transformation Process

### Mouse-to-PDF Coordinate Conversion

The coordinate transformation involves multiple steps to convert user interface coordinates to PDF-compatible coordinates:

```mermaid
flowchart TD
MouseDown["Mouse Down Event"] --> CaptureStart["Capture Start Position<br/>startX, startY"]
MouseMove["Mouse Move Event"] --> CaptureEnd["Capture End Position<br/>endX, endY"]
MouseUp["Mouse Up Event"] --> CalculateSelection["Calculate Selection Bounds<br/>SelectLeft = min(startX, endX)<br/>SelectTop = min(startY, endY)<br/>SelectWidth = abs(endX - startX)<br/>SelectHeight = abs(endY - startY)"]
CalculateSelection --> CalculateCoordinates["Calculate Coordinate Boundaries<br/>X0 = min(startX, endX)<br/>X1 = max(startX, endX)<br/>Y0 = imageHeight - max(startY, endY)<br/>Y1 = imageHeight - min(startY, endY)"]
CalculateCoordinates --> ValidatePDF["Validate PDF Coordinates<br/>Ensure within page bounds"]
ValidatePDF --> StorePreset["Store in ExtractionPreset"]
```

**Diagram sources**
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L106-L117)

### Coordinate System Differences

The transformation accounts for differences between:
- **Image Coordinates**: Top-left origin, scaled to UI dimensions
- **PDF Coordinates**: Bottom-left origin, unscaled to actual page dimensions
- **Word Coordinates**: PDF coordinate system, used for text filtering

### Precision Considerations

- **Double Precision**: All coordinates use `double` type for accuracy
- **Rounding**: Minimal rounding occurs during coordinate calculations
- **Validation**: Boundary checks prevent invalid coordinate combinations

**Section sources**
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L106-L117)

## Workflow for Creating and Applying Presets

### Creation Workflow

The preset creation process follows these steps:

```mermaid
flowchart TD
Start([User Opens PDF]) --> SelectArea["Select Extraction Area<br/>Draw Rectangle on Page"]
SelectArea --> UpdateCoords["Update ViewModel Coordinates<br/>X0, Y0, X1, Y1"]
UpdateCoords --> SaveChoice{"Save as Preset?"}
SaveChoice --> |Yes| GenerateName["Generate Preset Name<br/>Use Selected Name or Timestamp"]
SaveChoice --> |No| ExtractDirectly["Extract Text Directly"]
GenerateName --> SavePreset["Save to File System<br/>presets/extraction/{name}.json"]
SavePreset --> ReloadPresets["Reload Preset List"]
ReloadPresets --> SelectSaved["Select Saved Preset"]
SelectSaved --> ExtractDirectly
ExtractDirectly --> DisplayResults["Display Extracted Text"]
```

**Diagram sources**
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L134-L147)

### Application Workflow

When applying an existing preset:

```mermaid
flowchart TD
LoadPresets["Load Available Presets"] --> SelectPreset["User Selects Preset"]
SelectPreset --> ApplyPreset["Apply Preset Coordinates<br/>X0 = preset.X0<br/>Y0 = preset.Y0<br/>X1 = preset.X1<br/>Y1 = preset.Y1"]
ApplyPreset --> ValidateBounds["Validate Coordinate Bounds"]
ValidateBounds --> ReadyForExtraction["Ready for Text Extraction"]
ReadyForExtraction --> ExtractText["Extract Text Using Preset"]
```

**Diagram sources**
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L183-L194)

### Default Values and Behaviors

- **Default Name**: Uses "Current" when no preset is selected
- **Timestamp Generation**: Presets without explicit names get generated timestamps
- **Empty State**: Initial coordinates are zero until selection is made
- **Validation**: Coordinates are validated before extraction attempts

**Section sources**
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L119-L147)

## Best Practices and Considerations

### Coordinate Management
- **Boundary Validation**: Always validate that X0 ≤ X1 and Y0 ≤ Y1
- **PDF Dimension Awareness**: Ensure coordinates fit within actual PDF page dimensions
- **Precision Maintenance**: Use double precision for all coordinate calculations
- **Coordinate System Consistency**: Maintain bottom-left origin convention

### Preset Organization
- **Descriptive Naming**: Use meaningful names that describe the extraction purpose
- **Template Categories**: Group related presets by document type or layout
- **Version Control**: Consider backing up important preset collections
- **Documentation**: Add comments or external documentation for complex extraction scenarios

### Performance Considerations
- **Caching**: The PdfService implements rendering cache for improved performance
- **Batch Processing**: Extract multiple pages efficiently using the same preset
- **Memory Management**: Dispose of PDF resources properly in long-running operations

### Error Handling
- **File Access**: Handle file system errors during preset loading/saving
- **Coordinate Validation**: Validate coordinates before attempting extraction
- **PDF Compatibility**: Handle various PDF formats and page sizes
- **Resource Cleanup**: Ensure proper disposal of PDF resources

## Troubleshooting Guide

### Common Issues and Solutions

#### Issue: Coordinates Outside PDF Bounds
**Symptoms**: Extraction returns empty results or throws exceptions
**Cause**: Preset coordinates exceed PDF page dimensions
**Solution**: 
- Verify PDF page dimensions using `GetPageCountAsync()` and page metadata
- Adjust preset coordinates to fit within page boundaries
- Use the coordinate validation features built into the system

#### Issue: Preset Not Loading
**Symptoms**: Preset does not appear in dropdown list
**Cause**: JSON parsing errors or file system issues
**Solution**:
- Check JSON syntax in preset file
- Verify file permissions and accessibility
- Review application logs for detailed error messages

#### Issue: Inconsistent Extraction Results
**Symptoms**: Same preset produces different results across documents
**Cause**: Coordinate system mismatches or PDF scaling issues
**Solution**:
- Verify PDF page dimensions match preset expectations
- Check for PDF compression or rendering variations
- Recreate preset with precise measurements

#### Issue: Performance Problems
**Symptoms**: Slow extraction or UI responsiveness
**Cause**: Large PDF files or inefficient coordinate calculations
**Solution**:
- Optimize PDF rendering DPI settings
- Reduce extraction area size when possible
- Monitor memory usage during batch operations

### Debugging Tools and Techniques

1. **Logging**: Enable detailed logging in the PdfService for coordinate tracking
2. **Visualization**: Overlay extraction rectangles on rendered PDF pages
3. **Validation**: Use coordinate validation methods to verify boundary correctness
4. **Testing**: Create test cases with known document layouts and expected results

**Section sources**
- [PdfService.cs](file://src\PdfAnnotator.App\Services\PdfService.cs#L24-L27)
- [ExtractionViewModel.cs](file://src\PdfAnnotator.ViewModels\ExtractionViewModel.cs#L85-L94)