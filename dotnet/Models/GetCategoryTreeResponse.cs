using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class GetCategoryTreeResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("hasChildren")]
        public bool HasChildren { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("children")]
        public GetCategoryTreeResponse[] Children { get; set; }

        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("MetaTagDescription")]
        public string MetaTagDescription { get; set; }
    }
}
