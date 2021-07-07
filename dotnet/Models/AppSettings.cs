using Newtonsoft.Json;

namespace SheetsCatalogImport.Models
{
    public class AppSettings
    {
        [JsonProperty("isV2Catalog")]
        public bool IsV2Catalog { get; set; }
    }
}