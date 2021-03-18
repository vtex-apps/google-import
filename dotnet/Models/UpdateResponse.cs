using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class UpdateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string StatusCode { get; set; }
        public List<string> Results { get; set; }
    }
}
