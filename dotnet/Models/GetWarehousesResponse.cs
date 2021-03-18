using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class GetWarehousesResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("warehouseDocks")]
        public WarehouseDock[] WarehouseDocks { get; set; }

        [JsonProperty("pickupPointIds")]
        public string[] PickupPointIds { get; set; }

        [JsonProperty("priority")]
        public long Priority { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }
    }

    public class WarehouseDock
    {
        [JsonProperty("dockId")]
        public string DockId { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("cost")]
        public decimal Cost { get; set; }
    }
}
