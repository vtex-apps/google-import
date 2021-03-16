using SheetsCatalogImport.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Vtex.Api.Context;
using SheetsCatalogImport.Models;
using System.Text;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;

namespace SheetsCatalogImport.Services
{
    public class VtexAPIService : IVtexAPIService
    {
        private readonly IIOServiceContext _context;
        private readonly IVtexEnvironmentVariableProvider _environmentVariableProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ISheetsCatalogImportRepository _sheetsCatalogImportRepository;
        private readonly IGoogleSheetsService _googleSheetsService;
        private readonly string _applicationName;

        public VtexAPIService(IIOServiceContext context, IVtexEnvironmentVariableProvider environmentVariableProvider, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, ISheetsCatalogImportRepository sheetsCatalogImportRepository, IGoogleSheetsService googleSheetsService)
        {
            this._context = context ??
                            throw new ArgumentNullException(nameof(context));

            this._environmentVariableProvider = environmentVariableProvider ??
                                                throw new ArgumentNullException(nameof(environmentVariableProvider));

            this._httpContextAccessor = httpContextAccessor ??
                                        throw new ArgumentNullException(nameof(httpContextAccessor));

            this._clientFactory = clientFactory ??
                               throw new ArgumentNullException(nameof(clientFactory));

            this._sheetsCatalogImportRepository = sheetsCatalogImportRepository ??
                               throw new ArgumentNullException(nameof(sheetsCatalogImportRepository));

            this._googleSheetsService = googleSheetsService ??
                               throw new ArgumentNullException(nameof(googleSheetsService));

            this._applicationName =
                $"{this._environmentVariableProvider.ApplicationVendor}.{this._environmentVariableProvider.ApplicationName}";
        }

        public async Task<string> ProcessSheet()
        {
            string response = string.Empty;

            DateTime importStarted = await _sheetsCatalogImportRepository.CheckImportLock();
            TimeSpan elapsedTime = DateTime.Now - importStarted;
            if (elapsedTime.TotalHours < SheetsCatalogImportConstants.LOCK_TIMEOUT)
            {
                Console.WriteLine("Blocked by lock");
                _context.Vtex.Logger.Info("ProcessSheet", null, $"Blocked by lock.  Import started: {importStarted}");
                return ($"Import started {importStarted} in progress.");
            }

            await _sheetsCatalogImportRepository.SetImportLock(DateTime.Now);
            _context.Vtex.Logger.Info("ProcessSheet", null, $"Set new lock: {DateTime.Now}");

            bool updated = false;
            int doneCount = 0;
            int errorCount = 0;

            string importFolderId = null;
            string accountFolderId = null;
            string accountName = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME];

            FolderIds folderIds = await _sheetsCatalogImportRepository.LoadFolderIds(accountName);
            if (folderIds != null)
            {
                importFolderId = folderIds.ProductsFolderId;
                accountFolderId = folderIds.AccountFolderId;
            }

            ListFilesResponse spreadsheets = await _googleSheetsService.ListSheetsInFolder(accountFolderId);

            var sheetIds = spreadsheets.Files.Select(s => s.Id);
            if (sheetIds != null)
            {
                foreach (var sheetId in sheetIds)
                {
                    Dictionary<string, int> headerIndexDictionary = new Dictionary<string, int>();
                    Dictionary<string, string> columns = new Dictionary<string, string>();
                    string sheetContent = await _googleSheetsService.GetSheet(sheetId, string.Empty);
                    //_context.Vtex.Logger.Debug("SheetImport", null, $"[{sheetIds}] sheetContent: {sheetContent} ");

                    if (string.IsNullOrEmpty(sheetContent))
                    {
                        //await ClearLockAfterDelay(5000);
                        return ("Empty Spreadsheet Response.");
                    }

                    GoogleSheet googleSheet = JsonConvert.DeserializeObject<GoogleSheet>(sheetContent);
                    string valueRange = googleSheet.ValueRanges[0].Range;
                    string sheetName = valueRange.Split("!")[0];
                    string[] sheetHeader = googleSheet.ValueRanges[0].Values[0];
                    int headerIndex = 0;
                    int rowCount = googleSheet.ValueRanges[0].Values.Count();
                    int writeBlockSize = Math.Max(rowCount / SheetsCatalogImportConstants.WRITE_BLOCK_SIZE_DIVISOR, SheetsCatalogImportConstants.MIN_WRITE_BLOCK_SIZE);
                    Console.WriteLine($"Write block size = {writeBlockSize}");
                    int offset = 0;
                    _context.Vtex.Logger.Debug("ProcessSheet", null, $"'{sheetName}' Row count: {rowCount} ");
                    foreach (string header in sheetHeader)
                    {
                        //Console.WriteLine($"({headerIndex}) sheetHeader = {header}");
                        headerIndexDictionary.Add(header.ToLower(), headerIndex);
                        headerIndex++;
                    }

                    int statusColumnNumber = headerIndexDictionary["status"] + 65;
                    string statusColumnLetter = ((char)statusColumnNumber).ToString();
                    int messageColumnNumber = headerIndexDictionary["message"] + 65;
                    string messageColumnLetter = ((char)messageColumnNumber).ToString();
                    int dateColumnNumber = headerIndexDictionary["date"] + 65;
                    string dateColumnLetter = ((char)dateColumnNumber).ToString();
                    // ProductId,SkuId,Category,Brand,ProductName,Product Reference Code,SkuName,Sku EAN/GTIN,SKU Reference Code,Height,Width,Length,Weight,Product Description,Search Keywords,MetaTag Description,Image URL 1,Image URL 2,Image URL 3,Image URL 4,Image URL 5,Display if Out of Stock,MSRP Selling Price (Price to GPP),Available Quantity,ProductSpecs,Product Spec Group,Product Spec Field,Product Spec Value,Sku Specs
                    //int productIdColumnNumber = headerIndexDictionary["productid"] + 65;
                    //string productIdColumnLetter = ((char)productIdColumnNumber).ToString();
                    //int skuIdColumnNumber = headerIndexDictionary["skuid"] + 65;
                    //string skuIdColumnLetter = ((char)skuIdColumnNumber).ToString();
                    //int categoryColumnNumber = headerIndexDictionary["category"] + 65;
                    //string categoryColumnLetter = ((char)categoryColumnNumber).ToString();
                    //int brandColumnNumber = headerIndexDictionary["brand"] + 65;
                    //string brandColumnLetter = ((char)brandColumnNumber).ToString();
                    //int productNameColumnNumber = headerIndexDictionary["productname"] + 65;
                    //string productNameColumnLetter = ((char)productNameColumnNumber).ToString();
                    //int productReferenceCodeColumnNumber = headerIndexDictionary["productreferencecode"] + 65;
                    //string productReferenceCodeColumnLetter = ((char)productReferenceCodeColumnNumber).ToString();

                    GetCategoryTreeResponse[] categoryTree = await this.GetCategoryTree(10);
                    GetBrandListResponse[] brandList = await this.GetBrandList();
                    for (int index = 1; index < rowCount; index++)
                    {
                        string productid = string.Empty;
                        string skuid = string.Empty;
                        string category = string.Empty;
                        string brand = string.Empty;
                        string productName = string.Empty;
                        string productReferenceCode = string.Empty;
                        string skuName = string.Empty;
                        string skuEanGtin = string.Empty;
                        string skuReferenceCode = string.Empty;
                        string height = string.Empty;
                        string width = string.Empty;
                        string length = string.Empty;
                        string weight = string.Empty;
                        string productDescription = string.Empty;
                        string searchKeywords = string.Empty;
                        string metaTagDescription = string.Empty;
                        string imageUrl1 = string.Empty;
                        string imageUrl2 = string.Empty;
                        string imageUrl3 = string.Empty;
                        string imageUrl4 = string.Empty;
                        string imageUrl5 = string.Empty;
                        string displayIfOutOfStock = string.Empty;
                        string msrpSellingPrice = string.Empty;
                        string availableQuantity = string.Empty;
                        string productSpecs = string.Empty;
                        string productSpecGroup = string.Empty;
                        string productSpecField = string.Empty;
                        string productSpecValue = string.Empty;
                        string skuSpecs = string.Empty;
                        string[] dataValues = googleSheet.ValueRanges[0].Values[index];
                        if (headerIndexDictionary.ContainsKey("productid") && headerIndexDictionary["productid"] < dataValues.Count())
                            productid = dataValues[headerIndexDictionary["productid"]];
                        if (headerIndexDictionary.ContainsKey("skuid") && headerIndexDictionary["skuid"] < dataValues.Count())
                            skuid = dataValues[headerIndexDictionary["skuid"]];
                        if (headerIndexDictionary.ContainsKey("category") && headerIndexDictionary["category"] < dataValues.Count())
                            category = dataValues[headerIndexDictionary["category"]];
                        if (headerIndexDictionary.ContainsKey("brand") && headerIndexDictionary["brand"] < dataValues.Count())
                            brand = dataValues[headerIndexDictionary["brand"]];
                        if (headerIndexDictionary.ContainsKey("productname") && headerIndexDictionary["productname"] < dataValues.Count())
                            productName = dataValues[headerIndexDictionary["productname"]];
                        if (headerIndexDictionary.ContainsKey("productreferencecode") && headerIndexDictionary["productreferencecode"] < dataValues.Count())
                            productReferenceCode = dataValues[headerIndexDictionary["productreferencecode"]];
                        if (headerIndexDictionary.ContainsKey("skuname") && headerIndexDictionary["skuname"] < dataValues.Count())
                            skuName = dataValues[headerIndexDictionary["skuname"]];
                        if (headerIndexDictionary.ContainsKey("sku ean/gtin") && headerIndexDictionary["sku ean/gtin"] < dataValues.Count())
                            skuEanGtin = dataValues[headerIndexDictionary["sku ean/gtin"]];
                        if (headerIndexDictionary.ContainsKey("sku reference code") && headerIndexDictionary["sku reference code"] < dataValues.Count())
                            skuReferenceCode = dataValues[headerIndexDictionary["sku reference code"]];
                        if (headerIndexDictionary.ContainsKey("height") && headerIndexDictionary["height"] < dataValues.Count())
                            height = dataValues[headerIndexDictionary["height"]];
                        if (headerIndexDictionary.ContainsKey("width") && headerIndexDictionary["width"] < dataValues.Count())
                            width = dataValues[headerIndexDictionary["width"]];
                        if (headerIndexDictionary.ContainsKey("length") && headerIndexDictionary["length"] < dataValues.Count())
                            length = dataValues[headerIndexDictionary["length"]];
                        if (headerIndexDictionary.ContainsKey("weight") && headerIndexDictionary["weight"] < dataValues.Count())
                            weight = dataValues[headerIndexDictionary["weight"]];
                        if (headerIndexDictionary.ContainsKey("product description") && headerIndexDictionary["product description"] < dataValues.Count())
                            productDescription = dataValues[headerIndexDictionary["product description"]];
                        if (headerIndexDictionary.ContainsKey("search keywords") && headerIndexDictionary["search keywords"] < dataValues.Count())
                            searchKeywords = dataValues[headerIndexDictionary["search keywords"]];
                        if (headerIndexDictionary.ContainsKey("metatag description") && headerIndexDictionary["metatag description"] < dataValues.Count())
                            metaTagDescription = dataValues[headerIndexDictionary["metatag description"]];
                        if (headerIndexDictionary.ContainsKey("image url 1") && headerIndexDictionary["image url 1"] < dataValues.Count())
                            imageUrl1 = dataValues[headerIndexDictionary["image url 1"]];
                        if (headerIndexDictionary.ContainsKey("image url 2") && headerIndexDictionary["image url 2"] < dataValues.Count())
                            imageUrl2 = dataValues[headerIndexDictionary["image url 2"]];
                        if (headerIndexDictionary.ContainsKey("image url 3") && headerIndexDictionary["image url 3"] < dataValues.Count())
                            imageUrl3 = dataValues[headerIndexDictionary["image url 3"]];
                        if (headerIndexDictionary.ContainsKey("image url 4") && headerIndexDictionary["image url 4"] < dataValues.Count())
                            imageUrl4 = dataValues[headerIndexDictionary["image url 4"]];
                        if (headerIndexDictionary.ContainsKey("image url 5") && headerIndexDictionary["image url 5"] < dataValues.Count())
                            imageUrl5 = dataValues[headerIndexDictionary["image url 5"]];
                        if (headerIndexDictionary.ContainsKey("display if out of stock") && headerIndexDictionary["display if out of stock"] < dataValues.Count())
                            displayIfOutOfStock = dataValues[headerIndexDictionary["display if out of stock"]];
                        if (headerIndexDictionary.ContainsKey("msrp selling price (price to gpp)") && headerIndexDictionary["msrp selling price (price to gpp)"] < dataValues.Count())
                            msrpSellingPrice = dataValues[headerIndexDictionary["msrp selling price (price to gpp)"]];
                        if (headerIndexDictionary.ContainsKey("available quantity") && headerIndexDictionary["available quantity"] < dataValues.Count())
                            availableQuantity = dataValues[headerIndexDictionary["available quantity"]];
                        if (headerIndexDictionary.ContainsKey("productspecs") && headerIndexDictionary["aproductspecs"] < dataValues.Count())
                            productSpecs = dataValues[headerIndexDictionary["productspecs"]];
                        if (headerIndexDictionary.ContainsKey("product spec group") && headerIndexDictionary["product spec group"] < dataValues.Count())
                            productSpecGroup = dataValues[headerIndexDictionary["product spec group"]];
                        if (headerIndexDictionary.ContainsKey("product spec field") && headerIndexDictionary["product spec field"] < dataValues.Count())
                            productSpecField = dataValues[headerIndexDictionary["product spec field"]];
                        if (headerIndexDictionary.ContainsKey("product spec value") && headerIndexDictionary["product spec value"] < dataValues.Count())
                            productSpecValue = dataValues[headerIndexDictionary["product spec value"]];
                        if (headerIndexDictionary.ContainsKey("sku specs") && headerIndexDictionary["sku specs"] < dataValues.Count())
                            skuSpecs = dataValues[headerIndexDictionary["sku specs"]];

                        long? brandId = null;
                        long? categoryId = await GetCategoryId(categoryTree, category);
                        //if(categoryId == null)
                        //{
                        //    CreateCategoryRequest createCategoryRequest = new CreateCategoryRequest
                        //    {
                        //        ActiveStoreFrontLink = false,
                        //        AdWordsRemarketingCode = null,
                        //        IsActive = false,
                        //        Description = string.Empty,
                        //        a
                        //    }
                        //}

                        if(!string.IsNullOrEmpty(brand))
                        {
                            brandId = brandList.Where(b => b.Name.Equals(brand)).Select(b => b.Id).FirstOrDefault();
                        }

                        GetProductByIdResponse getProductById = null;
                        if (!string.IsNullOrEmpty(productid))
                        {
                            getProductById = await this.GetProductById(productid);
                        }

                        ProductResponse productResponse = null;
                        ProductRequest productRequest = new ProductRequest
                        {
                            AdWordsRemarketingCode = null,
                            IsActive = true,
                            BrandId = brandId,
                            CategoryId = categoryId,
                            DepartmentId = null,
                            Description = productDescription,
                            DescriptionShort = productDescription,
                            IsVisible = true,
                            KeyWords = null,
                            LinkId = null,
                            LomadeeCampaignCode = null,
                            MetaTagDescription = null,
                            Name = productName,
                            RefId = null,
                            ReleaseDate = null,
                            Score = null,
                            ShowWithoutStock = false,
                            SupplierId = null,
                            TaxCode = null,
                            Title = null
                        };

                        if (getProductById == null)
                        {
                            productResponse = await this.CreateProduct(productRequest);
                        }
                        else
                        {
                            productResponse = await this.UpdateProduct(productid, productRequest);
                        }

                        GetBrandListResponse brandListResponse = brandList.Where(b => b.Name.Equals(brand)).FirstOrDefault();
                        int page = 1;
                        int pageSize = 1000;
                        long[] skuIds = await this.ListSkuIds(page, pageSize);
                        while (skuIds.Length.Equals(page * pageSize))
                        {
                            page++;
                            List<long> skuIdsList = skuIds.ToList();
                            long[] skuIdsTemp = await this.ListSkuIds(page, pageSize);
                            skuIdsList.AddRange(skuIdsTemp.ToList());
                            skuIds = skuIdsList.ToArray();
                        }

                        SkuResponse skuResponse = null;
                        SkuRequest skuRequest = new SkuRequest
                        {
                            EstimatedDateArrival = null,
                            IsActive = true,
                            KitItensSellApart = false,
                            CommercialConditionId = null,
                            CreationDate = null,
                            CubicWeight = double.Parse(weight),
                            Height = double.Parse(height),
                            IsKit = false,
                            Length = double.Parse(length),
                            ManufacturerCode = null,
                            MeasurementUnit = null,
                            ModalType = null,
                            Name = skuName,
                            PackagedHeight = null,
                            PackagedLength = null,
                            PackagedWeightKg = null,
                            PackagedWidth = null,
                            ProductId = long.Parse(productid),
                            RefId = skuReferenceCode,
                            RewardValue = null,
                            UnitMultiplier = null,
                            WeightKg = null,
                            Width = double.Parse(width)
                        };

                        if (!skuIds.Any(s => s.Equals(skuid)))
                        {
                            skuResponse = await this.CreateSku(skuRequest);
                        }
                        else
                        {
                            skuResponse = await this.UpdateSku(skuid, skuRequest);
                        }
                    }
                }
            }

            return response;
        }

        private async Task<long?> GetCategoryId(GetCategoryTreeResponse[] categoryTree, string categoryName)
        {
            long? categoryId = null;
            if (!string.IsNullOrEmpty(categoryName))
            {
                string[] nameArr = categoryName.Split('/');
                string currentLevelCategoryName = nameArr[0];
                GetCategoryTreeResponse currentLevelCategoryTree = categoryTree.Where(t => t.Name.Equals(currentLevelCategoryName)).FirstOrDefault();
                if (nameArr.Length == 0)
                {
                    categoryId = currentLevelCategoryTree.Id;
                }
                else if(currentLevelCategoryTree.HasChildren)
                {
                    categoryName = categoryName.Replace($"{currentLevelCategoryName}/", string.Empty);
                    categoryId = await GetCategoryId(currentLevelCategoryTree.Children, categoryName);
                }
            }

            return categoryId;
        }

        //private async Task<long?> GetCategoryId(GetCategoryTreeResponse[] categoryTree, string categoryName)
        //{
        //    long? categoryId = null;
        //    if (!string.IsNullOrEmpty(categoryName))
        //    {
        //        foreach (GetCategoryTreeResponse categoryObj in categoryTree)
        //        {
        //            if(categoryObj.Name.Equals(categoryName))
        //            {
        //                categoryId = categoryObj.Id;
        //                break;
        //            }
        //            else if(categoryObj.HasChildren)
        //            {
        //                categoryId = await GetCategoryId(categoryObj.Children, categoryName);
        //                if(categoryId != null)
        //                {
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    return categoryId;
        //}

        public async Task<ProductResponse> CreateProduct(ProductRequest createProductRequest)
        {
            // POST https://{accountName}.{environment}.com.br/api/catalog/pvt/product

            string responseContent = string.Empty;
            ProductResponse createProductResponse = null;

            try
            {
                string jsonSerializedData = JsonConvert.SerializeObject(createProductRequest);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog/pvt/product"),
                    Content = new StringContent(jsonSerializedData, Encoding.UTF8, SheetsCatalogImportConstants.APPLICATION_JSON)
                };

                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);

                responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    createProductResponse = JsonConvert.DeserializeObject<ProductResponse>(responseContent);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CreateProduct", null, $"Error creating product {createProductRequest.Title}", ex);
            }

            return createProductResponse;
        }

        public async Task<ProductResponse> UpdateProduct(string productId, ProductRequest updateProductRequest)
        {
            // PUT https://{accountName}.{environment}.com.br/api/catalog/pvt/product/productId

            string responseContent = string.Empty;
            ProductResponse updateProductResponse = null;

            try
            {
                string jsonSerializedData = JsonConvert.SerializeObject(updateProductRequest);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog/pvt/product/{productId}"),
                    Content = new StringContent(jsonSerializedData, Encoding.UTF8, SheetsCatalogImportConstants.APPLICATION_JSON)
                };

                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);

                responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    updateProductResponse = JsonConvert.DeserializeObject<ProductResponse>(responseContent);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("UpdateProduct", null, $"Error updating product {updateProductResponse.Title}", ex);
            }

            return updateProductResponse;
        }

        public async Task<SkuResponse> CreateSku(SkuRequest createSkuRequest)
        {
            // POST https://{accountName}.{environment}.com.br/api/catalog/pvt/stockkeepingunit

            string responseContent = string.Empty;
            SkuResponse createSkuResponse = null;

            try
            {
                string jsonSerializedData = JsonConvert.SerializeObject(createSkuRequest);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog/pvt/stockkeepingunit"),
                    Content = new StringContent(jsonSerializedData, Encoding.UTF8, SheetsCatalogImportConstants.APPLICATION_JSON)
                };

                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);

                responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    createSkuResponse = JsonConvert.DeserializeObject<SkuResponse>(responseContent);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CreateSku", null, $"Error creating sku {createSkuRequest.Name}", ex);
            }

            return createSkuResponse;
        }

        public async Task<SkuResponse> UpdateSku(string skuId, SkuRequest updateSkuRequest)
        {
            // PUT https://{accountName}.{environment}.com.br/api/catalog/pvt/stockkeepingunit/skuId

            string responseContent = string.Empty;
            SkuResponse updateSkuResponse = null;

            try
            {
                string jsonSerializedData = JsonConvert.SerializeObject(updateSkuRequest);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog/pvt/stockkeepingunit/{skuId}"),
                    Content = new StringContent(jsonSerializedData, Encoding.UTF8, SheetsCatalogImportConstants.APPLICATION_JSON)
                };

                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);

                responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    updateSkuResponse = JsonConvert.DeserializeObject<SkuResponse>(responseContent);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("UpdateSku", null, $"Error updating sku {updateSkuRequest.Name}", ex);
            }

            return updateSkuResponse;
        }

        public async Task<GetCategoryTreeResponse[]> GetCategoryTree(int categoryLevels)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog_system/pub/category/tree/categoryLevels

            GetCategoryTreeResponse[] getCategoryTreeResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog_system/pub/category/tree/{categoryLevels}")
                };

                request.Headers.Add(SheetsCatalogImportConstants.USE_HTTPS_HEADER_NAME, "true");
                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    getCategoryTreeResponse = JsonConvert.DeserializeObject<GetCategoryTreeResponse[]>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetCategoryTree", null, $"Could not get category tree '{categoryLevels}' [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetCategoryTree", null, $"Error getting category tree '{categoryLevels}'", ex);
            }

            return getCategoryTreeResponse;
        }

        public async Task<CategoryResponse> CreateCategory(CategoryRequest createCategoryRequest)
        {
            // POST https://{accountName}.{environment}.com.br/api/catalog/pvt/category

            string responseContent = string.Empty;
            CategoryResponse createCategoryResponse = null;

            try
            {
                string jsonSerializedData = JsonConvert.SerializeObject(createCategoryRequest);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog/pvt/category"),
                    Content = new StringContent(jsonSerializedData, Encoding.UTF8, SheetsCatalogImportConstants.APPLICATION_JSON)
                };

                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);

                responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    createCategoryResponse = JsonConvert.DeserializeObject<CategoryResponse>(responseContent);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CreateCategory", null, $"Error creating category {createCategoryRequest.Title}", ex);
            }

            return createCategoryResponse;
        }

        public async Task<GetBrandListResponse[]> GetBrandList()
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog_system/pvt/brand/list

            GetBrandListResponse[] getBrandListResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog_system/pvt/brand/list")
                };

                request.Headers.Add(SheetsCatalogImportConstants.USE_HTTPS_HEADER_NAME, "true");
                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    getBrandListResponse = JsonConvert.DeserializeObject<GetBrandListResponse[]>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetBrandList", null, $"Could not get brand list [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetBrandList", null, $"Error getting brand list", ex);
            }

            return getBrandListResponse;
        }

        public async Task<GetProductByIdResponse> GetProductById(string productId)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog/pvt/product/productId

            GetProductByIdResponse getProductByIdResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog/pvt/product/{productId}")
                };

                request.Headers.Add(SheetsCatalogImportConstants.USE_HTTPS_HEADER_NAME, "true");
                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    getProductByIdResponse = JsonConvert.DeserializeObject<GetProductByIdResponse>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetProductById", null, $"Could not get product for id '{productId}' [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetProductById", null, $"Error getting product for id '{productId}'", ex);
            }

            return getProductByIdResponse;
        }

        public async Task<long[]> ListSkuIds(int page = 1, int pagesize = 1000)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog_system/pvt/sku/stockkeepingunitids

            long[] listSkuIdsResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog_system/pvt/sku/stockkeepingunitids?page={page}&pagesize={pagesize}")
                };

                request.Headers.Add(SheetsCatalogImportConstants.USE_HTTPS_HEADER_NAME, "true");
                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(SheetsCatalogImportConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(SheetsCatalogImportConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    listSkuIdsResponse = JsonConvert.DeserializeObject<long[]>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("ListSkuIds", null, $"Could not get sku ids {page}/{pagesize} [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("ListSkuIds", null, $"Error getting sku ids {page}/{pagesize}", ex);
            }

            return listSkuIdsResponse;
        }
    }
}
