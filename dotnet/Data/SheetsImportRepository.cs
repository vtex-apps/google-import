namespace SheetsCatalogImport.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using SheetsCatalogImport.Models;
    using SheetsCatalogImport.Services;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using Vtex.Api.Context;

    public class SheetsCatalogImportRepository : ISheetsCatalogImportRepository
    {
        private readonly IVtexEnvironmentVariableProvider _environmentVariableProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IIOServiceContext _context;
        private readonly string _applicationName;

        public SheetsCatalogImportRepository(IVtexEnvironmentVariableProvider environmentVariableProvider, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, IIOServiceContext context)
        {
            this._environmentVariableProvider = environmentVariableProvider ??
                                                throw new ArgumentNullException(nameof(environmentVariableProvider));

            this._httpContextAccessor = httpContextAccessor ??
                                        throw new ArgumentNullException(nameof(httpContextAccessor));

            this._clientFactory = clientFactory ??
                               throw new ArgumentNullException(nameof(clientFactory));

            this._context = context ??
                               throw new ArgumentNullException(nameof(context));

            this._applicationName =
                $"{this._environmentVariableProvider.ApplicationVendor}.{this._environmentVariableProvider.ApplicationName}";
        }

        public async Task<Token> LoadToken()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._environmentVariableProvider.Workspace}/buckets/{this._applicationName}/{SheetsCatalogImportConstants.BUCKET}/files/{SheetsCatalogImportConstants.TOKEN}")
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            request.Headers.Add("Cache-Control", "no-cache");

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _context.Vtex.Logger.Info("LoadToken", null, "Token not found!");
                return null;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            _context.Vtex.Logger.Info("LoadToken", null, responseContent);
            Token token = JsonConvert.DeserializeObject<Token>(responseContent);

            return token;
        }

        public async Task<bool> SaveToken(Token token)
        {
            var jsonSerializedToken = JsonConvert.SerializeObject(token);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._environmentVariableProvider.Workspace}/buckets/{this._applicationName}/{SheetsCatalogImportConstants.BUCKET}/files/{SheetsCatalogImportConstants.TOKEN}"),
                Content = new StringContent(jsonSerializedToken, Encoding.UTF8, SheetsCatalogImportConstants.APPLICATION_JSON)
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            _context.Vtex.Logger.Info("SaveToken", null, $"[{response.StatusCode}] '{responseContent}' {jsonSerializedToken}");
            return response.IsSuccessStatusCode;
        }

        public async Task<FolderIds> LoadFolderIds(string accountName)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._environmentVariableProvider.Workspace}/buckets/{this._applicationName}/{SheetsCatalogImportConstants.BUCKET}/files/{accountName}")
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            request.Headers.Add("Cache-Control", "no-cache");

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            _context.Vtex.Logger.Info("LoadFolderIds", null, $"Account '{accountName}' [{response.StatusCode}] {responseContent}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            FolderIds folderIds = JsonConvert.DeserializeObject<FolderIds>(responseContent);

            return folderIds;
        }

        public async Task<bool> SaveFolderIds(FolderIds folderIds, string accountName)
        {
            var jsonSerializedToken = JsonConvert.SerializeObject(folderIds);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._environmentVariableProvider.Workspace}/buckets/{this._applicationName}/{SheetsCatalogImportConstants.BUCKET}/files/{accountName}"),
                Content = new StringContent(jsonSerializedToken, Encoding.UTF8, SheetsCatalogImportConstants.APPLICATION_JSON)
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            _context.Vtex.Logger.Info("SaveFolderIds", null, $"Account '{accountName}' [{response.StatusCode}] {jsonSerializedToken}");

            return response.IsSuccessStatusCode;
        }

        public async Task SetImportLock(DateTime importStartTime)
        {
            var importLock = new Lock
            {
                ImportStarted = importStartTime,
            };

            var jsonSerializedLock = JsonConvert.SerializeObject(importLock);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_WORKSPACE_HEADER_NAME]}/buckets/{this._applicationName}/{SheetsCatalogImportConstants.BUCKET}/files/{SheetsCatalogImportConstants.LOCK}"),
                Content = new StringContent(jsonSerializedLock, Encoding.UTF8, SheetsCatalogImportConstants.APPLICATION_JSON)
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
        }

        public async Task<DateTime> CheckImportLock()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_WORKSPACE_HEADER_NAME]}/buckets/{this._applicationName}/{SheetsCatalogImportConstants.BUCKET}/files/{SheetsCatalogImportConstants.LOCK}")
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            request.Headers.Add("Cache-Control", "no-cache");

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new DateTime();
            }

            Lock importLock = JsonConvert.DeserializeObject<Lock>(responseContent);

            if (importLock.ImportStarted == null)
            {
                return new DateTime();
            }

            return importLock.ImportStarted;
        }

        public async Task ClearImportLock()
        {
            var importLock = new Lock
            {
                ImportStarted = new DateTime(),
            };

            var jsonSerializedLock = JsonConvert.SerializeObject(importLock);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_WORKSPACE_HEADER_NAME]}/buckets/{this._applicationName}/{SheetsCatalogImportConstants.BUCKET}/files/{SheetsCatalogImportConstants.LOCK}"),
                Content = new StringContent(jsonSerializedLock, Encoding.UTF8, SheetsCatalogImportConstants.APPLICATION_JSON)
            };

            request.Headers.Add("Cache-Control", "no-cache");

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
        }

        public async Task<AppSettings> GetAppSettings()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://apps.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_WORKSPACE_HEADER_NAME]}/apps/{SheetsCatalogImportConstants.APP_SETTINGS}/settings"),
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            AppSettings appSettings = JsonConvert.DeserializeObject<AppSettings>(responseContent);

            return appSettings;
        }
    }
}
