using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PdfAnnotator.App.Logging;
using PdfAnnotator.App.Services;
using PdfAnnotator.App.ViewModels;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;

namespace PdfAnnotator.App.Services;

public static class AppBootstrapper
{
    public static ServiceProvider Configure()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new FileLoggerProvider("logs/app.log"));
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<ICsvService, CsvService>();
        services.AddSingleton<IPresetService<ExtractionPreset>>(sp => new PresetService<ExtractionPreset>("presets/extraction"));
        services.AddSingleton<IPresetService<AnnotationPreset>>(sp => new PresetService<AnnotationPreset>("presets/annotation"));
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IPdfService, PdfService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ExtractionViewModel>();
        services.AddSingleton<TableViewModel>();
        services.AddSingleton<AnnotationViewModel>();

        return services.BuildServiceProvider();
    }
}
