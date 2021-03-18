using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class CreatePrice
    {
        [JsonProperty("basePrice")]
        public decimal BasePrice { get; set; }

        [JsonProperty("listPrice")]
        public decimal ListPrice { get; set; }
    }
}
