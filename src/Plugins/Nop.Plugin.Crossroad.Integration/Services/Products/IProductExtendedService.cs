using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Crossroad.Integration.Services.Onix;

namespace Nop.Plugin.Crossroad.Integration.Services.Products;

public interface IProductExtendedService : Nop.Services.Catalog.IProductAttributeService
{
    public Task<bool> IsProductAttributeExists(string colName);

    public Task<bool> IsProductAttributeMappingExists(int productAttributeId, int productId);

    public Task<ProductAttributeValue> GetProductAttributeValueByNameAndProductAttributeMappingId(string name, int productAttributeMappingId);

    public Task<int> GetProductAttributeIdByName(string name);

    public Task<int> GetProductAttributeMappingByProductIdAndProductAttributeId(int productAttributeId, int productId);

    public Task<List<Contracts.ProductPrices>> GetProductIds();

    public Task<int> GetProductIdByName(string name);

    public Task<ProductAttributeCombination> GetProductAttributeCombinationByXmlAndProductId(string xml, int productId);

    public Task<string> GetProductIsbnAndTypeAsync(int productId);

    public Task<(decimal height, decimal width)> GetTrimSizesByProductName(int productId);
}