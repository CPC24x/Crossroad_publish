using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Plugin.Crossroad.Integration.Services.Manufacturer;

public class ManufacturerExtendedService : ManufacturerService, IManufacturerExtendedService
{
    private readonly IRepository<Core.Domain.Catalog.Manufacturer> _manufacturerRepository;

    public ManufacturerExtendedService(CatalogSettings catalogSettings,
        IAclService aclService,
        ICategoryService categoryService,
        ICustomerService customerService,
        IRepository<DiscountManufacturerMapping> discountManufacturerMappingRepository,
        IRepository<Core.Domain.Catalog.Manufacturer> manufacturerRepository,
        IRepository<Product> productRepository,
        IRepository<ProductManufacturer> productManufacturerRepository,
        IRepository<ProductCategory> productCategoryRepository,
        IStaticCacheManager staticCacheManager,
        IStoreContext storeContext,
        IStoreMappingService storeMappingService,
        IWorkContext workContext) : base(catalogSettings, aclService, categoryService, customerService, discountManufacturerMappingRepository, manufacturerRepository, productRepository, productManufacturerRepository, productCategoryRepository, staticCacheManager, storeContext, storeMappingService, workContext)
    {
        _manufacturerRepository = manufacturerRepository;
    }

    public async Task<Core.Domain.Catalog.Manufacturer> GetManufacturerByNameAsync(string name) => await _manufacturerRepository.Table.FirstOrDefaultAsync(m => m.Name == name && m.Deleted == false);
}