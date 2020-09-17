using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PDFLibrary.Api.Models
{
    public class PdfFile
    {
        public string Name{ get; set; }
        public Stream Content { get; set; }
        public string ContentType { get; set; }
    }
}
