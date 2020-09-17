using PDFLibrary.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PDFLibrary.Api.Services
{
    public interface IPDFStoreBlobStorage
    {
        Task Add(PdfFile file);
        Task Delete(string fileName);
        Task<PdfFile> Download(string fileName);
        Task<List<PdfFileListItem>> List();
        Task ReOrder(List<string> newOrder);
        Task<bool> CheckExists(string fileName);
    }
}