using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

namespace Nop.Plugin.Crossroad.Integration.Services.SpecificationAttributes;

public interface IProductSpecificationAttributeService : ISpecificationAttributeService
{
    public Task<bool> IsSpecificationAttributeExists(string colName);

    public Task<SpecificationAttribute> GetSpecificationAttributeByName(string attributeName);

    public Task<int> GetSpecificationAttributeOptionIdByName(string name);

    public Task<SpecificationAttributeOption> GetSpecificationAttributeOptionsBySpecificationAttributeId(int specificationAttributeId);

    public Task<SpecificationAttributeOption> GetSpecificationAttributeOptionIdByNameBySpecificationAttributeIdAsync(string attributeName, int specificationAttributeId);

    public Task<bool> IsSpecificationAttributeOptionsExists(string columnName, int specificationAttributeId);

    public Task<bool> IsSpecificationAttributeMappingExists(int productId, int specificationAttributeOptionId);
}