using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class GetPriceResponse
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; }

        [JsonProperty("listPrice")]
        public long ListPrice { get; set; }

        [JsonProperty("costPrice")]
        public long CostPrice { get; set; }

        [JsonProperty("markup")]
        public long Markup { get; set; }

        [JsonProperty("basePrice")]
        public long BasePrice { get; set; }
    }
}
