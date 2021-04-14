using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class SkuSpecification
    {
        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("SkuId")]
        public long SkuId { get; set; }

        [JsonProperty("FieldId")]
        public long FieldId { get; set; }

        [JsonProperty("FieldValueId")]
        public long FieldValueId { get; set; }

        [JsonProperty("Text")]
        public string Text { get; set; }
    }
}
