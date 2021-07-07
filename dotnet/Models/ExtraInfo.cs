using Newtonsoft.Json;
using System;

namespace SheetsCatalogImport.Models
{
    public class ExtraInfo
    {
        [JsonProperty("productId")]
        public string ProductId { get; set; }

        [JsonProperty("storeFront")]
        public StoreFront StoreFront { get; set; }
    }

    public class StoreFront
    {
        [JsonProperty("showOutOfStock")]
        public bool ShowOutOfStock { get; set; }
    }
}
