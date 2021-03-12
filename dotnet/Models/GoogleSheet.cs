using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class GoogleSheet
    {
        [JsonProperty("spreadsheetId")]
        public string SpreadsheetId { get; set; }

        [JsonProperty("valueRanges")]
        public ValueRange[] ValueRanges { get; set; }
    }

    public partial class ValueRange
    {
        [JsonProperty("range")]
        public string Range { get; set; }

        [JsonProperty("majorDimension")]
        public string MajorDimension { get; set; }

        [JsonProperty("values")]
        public string[][] Values { get; set; }
    }
}
