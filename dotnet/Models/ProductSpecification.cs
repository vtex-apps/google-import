using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class ProductSpecification
    {
        [JsonProperty("Value")]
        public string[] Value { get; set; }

        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }
    }
}
