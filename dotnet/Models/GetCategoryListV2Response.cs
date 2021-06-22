namespace SheetsCatalogImport.Models
{
    using System;
    using Newtonsoft.Json;

    public class GetCategoryListV2Response
    {
        [JsonProperty("data")]
        public Datum[] Data { get; set; }

        [JsonProperty("_metadata")]
        public Metadata Metadata { get; set; }
    }
}
