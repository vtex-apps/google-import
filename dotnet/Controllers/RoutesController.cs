namespace SheetsCatalogImport.Controllers
{
    using SheetsCatalogImport.Data;
    using SheetsCatalogImport.Models;
    using SheetsCatalogImport.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.TagHelpers;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Vtex.Api.Context;

    public class RoutesController : Controller
    {
        private readonly IIOServiceContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IGoogleSheetsService _googleSheetsService;
        private readonly IVtexAPIService _vtexAPIService;
        private readonly ISheetsCatalogImportRepository _sheetsCatalogImportRepository;

        public RoutesController(IIOServiceContext context, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, IGoogleSheetsService googleSheetsService, IVtexAPIService vtexAPIService, ISheetsCatalogImportRepository SheetsCatalogImportRepository)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            this._clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            this._googleSheetsService = googleSheetsService ?? throw new ArgumentNullException(nameof(googleSheetsService));
            this._vtexAPIService = vtexAPIService ?? throw new ArgumentNullException(nameof(vtexAPIService));
            this._sheetsCatalogImportRepository = SheetsCatalogImportRepository ?? throw new ArgumentNullException(nameof(SheetsCatalogImportRepository));
        }

        public async Task<IActionResult> SheetsCatalogImport()
        {
            Response.Headers.Add("Cache-Control", "no-cache");

            return Ok();
        }

        public async Task<IActionResult> ProcessReturnUrl()
        {
            string code = _httpContextAccessor.HttpContext.Request.Query["code"];
            string siteUrl = _httpContextAccessor.HttpContext.Request.Query["state"];

            _context.Vtex.Logger.Info("ProcessReturnUrl", null, $"site=[{siteUrl}]");

            if (string.IsNullOrEmpty(siteUrl))
            {
                return BadRequest();
            }
            else
            {
                string redirectUri = $"https://{siteUrl}/{SheetsCatalogImportConstants.APP_NAME}/{SheetsCatalogImportConstants.REDIRECT_PATH}-code/?code={code}";
                return Redirect(redirectUri);
            }
        }

        public async Task<IActionResult> GoogleAuthorize()
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            string url = await _googleSheetsService.GetAuthUrl();
            if (string.IsNullOrEmpty(url))
            {
                return Json("Error");
            }
            else
            {
                return Redirect(url);
            }
        }

        public async Task<bool> SaveToken()
        {
            bool success = false;
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                Token token = JsonConvert.DeserializeObject<Token>(bodyAsText);

                success = await _sheetsCatalogImportRepository.SaveToken(token);
                success &= await _googleSheetsService.ShareToken(token);
            }

            return success;
        }

        public async Task<bool> ShareToken()
        {
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                Token token = JsonConvert.DeserializeObject<Token>(bodyAsText);

                return await _googleSheetsService.SaveToken(token);
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> HaveToken()
        {
            bool haveToken = false;
            Token token = await _googleSheetsService.GetGoogleToken();
            haveToken = token != null && !string.IsNullOrEmpty(token.RefreshToken);
            Console.WriteLine($"Have Token? {haveToken}");
            Response.Headers.Add("Cache-Control", "no-cache");
            return haveToken;
        }

        public async Task<IActionResult> GetOwners()
        {
            ListFilesResponse listFilesResponse = await _googleSheetsService.ListSheetsInFolder(string.Empty);
            var owners = listFilesResponse.Files.Select(o => o.Owners.Distinct());
            Response.Headers.Add("Cache-Control", "no-cache");
            return Json(owners);
        }

        public async Task<IActionResult> ListFiles()
        {
            ListFilesResponse listFilesResponse = await _googleSheetsService.ListSheetsInFolder(string.Empty);
            Response.Headers.Add("Cache-Control", "no-cache");
            return Json(listFilesResponse);
        }

        public async Task<IActionResult> GetOwnerEmail()
        {
            return Json(await _googleSheetsService.GetOwnerEmail());
        }

        public async Task<IActionResult> RevokeToken()
        {
            bool revoked = false;
            revoked = await _googleSheetsService.RevokeGoogleAuthorizationToken();
            Response.Headers.Add("Cache-Control", "no-cache");
            return Json(revoked);
        }

        public async Task<IActionResult> GetCategoryTree()
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            return Json(await _vtexAPIService.GetCategoryTree(5));
        }

        public async Task ClearLock()
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            await _sheetsCatalogImportRepository.ClearImportLock();
        }

        public async Task<IActionResult> Export()
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            var queryString = HttpContext.Request.Query;
            string query = queryString["q"];
            return Json(await _vtexAPIService.ExportToSheet(query));
        }

        public async Task<IActionResult> SearchTotals()
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            var queryString = HttpContext.Request.Query;
            string query = queryString["q"];
            return Json(await _vtexAPIService.SearchTotal(query));
        }
    }
}
