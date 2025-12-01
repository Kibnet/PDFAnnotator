using System.Collections.Generic;
using System.Threading.Tasks;
using PdfAnnotator.Core.Models;

namespace PdfAnnotator.Core.Services;

public interface ICsvService
{
    Task SaveCsvAsync(string path, List<TableRow> rows);
    Task<List<TableRow>> LoadCsvAsync(string path);
}
