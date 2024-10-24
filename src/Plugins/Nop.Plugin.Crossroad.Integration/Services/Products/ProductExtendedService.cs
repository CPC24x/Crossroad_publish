using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Crossroad.Integration.Services.Onix;
using Nop.Services.Catalog;

namespace Nop.Plugin.Crossroad.Integration.Services.Products;

public class ProductExtendedService : ProductAttributeService, IProductExtendedService
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<ProductAttribute> _productAttributeRepository;
    private readonly IRepository<ProductAttributeCombination> _productAttributeCombinationRepository;
    private readonly IRepository<ProductAttributeMapping> _productAttributeMappingRepository;
    private readonly IRepository<ProductAttributeValue> _productAttributeValueRepository;
    private readonly IRepository<SpecificationAttribute> _specificationAttributeRepository;
    private readonly IRepository<SpecificationAttributeOption> _specificationAttributeOptionsRepository;
    private readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;

    public ProductExtendedService(
        IRepository<PredefinedProductAttributeValue> predefinedProductAttributeValueRepository,
        IRepository<Product> productRepository,
        IRepository<ProductAttribute> productAttributeRepository,
        IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
        IRepository<ProductAttributeMapping> productAttributeMappingRepository,
        IRepository<ProductAttributeValue> productAttributeValueRepository,
        IStaticCacheManager staticCacheManager,
        IRepository<SpecificationAttribute> specificationAttributeRepository,
        IRepository<SpecificationAttributeOption> specificationAttributeOptionsRepository,
        IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository) : base(
        predefinedProductAttributeValueRepository, productRepository,
        productAttributeRepository, productAttributeCombinationRepository, productAttributeMappingRepository,
        productAttributeValueRepository, staticCacheManager)
    {
        _productRepository = productRepository;
        _productAttributeRepository = productAttributeRepository;
        _productAttributeCombinationRepository = productAttributeCombinationRepository;
        _productAttributeMappingRepository = productAttributeMappingRepository;
        _productAttributeValueRepository = productAttributeValueRepository;
        _specificationAttributeRepository = specificationAttributeRepository;
        _specificationAttributeOptionsRepository = specificationAttributeOptionsRepository;
        _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
    }

    public async Task<bool> IsProductAttributeExists(string colName) =>
        await _productAttributeRepository.Table.AnyAsync(specificationAttribute =>
            specificationAttribute.Name == colName);

    public async Task<bool> IsProductAttributeMappingExists(int productAttributeId, int productId) =>
        await _productAttributeMappingRepository.Table.AnyAsync(productAttributeMapping =>
            productAttributeMapping.ProductAttributeId == productAttributeId &&
            productAttributeMapping.ProductId == productId);

    public async Task<ProductAttributeValue> GetProductAttributeValueByNameAndProductAttributeMappingId(string name, int productAttributeMappingId) =>
        await _productAttributeValueRepository.Table.FirstOrDefaultAsync(pav =>
            pav.Name == name && pav.AssociatedProductId == productAttributeMappingId);

    public async Task<int> GetProductAttributeIdByName(string name) =>
        (await _productAttributeRepository.Table.FirstOrDefaultAsync(pa => pa.Name == name)).Id;

    public async Task<int> GetProductAttributeMappingByProductIdAndProductAttributeId(int productAttributeId, int productId) =>
        (await _productAttributeMappingRepository.Table.FirstOrDefaultAsync(pam => pam.ProductId == productId && pam.ProductAttributeId == productAttributeId)).Id;

    public async Task<List<Contracts.ProductPrices>> GetProductIds()
    {
        var specificationAttributeId = (await _specificationAttributeRepository.Table.FirstOrDefaultAsync(sa => sa.Name == IntegrationDefaults.ProductAttributeColumnName)).Id;

        var specificationAttributeOptions = _specificationAttributeOptionsRepository.Table.Where(sao => sao.SpecificationAttributeId == specificationAttributeId).Select(sao => new
        {
            sao.Id,
            sao.Name
        });

        return (from psm in _productSpecificationAttributeRepository.Table
                join sao in specificationAttributeOptions on psm.SpecificationAttributeOptionId equals sao.Id
                join p in _productRepository.Table on psm.ProductId equals p.Id
                where _specificationAttributeOptionsRepository.Table
                                                              .Any(s => s.Id == psm.SpecificationAttributeOptionId && s.SpecificationAttributeId == specificationAttributeId)
                group new { sao.Name, p.Price, p.Sku } by p.Name
                into g
                select new Contracts.ProductPrices(g.Key,
                                             g.GroupBy(x => x.Name)
                    .ToDictionary(x => x.Key, x => new Contracts.ProductSkus(x.First().Sku, x.First().Price)))).ToList();
    }

    public async Task<int> GetProductIdByName(string name)
    {
        // izvuci id proizvoda koji ima tip Paperback ili Hardcopy 
        var specificationAttributeId = (await _specificationAttributeRepository.Table.FirstOrDefaultAsync(sa => sa.Name == IntegrationDefaults.ProductAttributeColumnName)).Id;

        var specificationAttributeOptions = _specificationAttributeOptionsRepository.Table.Where(sao => sao.SpecificationAttributeId == specificationAttributeId).Select(sao => new
        {
            sao.Id,
            sao.Name
        });

        var product = from psm in _productSpecificationAttributeRepository.Table
            join sao in specificationAttributeOptions on psm.SpecificationAttributeOptionId equals sao.Id
            join p in _productRepository.Table on psm.ProductId equals p.Id
            where p.Name == name && (sao.Name == "Paperback" || sao.Name == "Hardcover")
            select p.Id;

        return await product.FirstOrDefaultAsync();
    }

    public async Task<ProductAttributeCombination> GetProductAttributeCombinationByXmlAndProductId(string xml, int productId) =>
            await _productAttributeCombinationRepository.Table.FirstOrDefaultAsync(pac => pac.AttributesXml == xml && pac.ProductId == productId);

    public async Task<string> GetProductIsbnAndTypeAsync(int productId)
    {
        var productName = (await _productRepository.Table.FirstOrDefaultAsync(p => p.Id == productId)).Name;

        var specificationAttributeId = (await _specificationAttributeRepository.Table.FirstOrDefaultAsync(sa => sa.Name == IntegrationDefaults.ProductAttributeColumnName)).Id;

        var specificationAttributeOptions = await _specificationAttributeOptionsRepository.Table.Where(sao => sao.SpecificationAttributeId == specificationAttributeId).Select(sao => new
        {
            sao.Id,
            sao.Name
        }).ToListAsync();

        var products = await _productRepository.Table.Where(p => p.Name == productName).ToListAsync();

        var productSpecificationAttributeMappings = _productSpecificationAttributeRepository.Table
            .Where(psam =>
                specificationAttributeOptions.Select(sao => sao.Id)
                                             .Contains(psam.SpecificationAttributeOptionId) &&
                products.Select(p => p.Id)
                        .Contains(psam.ProductId))
            .Select(sao => new
            {
                sao.ProductId,
                sao.SpecificationAttributeOptionId
            });

        StringBuilder sb = new();

        foreach (var productSpecificationAttributeMapping in productSpecificationAttributeMappings)
        {
            var ISBN = (await _productRepository.Table.FirstOrDefaultAsync(p => p.Id == productSpecificationAttributeMapping.ProductId)).Sku;
            var productType = (await _specificationAttributeOptionsRepository.Table.FirstOrDefaultAsync(p => p.Id == productSpecificationAttributeMapping.SpecificationAttributeOptionId)).Name;

            sb.Append($"{productType} ({ISBN}), ");
        }

        return sb.ToString().TrimEnd(',');
    }

    public async Task<(decimal height, decimal width)> GetTrimSizesByProductName(int productId)
    {
        var productName = (await _productRepository.Table.FirstOrDefaultAsync(p => p.Id == productId)).Name;

        var query = await (from p in _productRepository.Table
                           where p.Name == productName
                           select p).ToListAsync();

        foreach (var product in query.Where(product => product is { Height: not 0, Width: not 0 }))
        {
            return (product.Height, product.Width);
        }

        return (default, default);
    }
}