using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class CreateSkuRequest
    {
        [JsonProperty("ProductId")]
        public long ProductId { get; set; }

        [JsonProperty("IsActive")]
        public bool IsActive { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("RefId")]
        public string RefId { get; set; }

        [JsonProperty("PackagedHeight")]
        public double? PackagedHeight { get; set; }

        [JsonProperty("PackagedLength")]
        public double? PackagedLength { get; set; }

        [JsonProperty("PackagedWidth")]
        public double? PackagedWidth { get; set; }

        [JsonProperty("PackagedWeightKg")]
        public double? PackagedWeightKg { get; set; }

        [JsonProperty("Height")]
        public double? Height { get; set; }

        [JsonProperty("Length")]
        public double? Length { get; set; }

        [JsonProperty("Width")]
        public double? Width { get; set; }

        [JsonProperty("WeightKg")]
        public double? WeightKg { get; set; }

        [JsonProperty("CubicWeight")]
        public double? CubicWeight { get; set; }

        [JsonProperty("IsKit")]
        public bool IsKit { get; set; }

        [JsonProperty("CreationDate")]
        public string CreationDate { get; set; }

        [JsonProperty("RewardValue")]
        public object RewardValue { get; set; }

        [JsonProperty("EstimatedDateArrival")]
        public string EstimatedDateArrival { get; set; }

        [JsonProperty("ManufacturerCode")]
        public string ManufacturerCode { get; set; }

        [JsonProperty("CommercialConditionId")]
        public long? CommercialConditionId { get; set; }

        [JsonProperty("MeasurementUnit")]
        public string MeasurementUnit { get; set; }

        [JsonProperty("UnitMultiplier")]
        public long? UnitMultiplier { get; set; }

        [JsonProperty("ModalType")]
        public object ModalType { get; set; }

        [JsonProperty("KitItensSellApart")]
        public bool KitItensSellApart { get; set; }
    }
}

