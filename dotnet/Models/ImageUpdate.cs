using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class ImageUpdate
    {
        [JsonProperty("IsMain")]
        public bool IsMain { get; set; }

        [JsonProperty("Label")]
        public object Label { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Text")]
        public object Text { get; set; }

        [JsonProperty("Url")]
        public string Url { get; set; }
    }
}
