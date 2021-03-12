using SheetsCatalogImport.Data;
using SheetsCatalogImport.Models;
using SheetsCatalogImport.Services;
using GraphQL;
using GraphQL.Types;
using System.Linq;

namespace SheetsCatalogImport.GraphQL
{
    [GraphQLMetadata("Query")]
    public class Query : ObjectGraphType<object>
    {
        public Query(IGoogleSheetsService googleSheetsService, ISheetsCatalogImportRepository sheetsCatalogImportRepository)
        {
            Name = "Query";

            FieldAsync<BooleanGraphType>(
                "haveToken",
                resolve: async context =>
                {
                    Token token = await googleSheetsService.GetGoogleToken();
                    return token != null && !string.IsNullOrEmpty(token.RefreshToken);
                }
            );

            /// query Reviews($searchTerm: String, $from: Int, $to: Int, $orderBy: String, $status: Boolean)
            FieldAsync<StringGraphType>(
                "getOwnerEmail",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "accountName", Description = "Account Name" }
                ),
                resolve: async context =>
                {
                    string email = string.Empty;
                    string accountName = context.GetArgument<string>("accountName");
                    Token token = await googleSheetsService.GetGoogleToken();
                    if (token != null)
                    {
                        string productsFolderId = string.Empty;
                        FolderIds folderIds = await sheetsCatalogImportRepository.LoadFolderIds(accountName);
                        if (folderIds != null)
                        {
                            productsFolderId = folderIds.ProductsFolderId;
                            ListFilesResponse listFilesResponse = await googleSheetsService.ListSheetsInFolder(productsFolderId);
                            if (listFilesResponse != null)
                            {
                                var owners = listFilesResponse.Files.Select(o => o.Owners.Distinct()).FirstOrDefault();
                                if (owners != null)
                                {
                                    email = owners.Select(o => o.EmailAddress).FirstOrDefault();
                                }
                            }
                        }
                    }

                    return email;
                }
            );

            FieldAsync<StringGraphType>(
                "sheetLink",
                resolve: async context =>
                {
                    return await googleSheetsService.GetSheetLink();
                }
            );
        }
    }
}