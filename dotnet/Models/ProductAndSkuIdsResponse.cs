using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class ProductAndSkuIdsResponse
    {
        [JsonProperty("data")]
        public Dictionary<string, long[]> Data { get; set; }

        [JsonProperty("range")]
        public Range Range { get; set; }
    }

    public class Range
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("from")]
        public long From { get; set; }

        [JsonProperty("to")]
        public long To { get; set; }
    }
}
