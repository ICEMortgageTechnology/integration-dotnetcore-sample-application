using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Models
{
    public class FileInfo
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
        public string FilePath { get; set; }
        public string FileDirectory { get; set; }
    }
}
