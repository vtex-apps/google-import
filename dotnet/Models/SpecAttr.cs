using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class SpecAttr
    {
        [JsonProperty("FieldName")]
        public string FieldName { get; set; }

        [JsonProperty("GroupName")]
        public string GroupName { get; set; }

        [JsonProperty("RootLevelSpecification")]
        public bool RootLevelSpecification { get; set; }

        [JsonProperty("FieldValues")]
        public string[] FieldValues { get; set; }

        [JsonProperty("FieldValue")]
        public string FieldValue { get; set; }
    }
}
