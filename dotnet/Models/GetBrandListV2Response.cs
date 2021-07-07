namespace SheetsCatalogImport.Models
{
    using System;
    using Newtonsoft.Json;

    public class GetBrandListV2Response
    {
        [JsonProperty("data")]
        public Datum[] Data { get; set; }

        [JsonProperty("_metadata")]
        public Metadata Metadata { get; set; }
    }

    public class Datum
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        [JsonProperty("score")]
        public long Score { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("displayOnMenu")]
        public bool DisplayOnMenu { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("from")]
        public long From { get; set; }

        [JsonProperty("to")]
        public long To { get; set; }

        [JsonProperty("orderBy")]
        public string OrderBy { get; set; }
    }
}
