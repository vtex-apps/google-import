using Newtonsoft.Json;
using System;

namespace SheetsCatalogImport.Models
{
    public class Credentials
    {
        [JsonProperty("web")]
        public Web Web { get; set; }
    }

    public class Web
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("project_id")]
        public string ProjectId { get; set; }

        [JsonProperty("auth_uri")]
        public Uri AuthUri { get; set; }

        [JsonProperty("token_uri")]
        public Uri TokenUri { get; set; }

        [JsonProperty("auth_provider_x509_cert_url")]
        public Uri AuthProviderX509CertUrl { get; set; }

        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }
    }
}
