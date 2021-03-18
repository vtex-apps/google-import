using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class InventoryRequest
    {
        [JsonProperty("unlimitedQuantity")]
        public bool UnlimitedQuantity { get; set; }

        [JsonProperty("dateUtcOnBalanceSystem")]
        public string DateUtcOnBalanceSystem { get; set; }

        [JsonProperty("quantity")]
        public long Quantity { get; set; }
    }
}
