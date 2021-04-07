using System;
using System.Collections.Generic;
using System.Text;

namespace SheetsCatalogImport.Data
{
    public class SheetsCatalogImportConstants
    {
        public const string APP_NAME = "sheets-catalog-import";

        public const string APP_TOKEN = "X-Vtex-Api-AppToken";
        public const string APP_KEY = "X-Vtex-Api-AppKey";

        public const string FORWARDED_HEADER = "X-Forwarded-For";
        public const string FORWARDED_HOST = "X-Forwarded-Host";
        public const string APPLICATION_JSON = "application/json";
        public const string TEXT = "text/plain";
        public const string APPLICATION_FORM = "application/x-www-form-urlencoded";
        public const string HEADER_VTEX_CREDENTIAL = "X-Vtex-Credential";
        public const string AUTHORIZATION_HEADER_NAME = "Authorization";
        public const string PROXY_AUTHORIZATION_HEADER_NAME = "Proxy-Authorization";
        public const string USE_HTTPS_HEADER_NAME = "X-Vtex-Use-Https";
        public const string PROXY_TO_HEADER_NAME = "X-Vtex-Proxy-To";
        public const string VTEX_ACCOUNT_HEADER_NAME = "X-Vtex-Account";
        public const string ENVIRONMENT = "vtexcommercestable";
        public const string LOCAL_ENVIRONMENT = "myvtex";
        public const string VTEX_ID_HEADER_NAME = "VtexIdclientAutCookie";
        public const string VTEX_WORKSPACE_HEADER_NAME = "X-Vtex-Workspace";
        public const string APP_SETTINGS = "vtex.sheets-catalog-import";
        public const string ACCEPT = "Accept";
        public const string CONTENT_TYPE = "Content-Type";
        public const string HTTP_FORWARDED_HEADER = "HTTP_X_FORWARDED_FOR";

        //public const string BUCKET = "google-sheet-catalog";
        public const string BUCKET = "google-drive";
        public const string CREDENTIALS = "google-credentials";
        public const string TOKEN = "google-token";
        public const string LOCK = "catalog-import-lock";

        public const string GOOGLE_REPONSE_TYPE = "code";
        public const string GOOGLE_ACCESS_TYPE = "offline";
        public const string AUTH_SITE_BASE = "googleauth.myvtex.com";
        public const string REDIRECT_PATH = "return";

        public const string AUTH_APP_PATH = "google-auth";
        public const string AUTH_PATH = "auth";
        public const string REVOKE_PATH = "revoke-token";
        public const string REFRESH_PATH = "refresh-token";

        public const string ADMIN_PAGE = "admin/sheets-catalog-import";

        public const string GOOGLE_DRIVE_URL = "https://www.googleapis.com/drive/v3";
        public const string GOOGLE_DRIVE_URL_V2 = "https://www.googleapis.com/drive/v2";
        public const string GOOGLE_SHEET_URL = "https://sheets.googleapis.com/v4";
        public const string GOOGLE_DRIVE_FILES = "files";
        public const string GOOGLE_DRIVE_SHEETS = "spreadsheets";
        public const string GOOGLE_DRIVE_UPLOAD_URL = "https://www.googleapis.com/upload/drive/v3";
        public const string GOOGLE_DRIVE_PAGE_SIZE = "1000";

        public const string GRANT_TYPE_AUTH = "authorization_code";
        public const string GRANT_TYPE_REFRESH = "refresh_token";

        public const string APP_TYPE = "catalog";

        public const string ARCHIVE_CREATED = "Sku archive already created";

        public const int MIN_WRITE_BLOCK_SIZE = 5;
        public const int WRITE_BLOCK_SIZE_DIVISOR = 50;
        public const int DEFAULT_SHEET_SIZE = 1000;

        public const int LOCK_TIMEOUT = 1;

        public const string HEADER = "ProductId,SkuId,Category,Brand,ProductName,Product Reference Code,SkuName,Sku EAN/GTIN,SKU Reference Code,Height,Width,Length,Weight,Product Description,Search Keywords,MetaTag Description,Image URL 1,Image URL 2,Image URL 3,Image URL 4,Image URL 5,Display if Out of Stock,MSRP,Selling Price (Price to GPP),Available Quantity,ProductSpecs,Sku Specs,Update,Status,Message";
        public const long VOLUMETIC_FACTOR = 166;

        public class FolderNames
        {
            // Folder Structure:
            // Google Drive root
            // --VTEX Import
            // ----{VTEX Account}
            // ------Products

            public const string IMPORT = "VTEX Import";
            public const string PRODUCTS = "Spreadsheet Import";
        }

        public class SheetNames
        {
            public const string SHEET_NAME = "VtexCatalogImport";
            public const string PRODUCTS = "ProductsForImport";
            public const string INSTRUCTIONS = "Instructions";
            public const string IMAGES = "Images";
        }

        public class ProductDisplayModes
        {
            // List of SKUs
            public const string LIST = "LIST";

            // Combo Boxes
            public const string COMBO = "COMBO";

            // Icons with radio selection(radio box)
            public const string RADIO = "RADIO";

            // Following definition of SKU specification
            public const string SPECIFICATION = "SPECIFICATION";
        }
    }
}
