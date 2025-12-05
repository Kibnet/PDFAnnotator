using System.IO;
using PdfAnnotator.App.ViewModels;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace PdfAnnotator.Tests;

public class ExtractionViewModelTests
{
    [Fact]
    public void ShouldApplyPresetValuesWhenSelectedPresetChanges()
    {
        // Arrange
        var pdfServiceMock = new Mock<IPdfService>();
        var presetServiceMock = new Mock<IPresetService<ExtractionPreset>>();
        var loggerMock = new Mock<ILogger<ExtractionViewModel>>();
        
        var viewModel = new ExtractionViewModel(
            pdfServiceMock.Object, 
            presetServiceMock.Object, 
            loggerMock.Object);
            
        var preset = new ExtractionPreset
        {
            Name = "TestPreset",
            X0 = 100,
            Y0 = 200,
            X1 = 300,
            Y1 = 400
        };

        // Act
        viewModel.SelectedPreset = preset;

        // Assert
        Assert.Equal(100, viewModel.X0);
        Assert.Equal(200, viewModel.Y0);
        Assert.Equal(300, viewModel.X1);
        Assert.Equal(400, viewModel.Y1);
    }
    
    [Fact]
    public async Task ShouldUpdateExtractedTextPreviewWhenPresetIsSelected()
    {
        // Arrange
        var pdfServiceMock = new Mock<IPdfService>();
        var presetServiceMock = new Mock<IPresetService<ExtractionPreset>>();
        var loggerMock = new Mock<ILogger<ExtractionViewModel>>();
        
        // Mock the extraction service to return some test data
        var testRows = new List<TableRow>
        {
            new() { Page = 1, FieldText = "Test text 1", Code = "" },
            new() { Page = 1, FieldText = "Test text 2", Code = "" }
        };
        
        pdfServiceMock.Setup(x => x.ExtractTextAsync(It.IsAny<string>(), It.IsAny<ExtractionPreset>()))
            .ReturnsAsync(testRows);
        
        // Mock the new ExtractTextFromPageAsync method used by preview
        pdfServiceMock.Setup(x => x.ExtractTextFromPageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<ExtractionPreset>()))
            .ReturnsAsync("Test text 1 Test text 2");
            
        var viewModel = new ExtractionViewModel(
            pdfServiceMock.Object, 
            presetServiceMock.Object, 
            loggerMock.Object)
            {
                // Use a temporary file path that exists for testing
                PdfPath = Path.GetTempFileName(),
                CurrentPage = 1
            };
            
        var preset = new ExtractionPreset
        {
            Name = "TestPreset",
            X0 = 100,
            Y0 = 200,
            X1 = 300,
            Y1 = 400
        };

        // Act
        viewModel.SelectedPreset = preset;
        
        // Give some time for the async operation to complete
        await Task.Delay(100);

        // Assert
        Assert.Contains("Test text 1", viewModel.ExtractedTextPreview);
        Assert.Contains("Test text 2", viewModel.ExtractedTextPreview);
        
        // Clean up the temporary file
        if (File.Exists(viewModel.PdfPath))
        {
            File.Delete(viewModel.PdfPath);
        }
    }
}