using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Models
{
    public class ProductSkusResponse
    {
        [JsonProperty("IsPersisted")]
        public bool IsPersisted { get; set; }

        [JsonProperty("IsRemoved")]
        public bool IsRemoved { get; set; }

        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("ProductId")]
        public string ProductId { get; set; }

        [JsonProperty("IsActive")]
        public bool IsActive { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Height")]
        public long Height { get; set; }

        [JsonProperty("RealHeight")]
        public object RealHeight { get; set; }

        [JsonProperty("Width")]
        public long Width { get; set; }

        [JsonProperty("RealWidth")]
        public object RealWidth { get; set; }

        [JsonProperty("Length")]
        public long Length { get; set; }

        [JsonProperty("RealLength")]
        public object RealLength { get; set; }

        [JsonProperty("WeightKg")]
        public long WeightKg { get; set; }

        [JsonProperty("RealWeightKg")]
        public object RealWeightKg { get; set; }

        [JsonProperty("ModalId")]
        public long ModalId { get; set; }

        [JsonProperty("RefId")]
        public string RefId { get; set; }

        [JsonProperty("CubicWeight")]
        public long CubicWeight { get; set; }

        [JsonProperty("IsKit")]
        public bool IsKit { get; set; }

        [JsonProperty("IsDynamicKit")]
        public object IsDynamicKit { get; set; }

        [JsonProperty("InternalNote")]
        public object InternalNote { get; set; }

        [JsonProperty("DateUpdated")]
        public DateTimeOffset DateUpdated { get; set; }

        [JsonProperty("RewardValue")]
        public object RewardValue { get; set; }

        [JsonProperty("CommercialConditionId")]
        public object CommercialConditionId { get; set; }

        [JsonProperty("EstimatedDateArrival")]
        public object EstimatedDateArrival { get; set; }

        [JsonProperty("FlagKitItensSellApart")]
        public bool FlagKitItensSellApart { get; set; }

        [JsonProperty("ManufacturerCode")]
        public object ManufacturerCode { get; set; }

        [JsonProperty("ReferenceStockKeepingUnitId")]
        public object ReferenceStockKeepingUnitId { get; set; }

        [JsonProperty("Position")]
        public long Position { get; set; }

        [JsonProperty("EditionSkuId")]
        public object EditionSkuId { get; set; }

        [JsonProperty("ApprovedAdminId")]
        public object ApprovedAdminId { get; set; }

        [JsonProperty("EditionAdminId")]
        public object EditionAdminId { get; set; }

        [JsonProperty("ActivateIfPossible")]
        public bool ActivateIfPossible { get; set; }

        [JsonProperty("SupplierCode")]
        public object SupplierCode { get; set; }

        [JsonProperty("MeasurementUnit")]
        public string MeasurementUnit { get; set; }

        [JsonProperty("UnitMultiplier")]
        public long UnitMultiplier { get; set; }

        [JsonProperty("IsInventoried")]
        public object IsInventoried { get; set; }

        [JsonProperty("IsTransported")]
        public object IsTransported { get; set; }

        [JsonProperty("IsGiftCardRecharge")]
        public object IsGiftCardRecharge { get; set; }

        [JsonProperty("ModalType")]
        public object ModalType { get; set; }

        [JsonProperty("isKitOptimized")]
        public bool IsKitOptimized { get; set; }
    }
}
