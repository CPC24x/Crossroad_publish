// panorazzi

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Web.Models.Catalog;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace Nop.Web.BookHelpers
{
    public class BookHelpers
    {
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IRepository<SpecificationAttribute> _specificationAttributeRepository;
        private readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;
        private readonly IRepository<SpecificationAttributeOption> _specificationAttributeOptionRepository;


        public BookHelpers(
            ISpecificationAttributeService specificationAttributeService,
            ILocalizationService localizationService,
            IRepository<SpecificationAttribute> specificationAttributeRepository,
            IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
            IRepository<SpecificationAttributeOption> specificationAttributeOptionRepository
            )
        {
            _specificationAttributeService = specificationAttributeService;
            _localizationService = localizationService;
            _specificationAttributeRepository = specificationAttributeRepository;
            _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
            _specificationAttributeOptionRepository = specificationAttributeOptionRepository;
        }

        public async Task<(string, string)> GetProductSpecificationAttributeAsync(int productId, string attributeName)
        {
            var psa = await GetSpecificationAttributeOptionByAttributeNameAsync(productId, attributeName);

            if (psa == null)
                return (string.Empty, string.Empty);

            var data = string.Empty;
            data = string.Join(", ", psa.Values.Select(x => x.ValueRaw));


            return (psa.Name, data);
        }

        private async Task<ProductSpecificationAttributeModel> GetSpecificationAttributeOptionByAttributeNameAsync(int productId, string attributeName, bool? showOnProductPage = null)
        {

            var model = new ProductSpecificationAttributeModel();

            var dataList = from sao in _specificationAttributeOptionRepository.Table
                           join sa in _specificationAttributeRepository.Table on sao.SpecificationAttributeId equals sa.Id
                           join psa in _productSpecificationAttributeRepository.Table on sao.Id equals psa.SpecificationAttributeOptionId
                           where sa.Name == attributeName && psa.ProductId == productId
                           select new
                           {
                               Psa = psa,
                               Sa = sa,
                               Sao = sao,
                               ShowOnProductPage = psa.ShowOnProductPage,
                           };

            if (showOnProductPage.HasValue)
                dataList = dataList.Where(psa => psa.ShowOnProductPage == showOnProductPage.Value);

            if (dataList.Any())
                model = new ProductSpecificationAttributeModel
                {
                    Id = dataList.FirstOrDefault().Sa.Id,
                    Name = await _localizationService.GetLocalizedAsync(dataList.FirstOrDefault().Sa, x => x.Name)
                };

            foreach (var data in dataList)
            {
                var value = new ProductSpecificationAttributeValueModel
                {
                    AttributeTypeId = data.Psa.AttributeTypeId,
                    ColorSquaresRgb = data.Sao.ColorSquaresRgb,
                    ValueRaw = data.Psa.AttributeType switch
                    {
                        SpecificationAttributeType.Option => WebUtility.HtmlEncode(await _localizationService.GetLocalizedAsync(data.Sao, x => x.Name)),
                        SpecificationAttributeType.CustomText => WebUtility.HtmlEncode(await _localizationService.GetLocalizedAsync(data.Psa, x => x.CustomValue)),
                        SpecificationAttributeType.CustomHtmlText => await _localizationService.GetLocalizedAsync(data.Psa, x => x.CustomValue),
                        SpecificationAttributeType.Hyperlink => $"<a href='{data.Psa.CustomValue}' target='_blank'>{data.Psa.CustomValue}</a>",
                        _ => null
                    }
                };

                model.Values.Add(value);
            }

            return model;
        }
    }
}