using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class ProductSearchResponse
    {
        [JsonProperty("productId")]
        public string ProductId { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("brand")]
        public string Brand { get; set; }

        [JsonProperty("brandId")]
        public long BrandId { get; set; }

        [JsonProperty("brandImageUrl")]
        public object BrandImageUrl { get; set; }

        [JsonProperty("linkText")]
        public string LinkText { get; set; }

        [JsonProperty("productReference")]
        public string ProductReference { get; set; }

        [JsonProperty("categoryId")]
        public string CategoryId { get; set; }

        [JsonProperty("productTitle")]
        public string ProductTitle { get; set; }

        [JsonProperty("metaTagDescription")]
        public string MetaTagDescription { get; set; }

        [JsonProperty("releaseDate")]
        public DateTimeOffset ReleaseDate { get; set; }

        [JsonProperty("productClusters")]
        public Dictionary<string, string> ProductClusters { get; set; }

        [JsonProperty("searchableClusters")]
        public Dictionary<string, string> SearchableClusters { get; set; }

        [JsonProperty("categories")]
        public string[] Categories { get; set; }

        [JsonProperty("categoriesIds")]
        public string[] CategoriesIds { get; set; }

        [JsonProperty("link")]
        public Uri Link { get; set; }
    }
}
