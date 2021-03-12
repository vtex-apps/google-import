using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class GetBrandListResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("metaTagDescription")]
        public string MetaTagDescription { get; set; }

        [JsonProperty("imageUrl")]
        public object ImageUrl { get; set; }
    }
}
