using SheetsCatalogImport.Models;
using System.Threading.Tasks;

namespace SheetsCatalogImport.Services
{
    public interface IVtexAPIService
    {
        //Task<string> GetAuthUrl();
        //Task<bool> RevokeGoogleAuthorizationToken(Token token);
        //Task<Token> RefreshToken(string refreshToken);

        Task<string> ProcessSheet();
        Task<UpdateResponse> CreateProduct(ProductRequest createProductRequest);
        Task<UpdateResponse> CreateSku(SkuRequest createSkuRequest);
        Task<GetCategoryTreeResponse[]> GetCategoryTree(int categoryLevels);
        Task<GetBrandListResponse[]> GetBrandList();
        Task<long[]> ListSkuIds(int page, int pagesize);
        Task<string> ExportToSheet(string query);
        Task<SearchTotals> SearchTotal(string query);
        Task<string> ClearSheet();
        Task<ListFilesResponse> ListImageFiles();
        Task<string> AddImagesToSheet();

        Task<bool> SetBrandList();

        // Catalog V2
        Task<UpdateResponse> CreateProductV2(ProductRequestV2 createProductRequest);
        Task<UpdateResponse> UpdateProductV2(ProductRequestV2 updateProductRequest);
        Task<GetBrandListV2Response> GetBrandListV2();
    }
}