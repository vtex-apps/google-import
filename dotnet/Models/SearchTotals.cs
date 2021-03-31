using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class SearchTotals
    {
        [JsonProperty("TotalRecords")]
        public long TotalRecords { get; set; }

        [JsonProperty("Products")]
        public long Products { get; set; }

        [JsonProperty("Skus")]
        public long Skus { get; set; }

        [JsonProperty("Brands")]
        public long Brands { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }
    }
}
