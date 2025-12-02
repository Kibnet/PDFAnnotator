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
        var presetServiceMock = new Mock<IPresetService>();
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
}