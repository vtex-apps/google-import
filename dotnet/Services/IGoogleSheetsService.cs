using SheetsCatalogImport.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SheetsCatalogImport.Services
{
    public interface IGoogleSheetsService
    {
        Task<Token> RefreshGoogleAuthorizationToken(string refreshToken);
        Task<bool> RevokeGoogleAuthorizationToken();
        Task<bool> SaveToken(Token token);
        Task<Token> GetGoogleToken();
        Task<string> GetAuthUrl();
        Task<bool> ShareToken(Token token);

        Task<string> CreateSheet();
        Task<string> GetSheetLink();
        Task<ListFilesResponse> ListSheetsInFolder(string folderId);
        Task<ListFilesResponse> GetFolders();
        Task<Dictionary<string, string>> ListFolders(string parentId = null);
        Task<string> CreateFolder(string folderName, string parentId = null);
        Task<bool> MoveFile(string fileId, string folderId);
        Task<bool> SetPermission(string fileId);
        Task<bool> RenameFile(string fileId, string fileName);
        Task<string> SaveFile(StringBuilder file);
        Task<string> GetSheet(string fileId, string range);
        Task<string> GetOwnerEmail();
        Task<UpdateValuesResponse> WriteSpreadsheetValues(string fileId, ValueRange valueRange);
        Task<string> UpdateSpreadsheet(string fileId, BatchUpdate batchUpdate);
        Task<string> ClearSpreadsheet(string fileId, SheetRange sheetRange);
    }
}