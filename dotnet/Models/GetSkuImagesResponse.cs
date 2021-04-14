using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class GetSkuImagesResponse
    {
        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("SkuId")]
        public long SkuId { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("IsMain")]
        public bool IsMain { get; set; }

        [JsonProperty("Label")]
        public string Label { get; set; }
    }
}
