using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class CreateSkuResponse
    {
        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("ProductId")]
        public long ProductId { get; set; }

        [JsonProperty("IsActive")]
        public bool IsActive { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("RefId")]
        public string RefId { get; set; }

        [JsonProperty("PackagedHeight")]
        public long PackagedHeight { get; set; }

        [JsonProperty("PackagedLength")]
        public long PackagedLength { get; set; }

        [JsonProperty("PackagedWidth")]
        public long PackagedWidth { get; set; }

        [JsonProperty("PackagedWeightKg")]
        public long PackagedWeightKg { get; set; }

        [JsonProperty("Height")]
        public object Height { get; set; }

        [JsonProperty("Length")]
        public object Length { get; set; }

        [JsonProperty("Width")]
        public object Width { get; set; }

        [JsonProperty("WeightKg")]
        public object WeightKg { get; set; }

        [JsonProperty("CubicWeight")]
        public double CubicWeight { get; set; }

        [JsonProperty("IsKit")]
        public bool IsKit { get; set; }

        [JsonProperty("CreationDate")]
        public object CreationDate { get; set; }

        [JsonProperty("RewardValue")]
        public object RewardValue { get; set; }

        [JsonProperty("EstimatedDateArrival")]
        public object EstimatedDateArrival { get; set; }

        [JsonProperty("ManufacturerCode")]
        public string ManufacturerCode { get; set; }

        [JsonProperty("CommercialConditionId")]
        public long CommercialConditionId { get; set; }

        [JsonProperty("MeasurementUnit")]
        public string MeasurementUnit { get; set; }

        [JsonProperty("UnitMultiplier")]
        public long UnitMultiplier { get; set; }

        [JsonProperty("ModalType")]
        public object ModalType { get; set; }

        [JsonProperty("KitItensSellApart")]
        public bool KitItensSellApart { get; set; }
    }
}

