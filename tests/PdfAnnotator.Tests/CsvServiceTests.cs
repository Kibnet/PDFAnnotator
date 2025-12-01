using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;

namespace PdfAnnotator.Tests;

public class CsvServiceTests
{
    [Fact]
    public async Task SaveAndLoad_ShouldRoundtrip()
    {
        var service = new CsvService();
        var rows = new List<TableRow>
        {
            new() { Page = 1, FieldText = "text1", Code = "ABC" },
            new() { Page = 2, FieldText = "text2", Code = "" }
        };

        var path = Path.GetTempFileName();
        await service.SaveCsvAsync(path, rows);

        var loaded = await service.LoadCsvAsync(path);
        Assert.Equal(rows.Count, loaded.Count);
        Assert.Equal(rows[0].FieldText, loaded[0].FieldText);
        Assert.Equal(rows[1].Code, loaded[1].Code);
    }
}
