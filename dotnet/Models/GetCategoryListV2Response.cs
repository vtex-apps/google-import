namespace SheetsCatalogImport.Models
{
    using System;
    using Newtonsoft.Json;

    public class GetCategoryListV2Response
    {
        [JsonProperty("roots")]
        public Root[] Roots { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class Root
    {
        [JsonProperty("value")]
        public CategoryValue Value { get; set; }

        [JsonProperty("children")]
        public object[] Children { get; set; }
    }

    public class CategoryValue
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        [JsonProperty("displayOnMenu")]
        public bool DisplayOnMenu { get; set; }

        [JsonProperty("score")]
        public long Score { get; set; }

        [JsonProperty("filterByBrand")]
        public bool FilterByBrand { get; set; }

        [JsonProperty("productExhibitionMode")]
        public string ProductExhibitionMode { get; set; }

        [JsonProperty("isClickable")]
        public bool IsClickable { get; set; }
    }
}
