using SheetsCatalogImport.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SheetsCatalogImport.Data
{
    public interface ISheetsCatalogImportRepository
    {
        Task<Token> LoadToken();
        Task<bool> SaveToken(Token token);
        Task<FolderIds> LoadFolderIds(string accountName);
        Task<bool> SaveFolderIds(FolderIds folderIds, string accountName);
        Task SetImportLock(DateTime importStartTime);
        Task<DateTime> CheckImportLock();
        Task ClearImportLock();
        Task<AppSettings> GetAppSettings();
    }
}