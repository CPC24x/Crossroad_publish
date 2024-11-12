// panorazzi

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LinqToDB.Common;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;
using Org.BouncyCastle.Asn1.Cms;

namespace Nop.Web.BookHelpers
{
    public class BookHelpers
    {
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ILocalizationService _localizationService;

        public BookHelpers(
            ISpecificationAttributeService specificationAttributeService,
            ILocalizationService localizationService
            )
        {
            _specificationAttributeService = specificationAttributeService;
            _localizationService = localizationService;
        }

        public async Task<string> GetProductSpecificationAttributeAsync(int productId, string attributeName)
        {
            var psa = await GetSpecificationAttributeOptionByAttributeNameAsync(productId, attributeName);

            if (psa != null)
                return $"<li><strong>{psa.Name} :</strong> {string.Join(";", psa.Values.Select(x => x.ValueRaw))}</li>";
            return "";
        }

        private async Task<ProductSpecificationAttributeModel> GetSpecificationAttributeOptionByAttributeNameAsync(int productId, string attributeName)
        {
            var productSpecificationAttributes = await _specificationAttributeService.GetProductSpecificationAttributesAsync(
                   productId, showOnProductPage: true);

            var result = new List<ProductSpecificationAttributeModel>();

            foreach (var psa in productSpecificationAttributes)
            {
                var option = await _specificationAttributeService.GetSpecificationAttributeOptionByIdAsync(psa.SpecificationAttributeOptionId);

                var model = result.FirstOrDefault(model => model.Id == option.SpecificationAttributeId);
                if (model == null)
                {
                    var attribute = await _specificationAttributeService.GetSpecificationAttributeByIdAsync(option.SpecificationAttributeId);
                    model = new ProductSpecificationAttributeModel
                    {
                        Id = attribute.Id,
                        Name = await _localizationService.GetLocalizedAsync(attribute, x => x.Name)
                    };
                    result.Add(model);
                }

                var value = new ProductSpecificationAttributeValueModel
                {
                    AttributeTypeId = psa.AttributeTypeId,
                    ColorSquaresRgb = option.ColorSquaresRgb,
                    ValueRaw = psa.AttributeType switch
                    {
                        SpecificationAttributeType.Option => WebUtility.HtmlEncode(await _localizationService.GetLocalizedAsync(option, x => x.Name)),
                        SpecificationAttributeType.CustomText => WebUtility.HtmlEncode(await _localizationService.GetLocalizedAsync(psa, x => x.CustomValue)),
                        SpecificationAttributeType.CustomHtmlText => await _localizationService.GetLocalizedAsync(psa, x => x.CustomValue),
                        SpecificationAttributeType.Hyperlink => $"<a href='{psa.CustomValue}' target='_blank'>{psa.CustomValue}</a>",
                        _ => null
                    }
                };

                model.Values.Add(value);
            }

            return result.FirstOrDefault(x => x.Name.ToLowerInvariant() == attributeName.ToLowerInvariant());
        }
    }
}