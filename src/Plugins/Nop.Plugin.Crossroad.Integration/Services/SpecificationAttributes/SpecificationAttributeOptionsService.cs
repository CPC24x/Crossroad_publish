using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Plugin.Crossroad.Integration.Services.SpecificationAttributes;

public class ProductSpecificationAttributeService : SpecificationAttributeService, IProductSpecificationAttributeService
{
    private readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;
    private readonly IRepository<SpecificationAttribute> _specificationAttributeRepository;
    private readonly IRepository<SpecificationAttributeOption> _specificationAttributeOptionRepository;

    public ProductSpecificationAttributeService(CatalogSettings catalogSettings,
        IAclService aclService,
        ICategoryService categoryService,
        IRepository<Product> productRepository,
        IRepository<ProductCategory> productCategoryRepository,
        IRepository<ProductManufacturer> productManufacturerRepository,
        IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
        IRepository<SpecificationAttribute> specificationAttributeRepository,
        IRepository<SpecificationAttributeOption> specificationAttributeOptionRepository,
        IRepository<SpecificationAttributeGroup> specificationAttributeGroupRepository,
        IStoreContext storeContext,
        IStoreMappingService storeMappingService,
        IStaticCacheManager staticCacheManager, IWorkContext workContext) : base(catalogSettings, aclService, categoryService, productRepository, productCategoryRepository, productManufacturerRepository, productSpecificationAttributeRepository, specificationAttributeRepository, specificationAttributeOptionRepository, specificationAttributeGroupRepository, storeContext, storeMappingService, staticCacheManager, workContext)
    {
        _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
        _specificationAttributeRepository = specificationAttributeRepository;
        _specificationAttributeOptionRepository = specificationAttributeOptionRepository;
    }

    public Task<bool> IsSpecificationAttributeExists(string colName) =>
        _specificationAttributeRepository.Table.AnyAsync(specificationAttribute => specificationAttribute.Name == colName);

    public async Task<SpecificationAttribute> GetSpecificationAttributeByName(string attributeName) => await _specificationAttributeRepository.Table.FirstOrDefaultAsync(sa => sa.Name == attributeName);

    public async Task<SpecificationAttributeOption> GetSpecificationAttributeOptionIdByNameBySpecificationAttributeIdAsync(string attributeName, int specificationAttributeId) => await _specificationAttributeOptionRepository.Table.FirstOrDefaultAsync(sa => sa.Name == attributeName && sa.SpecificationAttributeId == specificationAttributeId);


    public async Task<int> GetSpecificationAttributeOptionIdByName(string name) => (await _specificationAttributeOptionRepository.Table.FirstOrDefaultAsync(sao => sao.Name == name)).Id;

    public async Task<SpecificationAttributeOption> GetSpecificationAttributeOptionsBySpecificationAttributeId(
        int specificationAttributeId) => await _specificationAttributeOptionRepository.Table.FirstOrDefaultAsync(sao => sao.SpecificationAttributeId == specificationAttributeId);

    public Task<bool> IsSpecificationAttributeOptionsExists(string columnValue, int specificationAttributeId) =>
        _specificationAttributeOptionRepository.Table.AnyAsync(specification => specification.Name == columnValue && specification.SpecificationAttributeId == specificationAttributeId);

    public Task<bool> IsSpecificationAttributeMappingExists(int productId, int specificationAttributeOptionId) =>
        _productSpecificationAttributeRepository.Table.AnyAsync(productSpecification =>
            productSpecification.ProductId == productId && productSpecification.SpecificationAttributeOptionId == specificationAttributeOptionId);
}