using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class SheetRange
    {
        [JsonProperty("ranges")]
        public List<string> Ranges { get; set; }
    }
}
