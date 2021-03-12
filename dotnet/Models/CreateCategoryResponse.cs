using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class CreateCategoryResponse
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Keywords")]
        public string Keywords { get; set; }

        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("AdWordsRemarketingCode")]
        public object AdWordsRemarketingCode { get; set; }

        [JsonProperty("LomadeeCampaignCode")]
        public object LomadeeCampaignCode { get; set; }

        [JsonProperty("FatherCategoryId")]
        public object FatherCategoryId { get; set; }

        [JsonProperty("GlobalCategoryId")]
        public long GlobalCategoryId { get; set; }

        [JsonProperty("ShowInStoreFront")]
        public bool ShowInStoreFront { get; set; }

        [JsonProperty("IsActive")]
        public bool IsActive { get; set; }

        [JsonProperty("ActiveStoreFrontLink")]
        public bool ActiveStoreFrontLink { get; set; }

        [JsonProperty("ShowBrandFilter")]
        public bool ShowBrandFilter { get; set; }

        [JsonProperty("Score")]
        public int Score { get; set; }

        /// <summary>
        /// LIST	List of SKUs
        /// COMBO Combo Boxes
        /// RADIO   Icons with radio selection(radio box)
        /// SPECIFICATION Following definition of SKU specification
        /// </summary>
        [JsonProperty("StockKeepingUnitSelectionMode")]
        public string StockKeepingUnitSelectionMode { get; set; }

        [JsonProperty("LinkId")]
        public string LinkId { get; set; }

        [JsonProperty("HasChildren")]
        public bool HasChildren { get; set; }
    }
}
