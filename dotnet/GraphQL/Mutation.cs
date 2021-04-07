using GraphQL;
using GraphQL.Types;
using SheetsCatalogImport.Data;
using SheetsCatalogImport.Services;

namespace SheetsCatalogImport.GraphQL
{
    [GraphQLMetadata("Mutation")]
    public class Mutation : ObjectGraphType<object>
    {
        public Mutation(IGoogleSheetsService googleSheetsService, ISheetsCatalogImportRepository sheetsCatalogImportRepository, IVtexAPIService vtexAPIService)
        {
            Name = "Mutation";

            Field<BooleanGraphType>(
                "revokeToken",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "accountName", Description = "Account Name" }
                ),
                resolve: context =>
                {
                    bool revoked = googleSheetsService.RevokeGoogleAuthorizationToken().Result;
                    if (revoked)
                    {
                        string accountName = context.GetArgument<string>("accountName");
                        sheetsCatalogImportRepository.SaveFolderIds(null, accountName);
                    }

                    return revoked;
                });

            Field<StringGraphType>(
                "googleAuthorize",
                resolve: context =>
                {
                    return googleSheetsService.GetAuthUrl();
                });

            Field<StringGraphType>(
                "createSheet",
                resolve: context =>
                {
                    return googleSheetsService.CreateSheet();
                });

            Field<StringGraphType>(
                "processSheet",
                resolve: context =>
                {
                    return vtexAPIService.ProcessSheet();
                });

            Field<StringGraphType>(
                "clearSheet",
                resolve: context =>
                {
                    return vtexAPIService.ClearSheet();
                });

            Field<StringGraphType>(
                "addImages",
                resolve: context =>
                {
                    return vtexAPIService.AddImagesToSheet();
                });
        }
    }
}