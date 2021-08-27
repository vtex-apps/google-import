namespace SheetsCatalogImport.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ProductRequestV2
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("externalId")]
        public string ExternalId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("brandId")]
        public string BrandId { get; set; }

        [JsonProperty("brandName")]
        public string BrandName { get; set; }

        [JsonProperty("categoryPath")]
        public string CategoryPath { get; set; }

        [JsonProperty("categoryIds")]
        public string[] CategoryIds { get; set; }

        [JsonProperty("salesChannels")]
        public string[] SalesChannels { get; set; }

        [JsonProperty("specs")]
        public ProductV2Spec[] Specs { get; set; }

        [JsonProperty("attributes")]
        public AttributeV2[] Attributes { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("images")]
        public ProductV2Image[] Images { get; set; }

        [JsonProperty("skus")]
        //public Skus[] Skus { get; set; }
        public List<Skus> Skus { get; set; }

        [JsonProperty("channels")]
        public Channel[] Channels { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AttributeV2
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("isFilterable")]
        public bool IsFilterable { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Channel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("productId")]
        public string ProductId { get; set; }

        [JsonProperty("categoryId")]
        public string CategoryId { get; set; }

        [JsonProperty("brandId")]
        public string BrandId { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ProductV2Image
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("alt")]
        public string Alt { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Skus
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("externalId")]
        public string ExternalId { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("sellers")]
        public Seller[] Sellers { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        [JsonProperty("weight")]
        public double? Weight { get; set; }

        [JsonProperty("dimensions")]
        public ProductV2Dimensions Dimensions { get; set; }

        [JsonProperty("specs")]
        public SkusSpec[] Specs { get; set; }

        [JsonProperty("images")]
        public ProductV2Image[] Images { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ean")]
        public string Ean { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ProductV2Dimensions
    {
        [JsonProperty("width")]
        public double? Width { get; set; }

        [JsonProperty("height")]
        public double? Height { get; set; }

        [JsonProperty("length")]
        public double? Length { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Seller
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class SkusSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ProductV2Spec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("values")]
        public string[] Values { get; set; }
    }
}
