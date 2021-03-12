using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class UpdateValuesResponse
    {
        [JsonProperty("spreadsheetId")]
        public string SpreadsheetId { get; set; }

        [JsonProperty("updatedRange")]
        public string UpdatedRange { get; set; }

        [JsonProperty("updatedRows")]
        public long UpdatedRows { get; set; }

        [JsonProperty("updatedColumns")]
        public long UpdatedColumns { get; set; }

        [JsonProperty("updatedCells")]
        public long UpdatedCells { get; set; }
    }
}
