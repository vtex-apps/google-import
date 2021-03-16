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
        Task<ProductResponse> CreateProduct(ProductRequest createProductRequest);
        Task<SkuResponse> CreateSku(SkuRequest createSkuRequest);
        Task<GetCategoryTreeResponse[]> GetCategoryTree(int categoryLevels);
        Task<GetBrandListResponse[]> GetBrandList();
        Task<long[]> ListSkuIds(int page, int pagesize);
    }
}