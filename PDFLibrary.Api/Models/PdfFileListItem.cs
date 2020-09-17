using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PDFLibrary.Api.Models
{
    public class PdfFileListItem
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public long FileSize { get; set; }
    }
}
