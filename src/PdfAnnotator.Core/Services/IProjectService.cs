using System.Threading.Tasks;
using PdfAnnotator.Core.Models;

namespace PdfAnnotator.Core.Services;

public interface IProjectService
{
    Task SaveProjectAsync(PdfProject project, string path);
    Task<PdfProject> LoadProjectAsync(string path);
}
