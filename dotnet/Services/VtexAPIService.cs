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
using System.Text.RegularExpressions;

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

            bool success = false;
            int doneCount = 0;
            int errorCount = 0;
            int statusColumnIndex = 0;
            StringBuilder sb = new StringBuilder();

            string importFolderId = null;
            string accountFolderId = null;
            string accountName = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME];

            FolderIds folderIds = await _sheetsCatalogImportRepository.LoadFolderIds(accountName);
            if (folderIds != null)
            {
                importFolderId = folderIds.ProductsFolderId;
                //accountFolderId = folderIds.AccountFolderId;
            }
            else
            {
                Console.WriteLine("LoadFolderIds returned Null!");
            }

            Console.WriteLine($"ListSheetsInFolder {importFolderId}");
            ListFilesResponse spreadsheets = await _googleSheetsService.ListSheetsInFolder(importFolderId);
            if (spreadsheets != null)
            {
                var sheetIds = spreadsheets.Files.Select(s => s.Id);
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
                    string[][] arrValuesToWrite = new string[writeBlockSize][];
                    Console.WriteLine($"Write block size = {writeBlockSize}");
                    int offset = 0;
                    _context.Vtex.Logger.Debug("ProcessSheet", null, $"'{sheetName}' Row count: {rowCount} ");
                    foreach (string header in sheetHeader)
                    {
                        //Console.WriteLine($"({headerIndex}) sheetHeader = {header}");
                        headerIndexDictionary.Add(header.ToLower(), headerIndex);
                        headerIndex++;
                    }

                    statusColumnIndex = headerIndexDictionary["status"];
                    string statusColumnLetter = await GetColumnLetter(headerIndexDictionary["status"]);
                    string messageColumnLetter = await GetColumnLetter(headerIndexDictionary["message"]);
                    //if (!headerIndexDictionary.ContainsKey("date"))
                    //{
                    //    BatchUpdate batchUpdate = new BatchUpdate
                    //    {
                    //        Requests = new Request[]
                    //        {
                    //            new Request
                    //            {
                    //                InsertDimension = new InsertDimension
                    //                {
                    //                    InheritFromBefore = false,
                    //                    Range = new InsertRange
                    //                    {
                    //                        SheetId = 0,
                    //                        Dimension = "COLUMNS",
                    //                        StartIndex = headerIndexDictionary["status"] + 1,
                    //                        EndIndex = headerIndexDictionary["message"] + 1
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    };

                    //    var updateSheet = await _googleSheetsService.UpdateSpreadsheet(sheetId, batchUpdate);
                    //    sheetContent = await _googleSheetsService.GetSheet(sheetId, string.Empty);
                    //    valueRange = googleSheet.ValueRanges[0].Range;
                    //    sheetName = valueRange.Split("!")[0];
                    //    sheetHeader = googleSheet.ValueRanges[0].Values[0];
                    //    foreach (string header in sheetHeader)
                    //    {
                    //        //Console.WriteLine($"({headerIndex}) sheetHeader = {header}");
                    //        headerIndexDictionary.Add(header.ToLower(), headerIndex);
                    //        headerIndex++;
                    //    }

                    //    statusColumnLetter = await GetColumnLetter(headerIndexDictionary["status"]);
                    //    messageColumnLetter = await GetColumnLetter(headerIndexDictionary["message"]);
                    //}

                    //string dateColumnLetter = await GetColumnLetter(headerIndexDictionary["date"]);
                    Console.WriteLine($"Status column = '{statusColumnLetter}' Message = '{messageColumnLetter}'");

                    for (int index = 1; index < rowCount; index++)
                    {
                        success = true;
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
                        string msrp = string.Empty;
                        string sellingPrice = string.Empty;
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
                        if (headerIndexDictionary.ContainsKey("product reference code") && headerIndexDictionary["product reference code"] < dataValues.Count())
                            productReferenceCode = dataValues[headerIndexDictionary["product reference code"]];
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
                        if (headerIndexDictionary.ContainsKey("msrp") && headerIndexDictionary["msrp"] < dataValues.Count())
                            msrp = dataValues[headerIndexDictionary["msrp"]];
                        if (headerIndexDictionary.ContainsKey("selling price (price to gpp)") && headerIndexDictionary["selling price (price to gpp)"] < dataValues.Count())
                            sellingPrice = dataValues[headerIndexDictionary["selling price (price to gpp)"]];
                        if (headerIndexDictionary.ContainsKey("available quantity") && headerIndexDictionary["available quantity"] < dataValues.Count())
                            availableQuantity = dataValues[headerIndexDictionary["available quantity"]];
                        if (headerIndexDictionary.ContainsKey("productspecs") && headerIndexDictionary["productspecs"] < dataValues.Count())
                            productSpecs = dataValues[headerIndexDictionary["productspecs"]];
                        if (headerIndexDictionary.ContainsKey("product spec group") && headerIndexDictionary["product spec group"] < dataValues.Count())
                            productSpecGroup = dataValues[headerIndexDictionary["product spec group"]];
                        if (headerIndexDictionary.ContainsKey("product spec field") && headerIndexDictionary["product spec field"] < dataValues.Count())
                            productSpecField = dataValues[headerIndexDictionary["product spec field"]];
                        if (headerIndexDictionary.ContainsKey("product spec value") && headerIndexDictionary["product spec value"] < dataValues.Count())
                            productSpecValue = dataValues[headerIndexDictionary["product spec value"]];
                        if (headerIndexDictionary.ContainsKey("sku specs") && headerIndexDictionary["sku specs"] < dataValues.Count())
                            skuSpecs = dataValues[headerIndexDictionary["sku specs"]];

                        string status = string.Empty;
                        if (headerIndexDictionary.ContainsKey("status") && headerIndexDictionary["status"] < dataValues.Count())
                            status = dataValues[headerIndexDictionary["status"]];

                        string doUpdateValue = string.Empty;
                        bool doUpdate = false;
                        if (headerIndexDictionary.ContainsKey("update") && headerIndexDictionary["update"] < dataValues.Count())
                            doUpdateValue = dataValues[headerIndexDictionary["update"]];
                        doUpdate = await ParseBool(doUpdateValue);

                        string activateSkuValue = string.Empty;
                        bool activateSku = false;
                        if (headerIndexDictionary.ContainsKey("activate sku") && headerIndexDictionary["activate sku"] < dataValues.Count())
                            skuSpecs = dataValues[headerIndexDictionary["activate sku"]];
                        activateSku = await ParseBool(activateSkuValue);

                        if (status.Equals("Done"))
                        {
                            // skip
                            string[] arrLineValuesToWrite = new string[] { null, null };
                            arrValuesToWrite[index - offset - 1] = arrLineValuesToWrite;
                            sb.Clear();
                        }
                        else
                        {
                            sb.AppendLine(DateTime.Now.ToString());
                            ProductRequest productRequest = new ProductRequest
                            {
                                Id = await ParseLong(productid),
                                Name = productName,
                                CategoryPath = category,
                                BrandName = brand,
                                RefId = productReferenceCode,
                                Title = productName,
                                LinkId = $"{productName}-{productReferenceCode}",
                                Description = productDescription,
                                ReleaseDate = DateTime.Now.ToString(),
                                KeyWords = searchKeywords,
                                IsVisible = true,
                                IsActive = true,
                                TaxCode = string.Empty,
                                MetaTagDescription = metaTagDescription,
                                ShowWithoutStock = await ParseBool(displayIfOutOfStock),
                                Score = 1
                            };

                            UpdateResponse productUpdateResponse = await this.CreateProduct(productRequest);

                            sb.AppendLine($"Product: [{productUpdateResponse.StatusCode}] {productUpdateResponse.Message}");
                            long productId = 0;
                            if (productUpdateResponse.Success)
                            {
                                ProductResponse productResponse = JsonConvert.DeserializeObject<ProductResponse>(productUpdateResponse.Message);
                                productId = productResponse.Id;
                                sb.AppendLine($"New Product Id {productId}");
                                success = true;
                                // Pause after creation to allow for caching
                                //await Task.Delay(1000 * 2);
                            }
                            else if (productUpdateResponse.StatusCode.Equals("Conflict"))
                            {
                                // 409 - Same ID "Product already created with this Id"
                                // 409 - Same RefId "There is already a product created with the same RefId with Product Id 100081202"
                                // 409 - Same link Id "There is already a product with the same LinkId with Product Id 100081169"
                                if (productUpdateResponse.Message.Contains("Product already created with this Id"))
                                {
                                    success = true;

                                    if(doUpdate)
                                    {
                                        productId = productRequest.Id ?? 0;
                                        //success = true;
                                        productUpdateResponse = await this.UpdateProduct(productid, productRequest);
                                        success = productUpdateResponse.Success;
                                        sb.AppendLine($"Product Update: [{productUpdateResponse.StatusCode}] {productUpdateResponse.Message}");
                                    }
                                }
                                else if (productUpdateResponse.Message.Contains("There is already a product"))
                                {
                                    Console.WriteLine(" --- There is already a product --- ");
                                    if (string.IsNullOrEmpty(productid))
                                    {
                                        string[] splitResponse = productUpdateResponse.Message.Split(" ");
                                        string parsedProductId = splitResponse[splitResponse.Length - 1];
                                        parsedProductId = parsedProductId.Remove(parsedProductId.Length - 1, 1);
                                        Console.WriteLine($" ---  parsed product id {parsedProductId} --- ");
                                        productId = await ParseLong(parsedProductId) ?? 0;
                                        if (productId > 0)
                                        {
                                            productid = productId.ToString();
                                            success = true;
                                            sb.AppendLine($"Using Product Id {productId}");
                                        }
                                        else
                                        {
                                            success = false;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine(" --- productid not empty --- ");
                                        success = false;
                                    }
                                }
                                else
                                {
                                    // What to do in this case?
                                    success = false;
                                }
                            }
                            else
                            {
                                // What to do in this case?
                                success = false;
                            }

                            UpdateResponse skuUpdateResponse = null;
                            SkuRequest skuRequest = null;
                            if (success)
                            {
                                double? packagedHeight = await ParseDouble(height);
                                double? packagedLength = await ParseDouble(length);
                                double? packagedWidth = await ParseDouble(width);

                                skuRequest = new SkuRequest
                                {
                                    Id = await ParseLong(skuid),
                                    ProductId = productId, //await ParseLong(productid) ?? 0,
                                    IsActive = false,
                                    Name = skuName,
                                    RefId = skuReferenceCode,
                                    PackagedHeight = await ParseDouble(height),
                                    PackagedLength = await ParseDouble(length),
                                    PackagedWidth = await ParseDouble(width),
                                    PackagedWeightKg = await ParseDouble(weight),
                                    CubicWeight = (packagedHeight * packagedLength * packagedWidth) / SheetsCatalogImportConstants.VOLUMETIC_FACTOR, // https://www.efulfillmentservice.com/2012/11/how-to-calculate-dimensional-weight/
                                    IsKit = false,
                                    CommercialConditionId = 1,
                                    MeasurementUnit = "un",
                                    UnitMultiplier = 1,
                                    KitItensSellApart = false
                                };


                                skuUpdateResponse = await this.CreateSku(skuRequest);
                                sb.AppendLine($"Sku: [{skuUpdateResponse.StatusCode}] {skuUpdateResponse.Message}");
                                if(skuUpdateResponse.Success && string.IsNullOrEmpty(skuid))
                                {
                                    SkuResponse skuResponse = JsonConvert.DeserializeObject<SkuResponse>(skuUpdateResponse.Message);
                                    skuid = skuResponse.Id.ToString();
                                    sb.AppendLine($"New Sku Id {skuid}");
                                }

                                if (skuUpdateResponse.StatusCode.Equals("Conflict"))
                                {
                                    if (skuUpdateResponse.Message.Contains("Sku can not be created because the RefId is registered in Sku id"))
                                    {
                                        if (string.IsNullOrEmpty(skuid))
                                        {
                                            string[] splitResponse = skuUpdateResponse.Message.Split(" ");
                                            skuid = splitResponse[splitResponse.Length - 1];
                                            skuid = skuid.Remove(skuid.Length - 1, 1);
                                            if (string.IsNullOrEmpty(skuid))
                                            {
                                                success &= false;
                                            }
                                            else
                                            {
                                                success &= true;
                                                sb.AppendLine($"Using Sku Id {skuid}");
                                            }
                                        }
                                    }
                                    else if (skuUpdateResponse.Message.Contains("Sku already created with this Id"))
                                    {
                                        if (doUpdate)
                                        {
                                            //success &= true;
                                            skuUpdateResponse = await this.UpdateSku(skuid, skuRequest);
                                            success &= skuUpdateResponse.Success;
                                            sb.AppendLine($"Sku Update: [{skuUpdateResponse.StatusCode}] {skuUpdateResponse.Message}");
                                        }
                                    }
                                }
                                else
                                {
                                    success &= skuUpdateResponse.Success;
                                }
                            }

                            if (success)
                            {
                                if (!string.IsNullOrEmpty(skuEanGtin))
                                {
                                    UpdateResponse eanResponse = await this.CreateEANGTIN(skuid, skuEanGtin);
                                    sb.AppendLine($"EAN/GTIN: [{eanResponse.StatusCode}] {eanResponse.Message}");
                                    success &= eanResponse.Success;
                                }
                                else
                                {
                                    sb.AppendLine($"EAN/GTIN: Empty");
                                }
                            }

                            if (success)
                            {
                                UpdateResponse updateResponse = null;
                                bool imageSuccess = true;
                                bool haveImage = false;
                                StringBuilder imageResults = new StringBuilder();
                                if (!string.IsNullOrEmpty(imageUrl1))
                                {
                                    haveImage = true;
                                    updateResponse = await this.CreateSkuFile(skuid, $"{skuName}-1", $"{skuName}-1", true, imageUrl1);
                                    imageSuccess &= updateResponse.Success;
                                    imageResults.AppendLine($"1: {updateResponse.Message}");
                                }

                                if (!string.IsNullOrEmpty(imageUrl2))
                                {
                                    haveImage = true;
                                    updateResponse = await this.CreateSkuFile(skuid, $"{skuName}-2", $"{skuName}-2", false, imageUrl2);
                                    imageSuccess &= updateResponse.Success;
                                    imageResults.AppendLine($"2: {updateResponse.Message}");
                                }

                                if (!string.IsNullOrEmpty(imageUrl3))
                                {
                                    haveImage = true;
                                    updateResponse = await this.CreateSkuFile(skuid, $"{skuName}-3", $"{skuName}-3", false, imageUrl3);
                                    imageSuccess &= updateResponse.Success;
                                    imageResults.AppendLine($"3: {updateResponse.Message}");
                                }

                                if (!string.IsNullOrEmpty(imageUrl4))
                                {
                                    haveImage = true;
                                    updateResponse = await this.CreateSkuFile(skuid, $"{skuName}-4", $"{skuName}-4", false, imageUrl4);
                                    imageSuccess &= updateResponse.Success;
                                    imageResults.AppendLine($"4: {updateResponse.Message}");
                                }

                                if (!string.IsNullOrEmpty(imageUrl5))
                                {
                                    haveImage = true;
                                    updateResponse = await this.CreateSkuFile(skuid, $"{skuName}-5", $"{skuName}-5", false, imageUrl5);
                                    imageSuccess &= updateResponse.Success;
                                    imageResults.AppendLine($"5: {updateResponse.Message}");
                                }

                                if (haveImage)
                                {
                                    success &= imageSuccess;
                                    sb.AppendLine($"Images: {imageSuccess} {imageResults}");
                                }
                                else
                                {
                                    sb.AppendLine($"Images: Empty");
                                }
                            }

                            if (success)
                            {
                                if (!string.IsNullOrEmpty(msrp) && !string.IsNullOrEmpty(sellingPrice))
                                {
                                    CreatePrice createPrice = new CreatePrice
                                    {
                                        BasePrice = await ParseCurrency(sellingPrice) ?? 0,
                                        ListPrice = await ParseCurrency(msrp) ?? 0,
                                        CostPrice = await ParseCurrency(msrp) ?? 0
                                    };

                                    UpdateResponse priceResponse = await this.CreatePrice(skuid, createPrice);
                                    success &= priceResponse.Success;
                                    sb.AppendLine($"Price: [{priceResponse.StatusCode}] {priceResponse.Message}");
                                }
                                else
                                {
                                    sb.AppendLine($"Price: Empty");
                                    success = false;
                                }
                            }

                            if (success)
                            {
                                if (!string.IsNullOrEmpty(availableQuantity))
                                {
                                    GetWarehousesResponse[] getWarehousesResponse = await GetWarehouses();
                                    //GetWarehousesResponse[] getWarehousesResponse = await ListAllWarehouses();
                                    if (getWarehousesResponse != null)
                                    {
                                        string warehouseId = getWarehousesResponse.Select(w => w.Id).FirstOrDefault();
                                        if (!string.IsNullOrEmpty(warehouseId))
                                        {
                                            InventoryRequest inventoryRequest = new InventoryRequest
                                            {
                                                DateUtcOnBalanceSystem = null,
                                                Quantity = await ParseLong(availableQuantity) ?? 0,
                                                UnlimitedQuantity = false
                                            };

                                            UpdateResponse inventoryResponse = await this.SetInventory(skuid, warehouseId, inventoryRequest);
                                            success &= inventoryResponse.Success;
                                            sb.AppendLine($"Inventory: [{inventoryResponse.StatusCode}] {inventoryResponse.Message}");
                                        }
                                        else
                                        {
                                            sb.AppendLine($"Inventory: No Warehouse");
                                            success = false;
                                        }
                                    }
                                    else
                                    {
                                        sb.AppendLine($"Inventory: Null Warehouse");
                                        success = false;
                                    }
                                }
                            }

                            if (success)
                            {
                                if (!string.IsNullOrEmpty(productSpecs))
                                {
                                    string[] allSpecs = productSpecs.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                                    for (int i = 0; i < allSpecs.Length; i++)
                                    {
                                        Console.WriteLine($"Processing Spec ({i}) '{allSpecs[i]}'");
                                        string[] specsArr = allSpecs[i].Split(':');
                                        string specName = specsArr[0];
                                        string[] specValueArr = specsArr[1].Split(',');

                                        SpecAttr prodSpec = new SpecAttr
                                        {
                                            GroupName = "Default",
                                            RootLevelSpecification = true,
                                            FieldName = specName,
                                            FieldValues = specValueArr
                                        };

                                        UpdateResponse prodSpecResponse = await this.SetProdSpecs(productId.ToString(), prodSpec);
                                        if (!prodSpecResponse.Success && prodSpecResponse.StatusCode.Equals("TooManyRequests"))
                                        {
                                            _context.Vtex.Logger.Warn("ProcessSheet", null, $"Prod Spec {i + 1}: [{prodSpecResponse.StatusCode}] - Retrying...");
                                            //Console.WriteLine($"!!!!!!!!!!! ------  Prod Spec {i + 1}: [{prodSpecResponse.StatusCode}] - Retrying... !!!!!!!!!!!!!!!");
                                            await Task.Delay(1000 * 10);
                                            prodSpecResponse = await this.SetProdSpecs(productId.ToString(), prodSpec);
                                        }

                                        success &= prodSpecResponse.Success;
                                        sb.AppendLine($"Prod Spec {i + 1}: [{prodSpecResponse.StatusCode}] {prodSpecResponse.Message}");
                                    }
                                }

                                if (!string.IsNullOrEmpty(skuSpecs))
                                {
                                    string[] allSpecs = skuSpecs.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                                    for (int i = 0; i < allSpecs.Length; i++)
                                    {
                                        Console.WriteLine($"Processing Sku Spec ({i}) '{allSpecs[i]}'");
                                        string[] specsArr = allSpecs[i].Split(':');
                                        string specName = specsArr[0];
                                        string specValue = specsArr[1];

                                        SpecAttr skuSpec = new SpecAttr
                                        {
                                            GroupName = "Default",
                                            RootLevelSpecification = true,
                                            FieldName = specName,
                                            FieldValue = specValue
                                        };

                                        UpdateResponse skuSpecResponse = await this.SetSkuSpec(skuid, skuSpec);
                                        if(!skuSpecResponse.Success && skuSpecResponse.StatusCode.Equals("TooManyRequests"))
                                        {
                                            _context.Vtex.Logger.Warn("ProcessSheet", null, $"Sku Spec {i + 1}: [{skuSpecResponse.StatusCode}] - Retrying...");
                                            Console.WriteLine($"!!!!!!!!!!! ------  Sku Spec {i + 1}: [{skuSpecResponse.StatusCode}] - Retrying... !!!!!!!!!!!!!!!");
                                            await Task.Delay(5000);
                                            skuSpecResponse = await this.SetSkuSpec(skuid, skuSpec);
                                        }

                                        success &= skuSpecResponse.Success;
                                        sb.AppendLine($"Sku Spec {i + 1}: [{skuSpecResponse.StatusCode}] {skuSpecResponse.Message}");
                                    }
                                }
                            }

                            if(success && activateSku)
                            {
                                skuRequest.IsActive = true;
                                skuUpdateResponse = await this.UpdateSku(skuid, skuRequest);
                                success &= skuUpdateResponse.Success;
                                sb.AppendLine($"Activate Sku: [{skuUpdateResponse.StatusCode}] {skuUpdateResponse.Message}");
                            }

                            string result = success ? "Done" : "Error";
                            string[] arrLineValuesToWrite = new string[] { result, sb.ToString() };
                            arrValuesToWrite[index - offset - 1] = arrLineValuesToWrite;
                            if (success)
                            {
                                doneCount++;
                            }
                            else
                            {
                                errorCount++;
                                _context.Vtex.Logger.Debug("ProcessSheet", null, $"Line {index}\r{string.Join('\n', dataValues)}");
                                _context.Vtex.Logger.Warn("ProcessSheet", null, $"Line {index}\n{sb}");
                            }

                            sb.Clear();
                        }

                        if (index % writeBlockSize == 0 || index + 1 == rowCount)
                        {
                            ValueRange valueRangeToWrite = new ValueRange
                            {
                                Range = $"{sheetName}!{statusColumnLetter}{offset + 2}:{messageColumnLetter}{offset + writeBlockSize + 1}",
                                Values = arrValuesToWrite
                            };

                            var writeToSheetResult = await _googleSheetsService.WriteSpreadsheetValues(sheetId, valueRangeToWrite);
                            _context.Vtex.Logger.Debug("ProcessSheet", null, $"Writing to sheet {JsonConvert.SerializeObject(writeToSheetResult)}");
                            offset += writeBlockSize;
                            arrValuesToWrite = new string[writeBlockSize][];
                        }
                    }

                    BatchUpdate batchUpdate = new BatchUpdate
                    {
                        Requests = new Request[]
                        {
                            new Request
                            {
                                AutoResizeDimensions = new AutoResizeDimensions
                                {
                                    Dimensions = new Dimensions
                                    {
                                        Dimension = "COLUMNS",
                                        EndIndex = statusColumnIndex+1,
                                        StartIndex = statusColumnIndex-1,
                                        SheetId = 0
                                    }
                                }
                            }
                        }
                    };

                    string resize = await _googleSheetsService.UpdateSpreadsheet(sheetId, batchUpdate);
                    Console.WriteLine($"RESIZE = {resize}");
                }
            }

            await _sheetsCatalogImportRepository.ClearImportLock();
            response = $"Done: {doneCount} Error: {errorCount}";
            _context.Vtex.Logger.Info("ProcessSheet", null, response);

            return response;
        }

        public async Task<SearchTotals> SearchTotal(string query)
        {
            SearchTotals searchTotals = new SearchTotals();
            if (string.IsNullOrEmpty(query))
            {
                searchTotals.Message = "Empty Search";
            }
            else
            {
                string[] queryArr = query.Split(':');
                string queryType = queryArr[0];
                string queryParam = queryArr[1];
                if (string.IsNullOrEmpty(queryType) || string.IsNullOrEmpty(queryParam))
                {
                    searchTotals.Message = "Invalid Search";
                }
                else
                {
                    if (queryType.ToLower().Equals("category"))
                    {
                        GetCategoryTreeResponse[] categoryTree = await this.GetCategoryTree(10);
                        Dictionary<long, string> categoryIds = await GetCategoryId(categoryTree);
                        categoryIds = categoryIds.Where(c => c.Value.Contains(queryParam, StringComparison.OrdinalIgnoreCase)).ToDictionary(c => c.Key, c => c.Value);
                        foreach (long categoryId in categoryIds.Keys)
                        {
                            ProductAndSkuIdsResponse productAndSkuIdsResponse = await GetProductAndSkuIds(categoryId);
                            if (productAndSkuIdsResponse.Range.Total > 0)
                            {
                                foreach (KeyValuePair<string,long[]> productSku in productAndSkuIdsResponse.Data)
                                {
                                    searchTotals.Products++;
                                    searchTotals.Skus += productSku.Value.Count();
                                }
                            }
                        }
                    }
                    else if (queryType.ToLower().Equals("brand"))
                    {
                        GetBrandListResponse[] brandList = await GetBrandList();
                        List<GetBrandListResponse> brand = brandList.Where(b => b.Name.Contains(queryParam, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    else if (queryType.ToLower().Equals("productid"))
                    {
                        searchTotals.Products++;
                        List<ProductSkusResponse> productSkusResponse = await this.GetSkusFromProductId(queryParam);
                        searchTotals.Skus += productSkusResponse.Count();
                    }
                    else if (queryType.ToLower().Equals("product"))
                    {
                        ProductSearchResponse[] productSearchResponses = await this.ProductSearch(queryParam);
                        if(productSearchResponses != null)
                        {
                            foreach(ProductSearchResponse productSearchResponse in productSearchResponses)
                            {
                                searchTotals.Products++;
                                List<ProductSkusResponse> productSkusResponse = await this.GetSkusFromProductId(productSearchResponse.ProductId);
                                searchTotals.Skus += productSkusResponse.Count();
                            }
                        }
                    }
                }
            }

            searchTotals.TotalRecords = searchTotals.Skus;

            return searchTotals;
        }

        public async Task<string> ExportToSheet(string query)
        {
            int writeBlockSize = 5;
            string[][] arrayToWrite = new string[writeBlockSize+1][];
            string importFolderId = null;
            string accountFolderId = null;
            string accountName = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME];

            FolderIds folderIds = await _sheetsCatalogImportRepository.LoadFolderIds(accountName);
            if (folderIds != null)
            {
                importFolderId = folderIds.ProductsFolderId;
            }
            else
            {
                Console.WriteLine("LoadFolderIds returned Null!");
            }

            Console.WriteLine($"ListSheetsInFolder {importFolderId}");
            ListFilesResponse spreadsheets = await _googleSheetsService.ListSheetsInFolder(importFolderId);
            if (spreadsheets != null)
            {
                var sheetIds = spreadsheets.Files.Select(s => s.Id);
                foreach (var sheetId in sheetIds)
                {
                    Dictionary<string, int> headerIndexDictionary = new Dictionary<string, int>();
                    Dictionary<string, string> columns = new Dictionary<string, string>();
                    string sheetContent = await _googleSheetsService.GetSheet(sheetId, string.Empty);

                    if (string.IsNullOrEmpty(sheetContent))
                    {
                        return ("Empty Spreadsheet Response.");
                    }

                    GoogleSheet googleSheet = JsonConvert.DeserializeObject<GoogleSheet>(sheetContent);
                    string valueRange = googleSheet.ValueRanges[0].Range;
                    string sheetName = valueRange.Split("!")[0];
                    string[] sheetHeader = googleSheet.ValueRanges[0].Values[0];
                    int headerIndex = 0;
                    foreach (string header in sheetHeader)
                    {
                        //Console.WriteLine($"({headerIndex}) sheetHeader = {header}");
                        headerIndexDictionary.Add(header.ToLower(), headerIndex);
                        headerIndex++;
                    }

                    List<string> productIdsToExport = new List<string>();
                    GetCategoryTreeResponse[] categoryTree = await this.GetCategoryTree(10);
                    Dictionary<long, string> categoryIds = await GetCategoryId(categoryTree);
                    //GetBrandListResponse[] brandList = await GetBrandList();
                    if (!string.IsNullOrEmpty(query))
                    {
                        string[] queryArr = query.Split(':');
                        string queryType = queryArr[0];
                        string queryParam = queryArr[1];
                        if (queryType.ToLower().Equals("all"))
                        {
                            foreach (long categoryId in categoryIds.Keys)
                            {
                                ProductAndSkuIdsResponse productAndSkuIdsResponse = await GetProductAndSkuIds(categoryId);
                                if (productAndSkuIdsResponse.Range.Total > 0)
                                {
                                    foreach (KeyValuePair<string, long[]> productSku in productAndSkuIdsResponse.Data)
                                    {
                                        Console.WriteLine($"productid: {productSku.Key}");
                                        productIdsToExport.Add(productSku.Key);
                                    }
                                }
                            }
                        }
                        else if (queryType.ToLower().Equals("category"))
                        {
                            categoryIds = categoryIds.Where(c => c.Value.Contains(queryParam, StringComparison.OrdinalIgnoreCase)).ToDictionary(c => c.Key, c => c.Value);
                            foreach (long categoryId in categoryIds.Keys)
                            {
                                ProductAndSkuIdsResponse productAndSkuIdsResponse = await GetProductAndSkuIds(categoryId);
                                if (productAndSkuIdsResponse.Range.Total > 0)
                                {
                                    foreach (KeyValuePair<string, long[]> productSku in productAndSkuIdsResponse.Data)
                                    {
                                        productIdsToExport.Add(productSku.Key);
                                    }
                                }
                            }
                        }
                        else if (queryType.ToLower().Equals("brand"))
                        {
                            GetBrandListResponse[] brandList = await GetBrandList();
                            List<GetBrandListResponse> brand = brandList.Where(b => b.Name.Contains(queryParam, StringComparison.OrdinalIgnoreCase)).ToList();
                        }
                        else if (queryType.ToLower().Equals("productid"))
                        {
                            string[] productIds = queryParam.Split(',');
                            foreach (string id in productIds)
                            {
                                Console.WriteLine($"productid: {id}");
                                productIdsToExport.Add(id);
                            }
                        }
                        else if (queryType.ToLower().Equals("product"))
                        {
                            ProductSearchResponse[] productSearchResponses = await this.ProductSearch(queryParam);
                            if (productSearchResponses != null)
                            {
                                foreach (ProductSearchResponse productSearchResponse in productSearchResponses)
                                {
                                    productIdsToExport.Add(productSearchResponse.ProductId);
                                }
                            }
                        }
                    }

                    if (productIdsToExport.Count > 0)
                    {
                        long index = 0;
                        long offset = 0;
                        foreach (string productId in productIdsToExport)
                        {
                            GetProductByIdResponse getProductByIdResponse = await GetProductById(productId);
                            List<ProductSkusResponse> productSkusResponses = await GetSkusFromProductId(productId);
                            if (productSkusResponses != null)
                            {
                                foreach (ProductSkusResponse productSkusResponse in productSkusResponses)
                                {
                                    SkuAndContextResponse skuAndContextResponse = null;
                                    try
                                    {
                                        skuAndContextResponse = await GetSkuAndContext(productSkusResponse.Id);
                                    }
                                    catch (Exception ex)
                                    {
                                        _context.Vtex.Logger.Error("ExportToSheet", "GetSkuAndContext", $"Error getting Sku and Context for skuId {productSkusResponse.Id}", ex);
                                        Console.WriteLine($"Error getting Sku and Context for skuId {productSkusResponse.Id}");
                                    }

                                    //string brandName = brandList.Where(b => b.Id.Equals(getProductByIdResponse.BrandId)).Select(b => b.Name).FirstOrDefault();
                                    //string[] eans = await GetEansBySkuId(skuId);
                                    //string ean = string.Empty;
                                    //if (eans != null)
                                    //{
                                    //    ean = string.Join("\n", eans);
                                    //}

                                    //GetSkuImagesResponse[] skuImages = await GetSkuImages(skuAndContextResponse.Id.ToString());
                                    GetPriceResponse getPriceResponse = await GetPrice(skuAndContextResponse.Id.ToString());
                                    StringBuilder prodSpecs = new StringBuilder();
                                    ProductSpecification[] productSpecifications = await GetProductSpecifications(productId);
                                    if (productSpecifications != null)
                                    {
                                        foreach (ProductSpecification productSpecification in productSpecifications)
                                        {
                                            prodSpecs.AppendLine($"{productSpecification.Name}:{string.Join(',', productSpecification.Value)}");
                                        }
                                    }

                                    StringBuilder skuSpecs = new StringBuilder();
                                    SkuSpecification[] skuSpecifications = await GetSkuSpecifications(productSkusResponse.Id);
                                    if (skuSpecifications != null)
                                    {
                                        foreach (SkuSpecification skuSpecification in skuSpecifications)
                                        {
                                            string skuSpecName = productSpecifications.Where(s => s.Id.Equals(skuSpecification.FieldValueId)).Select(s => s.Name).FirstOrDefault();
                                            string skuSpecValue = skuSpecification.Text;
                                            skuSpecs.AppendLine($"{skuSpecName}:{skuSpecValue}");
                                        }
                                    }

                                    //arrayToWrite[index] = new string[] { productId, skuId.ToString(), categoryIds[categoryId], brandName, getProductByIdResponse.Name, getProductByIdResponse.RefId, skuAndContextResponse.NameComplete, "EAN", "SKU REF", skuAndContextResponse.Dimension.Height.ToString(), skuAndContextResponse.Dimension.Width.ToString(), skuAndContextResponse.Dimension.Length.ToString(), skuAndContextResponse.Dimension.Weight.ToString(), getProductByIdResponse.Description, "SEARCH KEYWORDS", getProductByIdResponse.MetaTagDescription, skuAndContextResponse.ImageUrl, "", "", "", "", "", "0.00", "0.00", "0", "Material", "Color", "", "" };
                                    Console.WriteLine($"INDEX = {index}");
                                    arrayToWrite[index] = new string[headerIndexDictionary.Count];
                                    arrayToWrite[index][headerIndexDictionary["productid"]] = productId;
                                    arrayToWrite[index][headerIndexDictionary["skuid"]] = productSkusResponse.Id;
                                    arrayToWrite[index][headerIndexDictionary["category"]] = categoryIds[getProductByIdResponse.CategoryId];
                                    arrayToWrite[index][headerIndexDictionary["brand"]] = skuAndContextResponse.BrandName;
                                    arrayToWrite[index][headerIndexDictionary["productname"]] = getProductByIdResponse.Name;
                                    arrayToWrite[index][headerIndexDictionary["product reference code"]] = getProductByIdResponse.RefId;
                                    arrayToWrite[index][headerIndexDictionary["skuname"]] = skuAndContextResponse.NameComplete;
                                    arrayToWrite[index][headerIndexDictionary["sku ean/gtin"]] = skuAndContextResponse.AlternateIds.Ean;
                                    arrayToWrite[index][headerIndexDictionary["sku reference code"]] = skuAndContextResponse.AlternateIds.RefId;
                                    arrayToWrite[index][headerIndexDictionary["height"]] = skuAndContextResponse.Dimension.Height.ToString();
                                    arrayToWrite[index][headerIndexDictionary["width"]] = skuAndContextResponse.Dimension.Width.ToString();
                                    arrayToWrite[index][headerIndexDictionary["length"]] = skuAndContextResponse.Dimension.Length.ToString();
                                    arrayToWrite[index][headerIndexDictionary["weight"]] = skuAndContextResponse.Dimension.Weight.ToString();
                                    arrayToWrite[index][headerIndexDictionary["product description"]] = getProductByIdResponse.DescriptionShort;
                                    arrayToWrite[index][headerIndexDictionary["search keywords"]] = skuAndContextResponse.KeyWords;
                                    arrayToWrite[index][headerIndexDictionary["metatag description"]] = getProductByIdResponse.Description;
                                    arrayToWrite[index][headerIndexDictionary["image url 1"]] = skuAndContextResponse.ImageUrl;
                                    //arrayToWrite[index][headerIndexDictionary["image url 2"]] = skuAndContextResponse.ImageUrl;
                                    //arrayToWrite[index][headerIndexDictionary["image url 3"]] = skuAndContextResponse.ImageUrl;
                                    //arrayToWrite[index][headerIndexDictionary["image url 4"]] = skuAndContextResponse.ImageUrl;
                                    //arrayToWrite[index][headerIndexDictionary["image url 5"]] = skuAndContextResponse.ImageUrl;
                                    arrayToWrite[index][headerIndexDictionary["display if out of stock"]] = getProductByIdResponse.ShowWithoutStock.ToString().ToUpper();
                                    arrayToWrite[index][headerIndexDictionary["msrp"]] = getPriceResponse != null ? getPriceResponse.CostPrice.ToString() : string.Empty;
                                    arrayToWrite[index][headerIndexDictionary["selling price (price to gpp)"]] = getPriceResponse != null ? getPriceResponse.BasePrice.ToString() : string.Empty;
                                    //arrayToWrite[index][headerIndexDictionary["available quantity"]] = 
                                    arrayToWrite[index][headerIndexDictionary["productspecs"]] = prodSpecs.ToString();
                                    arrayToWrite[index][headerIndexDictionary["sku specs"]] = skuSpecs.ToString();
                                    //productId, skuId.ToString(), categoryIds[categoryId], brandName, getProductByIdResponse.Name, getProductByIdResponse.RefId, skuAndContextResponse.NameComplete, "EAN", "SKU REF", skuAndContextResponse.Dimension.Height.ToString(), skuAndContextResponse.Dimension.Width.ToString(), skuAndContextResponse.Dimension.Length.ToString(), skuAndContextResponse.Dimension.Weight.ToString(), getProductByIdResponse.Description, "SEARCH KEYWORDS", getProductByIdResponse.MetaTagDescription, skuAndContextResponse.ImageUrl, "", "", "", "", "", "0.00", "0.00", "0", "Material", "Color", "", "" 


                                    index++;
                                    if (index % writeBlockSize == 0)
                                    {
                                        ValueRange valueRangeToWrite = new ValueRange
                                        {
                                            Range = $"{sheetName}!A{offset + 2}:AZ{offset + writeBlockSize + 1}",
                                            Values = arrayToWrite
                                        };

                                        var writeToSheetResult = await _googleSheetsService.WriteSpreadsheetValues(sheetId, valueRangeToWrite);
                                        offset += writeBlockSize;
                                        arrayToWrite = new string[writeBlockSize + 1][];
                                        index = 0;
                                    }
                                }
                            }
                        }

                        ValueRange valueRangeToWriteRemaining = new ValueRange
                        {
                            Range = $"{sheetName}!A{offset + 2}:AZ{offset + writeBlockSize + 1}",
                            Values = arrayToWrite
                        };

                        var writeToSheetResultRemaining = await _googleSheetsService.WriteSpreadsheetValues(sheetId, valueRangeToWriteRemaining);
                    }
                }
            }

            return "Done";
        }

        private async Task<long?> GetCategoryIdByName(GetCategoryTreeResponse[] categoryTree, string categoryName)
        {
            long? categoryId = null;
            if (!string.IsNullOrEmpty(categoryName))
            {
                string[] nameArr = categoryName.Split('/');
                string currentLevelCategoryName = nameArr[0];
                Console.WriteLine($"Category Name = {currentLevelCategoryName}");
                GetCategoryTreeResponse currentLevelCategoryTree = categoryTree.Where(t => t.Name.Equals(currentLevelCategoryName)).FirstOrDefault();
                if (currentLevelCategoryTree != null)
                {
                    if (nameArr.Length == 0)
                    {
                        categoryId = currentLevelCategoryTree.Id;
                    }
                    else if (currentLevelCategoryTree.HasChildren)
                    {
                        categoryName = categoryName.Replace($"{currentLevelCategoryName}/", string.Empty);
                        categoryId = await GetCategoryIdByName(currentLevelCategoryTree.Children, categoryName);
                    }
                }
                else
                {
                    Console.WriteLine("Current Level Category Tree is Null");
                }
            }
            else
            {
                Console.WriteLine("Empty Category Name");
            }

            Console.WriteLine($"categoryId = _ {categoryId} _ ");

            return categoryId;
        }

        private async Task<Dictionary<long, string>> GetCategoryId(GetCategoryTreeResponse[] categoryTree)
        {
            Dictionary<long, string> categoryPath = new Dictionary<long, string>();
            foreach (GetCategoryTreeResponse categoryObj in categoryTree)
            {
                if (categoryObj.HasChildren)
                {
                    Dictionary<long, string> childCategoryPath = await GetCategoryId(categoryObj.Children);
                    foreach (long categoryId in childCategoryPath.Keys)
                    {
                        categoryPath.Add(categoryId, $"{categoryObj.Name}/{childCategoryPath[categoryId]}");
                    }
                }
                else
                {
                    categoryPath.Add(categoryObj.Id, categoryObj.Name);
                }
            }

            return categoryPath;
        }

        public async Task<UpdateResponse> CreateProduct(ProductRequest createProductRequest)
        {
            // POST https://{accountName}.{environment}.com.br/api/catalog/pvt/product

            string responseContent = string.Empty;
            bool success = false;
            string statusCode = string.Empty;

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

                success = response.IsSuccessStatusCode;
                statusCode = response.StatusCode.ToString();
                responseContent = await response.Content.ReadAsStringAsync();
                _context.Vtex.Logger.Debug("CreateProduct", null, $"{jsonSerializedData} [{statusCode}] {responseContent}");
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CreateProduct", null, $"Error creating product {createProductRequest.Title}", ex);
            }

            UpdateResponse updateResponse = new UpdateResponse
            {
                Success = success,
                Message = responseContent,
                StatusCode = statusCode
            };

            return updateResponse;
        }

        public async Task<UpdateResponse> UpdateProduct(string productId, ProductRequest updateProductRequest)
        {
            // PUT https://{accountName}.{environment}.com.br/api/catalog/pvt/product/productId

            string responseContent = string.Empty;
            bool success = false;
            string statusCode = string.Empty;

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

                success = response.IsSuccessStatusCode;
                statusCode = response.StatusCode.ToString();
                responseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("UpdateProduct", null, $"Error updating product {updateProductRequest.Title}", ex);
            }

            UpdateResponse updateResponse = new UpdateResponse
            {
                Success = success,
                Message = responseContent,
                StatusCode = statusCode
            };

            return updateResponse;
        }

        public async Task<UpdateResponse> CreateSku(SkuRequest createSkuRequest)
        {
            // POST https://{accountName}.{environment}.com.br/api/catalog/pvt/stockkeepingunit

            string responseContent = string.Empty;
            bool success = false;
            string statusCode = string.Empty;

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

                success = response.IsSuccessStatusCode;
                statusCode = response.StatusCode.ToString();
                responseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CreateSku", null, $"Error creating sku {createSkuRequest.Name}", ex);
            }

            UpdateResponse updateResponse = new UpdateResponse
            {
                Success = success,
                Message = responseContent,
                StatusCode = statusCode
            };

            return updateResponse;
        }

        public async Task<UpdateResponse> UpdateSku(string skuId, SkuRequest updateSkuRequest)
        {
            // PUT https://{accountName}.{environment}.com.br/api/catalog/pvt/stockkeepingunit/skuId

            string responseContent = string.Empty;
            bool success = false;
            string statusCode = string.Empty;

            try
            {
                string jsonSerializedData = JsonConvert.SerializeObject(updateSkuRequest);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
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

                success = response.IsSuccessStatusCode;
                statusCode = response.StatusCode.ToString();
                responseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("UpdateSku", null, $"Error updating sku {updateSkuRequest.Name}", ex);
            }

            UpdateResponse updateResponse = new UpdateResponse
            {
                Success = success,
                Message = responseContent,
                StatusCode = statusCode
            };

            return updateResponse;
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

        public async Task<UpdateResponse> CreateSkuFile(string skuId, string imageName, string imageText, bool isMain, string imageUrl)
        {
            //POST https://{accountName}.{environment}.com.br/api/catalog/pvt/stockkeepingunit/skuId/file

            bool success = false;
            string responseContent = string.Empty;
            string statusCode = string.Empty;

            if (string.IsNullOrEmpty(skuId) || string.IsNullOrEmpty(imageUrl))
            {
                responseContent = "Missing Parameter";
            }
            else
            {
                try
                {
                    ImageUpdate imageUpdate = new ImageUpdate
                    {
                        IsMain = isMain,
                        Label = imageText,
                        Name = imageName,
                        Text = imageText,
                        Url = imageUrl
                    };

                    string jsonSerializedData = JsonConvert.SerializeObject(imageUpdate);

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog/pvt/stockkeepingunit/{skuId}/file"),
                        Content = new StringContent(jsonSerializedData, Encoding.UTF8, SheetsCatalogImportConstants.APPLICATION_JSON)
                    };

                    //request.Headers.Add(SheetsCatalogImportConstants.USE_HTTPS_HEADER_NAME, "true");
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
                    success = response.IsSuccessStatusCode;
                    if (!success)
                    {
                        _context.Vtex.Logger.Info("UpdateSkuImage", null, $"Response: {responseContent}  [{response.StatusCode}] for request '{jsonSerializedData}' to {request.RequestUri}");
                    }

                    statusCode = response.StatusCode.ToString();
                    if (string.IsNullOrEmpty(responseContent))
                    {
                        responseContent = $"Updated:{success} {response.StatusCode}";
                    }
                    else if (responseContent.Contains(SheetsCatalogImportConstants.ARCHIVE_CREATED))
                    {
                        // If the image was already added to the sku, consider it a success
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    _context.Vtex.Logger.Error("UpdateSkuImage", null, $"Error updating sku '{skuId}' {imageName}", ex);
                    success = false;
                    responseContent = ex.Message;
                }
            }

            UpdateResponse updateResponse = new UpdateResponse
            {
                Success = success,
                Message = responseContent,
                StatusCode = statusCode
            };

            return updateResponse;
        }

        public async Task<UpdateResponse> CreateEANGTIN(string skuId, string ean)
        {
            // POST https://accountName.vtexcommercestable.com.br/api/catalog/pvt/stockkeepingunit/skuId/ean/ean

            bool success = false;
            string responseContent = string.Empty;
            string statusCode = string.Empty;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog/pvt/stockkeepingunit/{skuId}/ean/{ean}"),
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

                success = response.IsSuccessStatusCode;
                statusCode = response.StatusCode.ToString();
                responseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CreateEANGTIN", null, $"Error creating EAN/GTIN {ean} for Sku {skuId}", ex);
            }

            UpdateResponse updateResponse = new UpdateResponse
            {
                Success = success,
                Message = responseContent,
                StatusCode = statusCode
            };

            return updateResponse;
        }

        public async Task<UpdateResponse> CreatePrice(string skuId, CreatePrice createPrice)
        {
            // PUT https://api.vtex.com/accountName/pricing/prices/skuId

            bool success = false;
            string responseContent = string.Empty;
            string statusCode = string.Empty;

            try
            {
                string jsonSerializedData = JsonConvert.SerializeObject(createPrice);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri($"http://api.vtex.com/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}/pricing/prices/{skuId}"),
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

                success = response.IsSuccessStatusCode;
                statusCode = response.StatusCode.ToString();
                responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"CreatePrice '{request.RequestUri}' [{statusCode}] {responseContent} {jsonSerializedData}");
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CreatePrice", null, $"Error creating Price for Sku {skuId}", ex);
            }

            UpdateResponse updateResponse = new UpdateResponse
            {
                Success = success,
                Message = responseContent,
                StatusCode = statusCode
            };

            return updateResponse;
        }

        public async Task<GetWarehousesResponse[]> GetWarehouses()
        {
            // GET https://logistics.environment.com.br/api/logistics/pvt/configuration/warehouses?an=accountName

            GetWarehousesResponse[] getWarehousesResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://logistics.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/logistics/pvt/configuration/warehouses?an={this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}")
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
                //Console.WriteLine($"GetWarehouses [{response.StatusCode}] {responseContent}");
                if (response.IsSuccessStatusCode)
                {
                    getWarehousesResponse = JsonConvert.DeserializeObject<GetWarehousesResponse[]>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetWarehouses", null, $"Could not get warehouses' [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetWarehouses", null, $"Error getting warehouses", ex);
            }

            return getWarehousesResponse;
        }

        public async Task<GetWarehousesResponse[]> ListAllWarehouses()
        {
            GetWarehousesResponse[] listAllWarehousesResponse = null;
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/logistics/pvt/configuration/warehouses")
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
            Console.WriteLine($"ListAllWarehouses [{response.StatusCode}] {responseContent}");
            if (response.IsSuccessStatusCode)
            {
                listAllWarehousesResponse = JsonConvert.DeserializeObject<GetWarehousesResponse[]>(responseContent);
            }

            return listAllWarehousesResponse;
        }

        public async Task<UpdateResponse> SetInventory(string skuId, string warehouseId, InventoryRequest inventoryRequest)
        {
            // PUT https://logistics.vtexcommercestable.com.br/api/logistics/pvt/inventory/skus/skuId/warehouses/warehouseId?an=accountName

            bool success = false;
            string responseContent = string.Empty;
            string statusCode = string.Empty;

            try
            {
                string jsonSerializedData = JsonConvert.SerializeObject(inventoryRequest);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri($"http://logistics.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/logistics/pvt/inventory/skus/{skuId}/warehouses/{warehouseId}?an={this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}"),
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

                success = response.IsSuccessStatusCode;
                statusCode = response.StatusCode.ToString();
                responseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SetInventory", null, $"Error setting inventory for sku {skuId} in warehouse {warehouseId}", ex);
            }

            UpdateResponse updateResponse = new UpdateResponse
            {
                Success = success,
                Message = responseContent,
                StatusCode = statusCode
            };

            return updateResponse;
        }

        public async Task<UpdateResponse> SetProdSpecs(string productId, SpecAttr prodSpec)
        {
            // PUT http://accountName.environment.com.br/api/catalog/pvt/product/productId/specificationvalue

            bool success = false;
            string responseContent = string.Empty;
            string statusCode = string.Empty;

            try
            {
                string jsonSerializedData = JsonConvert.SerializeObject(prodSpec);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/catalog/pvt/product/{productId}/specificationvalue"),
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

                success = response.IsSuccessStatusCode;
                statusCode = response.StatusCode.ToString();
                responseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SetProdSpecs", null, $"Error setting product specs for prodId {productId}", ex);
            }

            UpdateResponse updateResponse = new UpdateResponse
            {
                Success = success,
                Message = responseContent,
                StatusCode = statusCode
            };

            return updateResponse;
        }

        public async Task<UpdateResponse> SetSkuSpec(string skuId, SpecAttr skuSpec)
        {
            // PUT http://accountName.vtexcommercestable.com.br/api/catalog/pvt/stockkeepingunit/SkuId/specificationvalue

            bool success = false;
            string responseContent = string.Empty;
            string statusCode = string.Empty;

            try
            {
                string jsonSerializedData = JsonConvert.SerializeObject(skuSpec);
                //Console.WriteLine($"SetSkuSpec: {jsonSerializedData}");
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/catalog/pvt/stockkeepingunit/{skuId}/specificationvalue"),
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

                success = response.IsSuccessStatusCode;
                statusCode = response.StatusCode.ToString();
                responseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SetSkuSpec", null, $"Error setting product specs for sku {skuId}", ex);
            }

            UpdateResponse updateResponse = new UpdateResponse
            {
                Success = success,
                Message = responseContent,
                StatusCode = statusCode
            };

            return updateResponse;
        }

        public async Task<ProductAndSkuIdsResponse> GetProductAndSkuIds(long categoryId)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog_system/pvt/products/GetProductAndSkuIds

            ProductAndSkuIdsResponse productAndSkuIdsResponse = new ProductAndSkuIdsResponse();

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/catalog_system/pvt/products/GetProductAndSkuIds?categoryId={categoryId}")
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
                //Console.WriteLine($"GetWarehouses [{response.StatusCode}] {responseContent}");
                if (response.IsSuccessStatusCode)
                {
                    productAndSkuIdsResponse = JsonConvert.DeserializeObject<ProductAndSkuIdsResponse>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetProductAndSkuIds", null, $"Could not get products and skus for category '{categoryId}' [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetProductAndSkuIds", null, $"Error getting products and skus for category '{categoryId}'", ex);
            }

            return productAndSkuIdsResponse;
        }

        public async Task<SkuAndContextResponse> GetSkuAndContext(string skuId)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog_system/pvt/sku/stockkeepingunitbyid/skuId

            SkuAndContextResponse skuAndContextResponse = new SkuAndContextResponse();

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/catalog_system/pvt/sku/stockkeepingunitbyid/{skuId}")
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
                //Console.WriteLine($"GetWarehouses [{response.StatusCode}] {responseContent}");
                if (response.IsSuccessStatusCode)
                {
                    skuAndContextResponse = JsonConvert.DeserializeObject<SkuAndContextResponse>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetSkuAndContext", null, $"Could not get sku '{skuId}' [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetSkuAndContext", null, $"Error getting sku '{skuId}'", ex);
            }

            return skuAndContextResponse;
        }

        public async Task<string[]> GetEansBySkuId(long skuId)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog/pvt/stockkeepingunit/skuId/ean

            string[] eans = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/catalog/pvt/stockkeepingunit/{skuId}/ean")
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
                Console.WriteLine($"GetEansBySkuId [{response.StatusCode}] {responseContent}");
                if (response.IsSuccessStatusCode)
                {
                    eans = JsonConvert.DeserializeObject<string[]>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetEanBySkuId", null, $"Could not get EAN for sku '{skuId}' [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetEanBySkuId", null, $"Error getting EAN for sku '{skuId}'", ex);
            }

            return eans;
        }

        public async Task<ProductSearchResponse[]> ProductSearch(string search)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog_system/pub/products/search/search

            ProductSearchResponse[] productSearchResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/catalog_system/pub/products/search/{search}")
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
                //Console.WriteLine($"ProductSearch [{response.StatusCode}] {responseContent}");
                if (response.IsSuccessStatusCode)
                {
                    productSearchResponse = JsonConvert.DeserializeObject<ProductSearchResponse[]>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("ProductSearch", null, $"Could not search '{search}' [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("ProductSearch", null, $"Error searching '{search}'", ex);
            }

            return productSearchResponse;
        }

        public async Task<List<ProductSkusResponse>> GetSkusFromProductId(string productId)
        {
            // GET https://{{accountName}}.vtexcommercestable.com.br/api/catalog_system/pvt/sku/stockkeepingunitByProductId/{{productId}}

            List<ProductSkusResponse> productSkusResponses = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog_system/pvt/sku/stockkeepingunitByProductId/{productId}")
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
                    productSkusResponses = JsonConvert.DeserializeObject<List<ProductSkusResponse>>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetSkusFromProductId", null, $"Could not get skus for product id '{productId}'  [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetSkusFromProductId", null, $"Error getting skus for product id '{productId}'", ex);
            }

            return productSkusResponses;
        }

        public async Task<string> ClearSheet()
        {
            string response = string.Empty;

            DateTime importStarted = await _sheetsCatalogImportRepository.CheckImportLock();
            TimeSpan elapsedTime = DateTime.Now - importStarted;
            if (elapsedTime.TotalHours < SheetsCatalogImportConstants.LOCK_TIMEOUT)
            {
                Console.WriteLine("Blocked by lock");
                _context.Vtex.Logger.Info("ClearSheet", null, $"Blocked by lock.  Import started: {importStarted}");
                return ($"Import started {importStarted} in progress.");
            }

            StringBuilder sb = new StringBuilder();

            string importFolderId = null;
            string accountFolderId = null;
            string accountName = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME];

            FolderIds folderIds = await _sheetsCatalogImportRepository.LoadFolderIds(accountName);
            if (folderIds != null)
            {
                importFolderId = folderIds.ProductsFolderId;
                //accountFolderId = folderIds.AccountFolderId;
            }
            else
            {
                Console.WriteLine("LoadFolderIds returned Null!");
            }

            Console.WriteLine($"ListSheetsInFolder {importFolderId}");
            ListFilesResponse spreadsheets = await _googleSheetsService.ListSheetsInFolder(importFolderId);
            if (spreadsheets != null)
            {
                var sheetIds = spreadsheets.Files.Select(s => s.Id);
                foreach (var sheetId in sheetIds)
                {
                    SheetRange sheetRange = new SheetRange();
                    sheetRange.Ranges = new List<string>();
                    sheetRange.Ranges.Add($"A2:ZZ{SheetsCatalogImportConstants.DEFAULT_SHEET_SIZE}");
                    var clearResponse = await _googleSheetsService.ClearSpreadsheet(sheetId, sheetRange);
                }
            }

            return response;
        }

        public async Task<ListFilesResponse> ListImageFiles()
        {
            // GET https://{{accountName}}.myvtex.com/google-drive-import/list-images

            ListFilesResponse listFilesResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.myvtex.com/google-drive-import/list-images")
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
                    listFilesResponse = JsonConvert.DeserializeObject<ListFilesResponse>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("ListImageFiles", null, $"Could not get image file list  [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("ListImageFiles", null, $"Error getting image file list", ex);
            }

            return listFilesResponse;
        }

        public async Task<string> AddImagesToSheet()
        {
            string response = string.Empty;
            string importFolderId = null;
            string accountFolderId = null;
            string accountName = this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME];

            FolderIds folderIds = await _sheetsCatalogImportRepository.LoadFolderIds(accountName);
            if (folderIds != null)
            {
                importFolderId = folderIds.ProductsFolderId;
            }
            else
            {
                Console.WriteLine("LoadFolderIds returned Null!");
            }

            Console.WriteLine($"ListSheetsInFolder {importFolderId}");
            ListFilesResponse spreadsheets = await _googleSheetsService.ListSheetsInFolder(importFolderId);
            if (spreadsheets != null)
            {
                var sheetIds = spreadsheets.Files.Select(s => s.Id);
                foreach (var sheetId in sheetIds)
                {
                    ListFilesResponse listFilesResponse = await this.ListImageFiles();
                    if (listFilesResponse != null)
                    {
                        string[][] filesToWrite = new string[listFilesResponse.Files.Count][];
                        int index = 0;
                        foreach (GoogleFile file in listFilesResponse.Files)
                        {
                            filesToWrite[index] = new string[] { file.Name, $"=IMAGE(\"{ file.ThumbnailLink}\")", file.WebViewLink.ToString() };
                            index++;
                        }

                        ValueRange valueRangeToWrite = new ValueRange
                        {
                            Range = $"{SheetsCatalogImportConstants.SheetNames.IMAGES}!A2:C{listFilesResponse.Files.Count + 1}",
                            Values = filesToWrite
                        };

                        var writeToSheetResult = await _googleSheetsService.WriteSpreadsheetValues(sheetId, valueRangeToWrite);
                    }
                }
            }

            return response;
        }

        public async Task<GetSkuImagesResponse[]> GetSkuImages(string skuId)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog/pvt/stockkeepingunit/skuId/file

            GetSkuImagesResponse[] getSkuResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog/pvt/stockkeepingunit/{skuId}/file")
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
                    getSkuResponse = JsonConvert.DeserializeObject<GetSkuImagesResponse[]>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetSkuImages", null, $"Did not get images for skuid '{skuId}'");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetSkuImages", null, $"Error getting images for skuid '{skuId}'", ex);
            }

            return getSkuResponse;
        }

        public async Task<GetPriceResponse> GetPrice(string skuId)
        {
            // GET https://api.vtex.com/{accountName}/pricing/prices/itemId

            GetPriceResponse getPriceResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://api.vtex.com/{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}/pricing/prices/{skuId}")
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
                Console.WriteLine($"GetPrice [{response.StatusCode}] {responseContent}");
                if (response.IsSuccessStatusCode)
                {
                    getPriceResponse = JsonConvert.DeserializeObject<GetPriceResponse>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetPrice", null, $"Could not get prices for sku '{skuId}' [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetPrice", null, $"Error getting prices for sku '{skuId}'", ex);
            }

            return getPriceResponse;
        }

        public async Task<ProductSpecification[]> GetProductSpecifications(string productId)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog_system/pvt/products/productId/specification

            ProductSpecification[] productSpecifications = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog_system/pvt/products/{productId}/specification")
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
                Console.WriteLine($"GetProductSpecifications [{response.StatusCode}] {responseContent}");
                if (response.IsSuccessStatusCode)
                {
                    productSpecifications = JsonConvert.DeserializeObject<ProductSpecification[]>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetProductSpecifications", null, $"Could not get product specifications for product id '{productId}' [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetProductSpecifications", null, $"Error getting product specifications for product id '{productId}'", ex);
            }

            return productSpecifications;
        }

        public async Task<SkuSpecification[]> GetSkuSpecifications(string skuId)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog/pvt/stockkeepingunit/skuId/specification

            SkuSpecification[] productSpecifications = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[SheetsCatalogImportConstants.VTEX_ACCOUNT_HEADER_NAME]}.{SheetsCatalogImportConstants.ENVIRONMENT}.com.br/api/catalog/pvt/stockkeepingunit/{skuId}/specification")
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
                Console.WriteLine($"GetSkuSpecifications [{response.StatusCode}] {responseContent}");
                if (response.IsSuccessStatusCode)
                {
                    productSpecifications = JsonConvert.DeserializeObject<SkuSpecification[]>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetSkuSpecifications", null, $"Could not get sku specifications for sku id '{skuId}' [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetSkuSpecifications", null, $"Error getting sku specifications for sku id '{skuId}'", ex);
            }

            return productSpecifications;
        }

        private async Task<double?> ParseDouble(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else
            {
                double retVal;
                if (double.TryParse(value, out retVal))
                {
                    return retVal;
                }
                else
                {
                    Console.WriteLine($"    --------------- Could not parse {value}");
                    _context.Vtex.Logger.Warn("ParseDouble", null, $"Could not parse {value}");
                    return null;
                }
            }
        }

        private async Task<decimal?> ParseCurrency(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else
            {
                var regex = new Regex(@"([\d,.]+)");
                var match = regex.Match(value);
                if (match.Success)
                {
                    value = match.Groups[1].Value;
                }

                decimal retVal;
                if (decimal.TryParse(value, out retVal))
                {
                    return retVal;
                }
                else
                {
                    Console.WriteLine($"    --------------- Could not parse {value}");
                    _context.Vtex.Logger.Warn("ParseCurrency", null, $"Could not parse {value}");
                    return null;
                }
            }
        }

        private async Task<long?> ParseLong(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else
            {
                long retVal;
                if (long.TryParse(value, out retVal))
                {
                    return retVal;
                }
                else
                {
                    Console.WriteLine($"    --------------- Could not parse {value}");
                    _context.Vtex.Logger.Warn("ParseLong", null, $"Could not parse {value}");
                    return null;
                }
            }
        }

        private async Task<bool> ParseBool(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            else
            {
                bool retVal;
                if (bool.TryParse(value, out retVal))
                {
                    return retVal;
                }
                else
                {
                    Console.WriteLine($"    --------------- Could not parse {value}");
                    _context.Vtex.Logger.Warn("ParseBool", null, $"Could not parse {value}");
                    return false;
                }
            }
        }

        private async Task<string> ToPrice(long? inPennies)
        {
            string priceString = string.Empty;
            if(inPennies != null)
            {
                decimal inDollars = (decimal)inPennies / 100;
                priceString = inDollars.ToString();
            }

            return priceString;
        }

        private async Task<string> GetColumnLetter(int columnNumber)
        {
            string columnLetter = string.Empty;
            int letterCode = columnNumber + 65;
            if (letterCode <= 90)
            {
                columnLetter = ((char)letterCode).ToString();
            }
            else
            {
                letterCode = letterCode - 26;
                columnLetter = ((char)letterCode).ToString();
                columnLetter = $"A{columnLetter}";
            }

            return columnLetter;
        }
    }
}
